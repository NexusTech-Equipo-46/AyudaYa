using System.ComponentModel.DataAnnotations;

namespace AyudaYa.Web.Models.Cuenta;

public sealed class RegistroViewModel
{
    private string? _dni;
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150)]
    [Display(Name = "Nombre y apellido")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repetí la contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Repetir contraseña")]
    public string ConfirmarPassword { get; set; } = string.Empty;

    /// <summary>A dónde volver después de crear la cuenta (solo direcciones de la app).</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>Mensaje orientativo cuando la persona llegó al registro desde un flujo puntual.</summary>
    public string? AvisoDestino => (ReturnUrl?.Split('?')[0].TrimEnd('/').ToLowerInvariant()) switch
    {
        "/organizacion/registrar" => "Primero creá tu cuenta y confirmá tu correo. Después te llevamos directo a registrar tu organización.",
        "/voluntariado" or "/voluntariado/index" => "Primero creá tu cuenta y confirmá tu correo. Después te llevamos directo a completar tus datos de voluntario.",
        "/solicitudes/nueva" => "Primero creá tu cuenta y confirmá tu correo. Después te llevamos directo a cargar tu pedido de ayuda.",
        _ => null
    };

    [RegularExpression(@"^[0-9]{6,9}$", ErrorMessage = "El DNI debe tener entre 6 y 9 dígitos.")]
    [Display(Name = "DNI (opcional)")]
    public string? Dni
    {
        get => _dni;
        set => _dni = string.IsNullOrWhiteSpace(value)
            ? null
            : new string(value.Where(c => c != '.' && !char.IsWhiteSpace(c)).ToArray());
    }
}
