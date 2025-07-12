using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;

namespace Sparc.Aura;

public class SparcAuraAuthenticator(HttpContext context, IOpenIddictScopeManager scopes)
{
    public async Task<IResult> Authorize()
    {
        var request = context.GetOpenIddictServerRequest();
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        return Results.SignIn(principal, null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
