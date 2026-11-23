namespace Camstep.Models;

public class UserPreference
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Theme { get; set; } = "Light";

    public string? BackgroundFileName { get; set; }

    public int BackgroundBlur { get; set; } = 0;

    public int BackgroundZoom { get; set; } = 100;

    public int BackgroundPositionX { get; set; } = 50;

    public int BackgroundPositionY { get; set; } = 50;

    public bool ShowRain { get; set; } = true;

    public bool NotifyTasks { get; set; } = true;
    public bool NotifyEvents { get; set; } = true;
    public bool NotifyGoals { get; set; } = true;
    public bool NotifyBudget { get; set; } = true;

    public int TaskLeadDays { get; set; } = 1;

    public int EventLeadMinutes { get; set; } = 60;

    public int GoalLeadDays { get; set; } = 7;

    public bool AmbientMusic { get; set; } = true;

    public int AmbientVolume { get; set; } = 35;

    public bool ShowQuotes { get; set; } = true;

    public int QuoteCursor { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
