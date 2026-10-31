using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

/// <summary>
/// Agregado público y aproximado por zona. No contiene dirección, teléfono,
/// descripción ni coordenadas exactas de las solicitudes.
/// </summary>
public partial class VMapaSolicitudesPorZona
{
    public Guid? CiudadId { get; set; }

    public string? CiudadNombre { get; set; }

    public Guid? ZonaId { get; set; }

    public string? ZonaNombre { get; set; }

    public double? LatitudAproximada { get; set; }

    public double? LongitudAproximada { get; set; }

    public long? SolicitudesAbiertas { get; set; }

    public long? PersonasAfectadas { get; set; }

    public string? Recursos { get; set; }
}
