using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

/// <summary>
/// Auditoría de acciones administrativas. No guardar datos sensibles innecesarios en detalle.
/// </summary>
public partial class EventoAdministrativo
{
    public Guid Id { get; set; }

    public Guid AdministradorId { get; set; }

    public string Entidad { get; set; } = null!;

    public Guid? EntidadId { get; set; }

    public string Accion { get; set; } = null!;

    public string? Detalle { get; set; }

    public DateTime CreadoAt { get; set; }

    public virtual UsuarioIdentity Administrador { get; set; } = null!;
}
