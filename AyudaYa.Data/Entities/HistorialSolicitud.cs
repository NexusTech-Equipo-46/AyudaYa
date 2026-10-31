using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

/// <summary>
/// Historial de cambios de estado de la solicitud. Lo escribe la capa Negocio en la misma transacción.
/// </summary>
public partial class HistorialSolicitud
{
    public Guid Id { get; set; }

    public Guid SolicitudId { get; set; }

    public string? EstadoAnterior { get; set; }

    public string EstadoNuevo { get; set; } = null!;

    public Guid ActorId { get; set; }

    public string? Motivo { get; set; }

    public DateTime CreadoAt { get; set; }

    public virtual UsuarioIdentity Actor { get; set; } = null!;

    public virtual Solicitud Solicitud { get; set; } = null!;
}
