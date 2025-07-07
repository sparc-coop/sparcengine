using Sparc.Blossom;
using Sparc.Blossom.Data;
using Sparc.Engine;
using Tovik;

var builder = BlossomApplication.CreateBuilder<Html>(args);

builder.Services.AddCosmos<TovikContext>(builder.Configuration.GetConnectionString("Cosmos")!, "sparc", ServiceLifetime.Scoped);
builder.Services.AddSparcEngine();

var app = builder.Build();

await app.RunAsync<Html>();