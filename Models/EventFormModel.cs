using System.ComponentModel.DataAnnotations;

namespace Camstep.Models;

public class EventFormModel : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Escribe un título para el evento.")]
    [StringLength(120, ErrorMessage = "El título admite máximo 120 caracteres.")]
    public string Title { get; set; } = "";

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    [StringLength(1000, ErrorMessage = "La descripción admite máximo 1000 caracteres.")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Elige la fecha del evento.")]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    [DataType(DataType.Time)]
    public TimeSpan? StartTime { get; set; }

    [DataType(DataType.Time)]
    public TimeSpan? EndTime { get; set; }

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    [StringLength(150, ErrorMessage = "El lugar admite máximo 150 caracteres.")]
    public string Location { get; set; } = "";

    public string Color { get; set; } = "azul";

    public int? ReminderMinutesBefore { get; set; }

    public static readonly (int? Value, string Name)[] ReminderOptions =
    {
        (null, "Predeterminado (según Avisos)"), (0, "Sin recordatorio"), (10, "10 minutos antes"),
        (30, "30 minutos antes"), (60, "1 hora antes"), (180, "3 horas antes"), (1440, "1 día antes"), (2880, "2 días antes")
    };

    public string? ReturnUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime != null && StartTime == null)
            yield return new ValidationResult("Si pones hora de fin, indica también la hora de inicio.", new[] { nameof(StartTime) });

        if (EndTime != null && StartTime != null && EndTime <= StartTime)
            yield return new ValidationResult("La hora de fin debe ser posterior a la de inicio.", new[] { nameof(EndTime) });

        if (Date.Year < 2000 || Date.Year > 2100)
            yield return new ValidationResult("Elige una fecha entre los años 2000 y 2100.", new[] { nameof(Date) });

        if (!CalendarEvent.Colors.Any(c => c.Id == Color))
            yield return new ValidationResult("Elige un color de la lista.", new[] { nameof(Color) });
    }

    public static EventFormModel From(CalendarEvent e, string? returnUrl) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Description = e.Description,
        Date = e.StartsAt.Date,
        StartTime = e.AllDay ? null : e.StartsAt.TimeOfDay,
        EndTime = e.EndsAt?.TimeOfDay,
        Location = e.Location,
        Color = e.Color,
        ReminderMinutesBefore = e.ReminderMinutesBefore,
        ReturnUrl = returnUrl
    };

    public void ApplyTo(CalendarEvent e)
    {
        e.Title = Title.Trim();
        e.Description = (Description ?? "").Trim();
        e.Location = (Location ?? "").Trim();
        e.Color = Color;
        e.ReminderMinutesBefore = ReminderMinutesBefore is null ? null : Math.Clamp(ReminderMinutesBefore.Value, 0, 10080);
        e.AllDay = StartTime == null;
        e.StartsAt = Date.Date + (StartTime ?? TimeSpan.Zero);
        e.EndsAt = EndTime == null ? null : Date.Date + EndTime.Value;
    }
}
