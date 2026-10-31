using AyudaYa.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AyudaYa.Negocio.Identity;

/// <summary>
/// SignInManager propio para aplicar la baja lógica de AyudaYa.
///
/// Identity no conoce la columna Activo. Si no se controla acá, un usuario
/// desactivado podría iniciar sesión con su contraseña.
/// </summary>
public sealed class AyudaYaSignInManager : SignInManager<UsuarioIdentity>
{
    public AyudaYaSignInManager(
        UserManager<UsuarioIdentity> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<UsuarioIdentity> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<UsuarioIdentity>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<UsuarioIdentity> confirmation)
        : base(
            userManager,
            contextAccessor,
            claimsFactory,
            optionsAccessor,
            logger,
            schemes,
            confirmation)
    {
    }

    public override async Task<bool> CanSignInAsync(UsuarioIdentity user)
    {
        if (!user.Activo)
        {
            return false;
        }

        return await base.CanSignInAsync(user);
    }
}
