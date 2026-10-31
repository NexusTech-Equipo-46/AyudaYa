using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

public partial class VCentrosPublicos
{
    public Guid? Id { get; set; }

    public string? Nombre { get; set; }

    public string? TipoCentroCodigo { get; set; }

    public string? TipoCentroNombre { get; set; }

    public Guid? OrganizacionId { get; set; }

    public string? OrganizacionNombre { get; set; }

    public string? OrganizacionTipo { get; set; }

    public Guid? CiudadId { get; set; }

    public string? CiudadNombre { get; set; }

    public Guid? ZonaId { get; set; }

    public string? ZonaNombre { get; set; }

    public string? Direccion { get; set; }

    public string? TelefonoContacto { get; set; }

    public string? EmailContacto { get; set; }

    public double? Latitud { get; set; }

    public double? Longitud { get; set; }

    public string? EstadoOperativo { get; set; }

    public string? Descripcion { get; set; }

    public DateTime? CentroActualizadoAt { get; set; }

    public DateTime? RecursosActualizadoAt { get; set; }

    public string? Recursos { get; set; }
}
