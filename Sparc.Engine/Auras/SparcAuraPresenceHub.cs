using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Sparc.Aura.Users;
using Sparc.Blossom.Authentication;
using System.Security.Claims;

namespace Sparc.Aura;

internal class SparcAuraPresenceHub(IHttpContextAccessor http, UserManager<SparcUser> users, IRepository<SparcAura> auras)
{
    public async Task<SparcAura> Hi()
    {
        var result = await http.HttpContext!.AuthenticateAsync();

        SparcUser? user = null;
        if (result.Succeeded)
            user = await users.GetUserAsync(result.Principal!);

        if (user == null)
        {
            user = new SparcUser();
            var created = await users.CreateAsync(user);
            if (!created.Succeeded)
                throw new Exception("Failed to create user: " + string.Join(", ", created.Errors.Select(e => e.Description)));
        }

        var principal = user.ToPrincipal();
        await http.HttpContext!.SignInAsync(principal);
        return await Me(principal);
    }

    public async Task<SparcAura> Me(ClaimsPrincipal principal)
    {
        var aura = await auras.FindAsync(principal.Id());
        if (aura == null)
        {
            var user = await users.GetUserAsync(principal);
            aura = new SparcAura(user!.Id, user.UserName!);
            await auras.AddAsync(aura);
        }

        return aura;
    }

    public async Task<SparcAura> Update(ClaimsPrincipal principal, SparcAura aura)
    {
        var existing = await auras.FindAsync(principal.Id());
        var usernameChanged = existing != null && existing.Username != aura.Username;

        if (existing == null)
        {
            aura.Id = principal.Id();
            await auras.AddAsync(aura);
        }
        else
        {
            existing.UpdateAvatar(aura);
            await auras.UpdateAsync(aura);
        }

        return aura;
    }

    internal async Task Bye()
    {
        await http.HttpContext!.SignOutAsync();
    }
}
