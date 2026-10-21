using Avra.ServiceDefaults;
using CommunityService.Data;
using CommunityService.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddAvraServiceDefaults("community-service");

builder.Services.AddDbContext<CommunityDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

builder.Services.AddHealthChecks().AddDbContextCheck<CommunityDbContext>("community-db");

builder.Services.AddScoped<ICommunityService, CommunityService.Services.CommunityService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IPostService, PostService>();

var app = builder.Build();

app.UseAvraServiceDefaults();
await app.BaselineMigrationsForLegacyDatabaseAsync();
await app.InitializeDatabaseAsync<CommunityDbContext>();
await app.EnsureLatestCommunitySchemaAsync();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "community-service terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
