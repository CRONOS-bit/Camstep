using System.ComponentModel.DataAnnotations;

namespace Camstep.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Escribe tu nombre.")]
    [StringLength(80, ErrorMessage = "El nombre admite máximo 80 caracteres.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "Escribe un correo válido.")]
    [StringLength(150)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Escribe una contraseña.")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [StringLength(100)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirma la contraseña.")]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = "";
}
