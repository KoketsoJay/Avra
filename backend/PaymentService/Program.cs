using Avra.EventBus;
using Avra.EventBus.IntegrationEvents;
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

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

builder.Services.AddOptions<PaymentSettings>()
    .Bind(builder.Configuration.GetSection(PaymentSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAvraEventBus(builder.Configuration, "payment-service")
    .AddSubscription<OrderStatusChangedToStockConfirmedIntegrationEvent, OrderStatusChangedToStockConfirmedIntegrationEventHandler>();

var host = builder.Build();

try
{
    await host.RunAsync();
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
