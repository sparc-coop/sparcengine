using Refit;
using Sparc.Blossom.Authentication;

namespace Sparc.Engine.Aura;

public interface ISparcAura
{
    /// <returns>OK</returns>
    /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
    [Headers("Accept: text/plain")]
    [Get("/tools/friendlyid")]
    Task<string> FriendlyId();

    [Get("/auth/login")]
    Task<BlossomUser> Login(string? emailOrToken = null);

    [Get("/auth/userinfo")]
    Task<BlossomUser> UserInfo();

    [Post("/auth/userinfo")]
    Task<BlossomUser> UpdateUserInfo([Body] BlossomAvatar avatar);
}
