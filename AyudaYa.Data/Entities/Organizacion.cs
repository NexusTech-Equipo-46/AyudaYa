using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

public partial class Organizacion
{
    public Guid Id { get; set; }

    public Guid CiudadId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public string EmailContacto { get; set; } = null!;

    public string TelefonoContacto { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? MotivoEstado { get; set; }

    public Guid RegistradaPor { get; set; }

    public Guid? AprobadaPor { get; set; }

    public DateTime? AprobadaAt { get; set; }

    public DateTime CreadoAt { get; set; }

    public DateTime ActualizadoAt { get; set; }

    public virtual UsuarioIdentity? AprobadaPorNavigation { get; set; }

    public virtual ICollection<Asignacion> Asignacion { get; set; } = new List<Asignacion>();

    public virtual ICollection<Centro> Centro { get; set; } = new List<Centro>();

    public virtual Ciudad Ciudad { get; set; } = null!;

    public virtual ICollection<OrganizacionUsuario> OrganizacionUsuario { get; set; } = new List<OrganizacionUsuario>();

    public virtual UsuarioIdentity RegistradaPorNavigation { get; set; } = null!;
}
