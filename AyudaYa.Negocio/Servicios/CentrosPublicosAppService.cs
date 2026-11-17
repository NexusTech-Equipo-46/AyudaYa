using System.Text.Json;
using System.Text.Json.Serialization;
using AyudaYa.Data;
using Microsoft.EntityFrameworkCore;

namespace AyudaYa.Negocio.Servicios;

// Información de un turno de atención.
public sealed class HorarioPublicoDto
{
    [JsonPropertyName("dia_semana")]
    public int DiaSemana { get; set; }

    [JsonPropertyName("abre")]
    public string? Abre { get; set; }

    [JsonPropertyName("cierra")]
    public string? Cierra { get; set; }

    [JsonPropertyName("todo_el_dia")]
    public bool TodoElDia { get; set; }

    [JsonPropertyName("notas")]
    public string? Notas { get; set; }
}

// Datos públicos que necesitan el mapa y el detalle.
public sealed class CentroPublicoDto
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = "";
    public string Ciudad { get; init; } = "";
    public Guid? ZonaId { get; init; }
    public string Zona { get; init; } = "";
    public string TipoCodigo { get; init; } = "";
    public string TipoNombre { get; init; } = "";
    public string? OrganizacionNombre { get; init; }
    public string? OrganizacionTipo { get; init; }
    public string? Direccion { get; init; }
    public string? Telefono { get; init; }
    public string? Email { get; init; }
    public double? Latitud { get; init; }
    public double? Longitud { get; init; }
    public string? EstadoOperativo { get; init; }
    public string? Descripcion { get; init; }
    public DateTime? RecursosActualizadoAt { get; init; }
    public Dictionary<string, string> Recursos { get; init; } = new();
    public List<HorarioPublicoDto> Horarios { get; init; } = new();
}

public sealed record TipoRecursoPublicoDto(string Codigo, string Nombre, bool Activo);

public sealed class CentrosPublicosAppService
{
    private readonly AyudaYaDbContext _db;

    public CentrosPublicosAppService(AyudaYaDbContext db)
    {
        _db = db;
    }

    public async Task<List<CentroPublicoDto>> ObtenerTodosAsync(
        CancellationToken cancellationToken = default)
    {
        var filas = await _db.VCentrosPublicos
            .AsNoTracking()
            .Where(c => c.Id != null)
            .OrderBy(c => c.Nombre)
            .ToListAsync(cancellationToken);

        return filas.Select(Convertir).ToList();
    }

    public async Task<CentroPublicoDto?> ObtenerPorIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var fila = await _db.VCentrosPublicos
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

        return fila is null ? null : Convertir(fila);
    }

    public Task<List<TipoRecursoPublicoDto>> ObtenerTiposRecursoAsync(
        CancellationToken cancellationToken = default) =>
        _db.TipoRecurso.AsNoTracking()
            .Select(t => new TipoRecursoPublicoDto(t.Codigo, t.Nombre, t.Activo))
            .ToListAsync(cancellationToken);

    private static CentroPublicoDto Convertir(VCentrosPublicos c)
    {
        return new CentroPublicoDto
        {
            Id = c.Id!.Value,
            Nombre = c.Nombre ?? "Centro sin nombre",
            Ciudad = c.CiudadNombre ?? "",
            ZonaId = c.ZonaId,
            Zona = c.ZonaNombre ?? "",
            TipoCodigo = c.TipoCentroCodigo ?? "",
            TipoNombre = c.TipoCentroNombre ?? "",
            OrganizacionNombre = c.OrganizacionNombre,
            OrganizacionTipo = c.OrganizacionTipo,
            Direccion = c.Direccion,
            Telefono = c.TelefonoContacto,
            Email = c.EmailContacto,
            Latitud = c.Latitud,
            Longitud = c.Longitud,
            EstadoOperativo = c.EstadoOperativo,
            Descripcion = c.Descripcion,
            RecursosActualizadoAt = c.RecursosActualizadoAt,
            Recursos = JsonSerializer.Deserialize<Dictionary<string, string>>(
                c.Recursos ?? "{}") ?? new(),
            Horarios = JsonSerializer.Deserialize<List<HorarioPublicoDto>>(
                c.Horarios ?? "[]") ?? new()
        };
    }
}
