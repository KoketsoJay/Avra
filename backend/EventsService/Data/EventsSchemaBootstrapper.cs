using Microsoft.EntityFrameworkCore;

namespace EventsService.Data;

/// <summary>
/// Applies forward-compatible schema updates for local databases that were created
/// before the latest model fields existed.
/// </summary>
public static class EventsSchemaBootstrapper
{
    public static async Task EnsureLatestEventsSchemaAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventsDbContext>>();

        const string addMembersOnlyColumnSql = """
IF OBJECT_ID(N'[Events]', N'U') IS NOT NULL AND COL_LENGTH('Events', 'IsMembersOnly') IS NULL
BEGIN
    ALTER TABLE [Events] ADD [IsMembersOnly] bit NOT NULL CONSTRAINT [DF_Events_IsMembersOnly] DEFAULT(0);
END;
""";

        const string createEventTicketsSql = """
IF OBJECT_ID(N'[EventTickets]', N'U') IS NULL
BEGIN
    CREATE TABLE [EventTickets] (
        [Id] nvarchar(255) NOT NULL,
        [RegistrationId] nvarchar(450) NOT NULL,
        [EventId] nvarchar(255) NOT NULL,
        [UserId] nvarchar(255) NOT NULL,
        [TicketNumber] nvarchar(64) NOT NULL,
        [QrCodePayload] nvarchar(1024) NOT NULL,
        [QrCodePngBase64] nvarchar(max) NOT NULL,
        [IssuedAt] datetime2 NOT NULL,
        [IsCancelled] bit NOT NULL CONSTRAINT [DF_EventTickets_IsCancelled] DEFAULT(0),
        CONSTRAINT [PK_EventTickets] PRIMARY KEY ([Id])
    );
END;
""";

        const string ensureEventTicketsIndexesSql = """
IF OBJECT_ID(N'[EventTickets]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_EventTickets_RegistrationId' AND [object_id] = OBJECT_ID(N'[EventTickets]'))
    BEGIN
        CREATE INDEX [IX_EventTickets_RegistrationId] ON [EventTickets] ([RegistrationId]);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_EventTickets_EventId' AND [object_id] = OBJECT_ID(N'[EventTickets]'))
    BEGIN
        CREATE INDEX [IX_EventTickets_EventId] ON [EventTickets] ([EventId]);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_EventTickets_UserId' AND [object_id] = OBJECT_ID(N'[EventTickets]'))
    BEGIN
        CREATE INDEX [IX_EventTickets_UserId] ON [EventTickets] ([UserId]);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_EventTickets_TicketNumber' AND [object_id] = OBJECT_ID(N'[EventTickets]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_EventTickets_TicketNumber] ON [EventTickets] ([TicketNumber]);
    END;
END;
""";

        try
        {
            await context.Database.ExecuteSqlRawAsync(addMembersOnlyColumnSql, cancellationToken);
            await context.Database.ExecuteSqlRawAsync(createEventTicketsSql, cancellationToken);
            await context.Database.ExecuteSqlRawAsync(ensureEventTicketsIndexesSql, cancellationToken);
            logger.LogInformation("Applied EventsService compatibility schema updates");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed applying EventsService compatibility schema updates");
        }
    }
}