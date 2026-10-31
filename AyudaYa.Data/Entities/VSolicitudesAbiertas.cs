using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

public partial class VSolicitudesAbiertas
{
    public Guid? Id { get; set; }

    public Guid? CiudadId { get; set; }

    public string? CiudadNombre { get; set; }

    public Guid? ZonaId { get; set; }

    public string? ZonaNombre { get; set; }

    public string? Urgencia { get; set; }

    public string? Estado { get; set; }

    public int? PersonasAfectadas { get; set; }

    public string? Descripcion { get; set; }

    public DateTime? CreadoAt { get; set; }

    public DateTime? ActualizadoAt { get; set; }

    public string? Recursos { get; set; }
}
