using System.ComponentModel.DataAnnotations;

namespace Camstep.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "Escribe un correo válido.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu contraseña.")]
    public string Password { get; set; } = "";
}
