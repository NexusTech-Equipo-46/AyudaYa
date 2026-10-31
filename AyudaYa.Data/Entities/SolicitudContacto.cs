using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

/// <summary>
/// Dirección y teléfono exactos. Solo solicitante, asignados activos y administrador.
/// </summary>
public partial class SolicitudContacto
{
    public Guid SolicitudId { get; set; }

    public string Direccion { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public DateTime CreadoAt { get; set; }

    public DateTime ActualizadoAt { get; set; }

    public virtual Solicitud Solicitud { get; set; } = null!;
}
