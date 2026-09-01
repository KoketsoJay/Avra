using Avra.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Serilog;
using UserService.Data;
using UserService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddAvraServiceDefaults("user-service");

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

builder.Services.AddHealthChecks().AddDbContextCheck<UserDbContext>("user-db");

builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IUserService, UserService.Services.UserService>();
builder.Services.AddScoped<IMembershipService, UserService.Services.MembershipService>();

var app = builder.Build();

app.UseAvraServiceDefaults();
await app.InitializeDatabaseAsync<UserDbContext>();
await app.SeedAdministratorAsync();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "user-service terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
