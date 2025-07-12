using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Refit;
using Scalar.AspNetCore;
using Sparc.Aura;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Realtime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<FriendlyId>()
    .AddScoped<SparcAuraAuthenticator>();

builder.Services.AddDbContext<SparcAuraContext>(options =>
{
    options.UseCosmos(builder.Configuration.GetConnectionString("Cosmos")!, "sparc");
    options.UseOpenIddict();
});

builder.Services.AddAuthorization()
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services.AddIdentityCore<BlossomUser>()
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

        options.UseAspNetCore().EnableTokenEndpointPassthrough();
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
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/tools/friendlyid", (FriendlyId friendlyId) => friendlyId.Create());
app.MapGet("/hi", () => "Hi from Sparc!");
app.MapMethods("/authorize", ["GET", "POST"], async (SparcAuraAuthenticator auth) => await auth.Authorize());

app.Run();
