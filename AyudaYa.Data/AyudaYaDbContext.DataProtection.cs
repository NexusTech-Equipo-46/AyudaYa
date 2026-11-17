using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AyudaYa.Data;

/// <summary>
/// Claves de protección de datos (cookies de sesión, tokens de correo) guardadas
/// en la base. Sirve cuando la app corre en un contenedor sin disco persistente:
/// sin esto, cada reinicio invalida todas las sesiones. Se activa con la
/// configuración DataProtection:PersistirEnBase = true (ver database/cambios).
/// </summary>
public partial class AyudaYaDbContext : IDataProtectionKeyContext
{
    public virtual DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
}
