using System.ComponentModel.DataAnnotations;
using AyudaYa.Negocio.Servicios;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AyudaYa.Web.Models.Solicitudes;

public sealed class NuevaSolicitudViewModel
{
    [Required(ErrorMessage = "Elegí una zona.")]
    public Guid? ZonaId { get; set; }

    [Required(ErrorMessage = "Elegí la urgencia.")]
    public string Urgencia { get; set; } = "normal";

    [Range(1, int.MaxValue, ErrorMessage = "Indicá al menos una persona afectada.")]
    public int PersonasAfectadas { get; set; }

    [StringLength(SolicitudAppService.LargoMaximoDescripcion)]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Ingresá la dirección.")]
    [StringLength(SolicitudAppService.LargoMaximoDireccion)]
    public string Direccion { get; set; } = "";

    [Required(ErrorMessage = "Ingresá el teléfono.")]
    [StringLength(SolicitudAppService.LargoMaximoTelefono)]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "Indicá la latitud.")]
    public string Latitud { get; set; } = "";

    [Required(ErrorMessage = "Indicá la longitud.")]
    public string Longitud { get; set; } = "";

    public List<RecursoNuevaSolicitud> Recursos { get; set; } = [];

    [BindNever]
    public IReadOnlyList<ZonaParaSolicitud> Zonas { get; set; } = [];

    [BindNever]
    public IReadOnlyDictionary<Guid, string> NombresRecursos { get; set; } =
        new Dictionary<Guid, string>();
}

public sealed class RecursoNuevaSolicitud
{
    public Guid TipoRecursoId { get; set; }
    public bool Seleccionado { get; set; }
    public int? Personas { get; set; }
    public string? Detalle { get; set; }
}