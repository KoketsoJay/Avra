using Microsoft.EntityFrameworkCore;

namespace OrderingService.Infrastructure.Idempotency;

/// <summary>
/// Stores accepted request ids in the ordering database so idempotency survives restarts and
/// is shared by every instance of the service.
/// </summary>
public class RequestManager(OrderingContext context) : IRequestManager
{
    public Task<bool> ExistAsync(Guid id) => context.ClientRequests.AnyAsync(r => r.Id == id);

    public async Task CreateRequestForCommandAsync<T>(Guid id)
    {
        if (await ExistAsync(id))
        {
            throw new InvalidOperationException($"Request with {id} already exists");
        }

        context.ClientRequests.Add(new ClientRequest
        {
            Id = id,
            Name = typeof(T).Name,
            Time = DateTime.UtcNow,
        });

        await context.SaveChangesAsync();
    }
}
