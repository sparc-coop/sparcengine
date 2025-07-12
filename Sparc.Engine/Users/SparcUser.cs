using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Sparc.Aura.Users;

public class SparcUser : IdentityUser
{
    public SparcUser()
    {
        Id = Guid.NewGuid().ToString();
        UserName = FriendlyId.CreateFakeWord(6, 2);
    }

    public ClaimsPrincipal ToPrincipal()
    {
        var identity = new ClaimsIdentity("Sparc", ClaimTypes.Name, ClaimTypes.Role);
        identity.AddClaim(new Claim("sub", Id));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Id));
        identity.AddClaim(new Claim(ClaimTypes.Name, UserName ?? string.Empty));
        return new ClaimsPrincipal(identity);
    }
}
