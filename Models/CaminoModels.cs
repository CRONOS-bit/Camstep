namespace Camstep.Models;

public class FocusSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? TaskId { get; set; }
    public string TaskTitle { get; set; } = "";
    public int Minutes { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
}

public class Habit
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "🌱";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static readonly string[] Icons = { "🌱", "📚", "💧", "🏃", "🧘", "🍎", "😴", "✍️", "🎸", "🧹", "💊", "🙏", "🚶", "💻", "🌞", "🐾" };
}

public class HabitLog
{
    public int Id { get; set; }
    public int HabitId { get; set; }
    public int UserId { get; set; }
    public DateTime Date { get; set; }
}

public class MoodEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime Date { get; set; }
    public int Mood { get; set; }
    public string Note { get; set; } = "";

    public static readonly (int Value, string Emoji, string Text)[] Scale =
    {
        (1, "😞", "Muy mal"), (2, "😕", "Mal"), (3, "😐", "Normal"), (4, "🙂", "Bien"), (5, "😄", "Muy bien")
    };

    public static string EmojiFor(int mood) => Scale.FirstOrDefault(s => s.Value == mood).Emoji ?? "😐";
    public static string TextFor(int mood) => Scale.FirstOrDefault(s => s.Value == mood).Text ?? "";
}

public class WeeklyReview
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime WeekStart { get; set; }
    public string Wins { get; set; } = "";
    public string Step1 { get; set; } = "";
    public string Step2 { get; set; } = "";
    public string Step3 { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
