using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sparc.Aura.Users;
using Sparc.Blossom.Authentication;
using System.Security.Claims;
using Sparc.Blossom.Data;

namespace Sparc.Aura;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddSparcAura(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<FriendlyId>()
            .AddScoped<SparcAuraPresenceHub>()
            .AddHttpContextAccessor();

        builder.Services.AddDbContext<SparcUserContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("Aura"));
        });

        builder.Services.AddCosmos<SparcAuraContext>(builder.Configuration.GetConnectionString("Cosmos")!, "Users", ServiceLifetime.Scoped);

        builder.Services.AddAuthorization()
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();

        builder.Services.AddIdentityCore<SparcUser>()
            .AddEntityFrameworkStores<SparcUserContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddTransient(s => BlossomUser.FromPrincipal(s.GetRequiredService<ClaimsPrincipal>()));

        builder.Services.AddPasswordlessSdk(x =>
        {
            x.ApiKey = "sparcaura:public:b227c6af0d244323aaab033cc9d392c8";
            x.ApiSecret = builder.Configuration.GetConnectionString("Passwordless") ?? throw new InvalidOperationException("Passwordless API Secret is not configured.");
        });

        return builder;
    }

    public static WebApplication UseSparcAura(this WebApplication app)
    {
        app.UseCookiePolicy(new()
        {
            MinimumSameSitePolicy = SameSiteMode.None,
            HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always,
            Secure = CookieSecurePolicy.Always
        });

        app.UseHttpsRedirection();
        app.UseForwardedHeaders();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
