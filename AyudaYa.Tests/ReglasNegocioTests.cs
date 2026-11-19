using AyudaYa.Negocio;

namespace AyudaYa.Tests;

/// <summary>Reglas puras: no necesitan base de datos.</summary>
public class ReglasNegocioTests
{
    [Theory]
    [InlineData(new[] { "pendiente" }, "pendiente")]
    [InlineData(new[] { "pendiente", "pendiente" }, "pendiente")]
    [InlineData(new[] { "asignada", "pendiente" }, "asignada")]
    [InlineData(new[] { "en_atencion", "pendiente" }, "en_atencion")]
    [InlineData(new[] { "en_atencion", "asignada" }, "en_atencion")]
    [InlineData(new[] { "resuelta" }, "resuelta")]
    [InlineData(new[] { "resuelta", "resuelta" }, "resuelta")]
    [InlineData(new[] { "resuelta", "pendiente" }, "parcial")]
    [InlineData(new[] { "resuelta", "en_atencion" }, "parcial")]
    [InlineData(new[] { "resuelta", "cancelada" }, "resuelta")]
    [InlineData(new[] { "cancelada", "pendiente" }, "pendiente")]
    [InlineData(new[] { "cancelada" }, "cancelada")]
    public void El_estado_de_la_solicitud_se_deriva_de_sus_recursos(string[] recursos, string esperado)
    {
        Assert.Equal(esperado, ReglasNegocio.CalcularEstadoSolicitud(recursos));
    }

    [Theory]
    [InlineData("asignada", false, true)]
    [InlineData("en_atencion", false, true)]
    [InlineData("completada", false, true)]
    [InlineData("completada", true, false)]
    [InlineData("cancelada", false, false)]
    [InlineData("liberada", false, false)]
    public void El_contacto_se_ve_solo_mientras_la_atencion_esta_viva_o_sin_confirmar(
        string estado, bool confirmada, bool esperado)
    {
        DateTime? confirmadaAt = confirmada ? DateTime.UtcNow : null;
        Assert.Equal(esperado, ReglasNegocio.AsignacionDaAccesoAlContacto(estado, confirmadaAt));
    }

    [Fact]
    public void Una_completada_confirmada_se_muestra_como_confirmada()
    {
        Assert.Equal("confirmada", ReglasNegocio.EstadoVisibleAsignacion("completada", DateTime.UtcNow));
        Assert.Equal("completada", ReglasNegocio.EstadoVisibleAsignacion("completada", null));
        Assert.Equal("liberada", ReglasNegocio.EstadoVisibleAsignacion("liberada", null));
    }

    [Theory]
    [InlineData("011 5555-0123", true)]
    [InlineData("+54 11 5555-0123", true)]
    [InlineData("(011) 4000 1001", true)]
    [InlineData("12345", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("011 5555-0123 <script>", false)]
    [InlineData("123456789012345678901", false)]
    public void El_telefono_se_valida(string telefono, bool valido)
    {
        Assert.Equal(valido, ReglasNegocio.TelefonoValido(telefono, out _));
    }

    [Theory]
    [InlineData(-34.45, -58.63, true)]
    [InlineData(90.0, 180.0, true)]
    [InlineData(-90.0, -180.0, true)]
    [InlineData(91.0, 0.0, false)]
    [InlineData(0.0, 181.0, false)]
    [InlineData(null, 10.0, false)]
    [InlineData(10.0, null, false)]
    [InlineData(double.NaN, 10.0, false)]
    [InlineData(double.PositiveInfinity, 10.0, false)]
    public void Las_coordenadas_se_validan(double? latitud, double? longitud, bool valido)
    {
        Assert.Equal(valido, ReglasNegocio.CoordenadasValidas(latitud, longitud));
    }

    [Theory]
    [InlineData("Camila Rodríguez", "Camila R.")]
    [InlineData("camila   rodríguez gómez", "camila R.")]
    [InlineData("Lucas", "Lucas")]
    [InlineData("", "Sin nombre")]
    [InlineData(null, "Sin nombre")]
    public void El_nombre_corto_no_muestra_el_apellido_completo(string? nombre, string esperado)
    {
        Assert.Equal(esperado, ReglasNegocio.NombreCorto(nombre));
    }

    [Fact]
    public void El_codigo_corto_tiene_ocho_caracteres_en_mayuscula()
    {
        var id = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000000");
        Assert.Equal("A1B2C3D4", ReglasNegocio.CodigoCorto(id));
    }

    [Theory]
    [InlineData("  hola  ", "hola")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void Limpiar_convierte_vacio_en_null(string? entrada, string? esperado)
    {
        Assert.Equal(esperado, ReglasNegocio.Limpiar(entrada));
    }

    [Theory]
    [InlineData("Calle 1", "calle 1")]
    [InlineData("  Calle   1  ", "calle 1")]
    [InlineData("Av. Hipólito Yrigoyen 1450, General Pacheco", "av hipolito yrigoyen 1450 general pacheco")]
    [InlineData("José Peña 20", "jose pena 20")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void La_direccion_se_normaliza_para_detectar_un_mismo_lugar(string? entrada, string esperado)
    {
        Assert.Equal(esperado, ReglasNegocio.NormalizarDireccion(entrada));
    }

    [Fact]
    public void Las_abreviaturas_no_se_expanden()
    {
        Assert.NotEqual(
            ReglasNegocio.NormalizarDireccion("Av. San Martín 100"),
            ReglasNegocio.NormalizarDireccion("Avenida San Martin 100"));
    }
}
