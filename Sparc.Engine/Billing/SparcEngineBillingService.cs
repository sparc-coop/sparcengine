using Sparc.Engine.Billing.Stripe;

namespace Sparc.Engine.Billing;

public record CreateOrderPaymentRequest(string Email, long Amount, string Currency);

public class SparcEngineBillingService(ExchangeRates rates, IConfiguration config) : StripePaymentService(rates, config), IBlossomEndpoints
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        var billingGroup = endpoints.MapGroup("/billing");

        billingGroup.MapPost("/payments", 
            async (SparcEngineBillingService svc, CreateOrderPaymentRequest req) =>
            {
                var intent = await svc.CreatePaymentIntentAsync(req.Email, req.Amount, req.Currency);
                return Results.Ok(new { intent.ClientSecret });
            });

        billingGroup.MapGet("/products/{productId}",
            async (SparcEngineBillingService svc, string productId) =>
            {
                var product = await svc.GetProductAsync(productId);
                return Results.Ok(product);
            });
    }
}