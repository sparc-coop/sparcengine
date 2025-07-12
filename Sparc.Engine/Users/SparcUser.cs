using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Sparc.Aura.Users;

public class SparcUser : IdentityUser<string>
{
    public SparcUser()
    {
        Id = Guid.NewGuid().ToString();
    }

    public ClaimsPrincipal ToPrincipal()
    {
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, ClaimTypes.Name, ClaimTypes.Role);
        identity.AddClaim(new Claim(Claims.Subject, Id));
        identity.AddClaim(new Claim(ClaimsIdentity.DefaultRoleClaimType, "User"));
        return new ClaimsPrincipal(identity);
    }
}
