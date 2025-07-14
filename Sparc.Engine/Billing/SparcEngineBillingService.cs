using Sparc.Engine.Billing.Stripe;

namespace Sparc.Engine.Billing;

public record SparcOrder(string Email, long Amount, string Currency, string? PaymentIntentId = null);

public class SparcEngineBillingService(ExchangeRates rates, IConfiguration config) : StripePaymentService(rates, config), IBlossomEndpoints
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        var billingGroup = endpoints.MapGroup("/billing");

        billingGroup.MapPost("/payments", 
            async (SparcEngineBillingService svc, SparcOrder req) =>
            {
                var intent = await svc.CreatePaymentIntentAsync(req.Email, req.Amount, req.Currency, req.PaymentIntentId);
                return Results.Ok(new 
                { 
                    intent.ClientSecret, 
                    PublishableKey = config["Stripe:PublishableKey"],
                    PaymentIntentId = intent.Id
                });
            });

        billingGroup.MapGet("/products/{productId}",
            async (SparcEngineBillingService svc, string productId, string? currency = null) =>
            {
                var product = await svc.GetProductAsync(productId);
                var price = string.IsNullOrWhiteSpace(currency) && product.DefaultPrice != null
                ? product.DefaultPrice.UnitAmountDecimal 
                : await svc.GetPriceAsync(productId, currency ?? "USD");
                
                return Results.Ok(new GetProductResponse(productId, product.Name, price ?? 0, currency ?? "USD"));
            });
    }
}