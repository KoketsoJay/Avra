using Avra.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WellnessService.Data;
using WellnessService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddAvraServiceDefaults("wellness-service");

builder.Services.AddDbContext<WellnessDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

builder.Services.AddHealthChecks().AddDbContextCheck<WellnessDbContext>("wellness-db");

builder.Services.AddScoped<IWellnessPlanService, WellnessPlanService>();
builder.Services.AddScoped<IProgressService, ProgressService>();

var app = builder.Build();

app.UseAvraServiceDefaults();
await app.InitializeDatabaseAsync<WellnessDbContext>();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "wellness-service terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
