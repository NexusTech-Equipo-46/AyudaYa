using AyudaYa.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AyudaYa.Negocio.Identity;

/// <summary>Vigencia propia del enlace de recuperación de contraseña (1 hora).</summary>
public sealed class TokenRestablecerClaveOptions : DataProtectionTokenProviderOptions
{
}

/// <summary>
/// Proveedor de tokens exclusivo de "recuperar contraseña". El proveedor por
/// defecto de Identity dura un día y lo comparte con la confirmación de correo;
/// separarlo permite que este enlace venza en una hora sin acortar el otro.
/// El token queda atado al SecurityStamp, así que se invalida solo al cambiar
/// la contraseña (un solo uso).
/// </summary>
public sealed class ProveedorTokenRestablecerClave : DataProtectorTokenProvider<UsuarioIdentity>
{
    public const string Nombre = "RestablecerClave";

    public ProveedorTokenRestablecerClave(
        IDataProtectionProvider proveedorProteccion,
        IOptions<TokenRestablecerClaveOptions> opciones,
        ILogger<DataProtectorTokenProvider<UsuarioIdentity>> logger)
        : base(proveedorProteccion, opciones, logger)
    {
    }
}
