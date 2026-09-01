using Avra.EventBus;
using Avra.EventBus.IntegrationEvents;
using Avra.ServiceDefaults;
using CartService.Data;
using CartService.IntegrationEvents.EventHandling;
using CartService.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddAvraServiceDefaults("cart-service");

builder.Services.AddDbContext<CartDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

builder.Services.AddHealthChecks().AddDbContextCheck<CartDbContext>("cart-db");

builder.Services.AddScoped<ICartService, CartService.Services.CartService>();
builder.Services.AddScoped<IOrderService, CartService.Services.OrderService>();

// Line prices come from the catalogue, never from the client.
builder.Services.AddProductCatalog(builder.Configuration);

// Member discounts are resolved at checkout, never taken from the request body.
builder.Services.AddMemberDiscounts(builder.Configuration);

builder.Services.AddAvraEventBus(builder.Configuration, "basket-service")
    .AddSubscription<ProductPriceChangedIntegrationEvent, ProductPriceChangedIntegrationEventHandler>()
    .AddSubscription<OrderStartedIntegrationEvent, OrderStartedIntegrationEventHandler>();

var app = builder.Build();

app.UseAvraServiceDefaults();
await app.InitializeDatabaseAsync<CartDbContext>();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "cart-service terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
