using Refit;

namespace Sparc.Engine.Billing;

public interface ISparcBilling
{
    [Post("/billing/create-order-payment")]
    Task<CreateOrderPaymentResponse> CreateOrderPaymentAsync([Body] CreateOrderPaymentRequest request);

    [Get("/billing/get-product/{productId}")]
    Task<GetProductResponse> GetProductAsync(string productId);
}
