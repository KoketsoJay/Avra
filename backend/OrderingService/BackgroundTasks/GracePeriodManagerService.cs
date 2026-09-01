using Avra.EventBus.Abstractions;
using Avra.EventBus.IntegrationEvents;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace OrderingService.BackgroundTasks;

/// <summary>
/// Settings for the grace period watchdog.
/// </summary>
public class BackgroundTaskOptions
{
    public const string SectionName = "GracePeriodManager";

    /// <summary>Minutes a buyer has to cancel before stock validation starts.</summary>
    public int GracePeriodTime { get; set; } = 1;

    /// <summary>How often submitted orders are re-checked.</summary>
    public int CheckUpdateTime { get; set; } = 15_000;
}

/// <summary>
/// Watches submitted orders and, once the buyer's cancellation window has closed, tells the
/// ordering service to move them on. Modelling the wait as a scheduled job rather than a timer
/// inside the aggregate keeps the domain model free of infrastructure concerns.
/// </summary>
public class GracePeriodManagerService(
    IOptions<BackgroundTaskOptions> options,
    IConfiguration configuration,
    IEventBus eventBus,
    ILogger<GracePeriodManagerService> logger) : BackgroundService
{
    private readonly BackgroundTaskOptions _options = options.Value;
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is not configured");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Grace period manager started with a {GracePeriod} minute window", _options.GracePeriodTime);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckConfirmedGracePeriodOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Grace period check failed; retrying on the next cycle");
            }

            await Task.Delay(_options.CheckUpdateTime, stoppingToken);
        }
    }

    private async Task CheckConfirmedGracePeriodOrdersAsync(CancellationToken cancellationToken)
    {
        var orderIds = await GetConfirmedGracePeriodOrdersAsync(cancellationToken);

        foreach (var orderId in orderIds)
        {
            logger.LogInformation("Grace period elapsed for order {OrderId}", orderId);
            await eventBus.PublishAsync(new GracePeriodConfirmedIntegrationEvent(orderId), cancellationToken);
        }
    }

    private async Task<IEnumerable<int>> GetConfirmedGracePeriodOrdersAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // OrderStatusId 1 is Submitted; DATEDIFF keeps the window evaluation on the server.
        return await connection.QueryAsync<int>(
            """
            SELECT [Id]
            FROM   [orders]
            WHERE  DATEDIFF(minute, [OrderDate], GETUTCDATE()) >= @GracePeriodTime
                   AND [OrderStatusId] = 1
            """,
            new { _options.GracePeriodTime });
    }
}
