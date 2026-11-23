using System.Globalization;
using Camstep.Services;

namespace Camstep.Models;

public class FocusViewModel
{
    public List<TaskItem> Tasks { get; set; } = new();
    public int? SelectedTaskId { get; set; }
    public int TodayMinutes { get; set; }
    public int TodaySessions { get; set; }
    public int WeekMinutes { get; set; }
    public List<FocusSession> Recent { get; set; } = new();
}

public class HabitView
{
    public Habit Habit { get; set; } = new();
    public int Total { get; set; }
    public HashSet<DateTime> Done { get; set; } = new();

    private static readonly (int Min, string Name)[] Stages =
    {
        (0, "Semilla"), (1, "Brote"), (5, "Plantita"), (15, "Arbusto"), (30, "Árbol"), (60, "Árbol en flor")
    };

    public int Stage => Stages.Count(s => Total >= s.Min) - 1;
    public string StageName => Stages[Stage].Name;
    public int? NextAt => Stage + 1 < Stages.Length ? Stages[Stage + 1].Min : null;
    public string NextText => NextAt is int n ? $"{n - Total} día(s) más para ser {Stages[Stage + 1].Name.ToLower(Format.Es)}" : "¡Creció del todo! Sigue cuidándolo.";
    public int WeekCount(IEnumerable<DateTime> days) => days.Count(d => Done.Contains(d));
}

public class HabitsViewModel
{
    public DateTime Today { get; set; }
    public List<DateTime> Days { get; set; } = new();
    public List<HabitView> Habits { get; set; } = new();
}

public class ReviewViewModel
{
    public DateTime Monday { get; set; }
    public DateTime Today { get; set; }
    public DateTime Sunday => Monday.AddDays(6);
    public bool IsCurrentWeek => Today >= Monday && Today <= Sunday;
    public WeeklyReview? Review { get; set; }
    public List<Footprint> Footprints { get; set; } = new();
    public List<string> TasksDone { get; set; } = new();
    public int FocusMinutes { get; set; }
    public int HabitChecks { get; set; }
    public decimal Spent { get; set; }
    public decimal Earned { get; set; }
    public (string Category, decimal Amount) TopCategory { get; set; }
    public double? MoodAverage { get; set; }
    public List<Goal> GoalsInProgress { get; set; } = new();
    public int PendingNextWeek { get; set; }
    public int Overdue { get; set; }
}

public class PathViewModel
{
    public int Week { get; set; }
    public DateTime Monday { get; set; }
    public DateTime Today { get; set; }
    public List<Footprint> Footprints { get; set; } = new();
    public int LastWeekCount { get; set; }
    public int Streak { get; set; }
    public List<(DateTime Monday, int Count)> History { get; set; } = new();

    public int DaysWalked => Footprints.Select(f => f.When.Date).Distinct().Count();
    public int MaxPerDay => 10;

    public const int Width = 1000, Height = 260;
    public string PathD { get; private set; } = "";
    public List<(double X, double Y, double Angle, bool Left, Footprint Print)> Prints { get; } = new();
    public List<(double X, double Y, string Label, int Count, bool IsToday, bool IsFuture)> DayMarks { get; } = new();
    public (double X, double Y) Raccoon { get; private set; }

    private static double Px(int day, double t) => 40 + (day + t) * ((Width - 80) / 7.0);
    private static double Py(double x) => 140 + 55 * Math.Sin(x / 95.0) + 18 * Math.Sin(x / 37.0);

    public void BuildDrawing()
    {
        var inv = CultureInfo.InvariantCulture;
        var points = new List<string>();
        for (var x = 40.0; x <= Width - 40; x += 8)
            points.Add($"{x.ToString("0.#", inv)},{Py(x).ToString("0.#", inv)}");
        PathD = "M" + string.Join(" L", points);

        var labels = new[] { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
        var lastX = Px(0, 0.02);
        var anyPrint = false;
        for (var d = 0; d < 7; d++)
        {
            var date = Monday.AddDays(d);
            var day = Footprints.Where(f => f.When.Date == date).OrderBy(f => f.When).ToList();
            var cx = Px(d, 0.5);
            DayMarks.Add((cx, Py(cx), labels[d] + " " + date.Day, day.Count, date == Today, date > Today));

            var shown = day.Take(MaxPerDay).ToList();
            for (var i = 0; i < shown.Count; i++)
            {
                var t = (i + 1.0) / (shown.Count + 1.0);
                var x = Px(d, t);
                var y = Py(x);
                var dy = Py(x + 1) - y;
                var angle = Math.Atan2(dy, 1) * 180 / Math.PI;
                var left = i % 2 == 0;
                var off = left ? -9 : 9;
                var rad = (angle + 90) * Math.PI / 180;
                Prints.Add((x + off * Math.Cos(rad), y + off * Math.Sin(rad), angle + 90, left, shown[i]));
                lastX = x;
                anyPrint = true;
            }
        }

        var rx = anyPrint ? Math.Min(lastX + 26, Width - 40) : Px(0, 0.05);
        Raccoon = (rx, Py(rx));
    }
}
