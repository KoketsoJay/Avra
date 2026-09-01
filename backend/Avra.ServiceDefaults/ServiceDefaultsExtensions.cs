using System.Text;
using System.Threading.RateLimiting;
using Avra.ServiceDefaults.Errors;
using Avra.ServiceDefaults.Middleware;
using Avra.ServiceDefaults.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

namespace Avra.ServiceDefaults;

/// <summary>
/// Cross-cutting wiring shared by every AVRA service: logging, security, resilience,
/// diagnostics and API conventions. Keeps each service's entry point to its own concerns.
/// </summary>
public static class ServiceDefaultsExtensions
{
    public const string CorsPolicyName = "AvraFrontend";
    public const string AuthRateLimitPolicy = "auth";

    /// <summary>
    /// Registers the shared platform services. Call once from a service's Program.cs.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="serviceName">Short service name used for log file naming and Swagger.</param>
    public static WebApplicationBuilder AddAvraServiceDefaults(this WebApplicationBuilder builder, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        builder.Configuration.AddEnvironmentVariables("AVRA_");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console()
            .WriteTo.File($"logs/{serviceName}-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
            .CreateLogger();

        builder.Host.UseSerilog();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        AddApiConventions(builder, serviceName);
        AddSecurity(builder);
        AddResilience(builder);
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        return builder;
    }

    private static void AddApiConventions(WebApplicationBuilder builder, string serviceName)
    {
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                if (context.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
                {
                    context.ProblemDetails.Extensions["correlationId"] = correlationId;
                }
            });
        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = $"AVRA {serviceName}", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the access token returned by the UserService login endpoint.",
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                }] = Array.Empty<string>(),
            });
        });
    }

    private static void AddSecurity(WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<JwtOptions>()
            .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        string.IsNullOrEmpty(jwt.SigningKey) ? new string('0', 32) : jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(null)
            .AddPolicy(AvraPolicies.AdministratorOnly, policy =>
                policy.RequireAuthenticatedUser().RequireRole(AvraRoles.Administrator));

        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "http://localhost:3000"];

        builder.Services.AddCors(options => options.AddPolicy(CorsPolicyName, policy => policy
            .WithOrigins(allowedOrigins)
            .WithHeaders("Authorization", "Content-Type", CorrelationIdMiddleware.HeaderName)
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));
    }

    private static void AddResilience(WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitsPerMinute", 300),
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            // Credential endpoints get a far tighter budget to blunt brute-force attempts.
            options.AddPolicy(AuthRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitsPerMinute", 10),
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });
    }

    private static string PartitionKey(HttpContext context) =>
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "anonymous";

    /// <summary>
    /// Applies the shared request pipeline. Order matters — do not rearrange casually.
    /// </summary>
    public static WebApplication UseAvraServiceDefaults(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseRouting();
        app.UseCors(CorsPolicyName);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") })
            .AllowAnonymous();
        app.MapHealthChecks("/health/ready").AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Brings the database schema up to date on start-up. Outside Development this only applies
    /// committed migrations; it never creates or reshapes a production schema implicitly.
    /// </summary>
    public static async Task InitializeDatabaseAsync<TContext>(this WebApplication app, CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();

        try
        {
            if (context.Database.GetMigrations().Any())
            {
                await context.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Applied pending migrations for {Context}", typeof(TContext).Name);
            }
            else if (app.Environment.IsDevelopment())
            {
                await context.Database.EnsureCreatedAsync(cancellationToken);
                logger.LogInformation("Created development schema for {Context}", typeof(TContext).Name);
            }
            else
            {
                logger.LogWarning("No migrations found for {Context}; schema management is expected to run out-of-band", typeof(TContext).Name);
            }
        }
        catch (Exception ex)
        {
            // A database that is not reachable yet must not take the service down; readiness reports it.
            logger.LogError(ex, "Database initialisation failed for {Context}", typeof(TContext).Name);
        }
    }
}
