using Dapper;
using Microsoft.Data.SqlClient;
using OrderingService.Domain.AggregatesModel.BuyerAggregate;
using OrderingService.Domain.AggregatesModel.OrderAggregate;

namespace OrderingService.Application.Queries;

/// <summary>
/// Dapper-backed read model over the same Azure SQL database the write model uses.
/// </summary>
public class OrderQueries(string connectionString) : IOrderQueries
{
    private readonly string _connectionString = !string.IsNullOrWhiteSpace(connectionString)
        ? connectionString
        : throw new ArgumentNullException(nameof(connectionString));

    public async Task<OrderViewModel?> GetOrderAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var rows = await connection.QueryAsync<dynamic>(
            """
            SELECT o.[Id] AS ordernumber, o.[OrderDate] AS [date], o.[Description] AS [description],
                   o.[OrderStatusId] AS [statusid],
                   o.[Street] AS [street], o.[City] AS [city], o.[State] AS [state],
                   o.[ZipCode] AS [zipcode], o.[Country] AS [country],
                   o.[Carrier] AS [carrier], o.[TrackingNumber] AS [trackingnumber], o.[ShippedAt] AS [shippedat],
                   oi.[ProductId] AS [productid], oi.[ProductName] AS [productname], oi.[Units] AS [units],
                   oi.[UnitPrice] AS [unitprice], oi.[PictureUrl] AS [pictureurl]
            FROM   [orders] o
                   LEFT JOIN [orderitems] oi ON o.[Id] = oi.[OrderId]
            WHERE  o.[Id] = @id
            """,
            new { id });

        var materialised = rows.ToList();
        return materialised.Count == 0 ? null : MapOrder(materialised);
    }

    public async Task<IEnumerable<OrderSummaryViewModel>> GetOrdersFromUserAsync(string userId)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var rows = await connection.QueryAsync<dynamic>(
            """
            SELECT o.[Id] AS ordernumber, o.[OrderDate] AS [date], o.[OrderStatusId] AS [statusid],
                   SUM(oi.[Units] * oi.[UnitPrice]) AS total
            FROM   [orders] o
                   LEFT JOIN [orderitems] oi ON o.[Id] = oi.[OrderId]
                   LEFT JOIN [buyers] ob ON o.[BuyerId] = ob.[Id]
            WHERE  ob.[IdentityGuid] = @userId
            GROUP BY o.[Id], o.[OrderDate], o.[OrderStatusId]
            ORDER BY o.[Id] DESC
            """,
            new { userId });

        return rows.Select(row => new OrderSummaryViewModel
        {
            OrderNumber = (int)row.ordernumber,
            Date = (DateTime)row.date,
            Status = ((OrderingService.Domain.AggregatesModel.OrderAggregate.OrderStatus)(int)row.statusid).ToString(),
            Total = row.total is null ? 0m : (decimal)row.total,
        }).ToList();
    }

    public async Task<IEnumerable<AdminOrderSummaryViewModel>> GetAllOrdersAsync(string? status)
    {
        // The status filter is resolved to an enum value here so no caller-supplied text ever
        // reaches the query, and an unknown name simply means "no filter".
        int? statusId = Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed)
            ? (int)parsed
            : null;

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var rows = await connection.QueryAsync<dynamic>(
            """
            SELECT o.[Id] AS ordernumber, o.[OrderDate] AS [date], o.[OrderStatusId] AS [statusid],
                   o.[City] AS [city], o.[Country] AS [country],
                   o.[Carrier] AS [carrier], o.[TrackingNumber] AS [trackingnumber], o.[ShippedAt] AS [shippedat],
                   ISNULL(ob.[Name], '') AS [buyername],
                   ISNULL(SUM(oi.[Units] * oi.[UnitPrice]), 0) AS total,
                   ISNULL(SUM(oi.[Units]), 0) AS itemcount
            FROM   [orders] o
                   LEFT JOIN [orderitems] oi ON o.[Id] = oi.[OrderId]
                   LEFT JOIN [buyers] ob ON o.[BuyerId] = ob.[Id]
            WHERE  (@statusId IS NULL OR o.[OrderStatusId] = @statusId)
            GROUP BY o.[Id], o.[OrderDate], o.[OrderStatusId], o.[City], o.[Country],
                     o.[Carrier], o.[TrackingNumber], o.[ShippedAt], ob.[Name]
            ORDER BY o.[Id] DESC
            """,
            new { statusId });

        return rows.Select(row =>
        {
            var orderStatus = (OrderStatus)(int)row.statusid;

            return new AdminOrderSummaryViewModel
            {
                OrderNumber = (int)row.ordernumber,
                Date = (DateTime)row.date,
                Status = orderStatus.ToString(),
                Total = (decimal)row.total,
                ItemCount = (int)row.itemcount,
                BuyerName = (string)row.buyername,
                City = (string)row.city,
                Country = (string)row.country,
                Carrier = row.carrier as string,
                TrackingNumber = row.trackingnumber as string,
                ShippedAt = row.shippedat as DateTime?,
                AwaitingDispatch = orderStatus == OrderStatus.Paid,
            };
        }).ToList();
    }

    public Task<IEnumerable<CardTypeViewModel>> GetCardTypesAsync() =>
        Task.FromResult<IEnumerable<CardTypeViewModel>>(
            Enum.GetValues<CardType>()
                .Select(value => new CardTypeViewModel { Id = (int)value, Name = value.ToString() })
                .ToList());

    private static OrderViewModel MapOrder(List<dynamic> rows)
    {
        var first = rows[0];

        var items = rows
            .Where(row => row.productname is not null)
            .Select(row => new OrderItemViewModel
            {
                ProductId = (int)row.productid,
                ProductName = (string)row.productname,
                Units = (int)row.units,
                UnitPrice = (decimal)row.unitprice,
                PictureUrl = row.pictureurl as string,
            })
            .ToList();

        return new OrderViewModel
        {
            OrderNumber = (int)first.ordernumber,
            Date = (DateTime)first.date,
            Description = first.description as string,
            Status = ((OrderingService.Domain.AggregatesModel.OrderAggregate.OrderStatus)(int)first.statusid)
                .ToString(),
            Street = (string)first.street,
            City = (string)first.city,
            State = (string)first.state,
            ZipCode = (string)first.zipcode,
            Country = (string)first.country,
            Carrier = first.carrier as string,
            TrackingNumber = first.trackingnumber as string,
            ShippedAt = first.shippedat as DateTime?,
            OrderItems = items,
            Total = items.Sum(i => i.UnitPrice * i.Units),
        };
    }
}
