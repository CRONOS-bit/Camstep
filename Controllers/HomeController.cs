using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Filters;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

public class HomeController : AppController
{
    private readonly AppDbContext _db;

    private readonly PathService _path;

    public HomeController(AppDbContext db, PathService path)
    {
        _db = db;
        _path = path;
    }

    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;
        var today = DateTime.Today;
        var now = DateTime.Now;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var tasks = await _db.Tasks.Where(x => x.UserId == userId).ToListAsync();
        var goals = await _db.Goals.Where(x => x.UserId == userId).ToListAsync();
        var monthExpenses = await _db.Expenses.Where(x => x.UserId == userId && x.Date >= monthStart && x.Date < monthEnd).ToListAsync();
        var monthIncomes = await _db.Incomes.Where(x => x.UserId == userId && x.Date >= monthStart && x.Date < monthEnd).ToListAsync();
        var budgets = await _db.Budgets.Where(x => x.UserId == userId).ToListAsync();
        var weekEnd = today.AddDays(8);
        var events = await _db.CalendarEvents
            .Where(e => e.UserId == userId && e.StartsAt >= today && e.StartsAt < weekEnd)
            .ToListAsync();

        var pending = tasks.Where(t => !t.IsCompleted).ToList();

        var vm = new HomeViewModel
        {
            Today = today,
            UserName = HttpContext.Session.GetString("UserName") ?? "",
            Greeting = now.Hour switch { < 12 => "Buenos días", < 19 => "Buenas tardes", _ => "Buenas noches" },

            PendingTasks = pending.Count,
            CompletedTasks = tasks.Count - pending.Count,
            OverdueTasks = pending.Count(t => t.IsOverdue(today)),
            TodayTasks = pending.Count(t => t.DueDate.Date == today),
            ImportantTasks = pending
                .Where(t => t.Priority == "Alta")
                .OrderBy(t => t.DueDate)
                .Take(5).ToList(),
            DueSoonTasks = pending
                .Where(t => t.DueDate.Date <= today.AddDays(7))
                .OrderBy(t => t.DueDate).ThenBy(t => Priorities.Rank(t.Priority))
                .Take(6).ToList(),
            UpcomingEvents = events
                .Where(e => e.AllDay || (e.EndsAt ?? e.StartsAt) >= now || e.StartsAt.Date > today)
                .OrderBy(e => e.StartsAt).Take(5).ToList(),

            MonthIncome = monthIncomes.Sum(i => i.Amount),
            MonthExpense = monthExpenses.Sum(e => e.Amount),
            MonthlyBudget = budgets.FirstOrDefault(b => b.Category == "")?.Amount,
            TopCategories = monthExpenses
                .GroupBy(e => e.Category)
                .Select(g => new CategoryTotal { Category = g.Key, Spent = g.Sum(e => e.Amount) })
                .OrderByDescending(c => c.Spent)
                .Take(4).ToList(),

            ActiveGoals = goals.Count(g => !g.IsCompleted),
            CompletedGoals = goals.Count(g => g.IsCompleted),
            AverageGoalProgress = goals.Count(g => !g.IsCompleted) == 0 ? 0 : (int)Math.Round(goals.Where(g => !g.IsCompleted).Average(g => g.Progress)),
            Goals = goals.Where(g => !g.IsCompleted).OrderBy(g => g.Deadline).Take(4).ToList()
        };

        vm.MoodToday = await _db.MoodEntries.AsNoTracking().FirstOrDefaultAsync(m => m.UserId == userId && m.Date == today);
        var habits = await _db.Habits.AsNoTracking().Where(h => h.UserId == userId).OrderBy(h => h.CreatedAt).ToListAsync();
        var todayLogs = await _db.HabitLogs.AsNoTracking().Where(l => l.UserId == userId && l.Date == today).Select(l => l.HabitId).ToListAsync();
        vm.HabitsToday = habits.Select(h => (h, todayLogs.Contains(h.Id))).ToList();
        var monday = Weeks.StartOf(today);
        vm.WeekFootprints = (await _path.BetweenAsync(userId, monday, monday.AddDays(7))).Count;
        vm.Streak = await _path.StreakAsync(userId, today);
        vm.FocusMinutesToday = await _db.FocusSessions.AsNoTracking()
            .Where(f => f.UserId == userId && f.EndedAt >= today).SumAsync(f => f.Minutes);
        vm.ReviewPending = today.DayOfWeek == DayOfWeek.Sunday &&
            !await _db.WeeklyReviews.AnyAsync(r => r.UserId == userId && r.WeekStart == monday);

        for (int i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            vm.Week.Add(new DayStat
            {
                Date = day,
                Label = i == 0 ? "Hoy" : Format.Capitalize(day.ToString("ddd", Format.Es).TrimEnd('.')),
                Completed = tasks.Count(t => t.IsCompleted && (t.CompletedAt ?? t.DueDate).Date == day),
                Due = tasks.Count(t => t.DueDate.Date == day)
            });
        }
        vm.CompletedThisWeek = vm.Week.Sum(d => d.Completed);

        if (vm.OverdueTasks > 0)
            vm.Alerts.Add($"Tienes {vm.OverdueTasks} tarea(s) vencida(s).");
        if (vm.MonthlyBudget is > 0 && vm.BudgetPercent >= 80)
            vm.Alerts.Add(vm.BudgetPercent > 100
                ? $"Superaste tu presupuesto del mes ({vm.BudgetPercent}%)."
                : $"Llevas el {vm.BudgetPercent}% de tu presupuesto del mes.");
        foreach (var b in budgets.Where(b => b.Category != "" && b.Amount > 0))
        {
            var spent = monthExpenses.Where(e => e.Category == b.Category).Sum(e => e.Amount);
            if (spent > b.Amount) vm.Alerts.Add($"Superaste el presupuesto de {b.Category}.");
        }
        var overdueGoals = goals.Count(g => g.IsOverdue(today));
        if (overdueGoals > 0)
            vm.Alerts.Add($"{overdueGoals} meta(s) pasaron su fecha límite.");

        return View(vm);
    }

    [AllowGuest]
    public IActionResult Error() => View();

    [AllowGuest]
    [Route("Home/Status/{code:int}")]
    public IActionResult Status(int code)
    {
        ViewBag.Code = code;
        return View();
    }
}
