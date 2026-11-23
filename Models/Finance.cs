using System.ComponentModel.DataAnnotations;

namespace Camstep.Models;

public class Income
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [Required(ErrorMessage = "Escribe una descripción para el ingreso.")]
    [StringLength(150, ErrorMessage = "La descripción admite máximo 150 caracteres.")]
    public string Description { get; set; } = "";

    [Range(typeof(decimal), "0.01", "999999999999", ErrorMessage = "El valor debe ser mayor que 0.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Elige una categoría.")]
    [StringLength(50)]
    public string Category { get; set; } = "Salario";

    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    public static readonly string[] Categories = { "Salario", "Trabajo independiente", "Ventas", "Regalo", "Reembolso", "Otros" };
}

public class Budget
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class UserCategory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "expense";
}

public class Movement
{
    public int Id { get; set; }
    public string Kind { get; set; } = "expense";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }

    public bool IsIncome => Kind == "income";
}

public class CategoryTotal
{
    public string Category { get; set; } = "";
    public decimal Spent { get; set; }
    public decimal? Budget { get; set; }
    public int Percent => Budget is > 0 ? (int)Math.Round(Spent / Budget.Value * 100) : 0;
    public string Level => Budget is not > 0 ? "none" : Percent > 100 ? "over" : Percent >= 80 ? "warn" : "ok";
}

public class MonthBar
{
    public string Label { get; set; } = "";
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}

public class FinanceViewModel
{
    public string Period { get; set; } = "month";
    public DateTime Anchor { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string PeriodLabel { get; set; } = "";
    public DateTime PrevAnchor { get; set; }
    public DateTime NextAnchor { get; set; }

    public string Type { get; set; } = "all";
    public string Category { get; set; } = "";
    public string Q { get; set; } = "";

    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance => TotalIncome - TotalExpense;

    public decimal PrevIncome { get; set; }
    public decimal PrevExpense { get; set; }
    public string PrevLabel { get; set; } = "";

    public decimal? MonthlyBudget { get; set; }
    public decimal MonthExpense { get; set; }
    public int BudgetPercent => MonthlyBudget is > 0 ? (int)Math.Round(MonthExpense / MonthlyBudget.Value * 100) : 0;
    public decimal Available => (MonthlyBudget ?? 0) - MonthExpense;

    public List<CategoryTotal> ByCategory { get; set; } = new();
    public List<string> Alerts { get; set; } = new();
    public List<Movement> Movements { get; set; } = new();
    public List<MonthBar> YearBars { get; set; } = new();
    public int Year { get; set; }

    public List<string> ExpenseCategories { get; set; } = new();
    public List<string> IncomeCategories { get; set; } = new();

    public static int? ChangePercent(decimal current, decimal previous) =>
        previous == 0 ? null : (int)Math.Round((current - previous) / previous * 100);
}

public class BudgetsViewModel
{
    public decimal? General { get; set; }
    public List<(string Category, decimal? Amount)> PerCategory { get; set; } = new();
    public List<UserCategory> CustomCategories { get; set; } = new();
}
