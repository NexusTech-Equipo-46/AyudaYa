using System.Globalization;

namespace AyudaYa.Web.Infraestructura;

/// <summary>
/// Formato de fechas, plurales y textos para las vistas. Las fechas se
/// guardan en UTC y se muestran en hora argentina.
/// </summary>
public static class Formato
{
    private static readonly CultureInfo Cultura = new("es-AR");

    private static readonly TimeZoneInfo ZonaArgentina = BuscarZona();

    private static TimeZoneInfo BuscarZona()
    {
        foreach (var id in new[] { "America/Argentina/Buenos_Aires", "Argentina Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("ART", TimeSpan.FromHours(-3), "ART", "ART");
    }

    public static DateTime Local(DateTime utc)
    {
        var fecha = utc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utc, DateTimeKind.Utc)
            : utc.ToUniversalTime();
        return TimeZoneInfo.ConvertTimeFromUtc(fecha, ZonaArgentina);
    }

    /// <summary>"12 de noviembre de 2026, 21:05"</summary>
    public static string FechaHora(DateTime utc) =>
        Local(utc).ToString("d 'de' MMMM 'de' yyyy, HH:mm", Cultura);

    public static string Fecha(DateTime utc) =>
        Local(utc).ToString("dd/MM/yyyy", Cultura);

    public static string Mes(DateOnly mes) =>
        mes.ToString("MMMM 'de' yyyy", Cultura);

    /// <summary>"hace 3 horas", "hace 2 días", "hace instantes".</summary>
    public static string HaceCuanto(DateTime utc)
    {
        var fecha = utc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utc, DateTimeKind.Utc)
            : utc.ToUniversalTime();
        var diferencia = DateTime.UtcNow - fecha;

        if (diferencia.TotalMinutes < 1)
        {
            return "hace instantes";
        }

        if (diferencia.TotalHours < 1)
        {
            var minutos = (int)diferencia.TotalMinutes;
            return $"hace {minutos} {(minutos == 1 ? "minuto" : "minutos")}";
        }

        if (diferencia.TotalDays < 1)
        {
            var horas = (int)diferencia.TotalHours;
            return $"hace {horas} {(horas == 1 ? "hora" : "horas")}";
        }

        if (diferencia.TotalDays < 30)
        {
            var dias = (int)diferencia.TotalDays;
            return $"hace {dias} {(dias == 1 ? "día" : "días")}";
        }

        return $"el {Fecha(utc)}";
    }

    public static string Plural(long cantidad, string singular, string plural) =>
        $"{cantidad.ToString(Cultura)} {(cantidad == 1 ? singular : plural)}";

    public static string Numero(long valor) => valor.ToString("N0", Cultura);

    public static string Decimal(double valor) =>
        valor.ToString("0.#####", CultureInfo.InvariantCulture);

    public static string NombreDia(int dia) => dia switch
    {
        1 => "Lunes",
        2 => "Martes",
        3 => "Miércoles",
        4 => "Jueves",
        5 => "Viernes",
        6 => "Sábado",
        7 => "Domingo",
        _ => "Día no informado"
    };

    /// <summary>
    /// Resume las filas de horario: "lun a vie 09:00–20:00 · sáb 09:00–13:00".
    /// Agrupa días consecutivos con el mismo turno.
    /// </summary>
    public static string ResumenHorario(IEnumerable<Negocio.Servicios.HorarioDto> horarios)
    {
        var lista = horarios.ToList();
        if (lista.Count == 0)
        {
            return "Horario no informado";
        }

        if (lista.Count(h => h.TodoElDia) == 7)
        {
            return "Todos los días, las 24 horas";
        }

        string[] abreviaturas = ["lun", "mar", "mié", "jue", "vie", "sáb", "dom"];
        static string Clave(Negocio.Servicios.HorarioDto h) =>
            h.TodoElDia ? "24 horas" : $"{h.Abre}–{h.Cierra}";

        var grupos = new List<(string Clave, int Inicio, int Fin)>();
        foreach (var h in lista.OrderBy(x => x.DiaSemana).ThenBy(x => x.Abre))
        {
            var indice = grupos.FindIndex(g => g.Clave == Clave(h) && g.Fin == h.DiaSemana - 1);
            if (indice >= 0)
            {
                grupos[indice] = (grupos[indice].Clave, grupos[indice].Inicio, h.DiaSemana);
            }
            else
            {
                grupos.Add((Clave(h), h.DiaSemana, h.DiaSemana));
            }
        }

        return string.Join(" · ", grupos.Select(g =>
        {
            var dias = g.Inicio == g.Fin
                ? abreviaturas[g.Inicio - 1]
                : $"{abreviaturas[g.Inicio - 1]}{(g.Fin == g.Inicio + 1 ? " y " : " a ")}{abreviaturas[g.Fin - 1]}";
            return $"{dias} {g.Clave}";
        }));
    }
}

/// <summary>Textos en español de los estados y valores del dominio.</summary>
public static class Textos
{
    public static string EstadoSolicitud(string estado) => estado switch
    {
        "pendiente" => "Pendiente",
        "asignada" => "Asignada",
        "en_atencion" => "En atención",
        "parcial" => "Atención parcial",
        "resuelta" => "Resuelta",
        "cancelada" => "Cancelada",
        _ => estado
    };

    public static string DescripcionEstadoSolicitud(string estado) => estado switch
    {
        "pendiente" => "Todavía no la tomó nadie.",
        "asignada" => "Alguien la tomó y se va a comunicar con vos.",
        "en_atencion" => "Te están asistiendo.",
        "parcial" => "Una parte ya se resolvió y otra sigue en espera.",
        "resuelta" => "Se resolvió lo que pediste.",
        "cancelada" => "La solicitud se canceló.",
        _ => string.Empty
    };

    public static string EstadoAsignacion(string estado) => estado switch
    {
        "asignada" => "Asignada",
        "en_atencion" => "En atención",
        "completada" => "Completada",
        "confirmada" => "Confirmada",
        "cancelada" => "Cancelada",
        "liberada" => "Liberada",
        _ => estado
    };

    public static string Verificacion(string estado) => estado switch
    {
        "aprobado" => "Aprobado",
        "activa" => "Activa",
        "pendiente" => "Pendiente de aprobación",
        "rechazado" => "Rechazado",
        "rechazada" => "Rechazada",
        "suspendido" => "Suspendido",
        "suspendida" => "Suspendida",
        _ => estado
    };

    public static string EstadoOperativo(string? estado) => estado switch
    {
        "operativo" => "Abierto",
        "saturado" => "Saturado",
        "cerrado" => "Cerrado temporalmente",
        _ => "Sin informar"
    };

    public static string Nivel(string? nivel) => nivel switch
    {
        "alta" => "Alta",
        "media" => "Media",
        "baja" => "Baja",
        "sin_stock" => "Sin stock",
        "no_ofrece" => "No ofrece",
        _ => "Sin informar"
    };

    public static string DescripcionNivel(string nivel) => nivel switch
    {
        "alta" => "hay suficiente",
        "media" => "queda una cantidad moderada",
        "baja" => "queda poco, conviene llamar antes",
        "sin_stock" => "lo ofrece, pero hoy no tiene",
        "no_ofrece" => "este centro no trabaja con ese recurso",
        _ => string.Empty
    };

    public static string TipoOrganizacion(string? tipo) => tipo switch
    {
        "comunitaria" => "Organización comunitaria",
        "institucional" => "Organización institucional",
        _ => "Organización"
    };

    public static string Rol(string rol) => rol == "responsable" ? "Responsable" : "Miembro";
}
