using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

public class ReviewController : AppController
{
    private readonly AppDbContext _db;
    private readonly PathService _path;

    public ReviewController(AppDbContext db, PathService path)
    {
        _db = db;
        _path = path;
    }

    public async Task<IActionResult> Index(DateTime? week)
    {
        var userId = CurrentUserId;
        var today = DateTime.Today;
        var monday = await PickWeekAsync(userId, today, week);
        var next = monday.AddDays(7);

        var prints = await _path.BetweenAsync(userId, monday, next);
        var expenses = await _db.Expenses.AsNoTracking().Where(e => e.UserId == userId && e.Date >= monday && e.Date < next).ToListAsync();
        var incomes = await _db.Incomes.AsNoTracking().Where(i => i.UserId == userId && i.Date >= monday && i.Date < next).ToListAsync();
        var moods = await _db.MoodEntries.AsNoTracking().Where(m => m.UserId == userId && m.Date >= monday && m.Date < next).ToListAsync();
        var goals = (await _db.Goals.AsNoTracking().Where(g => g.UserId == userId).ToListAsync())
            .Where(g => !g.IsCompleted).ToList();
        var pendingNext = await _db.Tasks.AsNoTracking()
            .Where(t => t.UserId == userId && !t.IsCompleted && t.DueDate >= next && t.DueDate < next.AddDays(7)).CountAsync();
        var overdue = await _db.Tasks.AsNoTracking()
            .Where(t => t.UserId == userId && !t.IsCompleted && t.DueDate < (today < next ? today : next)).CountAsync();

        var model = new ReviewViewModel
        {
            Monday = monday,
            Today = today,
            Review = await _db.WeeklyReviews.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == userId && r.WeekStart == monday),
            Footprints = prints,
            TasksDone = prints.Where(p => p.Kind == "task").Select(p => p.Title).ToList(),
            FocusMinutes = await _db.FocusSessions.AsNoTracking()
                .Where(f => f.UserId == userId && f.EndedAt >= monday && f.EndedAt < next).SumAsync(f => f.Minutes),
            HabitChecks = prints.Count(p => p.Kind == "habit"),
            Spent = expenses.Sum(e => e.Amount),
            Earned = incomes.Sum(i => i.Amount),
            TopCategory = expenses.GroupBy(e => e.Category).Select(g => (g.Key, g.Sum(e => e.Amount)))
                .OrderByDescending(x => x.Item2).FirstOrDefault(),
            MoodAverage = moods.Count > 0 ? moods.Average(m => m.Mood) : null,
            GoalsInProgress = goals.OrderBy(g => g.Deadline).Take(4).ToList(),
            PendingNextWeek = pendingNext,
            Overdue = overdue
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(DateTime week, string? wins, string? step1, string? step2, string? step3, bool createTasks = false)
    {
        var userId = CurrentUserId;
        var today = DateTime.Today;
        var monday = Weeks.StartOf(week);
        if (monday > Weeks.StartOf(today) || monday < Weeks.StartOf(today).AddDays(-7))
        {
            TempData["Error"] = "Solo puedes repasar esta semana o la anterior.";
            return RedirectToAction(nameof(Index));
        }

        static string Clean(string? s, int max) { s = (s ?? "").Trim(); return s.Length > max ? s[..max] : s; }
        var steps = new[] { Clean(step1, 120), Clean(step2, 120), Clean(step3, 120) };
        wins = Clean(wins, 1000);

        if (wins.Length == 0 && steps.All(s => s.Length == 0))
        {
            TempData["Error"] = "Escribe al menos algo que salió bien o un paso para la próxima semana.";
            return RedirectToAction(nameof(Index), new { week = monday.ToString("yyyy-MM-dd") });
        }

        var review = await _db.WeeklyReviews.FirstOrDefaultAsync(r => r.UserId == userId && r.WeekStart == monday);
        var isNew = review == null;
        if (review == null)
        {
            review = new WeeklyReview { UserId = userId, WeekStart = monday, CreatedAt = DateTime.Now };
            _db.WeeklyReviews.Add(review);
        }
        review.Wins = wins;
        review.Step1 = steps[0];
        review.Step2 = steps[1];
        review.Step3 = steps[2];
        review.UpdatedAt = DateTime.Now;

        var created = 0;
        if (createTasks)
        {
            var due = monday.AddDays(13);
            foreach (var step in steps.Where(s => s.Length > 0))
            {
                var exists = await _db.Tasks.AnyAsync(t => t.UserId == userId && t.Title == step && !t.IsCompleted);
                if (exists) continue;
                _db.Tasks.Add(new TaskItem
                {
                    UserId = userId,
                    Title = step,
                    Description = "Paso del domingo de calma (semana del " + Weeks.Range(monday) + ").",
                    DueDate = due,
                    Priority = "Media"
                });
                created++;
            }
        }

        await _db.SaveChangesAsync();
        TempData["Notice"] = (isNew ? "🍃 Domingo de calma guardado. Una huella más en tu sendero." : "🍃 Repaso actualizado.")
                             + (created > 0 ? $" Se crearon {created} tarea(s) para la próxima semana." : "");
        return RedirectToAction(nameof(Index), new { week = monday.ToString("yyyy-MM-dd") });
    }

    private async Task<DateTime> PickWeekAsync(int userId, DateTime today, DateTime? requested)
    {
        var current = Weeks.StartOf(today);
        if (requested != null)
        {
            var m = Weeks.StartOf(requested.Value);
            if (m == current || m == current.AddDays(-7)) return m;
        }
        if (today.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Tuesday)
        {
            var previous = current.AddDays(-7);
            var done = await _db.WeeklyReviews.AnyAsync(r => r.UserId == userId && r.WeekStart == previous);
            if (!done) return previous;
        }
        return current;
    }
}
