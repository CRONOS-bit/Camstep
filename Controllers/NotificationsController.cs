using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

public class NotificationsController : AppController
{
    private readonly AppDbContext _db;
    private readonly NotificationService _service;

    public NotificationsController(AppDbContext db, NotificationService service)
    {
        _db = db;
        _service = service;
    }

    public async Task<IActionResult> Index(string? filter)
    {
        var userId = CurrentUserId;
        await _service.GenerateAsync(userId, DateTime.Now);

        var query = _db.Notifications.Where(n => n.UserId == userId && !n.IsDismissed);
        var vm = new NotificationsViewModel
        {
            Filter = filter == "unread" ? "unread" : "all",
            UnreadCount = await query.CountAsync(n => !n.IsRead),
            Settings = await PreferenceSession.GetOrCreateAsync(_db, userId)
        };

        if (vm.Filter == "unread") query = query.Where(n => !n.IsRead);
        vm.Items = (await query.ToListAsync())
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAt)
            .Take(200)
            .ToList();

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Summary()
    {
        var userId = CurrentUserId;
        await _service.GenerateThrottledAsync(userId);

        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && !n.IsDismissed)
            .ToListAsync();

        return Json(new
        {
            unread = unread.Count,
            latest = unread.OrderByDescending(n => n.CreatedAt).Take(5)
                .Select(n => new { id = n.Id, title = n.Title, message = n.Message, url = Url.Action(nameof(Open), new { id = n.Id }) })
        });
    }

    [HttpGet]
    public async Task<IActionResult> Open(int id)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId && !x.IsDismissed);
        if (n == null) return RedirectToAction(nameof(Index));

        if (!n.IsRead)
        {
            n.IsRead = true;
            await _db.SaveChangesAsync();
        }

        return Url.IsLocalUrl(n.Link) ? LocalRedirect(n.Link) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, string? returnUrl)
    {
        await _db.Notifications
            .Where(n => n.Id == id && n.UserId == CurrentUserId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return RedirectBack(returnUrl, nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var count = await _db.Notifications
            .Where(n => n.UserId == CurrentUserId && !n.IsRead && !n.IsDismissed)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        TempData["Notice"] = count > 0 ? $"{count} aviso(s) marcados como leídos." : "No tenías avisos sin leer.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _db.Notifications.Where(n => n.Id == id && n.UserId == CurrentUserId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsDismissed, true).SetProperty(n => n.IsRead, true));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRead()
    {
        var count = await _db.Notifications.Where(n => n.UserId == CurrentUserId && n.IsRead && !n.IsDismissed)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsDismissed, true));
        TempData["Notice"] = $"{count} aviso(s) leídos eliminados.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings(bool notifyTasks, bool notifyEvents, bool notifyGoals, bool notifyBudget,
        int taskLeadDays, int eventLeadMinutes, int goalLeadDays)
    {
        var userId = CurrentUserId;
        var p = await PreferenceSession.GetOrCreateAsync(_db, userId);

        p.NotifyTasks = notifyTasks;
        p.NotifyEvents = notifyEvents;
        p.NotifyGoals = notifyGoals;
        p.NotifyBudget = notifyBudget;
        p.TaskLeadDays = Math.Clamp(taskLeadDays, 0, 30);
        p.EventLeadMinutes = Math.Clamp(eventLeadMinutes, 0, 10080);
        p.GoalLeadDays = Math.Clamp(goalLeadDays, 0, 60);
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _service.InvalidateThrottle(userId);
        TempData["Notice"] = "Configuración de avisos guardada.";
        return RedirectToAction(nameof(Index));
    }
}
