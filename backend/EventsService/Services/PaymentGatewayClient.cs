using System.Net.Http.Json;

namespace EventsService.Services;

public sealed class PaymentChargeRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string PaymentMethod { get; set; } = "Card";
    public string CardLast4 { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class PaymentChargeResponse
{
    public bool Approved { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string Reference { get; set; } = string.Empty;
}

public interface IPaymentGatewayClient
{
    Task<PaymentChargeResponse> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default);
}

public class PaymentGatewayClient(HttpClient httpClient, ILogger<PaymentGatewayClient> logger) : IPaymentGatewayClient
{
    public async Task<PaymentChargeResponse> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var response = await httpClient.PostAsJsonAsync("api/payments/charges", request, cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<PaymentChargeResponse>(cancellationToken: cancellationToken);
            if (payload is null)
            {
                return new PaymentChargeResponse
                {
                    Approved = false,
                    Status = "Unavailable",
                    Message = "Payment service returned an empty response.",
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Reference = request.Reference,
                };
            }

            return payload;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Payment gateway request failed");
            return new PaymentChargeResponse
            {
                Approved = false,
                Status = "Unavailable",
                Message = "Payment service is currently unavailable.",
                Amount = request.Amount,
                Currency = request.Currency,
                Reference = request.Reference,
            };
        }
    }
}

public static class PaymentGatewayServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentGateway(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var baseUrl = configuration["Services:Payments:BaseUrl"] ?? "http://localhost:5002";

        services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(12);
        });

        return services;
    }
}