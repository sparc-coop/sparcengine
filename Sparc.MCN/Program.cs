using Microsoft.AspNetCore.Cors.Infrastructure;
using Sparc.Engine;

var builder = BlossomApplication.CreateBuilder<Html>(args);

builder.Services.AddSparcEngine("https://localhost:7185");
//builder.Services.AddScoped<ICorsPolicyProvider, SparcEngineDomainPolicyProvider>();
//builder.Services.AddCors();
builder.Services.AddHybridCache();

var app = builder.Build();
await app.RunAsync<Html>();