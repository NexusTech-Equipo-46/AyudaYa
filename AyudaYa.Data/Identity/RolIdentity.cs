using Microsoft.AspNetCore.Identity;

namespace AyudaYa.Data;

/// <summary>
/// Rol de ASP.NET Core Identity. En el MVP solo se usan "Administrador" y
/// "Usuario". La tabla es "AspNetRoles" y la clave es uuid.
/// </summary>
public class RolIdentity : IdentityRole<Guid>
{
    public RolIdentity()
    {
    }

    public RolIdentity(string roleName)
        : base(roleName)
    {
    }
}
