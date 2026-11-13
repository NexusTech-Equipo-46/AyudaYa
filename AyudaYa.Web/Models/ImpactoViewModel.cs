using AyudaYa.Negocio.Servicios;

namespace AyudaYa.Web.Models;

public sealed class ImpactoViewModel
{
    public required ImpactoDto Impacto { get; init; }

    public required IReadOnlyList<CiudadDto> Ciudades { get; init; }

    public Guid? CiudadId { get; init; }
}
