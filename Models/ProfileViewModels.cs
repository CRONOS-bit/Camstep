using System.ComponentModel.DataAnnotations;

namespace Camstep.Models;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Escribe tu nombre.")]
    [StringLength(80, ErrorMessage = "El nombre admite máximo 80 caracteres.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "Escribe un correo válido.")]
    [StringLength(150)]
    public string Email { get; set; } = "";

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string CurrentPassword { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Escribe tu contraseña actual.")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Escribe la nueva contraseña.")]
    [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
    [StringLength(100)]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Confirma la nueva contraseña.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = "";
}

public class DeleteAccountViewModel
{
    [Required(ErrorMessage = "Escribe tu contraseña.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Escribe ELIMINAR para confirmar.")]
    public string Confirmation { get; set; } = "";
}

public class ProfilePageViewModel
{
    public ProfileViewModel Profile { get; set; } = new();
    public ChangePasswordViewModel Password { get; set; } = new();
    public DeleteAccountViewModel Delete { get; set; } = new();
    public Dictionary<string, int> Counts { get; set; } = new();
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "Escribe un correo válido.")]
    public string Email { get; set; } = "";
}

public class ResetPasswordViewModel
{
    public string Token { get; set; } = "";

    [Required(ErrorMessage = "Escribe la nueva contraseña.")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [StringLength(100)]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Confirma la nueva contraseña.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = "";
}
