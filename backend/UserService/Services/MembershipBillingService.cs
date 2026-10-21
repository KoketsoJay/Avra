using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UserService.Data;
using UserService.Models;

namespace UserService.Services;

public class MembershipBillingOptions
{
    public const string SectionName = "MembershipBilling";

    /// <summary>Length of the free introductory period.</summary>
    public int TrialDays { get; set; } = 30;

    /// <summary>Length of each paid period after the trial.</summary>
    public int BillingPeriodDays { get; set; } = 30;

    /// <summary>How often the worker sweeps for due charges.</summary>
    public int CheckIntervalMs { get; set; } = 30_000;

    /// <summary>Failed attempts tolerated before the membership lapses.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Delay before retrying a declined charge.</summary>
    public int RetryAfterHours { get; set; } = 24;

    /// <summary>
    /// Whether the simulated gateway approves charges. A real deployment replaces the charge call
    /// with a gateway client; the surrounding workflow does not change.
    /// </summary>
    public bool PaymentSucceeded { get; set; } = true;

    public string DeclineReason { get; set; } = "The payment was declined by the issuer.";
}

/// <summary>
/// Converts trials into paid memberships and takes the recurring charge. Runs on the same polling
/// pattern as the ordering service's grace period manager.
/// </summary>
public class MembershipBillingService(
    IServiceScopeFactory scopeFactory,
    IOptions<MembershipBillingOptions> options,
    ILogger<MembershipBillingService> logger) : BackgroundService
{
    private readonly MembershipBillingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Membership billing started: {TrialDays} day trial, sweeping every {Interval}ms",
            _options.TrialDays, _options.CheckIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await BillDueMembershipsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Membership billing sweep failed; retrying on the next cycle");
            }

            await Task.Delay(_options.CheckIntervalMs, stoppingToken);
        }
    }

    private async Task BillDueMembershipsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();

        var now = DateTime.UtcNow;

        var due = await context.UserMemberships
            .Where(m => m.NextBillingDate != null && m.NextBillingDate <= now)
            .Where(m => m.Status == MembershipStatus.Trialing || m.Status == MembershipStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var membership in due)
        {
            await ChargeAsync(context, membership, now, cancellationToken);
        }

        if (due.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ChargeAsync(UserDbContext context, UserMembership membership, DateTime now, CancellationToken cancellationToken)
    {
        var periodStart = membership.NextBillingDate ?? now;
        var periodEnd = periodStart.AddDays(_options.BillingPeriodDays);

        var invoice = new MembershipInvoice
        {
            MembershipId = membership.Id,
            UserId = membership.UserId,
            PlanName = membership.PlanName,
            Amount = membership.MonthlyPrice,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Attempt = membership.FailedAttempts + 1,
            CreatedAt = now,
        };

        var approved = await AuthoriseAsync(membership, cancellationToken);

        if (approved)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.SettledAt = now;

            membership.Status = MembershipStatus.Active;
            membership.TrialEndsAt = null;
            membership.FailedAttempts = 0;
            membership.NextBillingDate = periodEnd;

            logger.LogInformation(
                "Charged {Amount} for {PlanName} on membership {MembershipId}; next bill {NextBillingDate:u}",
                invoice.Amount, membership.PlanName, membership.Id, periodEnd);
        }
        else
        {
            invoice.Status = InvoiceStatus.Failed;
            invoice.FailureReason = _options.DeclineReason;

            membership.FailedAttempts++;

            if (membership.FailedAttempts >= _options.MaxAttempts)
            {
                // Out of retries: withdraw the benefits rather than serving them unpaid.
                membership.Status = MembershipStatus.Lapsed;
                membership.NextBillingDate = null;
                membership.EndDate = now;

                logger.LogWarning(
                    "Membership {MembershipId} lapsed after {Attempts} failed charges",
                    membership.Id, membership.FailedAttempts);
            }
            else
            {
                membership.NextBillingDate = now.AddHours(_options.RetryAfterHours);

                logger.LogWarning(
                    "Charge {Attempt} of {Max} failed for membership {MembershipId}; retrying at {Retry:u}",
                    membership.FailedAttempts, _options.MaxAttempts, membership.Id, membership.NextBillingDate);
            }
        }

        context.MembershipInvoices.Add(invoice);
    }

    /// <summary>The simulated gateway. Replace this with a real charge to go live.</summary>
    private Task<bool> AuthoriseAsync(UserMembership membership, CancellationToken cancellationToken)
    {
        _ = membership;
        _ = cancellationToken;
        return Task.FromResult(_options.PaymentSucceeded);
    }
}
