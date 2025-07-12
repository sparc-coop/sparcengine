using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Refit;
using Scalar.AspNetCore;
using Sparc.Aura;
using Sparc.Aura.Users;
using Sparc.Blossom.Realtime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
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

builder.Services.AddIdentityCore<SparcUser>()
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

// builder.AddSparcAura<BlossomUser>();

builder.Services.AddTwilio(builder.Configuration);
builder.Services.AddHybridCache();
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
});

var app = builder.Build();
//app.UseSparcAura<BlossomUser>();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseForwardedHeaders();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

using var scope = app.Services.CreateScope();
await scope.ServiceProvider.GetRequiredService<SparcAuraContext>().Database.EnsureCreatedAsync();

app.MapGet("/tools/friendlyid", (FriendlyId friendlyId) => friendlyId.Create());
app.MapGet("/hi", () => "Hi from Sparc!");
app.MapMethods("/authorize", ["GET", "POST"], async (SparcAuraAuthenticator auth) => await auth.Authorize());

app.Run();
