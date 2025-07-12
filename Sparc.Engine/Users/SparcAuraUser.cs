using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Sparc.Aura.Users;

public class SparcAuraUser : IdentityUser
{
    public SparcAuraUser()
    {
        Id = Guid.NewGuid().ToString();
        UserName = FriendlyId.CreateFakeWord(6, 2);
    }

    public ClaimsPrincipal ToPrincipal()
    {
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, ClaimTypes.Name, ClaimTypes.Role);
        identity.AddClaim(new Claim(Claims.Subject, Id));
        identity.AddClaim(new Claim(ClaimsIdentity.DefaultRoleClaimType, "User"));
        return new ClaimsPrincipal(identity);
    }
}
