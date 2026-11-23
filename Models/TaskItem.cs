using System.ComponentModel.DataAnnotations;

namespace Camstep.Models;

public class TaskItem
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [Required(ErrorMessage = "Escribe un título para la tarea.")]
    [StringLength(120, ErrorMessage = "El título admite máximo 120 caracteres.")]
    public string Title { get; set; } = "";

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    [StringLength(1000, ErrorMessage = "La descripción admite máximo 1000 caracteres.")]
    public string Description { get; set; } = "";

    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; } = DateTime.Today;

    public string Priority { get; set; } = "Media";

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public bool IsOverdue(DateTime today) => !IsCompleted && DueDate.Date < today.Date;

    public bool IsDueSoon(DateTime today) =>
        !IsCompleted && DueDate.Date >= today.Date && DueDate.Date <= today.Date.AddDays(2);
}
