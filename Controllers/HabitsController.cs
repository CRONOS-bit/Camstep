using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

public class HabitsController : AppController
{
    private readonly AppDbContext _db;

    public HabitsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;
        var today = DateTime.Today;
        var habits = await _db.Habits.AsNoTracking().Where(h => h.UserId == userId).OrderBy(h => h.CreatedAt).ToListAsync();
        var logs = await _db.HabitLogs.AsNoTracking().Where(l => l.UserId == userId).ToListAsync();

        var model = new HabitsViewModel
        {
            Today = today,
            Days = Enumerable.Range(0, 7).Select(i => today.AddDays(i - 6)).ToList(),
            Habits = habits.Select(h =>
            {
                var mine = logs.Where(l => l.HabitId == h.Id).Select(l => l.Date.Date).ToHashSet();
                return new HabitView { Habit = h, Total = mine.Count, Done = mine };
            }).ToList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? name, string? icon)
    {
        var userId = CurrentUserId;
        name = (name ?? "").Trim();
        if (name.Length is 0 or > 60)
        {
            TempData["Error"] = "El hábito debe tener entre 1 y 60 caracteres.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.Habits.CountAsync(h => h.UserId == userId) >= 30)
        {
            TempData["Error"] = "Puedes tener hasta 30 hábitos. Menos es más 🌱";
            return RedirectToAction(nameof(Index));
        }

        _db.Habits.Add(new Habit
        {
            UserId = userId,
            Name = name,
            Icon = Habit.Icons.Contains(icon) ? icon! : "🌱"
        });
        await _db.SaveChangesAsync();
        TempData["Notice"] = $"Sembraste «{name}». Cúmplelo y mira cómo crece.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, DateTime? date, string? returnUrl)
    {
        var userId = CurrentUserId;
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);
        if (habit == null) return NotFound();

        var today = DateTime.Today;
        var day = (date ?? today).Date;
        if (day > today || day < today.AddDays(-6))
        {
            TempData["Error"] = "Solo puedes marcar hoy y los últimos 6 días.";
            return RedirectBack(returnUrl, nameof(Index));
        }

        var log = await _db.HabitLogs.FirstOrDefaultAsync(l => l.HabitId == id && l.Date == day);
        if (log == null)
        {
            _db.HabitLogs.Add(new HabitLog { HabitId = id, UserId = userId, Date = day });
            TempData["Notice"] = $"{habit.Icon} «{habit.Name}» cumplido. ¡Tu brote creció!";
        }
        else
        {
            _db.HabitLogs.Remove(log);
            TempData["Notice"] = $"Quitaste la marca de «{habit.Name}» del {Format.Date(day)}.";
        }
        await _db.SaveChangesAsync();
        return RedirectBack(returnUrl, nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(int id, string? name, string? icon)
    {
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == CurrentUserId);
        if (habit == null) return NotFound();
        name = (name ?? "").Trim();
        if (name.Length is 0 or > 60)
        {
            TempData["Error"] = "El hábito debe tener entre 1 y 60 caracteres.";
            return RedirectToAction(nameof(Index));
        }
        habit.Name = name;
        if (Habit.Icons.Contains(icon)) habit.Icon = icon!;
        await _db.SaveChangesAsync();
        TempData["Notice"] = "Hábito actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId;
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);
        if (habit == null) return NotFound();
        _db.HabitLogs.RemoveRange(_db.HabitLogs.Where(l => l.HabitId == id && l.UserId == userId));
        _db.Habits.Remove(habit);
        await _db.SaveChangesAsync();
        TempData["Notice"] = $"Quitaste «{habit.Name}».";
        return RedirectToAction(nameof(Index));
    }
}
