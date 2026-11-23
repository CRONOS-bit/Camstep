using System.ComponentModel.DataAnnotations;

namespace Camstep.Models;

public class Expense
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [Required(ErrorMessage = "Escribe una descripción para el gasto.")]
    [StringLength(150, ErrorMessage = "La descripción admite máximo 150 caracteres.")]
    public string Description { get; set; } = "";

    [Range(typeof(decimal), "0.01", "999999999999", ErrorMessage = "El valor debe ser mayor que 0.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Elige una categoría.")]
    [StringLength(50)]
    public string Category { get; set; } = "Otros";

    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    public static readonly string[] Categories =
    {
        "Alimentación", "Transporte", "Estudio", "Entretenimiento", "Hogar", "Salud", "Servicios", "Otros"
    };
}
