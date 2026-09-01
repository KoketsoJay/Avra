namespace OrderingService.Application.Queries;

/// <summary>
/// The read side of the ordering service. Queries bypass the domain model entirely and project
/// straight from SQL into view models — no aggregates are loaded to render a screen.
/// </summary>
public interface IOrderQueries
{
    Task<OrderViewModel?> GetOrderAsync(int id);

    Task<IEnumerable<OrderSummaryViewModel>> GetOrdersFromUserAsync(string userId);

    /// <summary>
    /// The fulfilment queue: every buyer's orders, newest first, optionally narrowed to one status.
    /// </summary>
    /// <param name="status">Order status name to filter on, or null for all.</param>
    Task<IEnumerable<AdminOrderSummaryViewModel>> GetAllOrdersAsync(string? status);

    Task<IEnumerable<CardTypeViewModel>> GetCardTypesAsync();
}
