using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Sparc.Aura.Users;
using Sparc.Blossom.Authentication;

namespace Sparc.Aura;

internal class SparcAuraAuthenticator(IHttpContextAccessor http, 
    SparcAuraContext users,
    IOpenIddictApplicationManager apps)
{
    public async Task<IResult> Authorize()
    {
        var request = http.HttpContext!.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var isNoLogin = request.Prompt == null || request.HasPromptValue(OpenIddictConstants.PromptValues.None);

        var result = await http.HttpContext!.AuthenticateAsync();

        SparcUser? user;
        if (!result.Succeeded && isNoLogin)
        {
            user = new SparcUser();
            await users.AddAsync(user);
        }
        else if (isNoLogin)
        {
            user = await users.FindAsync<SparcUser>(result.Principal.Id());
            if (user == null)
                return Results.Forbid(authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }
        else
        {
            return Results.Redirect("/Login?returnUrl=" + request.RedirectUri);
        }

        var app = await apps.FindByClientIdAsync(request.ClientId!);

        var principal = user.ToPrincipal();
        return Results.SignIn(principal, null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
