using System.Data;
using Microsoft.EntityFrameworkCore;

namespace CommunityService.Data;

/// <summary>
/// Applies forward-compatible schema updates when the service is running without EF migrations.
/// This keeps local development databases usable after model evolution.
/// </summary>
public static class CommunitySchemaBootstrapper
{
    public static async Task BaselineMigrationsForLegacyDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CommunityDbContext>>();

        try
        {
            var migrations = context.Database.GetMigrations().ToList();
            if (migrations.Count == 0)
            {
                return;
            }

            var hasHistory = await TableExistsAsync(context, "__EFMigrationsHistory", cancellationToken);
            if (hasHistory)
            {
                return;
            }

            var hasLegacySchema = await TableExistsAsync(context, "Communities", cancellationToken);
            if (!hasLegacySchema)
            {
                return;
            }

            var productVersion = typeof(DbContext).Assembly.GetName().Version?.ToString(3) ?? "8.0.11";

            const string createHistorySql = """
IF OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
""";

            await context.Database.ExecuteSqlRawAsync(createHistorySql, cancellationToken);

            foreach (var migration in migrations)
            {
                await context.Database.ExecuteSqlInterpolatedAsync($"""
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = {migration})
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({migration}, {productVersion});
END;
""", cancellationToken);
            }

            logger.LogInformation("Baselined {Count} CommunityService migrations for legacy development database", migrations.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to baseline CommunityService migrations for legacy development database");
        }
    }

    public static async Task EnsureLatestCommunitySchemaAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CommunityDbContext>>();

