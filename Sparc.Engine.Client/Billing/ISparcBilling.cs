using Refit;

namespace Sparc.Engine.Billing;

public interface ISparcBilling
{
    [Post("/billing/payments")]
    Task<CreateOrderPaymentResponse> CreateOrderPaymentAsync([Body] CreateOrderPaymentRequest request);

    [Get("/billing/get-product/{productId}")]
    Task<GetProductResponse> GetProductAsync(string productId);
}
