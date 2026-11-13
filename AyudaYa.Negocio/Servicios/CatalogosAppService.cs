using AyudaYa.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AyudaYa.Negocio.Servicios;

public sealed record CiudadDto(Guid Id, string Nombre, string Provincia, bool Activa);

public sealed record ZonaDto(
    Guid Id,
    Guid CiudadId,
    string Nombre,
    bool Activa,
    double? LatitudCentro,
    double? LongitudCentro);

public sealed record TipoCatalogoDto(Guid Id, string Codigo, string Nombre);

/// <summary>Catálogos de solo lectura que alimentan los formularios y filtros.</summary>
public sealed class CatalogosAppService
{
    private readonly AyudaYaDbContext _db;

    public CatalogosAppService(AyudaYaDbContext db)
    {
        _db = db;
    }

    public Task<List<CiudadDto>> CiudadesActivasAsync(CancellationToken cancellationToken = default) =>
        _db.Ciudad.AsNoTracking()
            .Where(c => c.Activa)
            .OrderBy(c => c.Nombre)
            .Select(c => new CiudadDto(c.Id, c.Nombre, c.Provincia, c.Activa))
            .ToListAsync(cancellationToken);

    /// <summary>Zonas activas de ciudades activas, opcionalmente de una sola ciudad.</summary>
    public Task<List<ZonaDto>> ZonasActivasAsync(
        Guid? ciudadId = null, CancellationToken cancellationToken = default) =>
        _db.Zona.AsNoTracking()
            .Where(z => z.Activa && z.Ciudad.Activa && (ciudadId == null || z.CiudadId == ciudadId))
            .OrderBy(z => z.Nombre)
            .Select(z => new ZonaDto(
                z.Id, z.CiudadId, z.Nombre, z.Activa, z.LatitudCentro, z.LongitudCentro))
            .ToListAsync(cancellationToken);

    public Task<List<TipoCatalogoDto>> TiposRecursoActivosAsync(
        CancellationToken cancellationToken = default) =>
        _db.TipoRecurso.AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .Select(t => new TipoCatalogoDto(t.Id, t.Codigo, t.Nombre))
            .ToListAsync(cancellationToken);

    public Task<List<TipoCatalogoDto>> TiposCentroActivosAsync(
        CancellationToken cancellationToken = default) =>
        _db.TipoCentro.AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .Select(t => new TipoCatalogoDto(t.Id, t.Codigo, t.Nombre))
            .ToListAsync(cancellationToken);
}
