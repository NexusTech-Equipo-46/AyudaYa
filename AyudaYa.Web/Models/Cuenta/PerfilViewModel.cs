namespace AyudaYa.Web.Models.Cuenta;

public sealed class PerfilViewModel
{
    public string NombreCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    public bool Activo { get; set; }
}
