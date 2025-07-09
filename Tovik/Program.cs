using Microsoft.AspNetCore.Cors.Infrastructure;
using Sparc.Aura;
using Sparc.Blossom;
using Sparc.Blossom.Data;
using Sparc.Engine;
using Tovik;
using Tovik.Languages;

var builder = BlossomApplication.CreateBuilder<Html>(args);

builder.Services.AddCosmos<TovikContext>(builder.Configuration.GetConnectionString("Cosmos")!, "sparc", ServiceLifetime.Scoped);
builder.Services.AddSparcEngine();

builder.Services.AddScoped<IRepository<Language>, BlossomInMemoryRepository<Language>>();

builder.Services.AddHybridCache();

builder.Services.AddCors();
builder.Services.AddScoped<ICorsPolicyProvider, SparcAuraDomainPolicyProvider>();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var languages = scope.ServiceProvider.GetRequiredService<Languages>();
await languages.InitializeAsync(scope.ServiceProvider.GetRequiredService<IEnumerable<ITranslator>>());

await app.RunAsync<Html>();