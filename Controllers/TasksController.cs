using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

public class TasksController : AppController
{
    private readonly AppDbContext _db;
    public TasksController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? status, string? priority, string? range, string? sort, string? q)
    {
        var today = DateTime.Today;

        var all = await _db.Tasks.Where(x => x.UserId == CurrentUserId).ToListAsync();

        var vm = new TaskListViewModel
        {
            Today = today,
            Status = status is "pending" or "completed" or "overdue" ? status : "all",
            Priority = Priorities.IsValid(priority) ? priority! : "all",
            Range = range is "today" or "week" or "month" ? range : "all",
            Sort = sort is "date_desc" or "priority" ? sort : "date",
            Q = (q ?? "").Trim(),
            TotalCount = all.Count,
            PendingCount = all.Count(t => !t.IsCompleted),
            CompletedCount = all.Count(t => t.IsCompleted),
            OverdueCount = all.Count(t => t.IsOverdue(today))
        };

        IEnumerable<TaskItem> items = all;

        items = vm.Status switch
        {
            "pending" => items.Where(t => !t.IsCompleted),
            "completed" => items.Where(t => t.IsCompleted),
            "overdue" => items.Where(t => t.IsOverdue(today)),
            _ => items
        };

        if (vm.Priority != "all")
            items = items.Where(t => t.Priority == vm.Priority);

        if (vm.Range != "all")
        {
            var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            var (from, to) = vm.Range switch
            {
                "today" => (today, today),
                "week" => (weekStart, weekStart.AddDays(6)),
                _ => (new DateTime(today.Year, today.Month, 1), new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1))
            };
            items = items.Where(t => t.DueDate.Date >= from && t.DueDate.Date <= to);
        }

        if (vm.Q.Length > 0)
        {
            var needle = Text.Normalize(vm.Q);
            items = items.Where(t => Text.Normalize(t.Title).Contains(needle) || Text.Normalize(t.Description).Contains(needle));
        }

        items = vm.Sort switch
        {
            "date_desc" => items.OrderBy(t => t.IsCompleted).ThenByDescending(t => t.DueDate).ThenBy(t => Priorities.Rank(t.Priority)),
            "priority" => items.OrderBy(t => t.IsCompleted).ThenBy(t => Priorities.Rank(t.Priority)).ThenBy(t => t.DueDate),
            _ => items.OrderBy(t => t.IsCompleted).ThenBy(t => t.DueDate).ThenBy(t => Priorities.Rank(t.Priority))
        };

        vm.Items = items.ToList();
        return View(vm);
    }

    public IActionResult Create(DateTime? dueDate, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new TaskItem { DueDate = (dueDate ?? DateTime.Today).Date });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskItem item, string? returnUrl)
    {
        Validate(item);
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(item);
        }

        var task = new TaskItem
        {
            UserId = CurrentUserId,
            Title = item.Title.Trim(),
            Description = (item.Description ?? "").Trim(),
            DueDate = item.DueDate.Date,
            Priority = item.Priority,
            IsCompleted = false
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        TempData["Notice"] = "Tarea creada.";
        return RedirectBack(returnUrl, nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, string? returnUrl)
    {
        var item = await _db.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId);
        if (item == null) return NotFound();

        ViewBag.ReturnUrl = returnUrl;
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TaskItem item, string? returnUrl)
    {
        Validate(item);
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(item);
        }

        var existing = await _db.Tasks.FirstOrDefaultAsync(x => x.Id == item.Id && x.UserId == CurrentUserId);
        if (existing == null) return NotFound();

        existing.Title = item.Title.Trim();
        existing.Description = (item.Description ?? "").Trim();
        existing.DueDate = item.DueDate.Date;
        existing.Priority = item.Priority;
        if (existing.IsCompleted != item.IsCompleted)
            existing.CompletedAt = item.IsCompleted ? DateTime.Now : null;
        existing.IsCompleted = item.IsCompleted;

        await _db.SaveChangesAsync();

        TempData["Notice"] = "Cambios guardados.";
        return RedirectBack(returnUrl, nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, string? returnUrl)
    {
        var item = await _db.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId);

        if (item != null)
        {
            item.IsCompleted = !item.IsCompleted;
            item.CompletedAt = item.IsCompleted ? DateTime.Now : null;
            await _db.SaveChangesAsync();
        }

        return RedirectBack(returnUrl, nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var item = await _db.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId);

        if (item != null)
        {
            _db.Tasks.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Notice"] = "Tarea eliminada.";
        }

        return RedirectBack(returnUrl, nameof(Index));
    }

    private void Validate(TaskItem item)
    {
        if (!Priorities.IsValid(item.Priority))
            ModelState.AddModelError(nameof(item.Priority), "Elige una prioridad: Alta, Media o Baja.");

        if (item.DueDate.Year < 2000 || item.DueDate.Year > 2100)
            ModelState.AddModelError(nameof(item.DueDate), "Elige una fecha entre los años 2000 y 2100.");
    }
}
