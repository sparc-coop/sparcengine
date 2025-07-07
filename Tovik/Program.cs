using Sparc.Blossom;
using Sparc.Blossom.Data;
using Sparc.Engine;
using Tovik;
using Tovik.Languages;

var builder = BlossomApplication.CreateBuilder<Html>(args);

builder.Services.AddCosmos<TovikContext>(builder.Configuration.GetConnectionString("Cosmos")!, "sparc", ServiceLifetime.Scoped);
builder.Services.AddSparcEngine();

builder.Services.AddScoped<IRepository<Language>, BlossomInMemoryRepository<Language>>();

var app = builder.Build();

var languages = app.Services.GetRequiredService<Languages>();
await languages.InitializeAsync(app.Services.GetRequiredService<IEnumerable<ITranslator>>());

await app.RunAsync<Html>();