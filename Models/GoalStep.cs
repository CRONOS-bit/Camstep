namespace Camstep.Models;

public class GoalStep
{
    public int Id { get; set; }
    public int GoalId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public bool IsDone { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int SortOrder { get; set; }
}

public class GoalListViewModel
{
    public List<Goal> Goals { get; set; } = new();
    public Dictionary<int, (int Done, int Total)> Steps { get; set; } = new();
    public DateTime Today { get; set; } = DateTime.Today;
    public string Status { get; set; } = "all";
    public string Category { get; set; } = "";
    public int PendingCount { get; set; }
    public int CompletedCount { get; set; }
    public int OverdueCount { get; set; }
    public int TotalCount { get; set; }
}

public class GoalDetailsViewModel
{
    public Goal Goal { get; set; } = new();
    public List<GoalStep> Steps { get; set; } = new();
    public DateTime Today { get; set; } = DateTime.Today;
}
