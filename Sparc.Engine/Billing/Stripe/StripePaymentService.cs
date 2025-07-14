using Stripe;

namespace Sparc.Engine.Billing.Stripe;

public class StripePaymentService
{
    public readonly ExchangeRates _rates;

    public StripePaymentService(ExchangeRates rates, IConfiguration config)
    {
        _rates = rates;
        StripeConfiguration.ApiKey = config.GetConnectionString("Stripe")
            ?? throw new InvalidOperationException("Stripe connection string is missing in configuration.");
    }
    public async Task<PaymentIntent> CreatePaymentIntentAsync(string email, string productId, string currencyId, string? paymentIntentId = null)
    {
        var customerId = await GetOrCreateCustomerAsync(email);
        currencyId = currencyId.ToLower();

        var basePrice = await GetPriceAsync(productId, currencyId, true)
            ?? throw new InvalidOperationException($"Product {productId} does not have a price in currency {currencyId}.");

        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            var createOptions = new PaymentIntentCreateOptions
            {
                Amount = (long)basePrice,
                Currency = currencyId,
                Customer = customerId,
                SetupFutureUsage = "on_session",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
            };

            return await new PaymentIntentService().CreateAsync(createOptions);
        }

        else
        {
            var service = new PaymentIntentService();
            var options = new PaymentIntentUpdateOptions
            {
                Amount = (long)basePrice,
                Currency = currencyId
            };
            return await service.UpdateAsync(paymentIntentId, options);
        }
    }

    public async Task<PaymentIntent> ConfirmPaymentIntentAsync(
        string paymentIntentId,
        string paymentMethodId)
    {
        var service = new PaymentIntentService();
        var confirmOptions = new PaymentIntentConfirmOptions
        {
            PaymentMethod = paymentMethodId
        };
        return await service.ConfirmAsync(paymentIntentId, confirmOptions);
    }

    public async Task<Product> GetProductAsync(string productId)
    {
        var productService = new ProductService();
        var product = await productService.GetAsync(productId);
        // Use the line bellow to create a price with currency options for the product
        //var result = await CreatePriceWithCurrencyOptionsAsync(productId);

        return product;
    }

    public async Task<decimal?> GetPriceAsync(string productId, string currencyId, bool stripeFormat = false)
    {
        currencyId = currencyId.ToLower();

        var priceService = new PriceService();
        var listOptions = new PriceListOptions
        {
            Product = productId,
            Expand = ["data.currency_options"]
        };

        var pricesResult = await priceService.ListAsync(listOptions);
        var basePrice = pricesResult.Data.FirstOrDefault();
        if (basePrice?.UnitAmount == null)
            return null;

        var hasPrice = basePrice.CurrencyOptions.TryGetValue(currencyId, out var currentPrice);
        var newPrice = ToStripePrice(await _rates.ConvertAsync(FromStripePrice(basePrice.UnitAmount.Value, "USD"), "USD", currencyId, true), currencyId);
        var difference = currentPrice?.UnitAmount == null ? 1 : Math.Abs(newPrice - currentPrice.UnitAmount.Value) / (decimal)currentPrice.UnitAmount.Value;

        if (difference > 0.2M)
        {
            // Stripe doesn't let you update prices directly, so just return the newly calculated price
            //var priceUpdateOptions = new PriceUpdateOptions
            //{
            //    CurrencyOptions = new Dictionary<string, PriceCurrencyOptionsOptions>
            //    {
            //        { currencyId, new PriceCurrencyOptionsOptions { UnitAmount = newPrice } }
            //    }
            //};
            //await priceService.UpdateAsync(basePrice.Id, priceUpdateOptions);
            return stripeFormat ? newPrice : FromStripePrice(newPrice, currencyId);
        }
        
        return stripeFormat ? currentPrice.UnitAmount.Value : FromStripePrice(currentPrice.UnitAmount.Value, currencyId);
    }

    public async Task<string?> GetOrCreateCustomerAsync(string? email)
    {
        var customerService = new CustomerService();

        if (email != null)
        {
            var searchOptions = new CustomerSearchOptions
            {
                Query = $"email:'{email}'"
            };

            var stripeCustomerList = await customerService.SearchAsync(searchOptions);
            if (stripeCustomerList.Data.Count > 0)
                return stripeCustomerList.Data.First().Id;
        }

        var customerOptions = new CustomerCreateOptions
        {
            Email = email
        };

        var newCustomer = await customerService.CreateAsync(customerOptions);
        return newCustomer.Id;
    }

    public long ToStripePrice(decimal amount, string currencyId)
    {
        if (ZeroDecimalCurrencies.Contains(currencyId))
            return (long)amount;
        // Convert to cents for currencies that require it
        return (long)(amount * 100);
    }

    public decimal FromStripePrice(long amount, string currencyId)
    {
        if (ZeroDecimalCurrencies.Contains(currencyId))
            return amount;
        // Convert from cents for currencies that require it
        return amount / 100M;
    }

    static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg", "rwf",
        "ugx", "vnd", "vuv", "xaf", "xof", "xpf"
    };

    protected static readonly HashSet<string> Currencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "sll","aed","afn","all","amd","ang","aoa","ars","aud","awg","azn",
        "bam","bbd","bdt","bgn","bhd","bif","bmd","bnd","bob","brl","bsd",
        "bwp","byn","bzd","cad","cdf","chf","clp","cny","cop","crc","cve",
        "czk","djf","dkk","dop","dzd","egp","etb","eur","fjd","fkp","gbp",
        "gel","gip","gmd","gnf","gtq","gyd","hkd","hnl","htg","huf","mro",
        "idr","ils","inr","isk","jmd","jod","jpy","kes","kgs","khr","kmf",
        "krw","kwd","kyd","kzt","lak","lbp","lkr","lrd","lsl","mad","mdl",
        "mga","mkd","mmk","mnt","mop","mur","mvr","mwk","mxn","myr","mzn",
        "nad","ngn","nio","nok","npr","nzd","omr","pab","pen","pgk","php",
        "pkr","pln","pyg","qar","ron","rsd","rub","rwf","sar","sbd","scr",
        "sek","sgd","shp","sle","sos","srd","std","szl","thb","tjs","tnd",
        "top","try","ttd","twd","tzs","uah","ugx","uyu","usd","uzs","vnd","vuv",
        "wst","xaf","xcd","xcg","xof","xpf","yer","zar","zmw","btn",
        "ghs","eek","lvl","svc","vef","ltl"
    };
}