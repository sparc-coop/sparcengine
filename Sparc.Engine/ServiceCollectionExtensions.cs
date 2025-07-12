using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Sparc.Aura.Users;
using Sparc.Blossom.Authentication;
using System.Security.Claims;
using Twilio.Rest.Api.V2010.Account.Usage.Record;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Sparc.Aura;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddSparcAura(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<FriendlyId>()
            .AddScoped<SparcAuraAuthenticator>()
            .AddHttpContextAccessor();

        builder.Services.AddDbContext<SparcAuraContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("Aura"));
            options.UseOpenIddict();
        });

        builder.Services.AddAuthorization()
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();

        builder.Services.AddIdentityCore<SparcAuraUser>()
            .AddEntityFrameworkStores<SparcAuraContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore().UseDbContext<SparcAuraContext>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("authorize")
                    .SetTokenEndpointUris("token");

                options.AllowAuthorizationCodeFlow()
                    .AllowRefreshTokenFlow();

                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough();
            });

        builder.Services.AddTransient(s => BlossomUser.FromPrincipal(s.GetRequiredService<ClaimsPrincipal>()));

        builder.Services.AddPasswordlessSdk(x =>
        {
            x.ApiKey = "sparcengine:public:63cc565eb9544940ad6f2c387b228677";
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

    public static async Task InitializeSparcAura(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SparcAuraContext>().Database.EnsureCreatedAsync();

        // add Tovik client
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        if (await manager.FindByClientIdAsync("tovik") is null)
        {
            var application = new OpenIddictApplicationDescriptor
            {
                ClientId = "tovik",
                DisplayName = "Tovik",
                ClientType = ClientTypes.Public,
                ApplicationType = ApplicationTypes.Web,
                ConsentType = ConsentTypes.Implicit,
                PostLogoutRedirectUris =
        {
            new Uri("https://tovik.app/"),
            new Uri("https://localhost:7194/")
        },
                RedirectUris =
        {
            new Uri("https://tovik.app/"),
            new Uri("https://localhost:7194")
        },
                Permissions =
        {
            Permissions.Endpoints.Authorization,
            Permissions.Endpoints.Token,
            Permissions.GrantTypes.AuthorizationCode,
            Permissions.ResponseTypes.Code
        }
            };
            await manager.CreateAsync(application);
        }
    }

}
