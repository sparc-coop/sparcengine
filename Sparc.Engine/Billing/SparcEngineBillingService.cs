using Sparc.Blossom.Authentication;
using Sparc.Core.Billing;
using Sparc.Engine.Billing.Stripe;
using System.Security.Claims;

namespace Sparc.Engine.Billing;

public record SparcOrder(string Email, string ProductId, string Currency, string? PaymentIntentId = null);

public class SparcEngineBillingService(ExchangeRates rates, IConfiguration config) : StripePaymentService(rates, config), IBlossomEndpoints
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        var billingGroup = endpoints.MapGroup("/billing");

        billingGroup.MapPost("/payments", 
            async (SparcEngineBillingService svc, SparcOrder req) =>
            {
                var intent = await svc.CreatePaymentIntentAsync(req.Email, req.ProductId, req.Currency, req.PaymentIntentId);
                return Results.Ok(new SparcPaymentIntent
                { 
                    ClientSecret = intent.ClientSecret, 
                    PublishableKey = config["Stripe:PublishableKey"]!,
                    PaymentIntentId = intent.Id,
                    Amount = FromStripePrice(intent.Amount, req.Currency),
                    Currency = req.Currency,
                    FormattedAmount = SparcCurrency.From(req.Currency).ToString(FromStripePrice(intent.Amount, req.Currency))
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

        billingGroup.MapGet("/currency", (ClaimsPrincipal principal, HttpRequest request) => SparcCurrency.From(principal.Get("currency") ?? request.Headers.AcceptLanguage));
        billingGroup.MapPost("/currency", async (SparcAuthenticator<BlossomUser> auth, ClaimsPrincipal principal, SparcCurrency currency) =>
        {
            var user = await auth.GetAsync(principal);
            user.Avatar.Currency = currency;
            await auth.UpdateAsync(principal, user.Avatar);
            return currency;
        });

        billingGroup.MapGet("/currencies",
            () =>
            {
                var currencies = SparcCurrency.All()
                    .Where(x => Currencies.Contains(x.Id.ToLower()));
                return Results.Ok(currencies);
            }).CacheOutput();
    }
}