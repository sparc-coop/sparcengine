using Sparc.Engine.Billing.Stripe;

namespace Sparc.Engine.Billing;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddSparcBilling(
        this WebApplicationBuilder builder
    )
    {
        builder.Services.AddSingleton<StripePaymentService>();
        builder.Services.AddSingleton<ExchangeRates>();
        builder.Services.AddTransient<SparcEngineBillingService>();

        return builder;
    }

    public static WebApplication UseSparcBilling(
        this WebApplication app
    )
    {
        using var scope = app.Services.CreateScope();
        var billingSvc = scope
            .ServiceProvider
            .GetRequiredService<SparcEngineBillingService>();

        billingSvc.Map(app);
        return app;
    }
}


