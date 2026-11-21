using System.ComponentModel.DataAnnotations;
using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Validaciones;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AyudaYa.Web.Models.Solicitudes;

public sealed class RecursoSolicitudViewModel
{
    public Guid TipoRecursoId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public bool Seleccionado { get; set; }

    [Range(1, 100000, ErrorMessage = "Indicá cuántas personas necesitan este recurso (un número mayor a cero).")]
    public int? Personas { get; set; }

    [StringLength(SolicitudAppService.LargoMaximoDetalleRecurso,
        ErrorMessage = "El detalle no puede superar los 200 caracteres.")]
    public string? Detalle { get; set; }
}

public sealed class NuevaSolicitudViewModel
{
    [Required(ErrorMessage = "Elegí la ciudad donde estás.")]
    public Guid? CiudadId { get; set; }

    [Required(ErrorMessage = "Elegí la zona donde estás.")]
    public Guid? ZonaId { get; set; }

    [Required(ErrorMessage = "Elegí la urgencia.")]
    public string Urgencia { get; set; } = "normal";

    [Required(ErrorMessage = "Indicá cuántas personas necesitan ayuda.")]
    [Range(1, 100000, ErrorMessage = "Escribí un número entero mayor a cero.")]
    public int? PersonasAfectadas { get; set; }

    [StringLength(SolicitudAppService.LargoMaximoDescripcion,
        ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Escribí tu dirección para que puedan encontrarte.")]
    [StringLength(SolicitudAppService.LargoMaximoDireccion,
        ErrorMessage = "La dirección no puede superar los 200 caracteres.")]
    public string? Direccion { get; set; }

    [Required(ErrorMessage = "Escribí un teléfono de contacto.")]
    [StringLength(SolicitudAppService.LargoMaximoTelefono,
        ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    [TelefonoValido]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "Marcá en el mapa dónde estás (o usá tu ubicación).")]
    [Range(-90, 90, ErrorMessage = "La latitud tiene que estar entre -90 y 90.")]
    public double? Latitud { get; set; }

    [Required(ErrorMessage = "Marcá en el mapa dónde estás (o usá tu ubicación).")]
    [Range(-180, 180, ErrorMessage = "La longitud tiene que estar entre -180 y 180.")]
    public double? Longitud { get; set; }

    public List<RecursoSolicitudViewModel> Recursos { get; set; } = [];

    [ValidateNever]
    public IReadOnlyList<CiudadDto> Ciudades { get; set; } = [];

    [ValidateNever]
    public IReadOnlyList<ZonaDto> Zonas { get; set; } = [];

    [ValidateNever]
    public bool EmailConfirmado { get; set; } = true;
}
