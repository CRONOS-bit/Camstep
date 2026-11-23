using System.ComponentModel.DataAnnotations;

namespace Camstep.Models;

public class Goal
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [Required(ErrorMessage = "Escribe un nombre para la meta.")]
    [StringLength(120, ErrorMessage = "El nombre admite máximo 120 caracteres.")]
    public string Title { get; set; } = "";

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    [StringLength(1000, ErrorMessage = "La descripción admite máximo 1000 caracteres.")]
    public string Description { get; set; } = "";

    [Range(typeof(decimal), "0.01", "999999999999", ErrorMessage = "El objetivo debe ser mayor que 0.")]
    public decimal Target { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "El progreso no puede ser negativo.")]
    public decimal Current { get; set; }

    [DataType(DataType.Date)]
    public DateTime Deadline { get; set; } = DateTime.Today.AddMonths(1);

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    [StringLength(50)]
    public string Category { get; set; } = "Personal";

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    [StringLength(2000, ErrorMessage = "Las notas admiten máximo 2000 caracteres.")]
    public string Notes { get; set; } = "";

    public static readonly string[] Categories = { "Ahorro", "Salud", "Estudio", "Trabajo", "Personal", "Hogar", "Otros" };

    public bool IsCompleted => Target > 0 && Current >= Target;

    public bool IsOverdue(DateTime today) => !IsCompleted && Deadline.Date < today.Date;

    public string Status(DateTime today) => IsCompleted ? "completed" : IsOverdue(today) ? "overdue" : "pending";

    public int DaysLeft(DateTime today) => (Deadline.Date - today.Date).Days;

    public string TimeLeftText(DateTime today)
    {
        if (IsCompleted) return "Meta cumplida";
        var days = DaysLeft(today);
        return days switch
        {
            0 => "Vence hoy",
            1 => "Falta 1 día",
            > 1 => $"Faltan {days} días",
            -1 => "Venció ayer",
            _ => $"Venció hace {-days} días"
        };
    }

    public int Progress =>
        Target <= 0 ? 0 :
        Math.Clamp((int)Math.Round(Current / Target * 100), 0, 100);
}
