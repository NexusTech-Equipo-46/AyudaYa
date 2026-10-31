using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

public partial class Voluntario
{
    public Guid UsuarioId { get; set; }

    public Guid CiudadId { get; set; }

    public Guid? ZonaId { get; set; }

    public string? TelefonoContacto { get; set; }

    public bool Activo { get; set; }

    public DateTime CreadoAt { get; set; }

    public DateTime ActualizadoAt { get; set; }

    public virtual ICollection<Asignacion> Asignacion { get; set; } = new List<Asignacion>();

    public virtual Ciudad Ciudad { get; set; } = null!;

    public virtual UsuarioIdentity Usuario { get; set; } = null!;

    public virtual Zona? Zona { get; set; }
}
