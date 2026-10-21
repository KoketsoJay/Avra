using Avra.EventBus;
using Avra.EventBus.IntegrationEvents;
using Avra.EventBus.Outbox;
using Avra.ServiceDefaults;
using Avra.ServiceDefaults.Storage;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.IntegrationEvents;
using ProductService.IntegrationEvents.EventHandling;
using ProductService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddAvraServiceDefaults("product-service");

builder.AddAvraImageStorage();

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

builder.Services.AddHealthChecks().AddDbContextCheck<ProductDbContext>("product-db");

builder.Services.AddScoped<IProductService, ProductService.Services.ProductService>();

// Transactional outbox lives in the catalogue database.
builder.Services.AddScoped<IIntegrationEventLogService, IntegrationEventLogService<ProductDbContext>>();
builder.Services.AddScoped<ICatalogIntegrationEventService, CatalogIntegrationEventService>();

builder.Services.AddAvraEventBus(builder.Configuration, "catalog-service")
    .AddSubscription<OrderStatusChangedToAwaitingValidationIntegrationEvent, OrderStatusChangedToAwaitingValidationIntegrationEventHandler>()
    .AddSubscription<OrderStatusChangedToPaidIntegrationEvent, OrderStatusChangedToPaidIntegrationEventHandler>();

var app = builder.Build();

app.UseAvraServiceDefaults();
await app.InitializeDatabaseAsync<ProductDbContext>();
await app.EnsureLatestProductSchemaAsync();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "product-service terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
