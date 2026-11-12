using AyudaYa.Negocio.Servicios;

namespace AyudaYa.Web.Models;

public sealed class DetalleCentroViewModel
{
    public required CentroPublicoDto Centro { get; init; }
    public required IReadOnlyList<TipoRecursoPublicoDto> TiposRecurso { get; init; }
}
