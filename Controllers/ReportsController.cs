using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

public class ReportsController : AppController
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? period, DateTime? date, DateTime? from, DateTime? to)
        => View(await Build(period, date, from, to));

    public async Task<IActionResult> Export(string? period, DateTime? date, DateTime? from, DateTime? to)
    {
        var vm = await Build(period, date, from, to);
        var n = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "" };
        string M(decimal v) => v.ToString("0.##", n);

        var csv = new StringBuilder();
        csv.AppendLine($"Informe CAMSTEP;{vm.Label}");
        csv.AppendLine($"Desde;{vm.From:yyyy-MM-dd};Hasta;{vm.To:yyyy-MM-dd}");
        csv.AppendLine();
        csv.AppendLine("PRODUCTIVIDAD;Valor");
        csv.AppendLine($"Tareas con fecha en el periodo;{vm.TasksDue}");
        csv.AppendLine($"Completadas;{vm.TasksCompleted}");
        csv.AppendLine($"Pendientes;{vm.TasksPending}");
        csv.AppendLine($"Vencidas;{vm.TasksOverdue}");
        csv.AppendLine($"Porcentaje de cumplimiento;{vm.CompletionRate}%");
        csv.AppendLine($"Porcentaje periodo anterior;{vm.PrevCompletionRate}%");
        csv.AppendLine($"Eventos;{vm.EventsCount}");
        csv.AppendLine();
        csv.AppendLine("FINANZAS;Periodo;Periodo anterior");
        csv.AppendLine($"Ingresos;{M(vm.Income)};{M(vm.PrevIncome)}");
        csv.AppendLine($"Gastos;{M(vm.Expense)};{M(vm.PrevExpense)}");
        csv.AppendLine($"Balance;{M(vm.Balance)};{M(vm.PrevIncome - vm.PrevExpense)}");
        csv.AppendLine();
        csv.AppendLine("GASTOS POR CATEGORÍA;Valor;Porcentaje");
        foreach (var c in vm.ExpenseByCategory)
            csv.AppendLine($"{Csv(c.Category)};{M(c.Spent)};{(vm.Expense == 0 ? 0 : Math.Round(c.Spent / vm.Expense * 100))}%");
        csv.AppendLine();
        csv.AppendLine("METAS;Categoría;Progreso;Actual;Objetivo;Fecha límite;Estado");
        foreach (var g in vm.Goals)
            csv.AppendLine($"{Csv(g.Title)};{Csv(g.Category)};{g.Progress}%;{M(g.Current)};{M(g.Target)};{g.Deadline:yyyy-MM-dd};{(g.IsCompleted ? "Cumplida" : g.IsOverdue(DateTime.Today) ? "Vencida" : "En curso")}");
        csv.AppendLine();
        csv.AppendLine("PRODUCTIVIDAD SEMANAL;Tareas completadas");
        foreach (var (label, count) in vm.WeeklyProductivity) csv.AppendLine($"{label};{count}");
        csv.AppendLine();
        csv.AppendLine("PRODUCTIVIDAD MENSUAL;Tareas completadas");
        foreach (var (label, count) in vm.MonthlyProductivity) csv.AppendLine($"{label};{count}");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"CAMSTEP_informe_{vm.From:yyyy-MM-dd}_a_{vm.To:yyyy-MM-dd}.csv");
    }

    private async Task<ReportViewModel> Build(string? period, DateTime? date, DateTime? from, DateTime? to)
    {
        var userId = CurrentUserId;
        var today = DateTime.Today;
        var anchor = date?.Date is { Year: >= 2000 and <= 2100 } d ? d : today;

        var vm = new ReportViewModel { Anchor = anchor };

        if (period == "custom" && from is { Year: >= 2000 and <= 2100 } f && to is { Year: >= 2000 and <= 2100 } t)
        {
            if (t < f) (f, t) = (t, f);
            if ((t - f).TotalDays > 3660) t = f.AddDays(3660);
            vm.Period = "custom";
            vm.From = f.Date;
            vm.To = t.Date;
            vm.Label = $"Del {Format.Date(vm.From)} al {Format.Date(vm.To)}";
        }
        else
        {
            vm.Period = period is "week" or "year" ? period : "month";
            (vm.From, vm.To, vm.Label) = FinanceController.Range(vm.Period, anchor);
        }

        var length = (vm.To - vm.From).Days + 1;
        var prevFrom = vm.Period switch
        {
            "month" => vm.From.AddMonths(-1),
            "year" => vm.From.AddYears(-1),
            _ => vm.From.AddDays(-length)
        };
        var prevTo = vm.Period switch
        {
            "month" => vm.From.AddDays(-1),
            "year" => vm.From.AddDays(-1),
            _ => vm.From.AddDays(-1)
        };
        vm.PrevLabel = $"{Format.Date(prevFrom)} – {Format.Date(prevTo)}";

        var tasks = await _db.Tasks.AsNoTracking().Where(x => x.UserId == userId).ToListAsync();
        var expenses = await _db.Expenses.AsNoTracking().Where(x => x.UserId == userId).ToListAsync();
        var incomes = await _db.Incomes.AsNoTracking().Where(x => x.UserId == userId).ToListAsync();
        var goals = await _db.Goals.AsNoTracking().Where(x => x.UserId == userId).ToListAsync();

        bool In(DateTime x, DateTime a, DateTime b) => x.Date >= a && x.Date <= b;

        var periodTasks = tasks.Where(x => In(x.DueDate, vm.From, vm.To)).ToList();
        vm.TasksDue = periodTasks.Count;
        vm.TasksCompleted = periodTasks.Count(x => x.IsCompleted);
        vm.TasksPending = periodTasks.Count(x => !x.IsCompleted && !x.IsOverdue(today));
        vm.TasksOverdue = periodTasks.Count(x => x.IsOverdue(today));

        var prevTasks = tasks.Where(x => In(x.DueDate, prevFrom, prevTo)).ToList();
        vm.PrevCompletionRate = prevTasks.Count == 0 ? 0 : (int)Math.Round(prevTasks.Count(x => x.IsCompleted) * 100.0 / prevTasks.Count);

        vm.EventsCount = await _db.CalendarEvents.CountAsync(e => e.UserId == userId && e.StartsAt >= vm.From && e.StartsAt < vm.To.AddDays(1));

        var periodExpenses = expenses.Where(x => In(x.Date, vm.From, vm.To)).ToList();
        vm.Expense = periodExpenses.Sum(x => x.Amount);
        vm.Income = incomes.Where(x => In(x.Date, vm.From, vm.To)).Sum(x => x.Amount);
        vm.PrevExpense = expenses.Where(x => In(x.Date, prevFrom, prevTo)).Sum(x => x.Amount);
        vm.PrevIncome = incomes.Where(x => In(x.Date, prevFrom, prevTo)).Sum(x => x.Amount);
        vm.ExpenseByCategory = periodExpenses.GroupBy(x => x.Category)
            .Select(g => new CategoryTotal { Category = g.Key, Spent = g.Sum(x => x.Amount) })
            .OrderByDescending(c => c.Spent).ToList();

        vm.Goals = goals.OrderBy(g => g.IsCompleted).ThenBy(g => g.Deadline).ToList();
        vm.GoalsCompleted = goals.Count(g => g.IsCompleted);
        vm.GoalsOverdue = goals.Count(g => g.IsOverdue(today));
        vm.GoalsActive = goals.Count(g => !g.IsCompleted && !g.IsOverdue(today));

        DateTime Done(TaskItem x) => (x.CompletedAt ?? x.DueDate).Date;
        var completed = tasks.Where(x => x.IsCompleted).ToList();

        var chartEnd = vm.To < today ? vm.To : today;
        var lastWeekStart = chartEnd.AddDays(-(((int)chartEnd.DayOfWeek + 6) % 7));
        for (int i = 7; i >= 0; i--)
        {
            var start = lastWeekStart.AddDays(-7 * i);
            var end = start.AddDays(6);
            vm.WeeklyProductivity.Add((start.ToString("d MMM", Format.Es).TrimEnd('.'), completed.Count(x => In(Done(x), start, end))));
        }

        var lastMonth = new DateTime(chartEnd.Year, chartEnd.Month, 1);
        for (int i = 11; i >= 0; i--)
        {
            var m = lastMonth.AddMonths(-i);
            vm.MonthlyProductivity.Add((Format.Capitalize(m.ToString("MMM yy", Format.Es).Replace(".", "")),
                completed.Count(x => Done(x).Year == m.Year && Done(x).Month == m.Month)));
        }

        var moodFrom = chartEnd.AddDays(-29);
        vm.MoodDays = (await _db.MoodEntries.AsNoTracking()
                .Where(m => m.UserId == userId && m.Date >= moodFrom && m.Date <= chartEnd).ToListAsync())
            .OrderBy(m => m.Date).ToList();
        vm.MoodFrom = moodFrom;
        vm.MoodTo = chartEnd;
        vm.FocusMinutes = await _db.FocusSessions.AsNoTracking()
            .Where(f => f.UserId == userId && f.EndedAt >= vm.From && f.EndedAt < vm.To.AddDays(1)).SumAsync(f => f.Minutes);

        return vm;
    }

    private static string Csv(string value)
    {
        if (value.Length > 0 && "=+-@".Contains(value[0])) value = "'" + value;
        return value.Contains(';') || value.Contains('"') ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }
}
