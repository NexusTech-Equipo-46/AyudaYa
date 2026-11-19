using System.ComponentModel.DataAnnotations;

namespace AyudaYa.Web.Models.Cuenta;

public sealed class OlvideClaveViewModel
{
    [Required(ErrorMessage = "Escribí tu correo electrónico")]
    [EmailAddress(ErrorMessage = "Escribí un correo con el formato nombre@ejemplo.com")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Ya se procesó el pedido: se muestra la respuesta neutra.</summary>
    public bool Enviado { get; set; }
}

public sealed class RestablecerClaveViewModel
{
    public Guid UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elegí una contraseña nueva")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    [DataType(DataType.Password)]
    public string Nueva { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repetí la contraseña nueva")]
    [Compare(nameof(Nueva), ErrorMessage = "Las contraseñas no coinciden")]
    [DataType(DataType.Password)]
    public string Confirmacion { get; set; } = string.Empty;
}
