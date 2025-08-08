using Sparc.Blossom.Engine;
using Sparc.MCN.Shared;

var builder = BlossomApplication.CreateBuilder<BlossomApp<Program, MainLayout>>(args);

builder.Services.AddBlossomEngine("https://localhost:7185");

var app = builder.Build();
await app.RunAsync<BlossomApp<Program, MainLayout>>();