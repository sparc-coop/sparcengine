using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Passwordless;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;
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
    T? SparcUser;
    readonly IRepository<T> Users = users;

    public override async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal)
    {
        SparcUser = await GetUserAsync(principal);
        SparcUser.Login();
        UpdateFromHttpContext(principal);
        await Users.UpdateAsync(SparcUser!);

        var priorUser = BlossomUser.FromPrincipal(principal);
        var newPrincipal = SparcUser.ToPrincipal();
        if (priorUser != User && http.HttpContext != null)
        {
            http.HttpContext.User = newPrincipal;
            await http.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, newPrincipal, new() { IsPersistent = true });
        }

        return newPrincipal;

    }

    public async Task<BlossomUser> DoLogin(ClaimsPrincipal principal, string? emailOrToken = null)
    {
        Message = null;

        // 1. Convert the ClaimsPrincipal from the cookie into a BlossomUser
        // If the BlossomUser is already attached to Passwordless, they're logged in because their cookie is valid
        SparcUser = await GetUserAsync(principal);

        if (emailOrToken == null)
            return SparcUser;

        // Verify Authentication Token or Register
        if (emailOrToken.StartsWith("verify"))
            return await VerifyTokenAsync(emailOrToken);

        var authenticationType = TwilioService.IsValidEmail(emailOrToken) ? "Email" : "Phone";
        var identity = SparcUser.GetOrCreateIdentity(authenticationType, emailOrToken);
        if (!identity.IsVerified)
            await SendVerificationCodeAsync(identity);

        return SparcUser;
    }

    public async Task<BlossomUser> Register(ClaimsPrincipal principal)
    {
        SparcUser = await GetUserAsync(principal);
        if (SparcUser.HasIdentity("Passwordless"))
            return SparcUser;

        var passwordlessToken = await SignUpWithPasswordlessAsync(SparcUser);
        SparcUser.GetOrCreateIdentity("Passwordless", passwordlessToken);
        return SparcUser;
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
            .FirstOrDefaultAsync();

        if (user == null)
            SparcUser!.AddIdentity("Passwordless", verifiedUser.UserId);
        else
            SparcUser = user;

        await SaveAsync();
        return SparcUser;
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
        var options = new RegisterOptions(user.Id, user.Avatar.Username)
        {
            Aliases = [user.Avatar.Username]
        };

        var registerToken = await _passwordlessClient.CreateRegisterTokenAsync(options);
        return registerToken.Token;
    }

    private async Task SaveAsync()
    {
        await LoginAsync(SparcUser!.ToPrincipal());
        User = SparcUser;
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

    private void UpdateFromHttpContext(ClaimsPrincipal principal)
    {
        if (http?.HttpContext != null && User != null)
        {
            User.LastPageVisited = http.HttpContext.Request.Path;

            if (string.IsNullOrWhiteSpace(User.Avatar.Username))
            {
                User.ChangeUsername(friendlyId.Create(1, 2));
            }

            var acceptLanguage = http.HttpContext.Request.Headers.AcceptLanguage;
            if (User.Avatar.Language == null && !string.IsNullOrWhiteSpace(acceptLanguage))
            {
                var newLanguage = TovikTranslator.GetLanguage(acceptLanguage!);
                if (newLanguage != null)
                    User.ChangeLanguage(newLanguage);

                var newLocale = TovikTranslator.GetLocale(acceptLanguage!);
                if (newLocale != null)
                    User.Avatar.Locale = newLocale;
            }
        }
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/aura").RequireCors("Auth");
        //auth.MapGet("login", DoLogin);
        //auth.MapGet("logout", DoLogout);
        auth.MapPost("register", Register);
        auth.MapPost("login", DoLogin);
        auth.MapPost("logout", DoLogout);
        auth.MapGet("userinfo", GetAsync);
    }
}