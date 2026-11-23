namespace Camstep.Models;

public class CalendarViewModel
{
    public DateTime CurrentMonth { get; set; }
    public DateTime Today { get; set; } = DateTime.Today;
    public DateTime SelectedDate { get; set; }

    public int FirstDayOffset { get; set; }
    public int DaysInMonth { get; set; }

    public string View { get; set; } = "month";

    public DateTime WeekStart { get; set; }

    public IEnumerable<DateTime> WeekDays => Enumerable.Range(0, 7).Select(i => WeekStart.AddDays(i));

    public DateTime PrevDate => View switch
    {
        "week" => SelectedDate.AddDays(-7),
        "day" => SelectedDate.AddDays(-1),
        _ => new DateTime(CurrentMonth.AddMonths(-1).Year, CurrentMonth.AddMonths(-1).Month, 1)
    };

    public DateTime NextDate => View switch
    {
        "week" => SelectedDate.AddDays(7),
        "day" => SelectedDate.AddDays(1),
        _ => new DateTime(CurrentMonth.AddMonths(1).Year, CurrentMonth.AddMonths(1).Month, 1)
    };

    public string Show { get; set; } = "all";

    public List<TaskItem> Tasks { get; set; } = new();
    public List<CalendarEvent> Events { get; set; } = new();

    public List<AgendaItem> Upcoming { get; set; } = new();

    public bool ShowTasks => Show != "events";
    public bool ShowEvents => Show != "tasks";

    public List<TaskItem> TasksOn(DateTime date) =>
        ShowTasks
            ? Tasks.Where(t => t.DueDate.Date == date.Date)
                   .OrderBy(t => t.IsCompleted)
                   .ThenBy(t => Services.Priorities.Rank(t.Priority))
                   .ToList()
            : new();

    public List<CalendarEvent> EventsOn(DateTime date) =>
        ShowEvents
            ? Events.Where(e => e.StartsAt.Date == date.Date)
                    .OrderBy(e => !e.AllDay)
                    .ThenBy(e => e.StartsAt)
                    .ToList()
            : new();

    public List<TaskItem> SelectedDayTasks => TasksOn(SelectedDate);
    public List<CalendarEvent> SelectedDayEvents => EventsOn(SelectedDate);

    public DateTime PreviousMonth => CurrentMonth.AddMonths(-1);
    public DateTime NextMonth => CurrentMonth.AddMonths(1);

    public string SelectedUrlDate => SelectedDate.ToString("yyyy-MM-dd");
}

public class AgendaItem
{
    public DateTime When { get; set; }
    public string Title { get; set; } = "";
    public string Kind { get; set; } = "task";
    public string Detail { get; set; } = "";
    public bool Overdue { get; set; }
}

public class TaskListViewModel
{
    public List<TaskItem> Items { get; set; } = new();
    public DateTime Today { get; set; } = DateTime.Today;

    public string Status { get; set; } = "all";
    public string Priority { get; set; } = "all";
    public string Range { get; set; } = "all";
    public string Sort { get; set; } = "date";
    public string Q { get; set; } = "";

    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int CompletedCount { get; set; }
    public int OverdueCount { get; set; }

    public bool HasFilters =>
        Status != "all" || Priority != "all" || Range != "all" || !string.IsNullOrWhiteSpace(Q);
}
