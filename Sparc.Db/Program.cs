using Sparc.Blossom;
using Sparc.Blossom.Data;
using Sparc.Data;
using Sparc.Engine;

var builder = BlossomApplication.CreateBuilder<Html>(args);

builder.Services.AddCosmos<DataContext>(builder.Configuration.GetConnectionString("Cosmos")!, "sparc", ServiceLifetime.Scoped);
builder.Services.AddSparcEngine();

var app = builder.Build();

await app.RunAsync<Html>();