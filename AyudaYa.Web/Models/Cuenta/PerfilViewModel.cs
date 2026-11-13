using System.ComponentModel.DataAnnotations;

namespace AyudaYa.Web.Models.Cuenta;

public sealed class PerfilViewModel
{
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool Activo { get; set; }
    public bool EsVoluntario { get; set; }
    public DatosPerfilViewModel Datos { get; set; } = new();
    public CambiarClaveViewModel Clave { get; set; } = new();
}

public sealed class DatosPerfilViewModel
{
    private string? _dni;

    [Required(ErrorMessage = "Escribí tu nombre y apellido")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;

    [RegularExpression(@"^[0-9]{6,9}$", ErrorMessage = "El DNI tiene que tener entre 6 y 9 números")]
    public string? Dni
    {
        get => _dni;
        set => _dni = string.IsNullOrWhiteSpace(value)
            ? null
            : new string(value.Where(c => c != '.' && !char.IsWhiteSpace(c)).ToArray());
    }
}

public sealed class CambiarClaveViewModel
{
    [Required(ErrorMessage = "Escribí tu contraseña actual")]
    [DataType(DataType.Password)]
    public string Actual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elegí una contraseña nueva")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    [DataType(DataType.Password)]
    public string Nueva { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repetí la contraseña nueva")]
    [Compare(nameof(Nueva), ErrorMessage = "Las contraseñas no coinciden")]
    [DataType(DataType.Password)]
    public string Confirmacion { get; set; } = string.Empty;
}
