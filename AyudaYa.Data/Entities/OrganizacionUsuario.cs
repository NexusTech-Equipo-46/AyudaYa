using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

public partial class OrganizacionUsuario
{
    public Guid Id { get; set; }

    public Guid OrganizacionId { get; set; }

    public Guid UsuarioId { get; set; }

    public string Rol { get; set; } = null!;

    public bool Activo { get; set; }

    public Guid? CreadoPor { get; set; }

    public DateTime CreadoAt { get; set; }

    public DateTime ActualizadoAt { get; set; }

    public DateTime? DesactivadoAt { get; set; }

    public string? MotivoBaja { get; set; }

    public virtual UsuarioIdentity? CreadoPorNavigation { get; set; }

    public virtual Organizacion Organizacion { get; set; } = null!;

    public virtual UsuarioIdentity Usuario { get; set; } = null!;
}
