namespace AyudaYa.Negocio.Servicios;

/// <summary>
/// Resultado de una consulta o de una operación que devuelve un dato. Si
/// <see cref="Exito"/> es false, <see cref="Errores"/> trae códigos estables
/// que la capa Web traduce a mensajes o a un 404/403.
/// </summary>
public sealed record Resultado<T>(T? Dato, IReadOnlyList<ErrorNegocio> Errores)
{
    public bool Exito => Errores.Count == 0;

    public static Resultado<T> Ok(T dato) =>
        new(dato, Array.Empty<ErrorNegocio>());

    public static Resultado<T> Falla(string codigo, string mensaje) =>
        new(default, [new ErrorNegocio(codigo, mensaje)]);

    public static Resultado<T> Falla(IReadOnlyList<ErrorNegocio> errores) =>
        new(default, errores);

    public bool EsNoAutorizado => Errores.Any(e => e.Codigo == Codigos.NoAutorizado);

    public bool EsNoEncontrado => Errores.Any(e => e.Codigo.EndsWith("NoEncontrada", StringComparison.Ordinal)
        || e.Codigo.EndsWith("NoEncontrado", StringComparison.Ordinal));
}

/// <summary>Códigos de error compartidos entre servicios.</summary>
public static class Codigos
{
    public const string NoAutorizado = "NoAutorizado";
    public const string UsuarioInvalido = "UsuarioInvalido";
    public const string ConflictoDeEstado = "ConflictoDeEstado";
}

public static class ResultadoOperacionExtensiones
{
    /// <summary>Une todos los mensajes de error en una sola línea para mostrar al usuario.</summary>
    public static string Mensajes(this ResultadoOperacion resultado) =>
        string.Join(" ", resultado.Errores.Select(e => e.Mensaje));

    public static string Mensajes<T>(this Resultado<T> resultado) =>
        string.Join(" ", resultado.Errores.Select(e => e.Mensaje));
}
