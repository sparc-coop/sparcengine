using Sparc.Blossom.Payment.Stripe;
using Sparc.Engine.Billing;

namespace Sparc.Engine.Billing;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddSparcEngineBilling(
        this WebApplicationBuilder builder
    )
    {
        var ratesSection = builder.Configuration.GetSection("ExchangeRates");
        var stripeSection = builder.Configuration.GetSection("Stripe");

        builder.Services.AddExchangeRates(opts =>
        {
            ratesSection.Bind(opts);
        });

        builder.Services.AddStripePayments(opts =>
        {
            stripeSection.Bind(opts);
        });


        builder.Services.AddTransient<SparcEngineBillingService>();

        return builder;
    }

    public static WebApplication UseSparcEngineBilling(
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


