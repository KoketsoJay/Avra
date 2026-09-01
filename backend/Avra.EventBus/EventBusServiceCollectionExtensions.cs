using Avra.EventBus.Abstractions;
using Avra.EventBus.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Avra.EventBus;

/// <summary>
/// Fluent surface for declaring which integration events a service consumes.
/// </summary>
public interface IEventBusBuilder
{
    IServiceCollection Services { get; }
}

/// <summary>
/// Registration helpers for the Azure SQL backed integration event bus.
/// </summary>
public static class EventBusServiceCollectionExtensions
{
    private sealed class EventBusBuilder(IServiceCollection services) : IEventBusBuilder
    {
        public IServiceCollection Services { get; } = services;
    }

    /// <summary>
    /// Wires the publish side and the polling dispatcher for a service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration containing the <c>EventBus</c> section.</param>
    /// <param name="subscriberName">
    /// Logical consumer group for this service — one per service, shared by its instances.
    /// </param>
    public static IEventBusBuilder AddAvraEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        string subscriberName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriberName);

        services.AddOptions<SqlEventBusOptions>()
            .Bind(configuration.GetSection(SqlEventBusOptions.SectionName))
            .Configure(options =>
            {
                options.SubscriberName = subscriberName;
                if (string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    options.ConnectionString = configuration.GetConnectionString("EventBus") ?? string.Empty;
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EventBusSubscriptionInfo>();

        var connectionString = configuration.GetSection(SqlEventBusOptions.SectionName)["ConnectionString"]
            ?? configuration.GetConnectionString("EventBus");

        services.AddDbContextFactory<EventBusDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null)));

        services.TryAddSingleton<IEventBus, SqlEventBus>();
        services.AddHostedService<SqlEventBusDispatcher>();

        return new EventBusBuilder(services);
    }

    /// <summary>
    /// Subscribes a handler to an integration event. The event's type name is the wire contract,
    /// so producer and consumer must agree on it.
    /// </summary>
    public static IEventBusBuilder AddSubscription<TEvent, THandler>(this IEventBusBuilder builder)
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();
        builder.Services.Configure<EventBusSubscriptionInfo>(options =>
            options.EventTypes[typeof(TEvent).Name] = typeof(TEvent));

        return builder;
    }
}
