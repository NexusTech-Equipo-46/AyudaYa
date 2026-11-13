namespace AyudaYa.Negocio;

/// <summary>
/// Reglas puras de negocio, sin acceso a la base. Se centralizan acá para que
/// los servicios y las pruebas usen exactamente la misma lógica.
/// </summary>
public static class ReglasNegocio
{
    public const string EstadoPendiente = "pendiente";
    public const string EstadoAsignada = "asignada";
    public const string EstadoEnAtencion = "en_atencion";
    public const string EstadoParcial = "parcial";
    public const string EstadoResuelta = "resuelta";
    public const string EstadoCancelada = "cancelada";

    /// <summary>
    /// Estados que la base considera una asignación viva: ocupan el recurso
    /// (índice único ux_asignacion_activa) hasta que se cancelan o liberan.
    /// </summary>
    public static readonly string[] EstadosAsignacionViva =
        ["asignada", "en_atencion", "completada"];

    public static readonly string[] NivelesRecurso =
        ["alta", "media", "baja", "sin_stock"];

    public static readonly string[] EstadosOperativos =
        ["operativo", "saturado", "cerrado"];

    /// <summary>
    /// Deriva el estado de la solicitud a partir del estado de sus recursos
    /// no cancelados:
    /// - todos resueltos: resuelta;
    /// - alguno resuelto y otros no: parcial;
    /// - alguno en atención: en_atencion;
    /// - alguno asignado: asignada;
    /// - si no: pendiente.
    /// Sin recursos vigentes la solicitud queda cancelada.
    /// </summary>
    public static string CalcularEstadoSolicitud(IEnumerable<string> estadosRecurso)
    {
        var vigentes = estadosRecurso.Where(e => e != EstadoCancelada).ToList();
        if (vigentes.Count == 0)
        {
            return EstadoCancelada;
        }

        if (vigentes.All(e => e == EstadoResuelta))
        {
            return EstadoResuelta;
        }

        if (vigentes.Any(e => e == EstadoResuelta))
        {
            return EstadoParcial;
        }

        if (vigentes.Any(e => e == EstadoEnAtencion))
        {
            return EstadoEnAtencion;
        }

        if (vigentes.Any(e => e == EstadoAsignada))
        {
            return EstadoAsignada;
        }

        return EstadoPendiente;
    }

    /// <summary>
    /// Quién atiende puede ver dirección y teléfono mientras la asignación
    /// está en curso o completada pero todavía sin confirmar. Cuando la
    /// persona que pidió ayuda confirma, o si la asignación se cancela o
    /// libera, el acceso se corta.
    /// </summary>
    public static bool AsignacionDaAccesoAlContacto(string estado, DateTime? confirmadaAt) =>
        estado is "asignada" or "en_atencion"
        || (estado == "completada" && confirmadaAt is null);

    /// <summary>Estado que ve la persona: una completada confirmada se muestra "confirmada".</summary>
    public static string EstadoVisibleAsignacion(string estado, DateTime? confirmadaAt) =>
        estado == "completada" && confirmadaAt is not null ? "confirmada" : estado;

    /// <summary>
    /// Valida un teléfono: entre 8 y 13 dígitos y sin caracteres raros.
    /// Devuelve el texto limpio (sin espacios sobrantes) cuando es válido.
    /// </summary>
    public static bool TelefonoValido(string? telefono, out string limpio)
    {
        limpio = (telefono ?? string.Empty).Trim();
        if (limpio.Length is 0 or > 20)
        {
            return false;
        }

        if (limpio.Any(c => !(char.IsAsciiDigit(c) || c is ' ' or '-' or '+' or '(' or ')')))
        {
            return false;
        }

        var digitos = limpio.Count(char.IsAsciiDigit);
        return digitos is >= 8 and <= 13;
    }

    public static bool CoordenadasValidas(double? latitud, double? longitud) =>
        latitud is not null
        && longitud is not null
        && double.IsFinite(latitud.Value)
        && double.IsFinite(longitud.Value)
        && latitud is >= -90 and <= 90
        && longitud is >= -180 and <= 180;

    /// <summary>Código corto y estable para mostrar en pantalla (primeros 8 caracteres del id).</summary>
    public static string CodigoCorto(Guid id) =>
        id.ToString("N")[..8].ToUpperInvariant();

    /// <summary>"Camila Rodríguez" se muestra como "Camila R.".</summary>
    public static string NombreCorto(string? nombreCompleto)
    {
        var partes = (nombreCompleto ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return partes.Length switch
        {
            0 => "Sin nombre",
            1 => partes[0],
            _ => $"{partes[0]} {char.ToUpperInvariant(partes[1][0])}."
        };
    }

    /// <summary>
    /// Texto sin espacios sobrantes; vacío se convierte en null para que la
    /// base no guarde cadenas vacías donde el esquema espera null.
    /// </summary>
    public static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
