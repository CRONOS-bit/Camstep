namespace Camstep.Models;

public class HomeViewModel
{
    public string Greeting { get; set; } = "Hola";
    public string UserName { get; set; } = "";
    public DateTime Today { get; set; } = DateTime.Today;

    public int PendingTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int TodayTasks { get; set; }
    public List<TaskItem> ImportantTasks { get; set; } = new();
    public List<TaskItem> DueSoonTasks { get; set; } = new();
    public List<CalendarEvent> UpcomingEvents { get; set; } = new();

    public List<DayStat> Week { get; set; } = new();
    public int CompletedThisWeek { get; set; }

    public decimal MonthIncome { get; set; }
    public decimal MonthExpense { get; set; }
    public decimal MonthBalance => MonthIncome - MonthExpense;
    public decimal? MonthlyBudget { get; set; }
    public int BudgetPercent => MonthlyBudget is > 0 ? (int)Math.Round(MonthExpense / MonthlyBudget.Value * 100) : 0;
    public List<CategoryTotal> TopCategories { get; set; } = new();
    public List<string> Alerts { get; set; } = new();

    public List<Goal> Goals { get; set; } = new();
    public int ActiveGoals { get; set; }
    public int CompletedGoals { get; set; }
    public int AverageGoalProgress { get; set; }

    public MoodEntry? MoodToday { get; set; }
    public List<(Habit Habit, bool Done)> HabitsToday { get; set; } = new();
    public int WeekFootprints { get; set; }
    public int Streak { get; set; }
    public int FocusMinutesToday { get; set; }
    public bool ReviewPending { get; set; }
}

public class DayStat
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = "";
    public int Completed { get; set; }
    public int Due { get; set; }
}
