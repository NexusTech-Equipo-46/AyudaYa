using System.ComponentModel.DataAnnotations;
using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Validaciones;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AyudaYa.Web.Models.Voluntario;

public sealed class VoluntariadoViewModel
{
    [Required(ErrorMessage = "Elegí tu ciudad.")]
    public Guid? CiudadId { get; set; }

    public Guid? ZonaId { get; set; }

    [Required(ErrorMessage = "Escribí un teléfono de contacto.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    [TelefonoValido]
    public string? Telefono { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Tenés que aceptar para poder sumarte.")]
    public bool Acepto { get; set; }

    public bool YaEsVoluntario { get; set; }

    [ValidateNever]
    public IReadOnlyList<CiudadDto> Ciudades { get; set; } = [];

    [ValidateNever]
    public IReadOnlyList<ZonaDto> Zonas { get; set; } = [];

    [ValidateNever]
    public bool EmailConfirmado { get; set; } = true;
}

public sealed class PanelVoluntarioViewModel
{
    public required VoluntarioDto Voluntario { get; init; }

    public IReadOnlyList<AsignacionResumenDto> Activas { get; init; } = [];

    public int CantidadActivas { get; init; }

    public int CantidadPorConfirmar { get; init; }

    public int CantidadConfirmadas { get; init; }

    public IReadOnlyList<SolicitudDisponibleDto> Disponibles { get; init; } = [];

    public bool Ampliar { get; init; }

    public string? TipoRecurso { get; init; }

    public string? Urgencia { get; init; }

    public IReadOnlyList<TipoCatalogoDto> TiposRecurso { get; init; } = [];

    public bool HayFiltros => TipoRecurso is not null || Urgencia is not null;

    /// <summary>La consulta trae hasta un máximo: si lo alcanza, puede haber más.</summary>
    public bool ListaRecortada => Disponibles.Count >= SolicitudConsultasAppService.MaximoDisponibles;
}

public sealed class ListaAsignacionesViewModel
{
    public required IReadOnlyList<AsignacionResumenDto> Asignaciones { get; init; }

    public required string Filtro { get; init; }

    public int Total { get; init; }

    public Guid? OrganizacionId { get; init; }
}
