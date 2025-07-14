namespace Sparc.Engine.Billing;

public class SparcOrder()
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Email { get; set; }
}

public class CreateOrderPaymentResponse
{
    [Newtonsoft.Json.JsonProperty("clientSecret")]
    public string ClientSecret { get; set; } = default!;
    public string PublishableKey { get; set; } = default!;
}

public record ConfirmOrderPaymentRequest(string PaymentIntentId, string PaymentMethodId);
