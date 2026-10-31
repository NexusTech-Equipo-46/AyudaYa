using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

public partial class VMetricasPersonasPorRecurso
{
    public Guid? CiudadId { get; set; }

    public string? CiudadNombre { get; set; }

    public DateOnly? Mes { get; set; }

    public string? TipoRecursoCodigo { get; set; }

    public string? TipoRecursoNombre { get; set; }

    public long? RecursosResueltos { get; set; }

    public long? PersonasAlcanzadas { get; set; }
}
