namespace PaymentService;

/// <summary>
/// One payment authorization/capture request.
/// </summary>
public class PaymentChargeRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string PaymentMethod { get; set; } = "Card";
    public string CardLast4 { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Outcome returned by the payment processor.
/// </summary>
public class PaymentChargeResponse
{
    public bool Approved { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string Reference { get; set; } = string.Empty;
}