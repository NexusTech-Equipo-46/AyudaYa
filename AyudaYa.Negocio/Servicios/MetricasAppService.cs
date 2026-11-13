using System.Text.Json;
using System.Text.Json.Serialization;
using AyudaYa.Data;
using Microsoft.EntityFrameworkCore;

namespace AyudaYa.Negocio.Servicios;

public sealed record ImpactoRecursoDto(string Codigo, string Nombre, long RecursosResueltos, long Personas);

public sealed record ImpactoMesDto(DateOnly Mes, long RecursosResueltos, long Personas);

public sealed record ImpactoDto(
    long TotalPersonas,
    long TotalRecursosResueltos,
    IReadOnlyList<ImpactoRecursoDto> PorRecurso,
    IReadOnlyList<ImpactoMesDto> PorMes);

public sealed class NecesidadRecursoDto
{
    [JsonPropertyName("tipo_recurso")]
    public string Codigo { get; set; } = "";

    [JsonPropertyName("tipo_recurso_nombre")]
    public string Nombre { get; set; } = "";

    [JsonPropertyName("solicitudes")]
    public long Solicitudes { get; set; }

    [JsonPropertyName("personas")]
    public long Personas { get; set; }
}

/// <summary>Agregado público y aproximado: sin direcciones, teléfonos ni coordenadas exactas.</summary>
public sealed record NecesidadZonaDto(
    Guid ZonaId,
    string Zona,
    string Ciudad,
    double Latitud,
    double Longitud,
    long SolicitudesAbiertas,
    long PersonasAfectadas,
    IReadOnlyList<NecesidadRecursoDto> Recursos);

/// <summary>Métricas públicas construidas sobre las vistas agregadas de la base.</summary>
public sealed class MetricasAppService
{
    private readonly AyudaYaDbContext _db;

    public MetricasAppService(AyudaYaDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Personas alcanzadas por recurso resuelto. Una misma persona puede
    /// contarse en más de un recurso (es la unidad de la métrica).
    /// </summary>
    public async Task<ImpactoDto> ObtenerImpactoAsync(
        Guid? ciudadId, int ultimosMeses = 12, CancellationToken cancellationToken = default)
    {
        var desde = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-Math.Max(ultimosMeses, 1)));
        var desdeMes = new DateOnly(desde.Year, desde.Month, 1);

        var filas = await _db.VMetricasPersonasPorRecurso.AsNoTracking()
            .Where(m => (ciudadId == null || m.CiudadId == ciudadId) && m.Mes >= desdeMes)
            .ToListAsync(cancellationToken);

        var porRecurso = filas
            .GroupBy(f => new { f.TipoRecursoCodigo, f.TipoRecursoNombre })
            .Select(g => new ImpactoRecursoDto(
                g.Key.TipoRecursoCodigo ?? "",
                g.Key.TipoRecursoNombre ?? "",
                g.Sum(x => x.RecursosResueltos ?? 0),
                g.Sum(x => x.PersonasAlcanzadas ?? 0)))
            .OrderByDescending(x => x.Personas)
            .ToList();

        var porMes = filas
            .Where(f => f.Mes is not null)
            .GroupBy(f => f.Mes!.Value)
            .Select(g => new ImpactoMesDto(
                g.Key,
                g.Sum(x => x.RecursosResueltos ?? 0),
                g.Sum(x => x.PersonasAlcanzadas ?? 0)))
            .OrderBy(x => x.Mes)
            .ToList();

        return new ImpactoDto(
            porRecurso.Sum(x => x.Personas),
            porRecurso.Sum(x => x.RecursosResueltos),
            porRecurso,
            porMes);
    }

    public async Task<List<NecesidadZonaDto>> ObtenerNecesidadesPorZonaAsync(
        Guid? ciudadId, CancellationToken cancellationToken = default)
    {
        var filas = await _db.VMapaSolicitudesPorZona.AsNoTracking()
            .Where(v => ciudadId == null || v.CiudadId == ciudadId)
            .OrderBy(v => v.ZonaNombre)
            .ToListAsync(cancellationToken);

        return filas
            .Where(f => f.ZonaId is not null
                && f.LatitudAproximada is not null
                && f.LongitudAproximada is not null)
            .Select(f => new NecesidadZonaDto(
                f.ZonaId!.Value,
                f.ZonaNombre ?? "",
                f.CiudadNombre ?? "",
                f.LatitudAproximada!.Value,
                f.LongitudAproximada!.Value,
                f.SolicitudesAbiertas ?? 0,
                f.PersonasAfectadas ?? 0,
                JsonSerializer.Deserialize<List<NecesidadRecursoDto>>(f.Recursos ?? "[]") ?? []))
            .ToList();
    }
}
