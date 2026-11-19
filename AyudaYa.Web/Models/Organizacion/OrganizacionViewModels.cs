using System.ComponentModel.DataAnnotations;
using AyudaYa.Negocio.Servicios;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AyudaYa.Web.Models.Organizacion;

public sealed class RegistrarOrganizacionViewModel
{
    [Required(ErrorMessage = "Elegí la ciudad.")]
    public Guid? CiudadId { get; set; }

    [Required(ErrorMessage = "Ingresá el nombre de la organización.")]
    [StringLength(OrganizacionAppService.LargoMaximoNombre,
        ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string? Nombre { get; set; }

    [Required(ErrorMessage = "Elegí el tipo de organización.")]
    public string? Tipo { get; set; }

    [Required(ErrorMessage = "Escribí un correo de contacto.")]
    [EmailAddress(ErrorMessage = "Escribí un correo con el formato nombre@ejemplo.com.")]
    public string? EmailContacto { get; set; }

    [Required(ErrorMessage = "Escribí un teléfono de contacto.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    public string? TelefonoContacto { get; set; }

    [StringLength(OrganizacionAppService.LargoMaximoDescripcion,
        ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string? Descripcion { get; set; }

    [ValidateNever]
    public IReadOnlyList<CiudadDto> Ciudades { get; set; } = [];
}

/// <summary>Contexto común de las pantallas de organización (menú y selector).</summary>
public sealed class ContextoOrganizacion
{
    public required Guid OrganizacionId { get; init; }

    public required string Nombre { get; init; }

    public required string Estado { get; init; }

    public required string Rol { get; init; }

    public IReadOnlyList<MembresiaDto> Membresias { get; init; } = [];

    public bool EsResponsable => Rol == "responsable";

    public bool EstaActiva => Estado == "activa";
}

public sealed class PanelOrganizacionViewModel
{
    public required ContextoOrganizacion Contexto { get; init; }

    public required PanelOrganizacionDto Panel { get; init; }

    public IReadOnlyList<AsignacionResumenDto> Activas { get; init; } = [];
}

public sealed class SolicitudesOrganizacionViewModel
{
    public required ContextoOrganizacion Contexto { get; init; }

    public required IReadOnlyList<SolicitudDisponibleDto> Disponibles { get; init; }

    public required OrganizacionDto Organizacion { get; init; }

    public IReadOnlyList<ZonaDto> Zonas { get; init; } = [];

    public IReadOnlyList<TipoCatalogoDto> TiposRecurso { get; init; } = [];

    public Guid? FiltroZona { get; init; }

    public string? FiltroRecurso { get; init; }

    public string? FiltroUrgencia { get; init; }

    public bool HayFiltros => FiltroZona is not null || FiltroRecurso is not null || FiltroUrgencia is not null;

    /// <summary>La consulta trae hasta un máximo: si lo alcanza, puede haber más.</summary>
    public bool ListaRecortada => Disponibles.Count >= SolicitudConsultasAppService.MaximoDisponibles;
}

public sealed class AsignacionesOrganizacionViewModel
{
    public required ContextoOrganizacion Contexto { get; init; }

    public required IReadOnlyList<AsignacionResumenDto> Asignaciones { get; init; }

    public required string Filtro { get; init; }

    public int Total { get; init; }
}

public sealed class CentrosOrganizacionViewModel
{
    public required ContextoOrganizacion Contexto { get; init; }

    public required IReadOnlyList<CentroOrganizacionDto> Centros { get; init; }
}

public sealed class CentroFormularioViewModel
{
    public Guid? Id { get; set; }

    public Guid OrganizacionId { get; set; }

    [Required(ErrorMessage = "Ingresá el nombre del centro.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string? Nombre { get; set; }

    [Required(ErrorMessage = "Elegí el tipo de centro.")]
    public Guid? TipoCentroId { get; set; }

    [Required(ErrorMessage = "Elegí la zona.")]
    public Guid? ZonaId { get; set; }

    [Required(ErrorMessage = "Ingresá la dirección.")]
    [StringLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres.")]
    public string? Direccion { get; set; }

    [Required(ErrorMessage = "Ingresá un teléfono de contacto.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "El correo del centro no parece válido.")]
    public string? Email { get; set; }

    [StringLength(300, ErrorMessage = "La descripción no puede superar los 300 caracteres.")]
    public string? Descripcion { get; set; }

    [Range(-90, 90, ErrorMessage = "La latitud tiene que estar entre -90 y 90.")]
    public double? Latitud { get; set; }

    [Range(-180, 180, ErrorMessage = "La longitud tiene que estar entre -180 y 180.")]
    public double? Longitud { get; set; }

    public bool TodoElDia { get; set; }

    public List<int> Dias { get; set; } = [];

    public TimeOnly? Abre { get; set; }

    public TimeOnly? Cierra { get; set; }

    public string? EstadoVerificacion { get; set; }

    /// <summary>Motivo del rechazo, para mostrarlo mientras se corrige el centro.</summary>
    public string? MotivoEstado { get; set; }

    public bool EsRechazado => EsEdicion && EstadoVerificacion == "rechazado";

    [ValidateNever]
    public ContextoOrganizacion? Contexto { get; set; }

    [ValidateNever]
    public IReadOnlyList<TipoCatalogoDto> TiposCentro { get; set; } = [];

    [ValidateNever]
    public IReadOnlyList<ZonaDto> Zonas { get; set; } = [];

    public bool EsEdicion => Id is not null;
}

public sealed class RecursosCentroViewModel
{
    public required ContextoOrganizacion Contexto { get; init; }

    public required CentroOrganizacionDto Centro { get; init; }

    public required IReadOnlyList<TipoCatalogoDto> Tipos { get; init; }
}

public sealed class MiembrosViewModel
{
    public required ContextoOrganizacion Contexto { get; init; }

    public required IReadOnlyList<MiembroDto> Miembros { get; init; }

    public Guid UsuarioActualId { get; init; }

    public string? Email { get; set; }

    public string Rol { get; set; } = "miembro";
}
