using Avra.ServiceDefaults.Security;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using UserService.Services;

namespace UserService.Data;

/// <summary>
/// Creates the first administrator account from configuration so the admin portal can be reached
/// on a fresh database. Credentials are never hard-coded — the seeder is a no-op unless both
/// AVRA_Admin__Email and AVRA_Admin__Password are supplied.
/// </summary>
public static class AdministratorSeeder
{
    public static async Task SeedAdministratorAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        var email = app.Configuration["Admin:Email"];
        var password = app.Configuration["Admin:Password"];

        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<UserDbContext>>();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Admin bootstrap skipped; set AVRA_Admin__Email and AVRA_Admin__Password to create one");
            return;
        }

        if (password.Length < 12)
        {
            logger.LogError("Admin bootstrap rejected; the configured password must be at least 12 characters");
            return;
        }

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (existing is not null)
            {
                // Only ever promotes; an existing password is left untouched.
                if (!string.Equals(existing.Role, AvraRoles.Administrator, StringComparison.OrdinalIgnoreCase))
                {
                    existing.Role = AvraRoles.Administrator;
                    existing.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Promoted existing account to administrator");
                }

                return;
            }

            context.Users.Add(new User
            {
                Email = email,
                PasswordHash = hasher.Hash(password),
                Role = AvraRoles.Administrator,
                FirstName = app.Configuration["Admin:FirstName"] ?? "AVRA",
                LastName = app.Configuration["Admin:LastName"] ?? "Administrator",
                IsActive = true,
            });

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Administrator account created from configuration");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Administrator bootstrap failed");
        }
    }
}
