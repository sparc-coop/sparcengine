using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Sparc.Aura.Users;

namespace Sparc.Aura;

internal class SparcAuraAuthenticator(IHttpContextAccessor http, 
    UserManager<SparcUser> users,
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
            var created = await users.CreateAsync(user);
            if (!created.Succeeded)
            {
                return Results.BadRequest(created.Errors.Select(e => e.Description));
            }
        }
        else if (isNoLogin)
        {
            user = await users.GetUserAsync(result.Principal!);
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
