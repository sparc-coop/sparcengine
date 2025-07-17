using Sparc.Blossom.Authentication;
using Sparc.Core.Billing;
using Sparc.Engine.Billing.Stripe;
using System.Security.Claims;

namespace Sparc.Engine.Billing;

public class SparcEngineBillingService(ExchangeRates rates, ClaimsPrincipal principal, SparcAuthenticator<BlossomUser> auth, IConfiguration config) 
    : StripePaymentService(rates, config), IBlossomEndpoints
{
    public async Task<SparcPaymentIntent> StartCheckoutAsync(SparcOrder req)
    {
        var currency = req.Currency ?? principal.Get("currency") ?? "USD";

        var intent = await CreatePaymentIntentAsync(req.Email, req.ProductId, currency, req.PaymentIntentId);
        return new SparcPaymentIntent
        {
            ClientSecret = intent.ClientSecret,
            PublishableKey = config["Stripe:PublishableKey"]!,
            PaymentIntentId = intent.Id,
            Amount = FromStripePrice(intent.Amount, currency),
            Currency = currency,
            FormattedAmount = SparcCurrency.From(currency).ToString(FromStripePrice(intent.Amount, currency))
        };
    }

    public async Task<GetProductResponse> GetProduct(HttpRequest request, string productId, string? currency = null)
    {
        var sparcCurrency = SparcCurrency.From(currency ?? principal.Get("currency") ?? request.Headers.AcceptLanguage);

        var product = await GetProductAsync(productId);
        var price = await GetPriceAsync(productId, sparcCurrency.Id);

        return new GetProductResponse(productId,
            product.Name,
            price ?? 0,
            sparcCurrency.Id,
            sparcCurrency.ToString(price ?? 0));
    }

    public async Task<SparcCurrency> SetCurrencyAsync(SparcCurrency currency)
    {
        var user = await auth.GetAsync(principal);
        user.Avatar.Currency = currency;
        await auth.UpdateAsync(principal, user.Avatar);
        return currency;
    }

    public SparcCurrency GetCurrency(HttpRequest request)
        => SparcCurrency.From(principal.Get("currency") ?? request.Headers.AcceptLanguage);

    public IEnumerable<SparcCurrency> AllCurrencies() => SparcCurrency.All().Where(x => Currencies.Contains(x.Id.ToLower()));

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var billingGroup = endpoints.MapGroup("/billing");

        billingGroup.MapPost("/payments", async (SparcEngineBillingService svc, SparcOrder order) => await svc.StartCheckoutAsync(order));

        billingGroup.MapGet("/products/{productId}", async (SparcEngineBillingService svc, HttpRequest req, string productId) 
            => await svc.GetProduct(req, productId));

        billingGroup.MapGet("/currency", (SparcEngineBillingService svc, HttpRequest req) => svc.GetCurrency(req));
        billingGroup.MapPost("/currency", async (SparcEngineBillingService svc, SparcCurrency currency)
            => await svc.SetCurrencyAsync(currency));

        billingGroup.MapGet("/currencies", () => AllCurrencies()).CacheOutput();
    }
}