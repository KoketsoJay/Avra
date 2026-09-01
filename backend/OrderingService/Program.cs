using Avra.EventBus;
using Avra.EventBus.IntegrationEvents;
using Avra.EventBus.Outbox;
using Avra.ServiceDefaults;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderingService.Application.Behaviors;
using OrderingService.Application.IntegrationEvents;
using OrderingService.Application.IntegrationEvents.EventHandling;
using OrderingService.Application.Queries;
using OrderingService.Application.Validations;
using OrderingService.BackgroundTasks;
using OrderingService.Domain.AggregatesModel.BuyerAggregate;
using OrderingService.Domain.AggregatesModel.OrderAggregate;
using OrderingService.Infrastructure;
using OrderingService.Infrastructure.Idempotency;
using OrderingService.Infrastructure.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddAvraServiceDefaults("ordering-service");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required");

builder.Services.AddDbContext<OrderingContext>(options =>
    options.UseSqlServer(
        connectionString,
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

builder.Services.AddHealthChecks().AddDbContextCheck<OrderingContext>("ordering-db");

// Write side: aggregates, unit of work and idempotency.
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IBuyerRepository, BuyerRepository>();
builder.Services.AddScoped<IRequestManager, RequestManager>();

// Read side: projections straight from SQL, no aggregates involved.
builder.Services.AddScoped<IOrderQueries>(_ => new OrderQueries(connectionString));

// Transactional outbox living in the ordering database.
builder.Services.AddScoped<IIntegrationEventLogService, IntegrationEventLogService<OrderingContext>>();
builder.Services.AddScoped<IOrderingIntegrationEventService, OrderingIntegrationEventService>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    // Order matters: log, then validate, then wrap the handler in a transaction.
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidatorBehavior<,>));
    cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
});

builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();

builder.Services.AddAvraEventBus(builder.Configuration, "ordering-service")
    .AddSubscription<UserCheckoutAcceptedIntegrationEvent, UserCheckoutAcceptedIntegrationEventHandler>()
    .AddSubscription<GracePeriodConfirmedIntegrationEvent, GracePeriodConfirmedIntegrationEventHandler>()
    .AddSubscription<OrderStockConfirmedIntegrationEvent, OrderStockConfirmedIntegrationEventHandler>()
    .AddSubscription<OrderStockRejectedIntegrationEvent, OrderStockRejectedIntegrationEventHandler>()
    .AddSubscription<OrderPaymentSucceededIntegrationEvent, OrderPaymentSucceededIntegrationEventHandler>()
    .AddSubscription<OrderPaymentFailedIntegrationEvent, OrderPaymentFailedIntegrationEventHandler>();

builder.Services.Configure<BackgroundTaskOptions>(builder.Configuration.GetSection(BackgroundTaskOptions.SectionName));
builder.Services.AddHostedService<GracePeriodManagerService>();

var app = builder.Build();

app.UseAvraServiceDefaults();
await app.InitializeDatabaseAsync<OrderingContext>();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ordering-service terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
