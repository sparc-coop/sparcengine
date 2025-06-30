using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Passwordless;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Notifications.Twilio;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Sparc.Engine;

public class SparcAuthenticator<T>(
    IPasswordlessClient _passwordlessClient,
    IRepository<T> users,
    TwilioService twilio,
    FriendlyId friendlyId,
    IHttpContextAccessor http) : BlossomDefaultAuthenticator<T>(users), IBlossomEndpoints
    where T : BlossomUser, new()
{
    public override async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal)
    {
        var user = await GetAsync(principal);
        var newPrincipal = user.ToPrincipal();
        await Users.UpdateAsync((T)user);

        var priorUser = BlossomUser.FromPrincipal(principal);
        if (http?.HttpContext != null && priorUser != user)
        {
            http.HttpContext.User = newPrincipal;
            await http.HttpContext.SignOutAsync();
            await http.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, newPrincipal, new() { IsPersistent = true });
        }

        return principal;
    }

    public async Task<BlossomUser> DoLogin(ClaimsPrincipal principal, string? emailOrToken = null)
    {
        Message = null;

        // 1. Convert the ClaimsPrincipal from the cookie into a BlossomUser
        // If the BlossomUser is already attached to Passwordless, they're logged in because their cookie is valid
        User = await GetAsync(principal);

        if (User.HasIdentity("Passwordless") && emailOrToken == null)
            return User;

        // Verify Authentication Token or Register
        if (emailOrToken != null)
        {
            if (emailOrToken.StartsWith("verify"))
                return await VerifyTokenAsync(emailOrToken);
            
            if (new EmailAddressAttribute().IsValid(emailOrToken))
            {
                var identity = User.GetOrCreateIdentity("Email", emailOrToken);
                if (!identity.IsVerified)
                {
                    await SendVerificationCodeAsync(identity);
                    return User;
                }
            }

            var phoneNumber = new PhoneAttribute().

        }

        var passwordlessToken = await SignUpWithPasswordlessAsync(User);
        User.GetOrCreateIdentity("Passwordless", passwordlessToken);
        return User;
    }

    private async Task<BlossomUser> VerifyTokenAsync(string token)
    {
        var verifiedUser = await _passwordlessClient.VerifyAuthenticationTokenAsync(token);

        if (verifiedUser?.Success != true)
        {
            Message = "Invalid or expired token.";
            LoginState = LoginStates.Error;
            throw new InvalidOperationException(Message);
        }

        var user = await Users.Query
            .Where(x => x.Identities.Any(y => y.Type == "Passwordless" && y.Id == verifiedUser.UserId))
            .CosmosFirstOrDefaultAsync();

        if (user == null)
            User!.AddIdentity("Passwordless", verifiedUser.UserId);
        else
            User = user;

        await SaveAsync();
        return User;
    }

    public async Task<BlossomUser> DoLogout(ClaimsPrincipal principal, string? emailOrToken = null)
    {
        var user = await GetAsync(principal);

        user.Logout();
        await SaveAsync();

        return user;
    }

    private async Task<string> SignUpWithPasswordlessAsync(BlossomUser user)
    {
        var registerToken = await _passwordlessClient.CreateRegisterTokenAsync(new RegisterOptions(user.Id, user.Username)
        {
            Aliases = [user.Username]
        });

        return registerToken.Token;
    }

    protected override async Task<BlossomUser> GetUserAsync(ClaimsPrincipal principal)
    {
        await base.GetUserAsync(principal);

        if (User!.Username == null)
        {
            User.ChangeUsername(friendlyId.Create(1, 2));
            await SaveAsync();
        }

        return User;
    }

    private async Task SaveAsync()
    {
        await Users.UpdateAsync((T)User!);
        await LoginAsync(User!.ToPrincipal());
    }

    public async Task SendVerificationCodeAsync(BlossomIdentity identity)
    {
        identity.Revoke();

        var code = identity.GenerateVerificationCode();
        var message = $"Your Sparc verification code is: {code}";
        var subject = "Sparc Verification Code";

        await twilio.SendAsync(identity.Id, message, subject);
        await SaveAsync();
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/auth").RequireCors("Auth");
        auth.MapPost("login", DoLogin);
        auth.MapPost("logout", DoLogout);
        auth.MapGet("userinfo", GetAsync);

    }
}