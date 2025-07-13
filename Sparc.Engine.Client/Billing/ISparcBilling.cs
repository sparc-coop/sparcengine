using Refit;

namespace Sparc.Engine.Billing;

public interface ISparcBilling
{
    [Post("/billing/payments")]
    Task<CreateOrderPaymentResponse> CreateOrderPaymentAsync([Body] CreateOrderPaymentRequest request);

    [Get("/billing/products/{productId}")]
    Task<GetProductResponse> GetProductAsync(string productId, string? currency = null);
}
