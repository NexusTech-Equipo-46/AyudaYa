using AyudaYa.Negocio.Servicios;

namespace AyudaYa.Web.Models;

public sealed class MapaCentrosViewModel
{
    public IReadOnlyList<CentroPublicoDto> Centros { get; init; } = [];
    public IReadOnlyList<TipoRecursoPublicoDto> TiposRecurso { get; init; } = [];
    public bool ErrorCarga { get; init; }
}
