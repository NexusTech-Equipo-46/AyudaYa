using AyudaYa.Negocio.Servicios;

namespace AyudaYa.Web.Models;

public sealed class MapaCentrosViewModel
{
    public required IReadOnlyList<CentroPublicoDto> Centros { get; init; }

    public required IReadOnlyList<ZonaDto> Zonas { get; init; }

    public required IReadOnlyList<TipoCatalogoDto> TiposCentro { get; init; }

    public required IReadOnlyList<TipoCatalogoDto> TiposRecurso { get; init; }

    public required IReadOnlyList<NecesidadZonaDto> Necesidades { get; init; }

    public string? RecursoInicial { get; init; }

    /// <summary>Datos mínimos que necesita el JavaScript del mapa (sin teléfonos ni correos).</summary>
    public object DatosCentros => Centros.Select(c => new
    {
        id = c.Id,
        nombre = c.Nombre,
        direccion = c.Direccion,
        zonaId = c.ZonaId,
        zona = c.Zona,
        tipo = c.TipoCodigo,
        tipoNombre = c.TipoNombre,
        estado = c.EstadoOperativo,
        latitud = c.Latitud,
        longitud = c.Longitud,
        recursos = c.Recursos,
        actualizado = c.RecursosActualizadoAt
    }).ToList();

    public object DatosNecesidades => Necesidades.Select(n => new
    {
        zonaId = n.ZonaId,
        zona = n.Zona,
        latitud = n.Latitud,
        longitud = n.Longitud,
        solicitudes = n.SolicitudesAbiertas,
        personas = n.PersonasAfectadas,
        recursos = n.Recursos.Select(r => new { nombre = r.Nombre, solicitudes = r.Solicitudes, personas = r.Personas })
    }).ToList();
}
