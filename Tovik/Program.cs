using Sparc.Blossom;
using Sparc.Blossom.Data;
using Sparc.Engine;
using System.Security.Claims;
using Tovik;

var builder = BlossomApplication.CreateBuilder<Html>(args);

builder.Services.AddCosmos<TovikContext>(builder.Configuration.GetConnectionString("Cosmos")!, "sparc", ServiceLifetime.Scoped);
builder.Services.AddSparcEngine();

var app = builder.Build();

var group = app.MapGroup("translate").RequireCors("Tovik");
group.MapPost("", async (HttpRequest request, TextContent content) => await TranslateAsync(content));
group.MapGet("languages", GetLanguagesAsync).CacheOutput(x => x.Expire(TimeSpan.FromHours(1)));
group.MapGet("language", (ClaimsPrincipal principal, HttpRequest request) => GetLanguage(principal.Get("language") ?? request.Headers.AcceptLanguage));
group.MapPost("language", async (ClaimsPrincipal principal, Language language) =>
{
    var user = await auth.GetAsync(principal);
    user.Avatar.Language = language;
    await auth.UpdateAsync(principal, user.Avatar);
    return language;
});
group.MapPost("bulk", BulkTranslate);

await app.RunAsync<Html>();