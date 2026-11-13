using AyudaYa.Negocio.Servicios;

namespace AyudaYa.Web.Models;

public sealed class InicioViewModel
{
    public IReadOnlyList<CentroPublicoDto> CentrosRecientes { get; init; } = [];
    public bool CentrosNoDisponibles { get; init; }
}
