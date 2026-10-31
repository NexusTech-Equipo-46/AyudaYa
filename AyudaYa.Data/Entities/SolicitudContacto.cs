using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

/// <summary>
/// Dirección, teléfono y coordenadas exactas. Solo solicitante, personas asignadas activas,
/// miembros activos de la organización asignada y administrador.
/// </summary>
public partial class SolicitudContacto
{
    public Guid SolicitudId { get; set; }

    public string Direccion { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public double? Latitud { get; set; }

    public double? Longitud { get; set; }

    public DateTime CreadoAt { get; set; }

    public DateTime ActualizadoAt { get; set; }

    public virtual Solicitud Solicitud { get; set; } = null!;
}
