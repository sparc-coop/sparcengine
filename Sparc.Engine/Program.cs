using Microsoft.AspNetCore.Http.Json;
using Refit;
using Scalar.AspNetCore;
using Sparc.Aura;
using Sparc.Blossom.Realtime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddSparcAura();

builder.Services.AddTwilio(builder.Configuration);
builder.Services.AddHybridCache();
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
});

var app = builder.Build();
app.UseSparcAura();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}



app.MapGet("/tools/friendlyid", (FriendlyId friendlyId) => friendlyId.Create());
app.MapGet("/hi", () => "Hi from Sparc!");
app.MapMethods("/authorize", ["GET", "POST"], async (SparcAuraAuthenticator auth) => await auth.Authorize());

app.Run();
