using AyudaYa.Data;
using AyudaYa.Negocio.Servicios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AyudaYa.Tests;

/// <summary>
/// Las acciones sobre una asignación deben serializarse: si otra transacción
/// tiene la fila bloqueada, la acción espera y después lee el estado nuevo.
/// </summary>
public class ConcurrenciaAsignacionTests
{
    [FactBaseDeDatos]
    public async Task Iniciar_espera_el_bloqueo_de_la_asignacion_antes_de_leer_el_estado()
    {
        await using var e = new Escenario();
        await e.PrepararAsync();

        var solicitante = await e.CrearUsuarioAsync("Marina");
        var voluntario = await e.CrearVoluntarioAsync("Lucas", e.ZonaId);
        var solicitud = await e.CrearSolicitudAsync(solicitante, null, ("agua", 2));
        var recurso = solicitud.Recursos.Single();
        Assert.True((await e.Servicio<SolicitudAppService>()
            .AsignarAsync(voluntario, recurso.Id, voluntario, null)).Exito);

        var asignacionId = (await e.Servicio<AsignacionAppService>()
            .ListarDeVoluntarioAsync(voluntario)).Single().Id;

        // Una conexión independiente mantiene la fila bloqueada.
        await using var holderProvider = EntornoBaseDeDatos.CrearProveedor();
        await using var holderScope = holderProvider.CreateAsyncScope();
        var holderDb = holderScope.ServiceProvider.GetRequiredService<AyudaYaDbContext>();
        await holderDb.Database.OpenConnectionAsync();
        await using var holderTx = await holderDb.Database.BeginTransactionAsync();
        await holderDb.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM asignacion WHERE id = {asignacionId} FOR UPDATE");

        // La acción debe quedar esperando el lock, no completarse con el estado viejo.
        await using var serviceProvider = EntornoBaseDeDatos.CrearProveedor();
        await using var serviceScope = serviceProvider.CreateAsyncScope();
        var asignaciones = serviceScope.ServiceProvider.GetRequiredService<AsignacionAppService>();
        var tarea = asignaciones.IniciarAsync(voluntario, asignacionId);

        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.False(tarea.IsCompleted);

        await holderTx.RollbackAsync();
        var resultado = await tarea;
        Assert.True(resultado.Exito, resultado.Mensajes());
    }
}
