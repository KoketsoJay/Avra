using Avra.EventBus;
using Avra.EventBus.IntegrationEvents;
using Microsoft.Extensions.Options;
using PaymentService;
using PaymentService.IntegrationEvents.EventHandling;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "payment-service")
    .WriteTo.Console()
    .WriteTo.File("logs/payment-service-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(Log.Logger);

builder.Services.AddOptions<PaymentSettings>()
    .Bind(builder.Configuration.GetSection(PaymentSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAvraEventBus(builder.Configuration, "payment-service")
    .AddSubscription<OrderStatusChangedToStockConfirmedIntegrationEvent, OrderStatusChangedToStockConfirmedIntegrationEventHandler>();

var app = builder.Build();

app.MapPost("/api/payments/charges", (
    PaymentChargeRequest request,
    IOptions<PaymentSettings> options,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("PaymentApi");

    if (request.Amount <= 0)
    {
        return Results.BadRequest(new PaymentChargeResponse
        {
            Approved = false,
            Status = "Declined",
            Message = "Amount must be greater than zero.",
            Amount = request.Amount,
            Currency = request.Currency,
            Reference = request.Reference,
        });
    }

    if (!string.Equals(request.PaymentMethod, "Card", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new PaymentChargeResponse
        {
            Approved = false,
            Status = "Declined",
            Message = "Only card payments are currently supported.",
            Amount = request.Amount,
            Currency = request.Currency,
            Reference = request.Reference,
        });
    }

    if (string.IsNullOrWhiteSpace(request.CardLast4) || request.CardLast4.Length != 4 || !request.CardLast4.All(char.IsDigit))
    {
        return Results.BadRequest(new PaymentChargeResponse
        {
            Approved = false,
            Status = "Declined",
            Message = "Valid card last 4 digits are required.",
            Amount = request.Amount,
            Currency = request.Currency,
            Reference = request.Reference,
        });
    }

    var settings = options.Value;
    var approved = settings.PaymentSucceeded && request.CardLast4 != "0000";

    var response = new PaymentChargeResponse
    {
        Approved = approved,
        Status = approved ? "Paid" : "Declined",
        TransactionId = $"pay_{Guid.NewGuid():N}",
        Message = approved ? "Payment captured successfully." : settings.DeclineReason,
        Amount = request.Amount,
        Currency = request.Currency,
        Reference = request.Reference,
    };

    logger.LogInformation("Payment {Status} for {Reference} amount {Amount}", response.Status, request.Reference, request.Amount);
    return approved ? Results.Ok(response) : Results.BadRequest(response);
});

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "payment-service terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
