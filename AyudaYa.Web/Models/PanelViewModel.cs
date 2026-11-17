using AyudaYa.Negocio.Servicios;

namespace AyudaYa.Web.Models;

public sealed class PanelViewModel
{
    public required string Nombre { get; init; }

    public bool EmailConfirmado { get; init; }

    public bool EsVoluntario { get; init; }

    public IReadOnlyList<MembresiaDto> Organizaciones { get; init; } = [];

    public IReadOnlyList<SolicitudResumenDto> Abiertas { get; init; } = [];

    public int CantidadAbiertas { get; init; }

    public int CantidadResueltas { get; init; }

    public string PrimerNombre => Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? Nombre;
}
