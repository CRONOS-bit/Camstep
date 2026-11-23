namespace Camstep.Models;

public class CalendarEvent
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public bool AllDay { get; set; }
    public string Location { get; set; } = "";
    public string Color { get; set; } = "azul";

    public int? ReminderMinutesBefore { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static readonly (string Id, string Name)[] Colors =
    {
        ("azul", "Azul"), ("verde", "Verde"), ("morado", "Morado"), ("naranja", "Naranja"), ("rojo", "Rojo")
    };
}
