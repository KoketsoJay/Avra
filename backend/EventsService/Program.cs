using Avra.ServiceDefaults;
using EventsService.Data;
using EventsService.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddAvraServiceDefaults("events-service");

builder.Services.AddDbContext<EventsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

builder.Services.AddHealthChecks().AddDbContextCheck<EventsDbContext>("events-db");

builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();

var app = builder.Build();

app.UseAvraServiceDefaults();
await app.InitializeDatabaseAsync<EventsDbContext>();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "events-service terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
