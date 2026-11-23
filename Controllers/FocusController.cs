using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

public class FocusController : AppController
{
    private readonly AppDbContext _db;

    public FocusController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? taskId)
    {
        var userId = CurrentUserId;
        var today = DateTime.Today;
        var tasks = await _db.Tasks.AsNoTracking().Where(t => t.UserId == userId && !t.IsCompleted).ToListAsync();
        var sessions = await _db.FocusSessions.AsNoTracking()
            .Where(f => f.UserId == userId && f.EndedAt >= today.AddDays(-6)).ToListAsync();

        var model = new FocusViewModel
        {
            Tasks = tasks.OrderBy(t => t.DueDate).ThenBy(t => Priorities.Rank(t.Priority)).ThenBy(t => t.Title).ToList(),
            SelectedTaskId = taskId != null && tasks.Any(t => t.Id == taskId) ? taskId : null,
            TodayMinutes = sessions.Where(s => s.EndedAt >= today).Sum(s => s.Minutes),
            TodaySessions = sessions.Count(s => s.EndedAt >= today),
            WeekMinutes = sessions.Sum(s => s.Minutes),
            Recent = sessions.OrderByDescending(s => s.EndedAt).Take(6).ToList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int? taskId, int minutes, bool markDone = false)
    {
        var userId = CurrentUserId;
        if (minutes is < 1 or > 120)
            return BadRequest(new { ok = false, message = "La sesión debe durar entre 1 y 120 minutos." });

        var now = DateTime.Now;
        var started = now.AddMinutes(-minutes);

        var lastEnd = await _db.FocusSessions.Where(f => f.UserId == userId)
            .OrderByDescending(f => f.EndedAt).Select(f => (DateTime?)f.EndedAt).FirstOrDefaultAsync();
        if (lastEnd != null && lastEnd.Value > started.AddMinutes(1))
            return Conflict(new { ok = false, message = "Ya guardaste una sesión en ese tiempo." });

        TaskItem? task = null;
        if (taskId != null)
        {
            task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null) return NotFound(new { ok = false, message = "Esa tarea no existe." });
        }

        _db.FocusSessions.Add(new FocusSession
        {
            UserId = userId,
            TaskId = task?.Id,
            TaskTitle = task?.Title ?? "",
            Minutes = minutes,
            StartedAt = started,
            EndedAt = now
        });

        var completed = false;
        if (task != null && markDone && !task.IsCompleted)
        {
            task.IsCompleted = true;
            task.CompletedAt = now;
            completed = true;
        }
        await _db.SaveChangesAsync();

        return Json(new
        {
            ok = true,
            taskTitle = task?.Title,
            message = completed
                ? $"¡{minutes} minutos de enfoque y «{task!.Title}» terminada! Dos huellas nuevas en tu sendero."
                : $"¡{minutes} minutos de enfoque! Una huella nueva en tu sendero 🐾"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkTaskDone(int taskId)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == CurrentUserId);
        if (task == null) return NotFound(new { ok = false });
        if (!task.IsCompleted)
        {
            task.IsCompleted = true;
            task.CompletedAt = DateTime.Now;
            await _db.SaveChangesAsync();
        }
        return Json(new { ok = true, message = $"«{task.Title}» terminada. ✓" });
    }
}
