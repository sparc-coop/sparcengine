using Microsoft.AspNetCore.Cors.Infrastructure;
using Sparc.Blossom.Data;
using Sparc.Engine;
using Tovik;
using Tovik.Services;

var builder = BlossomApplication.CreateBuilder<Html>(args);

builder.Services.AddCosmos<TovikContext>(builder.Configuration.GetConnectionString("Cosmos")!, "sparc", ServiceLifetime.Scoped);
builder.Services.AddSparcEngine();
builder.Services.AddScoped<TovikDomainService>();
builder.Services.AddScoped<ICorsPolicyProvider, SparcEngineDomainPolicyProvider>();
builder.Services.AddCors();
builder.Services.AddHybridCache();

var app = builder.Build();
await app.RunAsync<Html>();