using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Passwordless;
using Sparc.Blossom.Authentication;
using Sparc.Notifications.Twilio;
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

        if (User.ExternalId != null)
            return User;

        // Verify Authentication Token or Register
        if (emailOrToken != null && emailOrToken.StartsWith("verify"))
        {
            var passwordlessUser = await _passwordlessClient.VerifyAuthenticationTokenAsync(emailOrToken);

            if (passwordlessUser?.Success == true)
            {
                //var parentUser = Users.Query.Where(x => x.ExternalId == User.UserId && x.ParentUserId == null).FirstOrDefault();
                var parentUser = Users.Query.Where(x => x.ExternalId == passwordlessUser.UserId && x.ParentUserId == null).FirstOrDefault();
                if (parentUser == null)
                {
                    User.ExternalId = passwordlessUser.UserId;

                    await SaveAsync();
                    return User;
                }
                else
                {
                    User.SetParentUser(parentUser);

                    await SaveAsync();
                    return User;
                }
            }
        }

        var passwordlessToken = await SignUpWithPasswordlessAsync(User);
        User.SetToken(passwordlessToken);
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

    public override async IAsyncEnumerable<LoginStates> Logout(ClaimsPrincipal principal)
    {
        var user = await GetAsync(principal);

        user.Logout();
        await SaveAsync();

        yield return LoginStates.LoggedOut;
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
    }

    public async Task<BlossomUser> UpdateUserAsync(ClaimsPrincipal principal, UpdateUserRequest request)
    {
        await base.GetUserAsync(principal);
        if (User is null)
            throw new InvalidOperationException("User not initialized");

        var shouldSave = false;

        if (!string.IsNullOrWhiteSpace(request.Username) && User.Username != request.Username)
        {
            User.Username = request.Username;
            shouldSave = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && User.Email != request.Email)
        {
            if (request.RequireEmailVerification)
            {
                await SendVerificationCodeAsync(principal, request.Email);
            }
            else
            {
                User.Email = request.Email;
                shouldSave = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && User.PhoneNumber != request.PhoneNumber)
        {
            if (request.RequirePhoneVerification)
            {
                await SendVerificationCodeAsync(principal, request.PhoneNumber);
            }
            else
            {
                User.PhoneNumber = request.PhoneNumber;
                shouldSave = true;
            }
        }

        if (shouldSave)
            await SaveAsync();

        return User;
    }

    public async Task<BlossomUser> UpdateAvatarAsync(ClaimsPrincipal principal, UpdateAvatarRequest request)
    {
        await base.GetUserAsync(principal);
        if (User is null)
            throw new InvalidOperationException("User not initialized");

        var avatar = new UserAvatar(User.Avatar)
        {
            Id = User.Id,
            Name = request.Name ?? User.Avatar.Name,
            BackgroundColor = request.BackgroundColor ?? User.Avatar.BackgroundColor,
            Pronouns = request.Pronouns ?? User.Avatar.Pronouns,
            Description = request.Description ?? User.Avatar.Description,
            Emoji = request.Emoji ?? User.Avatar.Emoji,
            Gender = request.Gender ?? User.Avatar.Gender
        };

        User.UpdateAvatar(avatar);
        await SaveAsync();
        return User;
    }

    public async Task SendVerificationCodeAsync(ClaimsPrincipal principal, string destination)
    {
        await base.GetUserAsync(principal);

        if (User is null)
            throw new InvalidOperationException("User not initialized");

        User.Revoke();
        User.EmailOrPhone = destination;

        var code = User.GenerateVerificationCode();
        var message = $"Your Sparc verification code is: {code}";
        var subject = "Sparc Verification Code";

        await twilio.SendAsync(destination, message, subject);
        await SaveAsync();
    }

    public async Task<bool> VerifyCodeAsync(ClaimsPrincipal principal, string destination, string code)
    {
        await base.GetUserAsync(principal);

        if (User is null)
            throw new InvalidOperationException("User not initialized");

        var success = User.VerifyCode(code);

        if (success)
        {
            if (destination.Contains("@"))
                User.Email = destination;
            else
                User.PhoneNumber = destination;

            await SaveAsync();
        }

        return success;
    }


    public void Map(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/auth").RequireCors("Auth");
        auth.MapPost("login", DoLogin);
        auth.MapPost("logout", DoLogout);
        auth.MapGet("userinfo", GetAsync);

    }
}