using Refit;

namespace Sparc.Engine.Billing;

public interface ISparcBilling
{
    [Post("/billing/payments")]
    Task<CreateOrderPaymentResponse> StartCheckoutAsync([Body] SparcOrder order);

    [Get("/billing/products/{productId}")]
    Task<GetProductResponse> GetProductAsync(string productId, string? currency = null);
}