        const string sql = """
IF COL_LENGTH('CommunityMembers', 'LeftAt') IS NULL
BEGIN
    ALTER TABLE [CommunityMembers] ADD [LeftAt] datetime2 NULL;
END;

IF COL_LENGTH('CommunityPosts', 'IsPinned') IS NULL
BEGIN
    ALTER TABLE [CommunityPosts] ADD [IsPinned] bit NOT NULL CONSTRAINT [DF_CommunityPosts_IsPinned] DEFAULT(0);
END;

IF COL_LENGTH('CommunityPosts', 'IsHidden') IS NULL
BEGIN
    ALTER TABLE [CommunityPosts] ADD [IsHidden] bit NOT NULL CONSTRAINT [DF_CommunityPosts_IsHidden] DEFAULT(0);
END;

IF COL_LENGTH('CommunityPosts', 'HiddenReason') IS NULL
BEGIN
    ALTER TABLE [CommunityPosts] ADD [HiddenReason] nvarchar(500) NULL;
END;

IF COL_LENGTH('CommunityPosts', 'HiddenAt') IS NULL
BEGIN
    ALTER TABLE [CommunityPosts] ADD [HiddenAt] datetime2 NULL;
END;

IF OBJECT_ID(N'[CommunityPostReactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [CommunityPostReactions] (
        [Id] nvarchar(450) NOT NULL,
        [PostId] nvarchar(450) NOT NULL,
        [UserId] nvarchar(255) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CommunityPostReactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CommunityPostReactions_CommunityPosts_PostId] FOREIGN KEY ([PostId]) REFERENCES [CommunityPosts]([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CommunityPostReactions_PostId_UserId' AND object_id = OBJECT_ID(N'[CommunityPostReactions]'))
BEGIN
    CREATE UNIQUE INDEX [IX_CommunityPostReactions_PostId_UserId] ON [CommunityPostReactions] ([PostId], [UserId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CommunityPostReactions_CreatedAt' AND object_id = OBJECT_ID(N'[CommunityPostReactions]'))
BEGIN
    CREATE INDEX [IX_CommunityPostReactions_CreatedAt] ON [CommunityPostReactions] ([CreatedAt]);
END;

IF OBJECT_ID(N'[CommunityComments]', N'U') IS NULL
BEGIN
    CREATE TABLE [CommunityComments] (
        [Id] nvarchar(450) NOT NULL,
        [PostId] nvarchar(450) NOT NULL,
        [CommunityId] nvarchar(255) NOT NULL,
        [UserId] nvarchar(255) NOT NULL,
        [UserName] nvarchar(200) NOT NULL,
        [UserAvatar] nvarchar(max) NULL,
        [Content] nvarchar(3000) NOT NULL,
        [IsHidden] bit NOT NULL CONSTRAINT [DF_CommunityComments_IsHidden] DEFAULT(0),
        [HiddenReason] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_CommunityComments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CommunityComments_CommunityPosts_PostId] FOREIGN KEY ([PostId]) REFERENCES [CommunityPosts]([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CommunityComments_PostId' AND object_id = OBJECT_ID(N'[CommunityComments]'))
BEGIN
    CREATE INDEX [IX_CommunityComments_PostId] ON [CommunityComments] ([PostId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CommunityComments_CommunityId' AND object_id = OBJECT_ID(N'[CommunityComments]'))
BEGIN
    CREATE INDEX [IX_CommunityComments_CommunityId] ON [CommunityComments] ([CommunityId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CommunityComments_UserId' AND object_id = OBJECT_ID(N'[CommunityComments]'))
BEGIN
    CREATE INDEX [IX_CommunityComments_UserId] ON [CommunityComments] ([UserId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CommunityComments_CreatedAt' AND object_id = OBJECT_ID(N'[CommunityComments]'))
BEGIN
    CREATE INDEX [IX_CommunityComments_CreatedAt] ON [CommunityComments] ([CreatedAt]);
END;

IF OBJECT_ID(N'[CommunityReports]', N'U') IS NULL
BEGIN
    CREATE TABLE [CommunityReports] (
        [Id] nvarchar(450) NOT NULL,
        [CommunityId] nvarchar(255) NOT NULL,
        [PostId] nvarchar(255) NULL,
        [CommentId] nvarchar(255) NULL,
        [ReportedByUserId] nvarchar(255) NOT NULL,
        [ReportedByUserName] nvarchar(200) NOT NULL,
        [Reason] nvarchar(240) NOT NULL,
        [Details] nvarchar(2000) NULL,
        [Status] int NOT NULL CONSTRAINT [DF_CommunityReports_Status] DEFAULT(0),
        [ResolutionNotes] nvarchar(1000) NULL,
        [ReviewedByUserId] nvarchar(255) NULL,
        [ReviewedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CommunityReports] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CommunityReports_Status' AND object_id = OBJECT_ID(N'[CommunityReports]'))
BEGIN
    CREATE INDEX [IX_CommunityReports_Status] ON [CommunityReports] ([Status]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CommunityReports_CreatedAt' AND object_id = OBJECT_ID(N'[CommunityReports]'))
BEGIN
    CREATE INDEX [IX_CommunityReports_CreatedAt] ON [CommunityReports] ([CreatedAt]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CommunityReports_CommunityId' AND object_id = OBJECT_ID(N'[CommunityReports]'))
BEGIN
    CREATE INDEX [IX_CommunityReports_CommunityId] ON [CommunityReports] ([CommunityId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CommunityReports_PostId' AND object_id = OBJECT_ID(N'[CommunityReports]'))
BEGIN
    CREATE INDEX [IX_CommunityReports_PostId] ON [CommunityReports] ([PostId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CommunityReports_CommentId' AND object_id = OBJECT_ID(N'[CommunityReports]'))
BEGIN
    CREATE INDEX [IX_CommunityReports_CommentId] ON [CommunityReports] ([CommentId]);
END;
""";

        try
        {
            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            logger.LogInformation("Applied CommunityService compatibility schema updates");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed applying CommunityService compatibility schema updates");
        }
    }

    private static async Task<bool> TableExistsAsync(CommunityDbContext context, string tableName, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;

        if (openedHere)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.tables WHERE [name] = @tableName) THEN 1 ELSE 0 END";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is int intResult && intResult == 1;
        }
        finally
        {
            if (openedHere)
            {
                await connection.CloseAsync();
            }
        }
    }
}
