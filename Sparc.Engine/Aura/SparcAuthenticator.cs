using Microsoft.AspNetCore.Authentication;
using Passwordless;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;
using Sparc.Engine.Aura;
using System.Security.Claims;

namespace Sparc.Engine;

public class SparcAuthenticator<T>(
    IPasswordlessClient _passwordlessClient,
    IRepository<T> users,
    TwilioService twilio,
    FriendlyId friendlyId,
    IHttpContextAccessor http,
    SparcTokens tokens) : BlossomDefaultAuthenticator<T>(users), IBlossomEndpoints
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

        if (!priorUser.Equals(SparcUser) && http.HttpContext != null)
        {
            http.HttpContext.User = newPrincipal;
        }

        return newPrincipal;

    }

    public async Task<BlossomLogin> DoLogin(ClaimsPrincipal principal, string? emailOrToken = null)
    {
        Message = null;

        // 1. Convert the ClaimsPrincipal from the cookie into a BlossomUser
        SparcUser = await GetUserAsync(principal);

        if (emailOrToken == null)
            return SparcUser.ToLogin();

        // Verify Authentication Token or Register
        if (emailOrToken.StartsWith("verify"))
            return await VerifyTokenAsync(emailOrToken);

        if (emailOrToken.StartsWith("totp"))
            return await VerifyTotpAsync(emailOrToken);

        var authenticationType = TwilioService.IsValidEmail(emailOrToken) ? "Email" : "Phone";
        var identity = SparcUser.GetOrCreateIdentity(authenticationType, emailOrToken);
        if (!identity.IsVerified)
            await SendVerificationCodeAsync(identity);

        return SparcUser.ToLogin();
    }

    private async Task<BlossomLogin> VerifyTotpAsync(string emailOrToken)
    {
        var matchingUserId = SparcCodes.Verify(emailOrToken)
                        ?? throw new InvalidOperationException("Invalid TOTP code.");

        var matchingUser = await Users.Query
            .Where(x => x.Id == matchingUserId)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("User not found for the provided TOTP code.");

        await LoginAsync(matchingUser.ToPrincipal());
        return matchingUser.ToLogin();
    }

    public async Task<SparcCode> Register(ClaimsPrincipal principal)
    {
        SparcUser = await GetUserAsync(principal);
        var passwordlessToken = await StartPasskeyRegistrationAsync(SparcUser);
        return new SparcCode(passwordlessToken);
    }

    private async Task<BlossomLogin> VerifyTokenAsync(string token)
    {
        var verifiedUser = await _passwordlessClient.VerifyAuthenticationTokenAsync(token);

        if (verifiedUser?.Success != true)
        {
            Message = "Invalid or expired token.";
            LoginState = LoginStates.Error;
            throw new InvalidOperationException(Message);
        }

        SparcUser = await Users.FindAsync(verifiedUser.UserId);

        if (SparcUser == null)
        {
            Message = "User not found for the provided token.";
            LoginState = LoginStates.Error;
            throw new InvalidOperationException(Message);
        }

        SparcUser.GetOrCreateIdentity("Passwordless", verifiedUser.UserId);

        await SaveAsync();
        return SparcUser.ToLogin();
    }
    
    public async Task<BlossomUser> DoLogout(ClaimsPrincipal principal, string? emailOrToken = null)
    {
        var user = await GetAsync(principal);

        user.Logout();
        await SaveAsync();

        if (http.HttpContext != null)
            await http.HttpContext.SignOutAsync();

        return user;
    }

    private async Task<string> StartPasskeyRegistrationAsync(BlossomUser user)
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

    public override async Task<BlossomUser> UpdateAsync(ClaimsPrincipal principal, BlossomAvatar avatar)
    {
        await base.UpdateAsync(principal, avatar);
        await LoginAsync(principal);
        return User!;
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

    internal async Task<SparcCode?> GetSparcCode(ClaimsPrincipal principal)
    {
        var user = await GetAsync(principal);
        return SparcCodes.Generate(user);
    }

    protected override async Task<T> GetUserAsync(ClaimsPrincipal principal)
    {
        var user = await base.GetUserAsync(principal);
        tokens.Create(user);
        return user;
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
        auth.MapPost("register", async (SparcAuthenticator<T> auth, ClaimsPrincipal principal) => await auth.Register(principal));
        auth.MapPost("login", async (SparcAuthenticator<T> auth, ClaimsPrincipal principal, string? emailOrToken = null) => await auth.DoLogin(principal, emailOrToken));
        auth.MapPost("logout", async (SparcAuthenticator<T> auth, ClaimsPrincipal principal, string? emailOrToken = null) => await auth.DoLogout(principal, emailOrToken));
        auth.MapGet("userinfo", async (SparcAuthenticator<T> auth, ClaimsPrincipal principal) => await GetAsync(principal));
        auth.MapGet("code", async (SparcAuthenticator<T> auth, ClaimsPrincipal principal) => await GetSparcCode(principal));
    }
}