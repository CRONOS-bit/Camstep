using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

public class FinanceController : AppController
{
    private readonly AppDbContext _db;
    public FinanceController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? period, DateTime? date, string? type, string? category, string? q)
    {
        var userId = CurrentUserId;
        var vm = new FinanceViewModel
        {
            Period = period is "day" or "week" or "year" ? period : "month",
            Anchor = ClampDate(date ?? DateTime.Today),
            Type = type is "expense" or "income" ? type : "all",
            Category = (category ?? "").Trim(),
            Q = (q ?? "").Trim()
        };

        (vm.From, vm.To, vm.PeriodLabel) = Range(vm.Period, vm.Anchor);
        vm.PrevAnchor = Shift(vm.Period, vm.Anchor, -1);
        vm.NextAnchor = Shift(vm.Period, vm.Anchor, 1);
        var (prevFrom, prevTo, prevLabel) = Range(vm.Period, vm.PrevAnchor);
        vm.PrevLabel = prevLabel;

        var expenses = await _db.Expenses.Where(x => x.UserId == userId).ToListAsync();
        var incomes = await _db.Incomes.Where(x => x.UserId == userId).ToListAsync();
        var budgets = await _db.Budgets.Where(x => x.UserId == userId).ToListAsync();

        bool In(DateTime d, DateTime from, DateTime to) => d.Date >= from && d.Date <= to;

        var periodExpenses = expenses.Where(e => In(e.Date, vm.From, vm.To)).ToList();
        var periodIncomes = incomes.Where(i => In(i.Date, vm.From, vm.To)).ToList();

        vm.TotalExpense = periodExpenses.Sum(e => e.Amount);
        vm.TotalIncome = periodIncomes.Sum(i => i.Amount);
        vm.PrevExpense = expenses.Where(e => In(e.Date, prevFrom, prevTo)).Sum(e => e.Amount);
        vm.PrevIncome = incomes.Where(i => In(i.Date, prevFrom, prevTo)).Sum(i => i.Amount);

        var monthStart = new DateTime(vm.Anchor.Year, vm.Anchor.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var monthExpenses = expenses.Where(e => In(e.Date, monthStart, monthEnd)).ToList();
        vm.MonthExpense = monthExpenses.Sum(e => e.Amount);
        vm.MonthlyBudget = budgets.FirstOrDefault(b => b.Category == "")?.Amount;

        var categoryBudgets = budgets.Where(b => b.Category != "").ToDictionary(b => b.Category, b => b.Amount);
        vm.ByCategory = periodExpenses
            .GroupBy(e => e.Category)
            .Select(g => new CategoryTotal
            {
                Category = g.Key,
                Spent = g.Sum(e => e.Amount),
                Budget = vm.Period == "month" && categoryBudgets.TryGetValue(g.Key, out var b) ? b : null
            })
            .Concat(vm.Period == "month"
                ? categoryBudgets.Where(kv => periodExpenses.All(e => e.Category != kv.Key))
                    .Select(kv => new CategoryTotal { Category = kv.Key, Spent = 0, Budget = kv.Value })
                : Enumerable.Empty<CategoryTotal>())
            .OrderByDescending(c => c.Spent)
            .ToList();

        if (vm.MonthlyBudget is > 0)
        {
            var label = Format.MonthYear(monthStart).ToLower(Format.Es);
            if (vm.BudgetPercent > 100)
                vm.Alerts.Add($"Superaste tu presupuesto de {label}: llevas {Format.Money(vm.MonthExpense)} de {Format.Money(vm.MonthlyBudget.Value)}.");
            else if (vm.BudgetPercent >= 80)
                vm.Alerts.Add($"Ya usaste el {vm.BudgetPercent}% de tu presupuesto de {label}.");
        }
        foreach (var (cat, limit) in categoryBudgets.Where(kv => kv.Value > 0))
        {
            var spent = monthExpenses.Where(e => e.Category == cat).Sum(e => e.Amount);
            var pct = (int)Math.Round(spent / limit * 100);
            if (pct > 100) vm.Alerts.Add($"{cat}: superaste el presupuesto ({Format.Money(spent)} de {Format.Money(limit)}).");
            else if (pct >= 80) vm.Alerts.Add($"{cat}: llevas el {pct}% del presupuesto.");
        }

        IEnumerable<Movement> movements = periodExpenses.Select(e => new Movement
        {
            Id = e.Id, Kind = "expense", Description = e.Description, Category = e.Category, Amount = e.Amount, Date = e.Date
        }).Concat(periodIncomes.Select(i => new Movement
        {
            Id = i.Id, Kind = "income", Description = i.Description, Category = i.Category, Amount = i.Amount, Date = i.Date
        }));
        vm.Movements = Filter(movements, vm.Type, vm.Category, vm.Q)
            .OrderByDescending(m => m.Date).ThenByDescending(m => m.Id)
            .ToList();

        vm.Year = vm.Anchor.Year;
        vm.YearBars = Enumerable.Range(1, 12).Select(m => new MonthBar
        {
            Label = Format.Capitalize(new DateTime(vm.Year, m, 1).ToString("MMM", Format.Es).TrimEnd('.')),
            Income = incomes.Where(i => i.Date.Year == vm.Year && i.Date.Month == m).Sum(i => i.Amount),
            Expense = expenses.Where(e => e.Date.Year == vm.Year && e.Date.Month == m).Sum(e => e.Amount)
        }).ToList();

        vm.ExpenseCategories = await Categories.ExpenseAsync(_db, userId);
        vm.IncomeCategories = await Categories.IncomeAsync(_db, userId);

        return View(vm);
    }

    public async Task<IActionResult> Export(string? period, DateTime? date, string? type, string? category, string? q)
    {
        var userId = CurrentUserId;
        var p = period is "day" or "week" or "year" ? period : "month";
        var (from, to, label) = Range(p, ClampDate(date ?? DateTime.Today));

        var expenses = await _db.Expenses.Where(x => x.UserId == userId).ToListAsync();
        var incomes = await _db.Incomes.Where(x => x.UserId == userId).ToListAsync();

        var movements = expenses.Where(e => e.Date.Date >= from && e.Date.Date <= to)
            .Select(e => new Movement { Kind = "expense", Description = e.Description, Category = e.Category, Amount = e.Amount, Date = e.Date })
            .Concat(incomes.Where(i => i.Date.Date >= from && i.Date.Date <= to)
            .Select(i => new Movement { Kind = "income", Description = i.Description, Category = i.Category, Amount = i.Amount, Date = i.Date }));

        var rows = Filter(movements, type is "expense" or "income" ? type : "all", (category ?? "").Trim(), (q ?? "").Trim())
            .OrderBy(m => m.Date).ToList();

        var number = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "" };
        var csv = new StringBuilder();
        csv.AppendLine("Fecha;Tipo;Descripción;Categoría;Valor");
        foreach (var m in rows)
        {
            var signed = m.IsIncome ? m.Amount : -m.Amount;
            csv.AppendLine(string.Join(";",
                m.Date.ToString("yyyy-MM-dd"),
                m.IsIncome ? "Ingreso" : "Gasto",
                Csv(m.Description),
                Csv(m.Category),
                signed.ToString("0.##", number)));
        }
        csv.AppendLine($";;;Total ingresos;{rows.Where(r => r.IsIncome).Sum(r => r.Amount).ToString("0.##", number)}");
        csv.AppendLine($";;;Total gastos;{rows.Where(r => !r.IsIncome).Sum(r => r.Amount).ToString("0.##", number)}");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        var fileName = $"CAMSTEP_movimientos_{from:yyyy-MM-dd}_a_{to:yyyy-MM-dd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    public async Task<IActionResult> Budgets()
    {
        var userId = CurrentUserId;
        var budgets = await _db.Budgets.Where(b => b.UserId == userId).ToListAsync();
        var categories = await Categories.ExpenseAsync(_db, userId);

        return View(new BudgetsViewModel
        {
            General = budgets.FirstOrDefault(b => b.Category == "")?.Amount,
            PerCategory = categories.Select(c => (c, budgets.FirstOrDefault(b => b.Category == c)?.Amount)).ToList(),
            CustomCategories = await _db.UserCategories.Where(c => c.UserId == userId)
                .OrderBy(c => c.Kind).ThenBy(c => c.Name).ToListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBudgets(string? general, Dictionary<string, string>? categories)
    {
        var userId = CurrentUserId;
        var allowed = await Categories.ExpenseAsync(_db, userId);
        var existing = await _db.Budgets.Where(b => b.UserId == userId).ToListAsync();

        var wanted = new Dictionary<string, string?> { [""] = general };
        foreach (var kv in categories ?? new())
            if (allowed.Contains(kv.Key)) wanted[kv.Key] = kv.Value;

        foreach (var (category, raw) in wanted)
        {
            var row = existing.FirstOrDefault(b => b.Category == category);

            if (string.IsNullOrWhiteSpace(raw))
            {
                if (row != null) _db.Budgets.Remove(row);
                continue;
            }

            if (!decimal.TryParse(raw.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
                || amount <= 0 || amount > 999_999_999_999m)
            {
                TempData["Error"] = $"El presupuesto de {(category == "" ? "todo el mes" : category)} debe ser un número mayor que 0.";
                return RedirectToAction(nameof(Budgets));
            }

            if (row == null)
                _db.Budgets.Add(new Budget { UserId = userId, Category = category, Amount = amount });
            else
            {
                row.Amount = amount;
                row.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        TempData["Notice"] = "Presupuestos guardados.";
        return RedirectToAction(nameof(Budgets));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(string? name, string? kind)
    {
        var userId = CurrentUserId;
        name = (name ?? "").Trim();
        kind = kind == "income" ? "income" : "expense";

        if (name.Length is < 2 or > 40)
        {
            TempData["Error"] = "El nombre de la categoría debe tener entre 2 y 40 caracteres.";
            return RedirectToAction(nameof(Budgets));
        }

        var current = kind == "income" ? await Categories.IncomeAsync(_db, userId) : await Categories.ExpenseAsync(_db, userId);
        if (current.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            TempData["Error"] = $"Ya tienes una categoría llamada «{name}».";
            return RedirectToAction(nameof(Budgets));
        }

        _db.UserCategories.Add(new UserCategory { UserId = userId, Name = name, Kind = kind });
        await _db.SaveChangesAsync();

        TempData["Notice"] = $"Categoría «{name}» creada.";
        return RedirectToAction(nameof(Budgets));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var userId = CurrentUserId;
        var category = await _db.UserCategories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (category == null) return RedirectToAction(nameof(Budgets));

        var inUse = category.Kind == "income"
            ? await _db.Incomes.CountAsync(i => i.UserId == userId && i.Category == category.Name)
            : await _db.Expenses.CountAsync(e => e.UserId == userId && e.Category == category.Name);

        if (inUse > 0)
        {
            TempData["Error"] = $"No se puede eliminar «{category.Name}»: la usan {inUse} movimiento(s). Cámbialos de categoría primero.";
            return RedirectToAction(nameof(Budgets));
        }

        var budget = await _db.Budgets.FirstOrDefaultAsync(b => b.UserId == userId && b.Category == category.Name);
        if (budget != null) _db.Budgets.Remove(budget);

        _db.UserCategories.Remove(category);
        await _db.SaveChangesAsync();

        TempData["Notice"] = $"Categoría «{category.Name}» eliminada.";
        return RedirectToAction(nameof(Budgets));
    }

    private static IEnumerable<Movement> Filter(IEnumerable<Movement> items, string type, string category, string q)
    {
        if (type != "all") items = items.Where(m => m.Kind == type);
        if (category.Length > 0) items = items.Where(m => m.Category == category);
        if (q.Length > 0)
        {
            var needle = Text.Normalize(q);
            items = items.Where(m => Text.Normalize(m.Description).Contains(needle) || Text.Normalize(m.Category).Contains(needle));
        }
        return items;
    }

    private static DateTime ClampDate(DateTime d) =>
        d.Year < 2000 || d.Year > 2100 ? DateTime.Today : d.Date;

    public static (DateTime From, DateTime To, string Label) Range(string period, DateTime anchor)
    {
        switch (period)
        {
            case "day":
                return (anchor, anchor, Format.LongDate(anchor));
            case "week":
                var start = anchor.AddDays(-(((int)anchor.DayOfWeek + 6) % 7));
                var end = start.AddDays(6);
                return (start, end, $"Semana del {start.ToString("d 'de' MMMM", Format.Es)} al {end.ToString("d 'de' MMMM 'de' yyyy", Format.Es)}");
            case "year":
                return (new DateTime(anchor.Year, 1, 1), new DateTime(anchor.Year, 12, 31), $"Año {anchor.Year}");
            default:
                var first = new DateTime(anchor.Year, anchor.Month, 1);
                return (first, first.AddMonths(1).AddDays(-1), Format.MonthYear(first));
        }
    }

    private static DateTime Shift(string period, DateTime anchor, int direction) => period switch
    {
        "day" => anchor.AddDays(direction),
        "week" => anchor.AddDays(7 * direction),
        "year" => anchor.AddYears(direction),
        _ => anchor.AddMonths(direction)
    };

    private static string Csv(string value)
    {
        if (value.Length > 0 && "=+-@".Contains(value[0])) value = "'" + value;
        return value.Contains(';') || value.Contains('"') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
