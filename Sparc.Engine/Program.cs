using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Json;
using Scalar.AspNetCore;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;
using Sparc.Engine;
using Sparc.Engine.Billing;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddScoped<FriendlyId>();

builder.Services.AddCosmos<SparcEngineContext>(builder.Configuration.GetConnectionString("Cosmos")!, "sparc", ServiceLifetime.Scoped);

builder.AddSparcAuthentication<SparcUser>();
builder.AddSparcEngineBilling();
builder.AddTovikTranslator();

builder.Services.AddMediatR(options =>
{
    options.RegisterServicesFromAssemblyContaining<Program>();
    options.RegisterServicesFromAssemblyContaining<BlossomEvent>();
    options.NotificationPublisher = new TaskWhenAllPublisher();
    options.NotificationPublisherType = typeof(TaskWhenAllPublisher);
});

builder.Services.AddTwilio(builder.Configuration);

builder.Services.AddScoped<ICorsPolicyProvider, SparcEngineDomainPolicyProvider>();
builder.Services.AddCors();

builder.Services.AddHybridCache();
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
});

var app = builder.Build();
app.MapStaticAssets();
app.UseSparcAuthentication<SparcUser>();
app.UseSparcEngineBilling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();

app.MapGet("/tools/friendlyid", (FriendlyId friendlyId) => friendlyId.Create());
app.MapGet("/hi", () => "Hi from Sparc!");

using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<Contents>().Map(app);

if (!string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("Cognitive")))
{
    var translator = scope.ServiceProvider.GetRequiredService<TovikTranslator>();
    await translator.GetLanguagesAsync();
}
app.Run();
