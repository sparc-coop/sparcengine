using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Scalar.AspNetCore;
using Sparc.Aura;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Realtime;
using System.Reflection;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddSparcAura();

builder.Services.AddTwilio(builder.Configuration);
builder.Services.AddHybridCache();

builder.Services.AddMediatR(options =>
{
    options.RegisterServicesFromAssembly(Assembly.GetEntryAssembly());
    options.RegisterServicesFromAssemblyContaining<BlossomEvent>();
    options.NotificationPublisher = new TaskWhenAllPublisher();
    options.NotificationPublisherType = typeof(TaskWhenAllPublisher);
});

builder.Services.AddCors();
builder.Services.AddScoped<ICorsPolicyProvider, SparcAuraDomainPolicyProvider>();

var app = builder.Build();
app.UseSparcAura();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("auth");

app.MapGet("/tools/friendlyid", (FriendlyId friendlyId) => friendlyId.Create());
app.MapMethods("/hi", ["GET", "POST"], async (SparcAuraPresenceHub auth) => await auth.Hi());
app.MapGet("/me", async (SparcAuraPresenceHub auth, ClaimsPrincipal principal) => await auth.Me(principal)).RequireAuthorization();
app.MapPatch("/me", async (SparcAuraPresenceHub auth, ClaimsPrincipal principal, SparcAura aura) => await auth.Update(principal, aura)).RequireAuthorization();
app.MapMethods("/bye", ["GET", "POST"], async (SparcAuraPresenceHub auth) => await auth.Bye()).RequireAuthorization();

app.Run();
