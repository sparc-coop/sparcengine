using Sparc.Blossom;
using Sparc.Engine;
using Timbii;

var builder = BlossomApplication.CreateBuilder<Html>(args);
builder.Services.AddSparcEngine("https://localhost:7197");
var app = builder.Build();

await app.RunAsync<Html>();