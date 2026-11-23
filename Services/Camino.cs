using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;

namespace Camstep.Services;

public static class Weeks
{
    public static DateTime StartOf(DateTime day)
    {
        var d = day.Date;
        var diff = ((int)d.DayOfWeek + 6) % 7;
        return d.AddDays(-diff);
    }

    public static string Range(DateTime monday)
    {
        var sunday = monday.AddDays(6);
        return monday.Month == sunday.Month
            ? $"{monday.Day} al {sunday.Day} de {sunday.ToString("MMMM", Format.Es)}"
            : $"{monday.Day} de {monday.ToString("MMMM", Format.Es)} al {sunday.Day} de {sunday.ToString("MMMM", Format.Es)}";
    }
}

public record Footprint(DateTime When, string Kind, string Title)
{
    public string Icon => Kind switch
    {
        "task" => "✓",
        "step" => "⛰",
        "focus" => "🌿",
        "habit" => "🌱",
        "review" => "🍃",
        _ => "•"
    };

    public string KindText => Kind switch
    {
        "task" => "Tarea terminada",
        "step" => "Paso de una meta",
        "focus" => "Sesión de enfoque",
        "habit" => "Hábito cumplido",
        "review" => "Domingo de calma",
        _ => ""
    };
}

public class PathService
{
    private readonly AppDbContext _db;

    public PathService(AppDbContext db) => _db = db;

    public async Task<List<Footprint>> BetweenAsync(int userId, DateTime from, DateTime to)
    {
        var list = new List<Footprint>();

        var tasks = await _db.Tasks.AsNoTracking()
            .Where(t => t.UserId == userId && t.IsCompleted && t.CompletedAt != null && t.CompletedAt >= from && t.CompletedAt < to)
            .Select(t => new { t.CompletedAt, t.Title }).ToListAsync();
        list.AddRange(tasks.Select(t => new Footprint(t.CompletedAt!.Value, "task", t.Title)));

        var steps = await _db.GoalSteps.AsNoTracking()
            .Where(s => s.UserId == userId && s.IsDone && s.CompletedAt != null && s.CompletedAt >= from && s.CompletedAt < to)
            .Select(s => new { s.CompletedAt, s.Title }).ToListAsync();
        list.AddRange(steps.Select(s => new Footprint(s.CompletedAt!.Value, "step", s.Title)));

        var focus = await _db.FocusSessions.AsNoTracking()
            .Where(f => f.UserId == userId && f.EndedAt >= from && f.EndedAt < to)
            .Select(f => new { f.EndedAt, f.Minutes, f.TaskTitle }).ToListAsync();
        list.AddRange(focus.Select(f => new Footprint(f.EndedAt, "focus",
            $"{f.Minutes} min de enfoque{(string.IsNullOrEmpty(f.TaskTitle) ? "" : " · " + f.TaskTitle)}")));

        var habits = await _db.Habits.AsNoTracking().Where(h => h.UserId == userId).ToDictionaryAsync(h => h.Id);
        var logs = await _db.HabitLogs.AsNoTracking()
            .Where(l => l.UserId == userId && l.Date >= from.Date && l.Date < to)
            .ToListAsync();
        list.AddRange(logs.Where(l => habits.ContainsKey(l.HabitId))
            .Select(l => new Footprint(l.Date.Date.AddHours(12), "habit", habits[l.HabitId].Icon + " " + habits[l.HabitId].Name)));

        var reviews = await _db.WeeklyReviews.AsNoTracking()
            .Where(r => r.UserId == userId && r.CreatedAt >= from && r.CreatedAt < to)
            .Select(r => new { r.CreatedAt, r.WeekStart }).ToListAsync();
        list.AddRange(reviews.Select(r => new Footprint(r.CreatedAt, "review",
            "Repaso de la semana del " + Weeks.Range(r.WeekStart))));

        return list.OrderBy(f => f.When).ToList();
    }

    public async Task<int> StreakAsync(int userId, DateTime today)
    {
        var prints = await BetweenAsync(userId, today.AddDays(-120), today.AddDays(1));
        var days = prints.Select(p => p.When.Date).ToHashSet();
        var day = days.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;
        while (days.Contains(day)) { streak++; day = day.AddDays(-1); }
        return streak;
    }
}
