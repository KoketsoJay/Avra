namespace UserService.Models;

/// <summary>
/// Represents a user account
/// </summary>
public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash with its salt and iteration count embedded. Never the password itself.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Authorisation role issued in the access token.</summary>
    public string Role { get; set; } = "Customer";

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Avatar { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Represents user membership/subscription
/// </summary>
public class UserMembership
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public MembershipStatus Status { get; set; } = MembershipStatus.Trialing;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public bool AutoRenew { get; set; } = true;

    /// <summary>When the free trial stops. Null once the membership has been billed at least once.</summary>
    public DateTime? TrialEndsAt { get; set; }

    /// <summary>When the next charge falls due. Null once the membership is cancelled or lapsed.</summary>
    public DateTime? NextBillingDate { get; set; }

    /// <summary>Price captured at sign-up, so a later tier price change does not alter this member's bill.</summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>Consecutive failed charge attempts for the current billing period.</summary>
    public int FailedAttempts { get; set; }

    public User? User { get; set; }

    /// <summary>True while the member is inside the free period and has not been charged.</summary>
    public bool IsInTrial(DateTime asOf) =>
        Status == MembershipStatus.Trialing && TrialEndsAt is not null && TrialEndsAt > asOf;

    /// <summary>A membership entitles the member to their tier benefits while trialing or active.</summary>
    public bool IsEntitled =>
        Status == MembershipStatus.Trialing || Status == MembershipStatus.Active;
}

/// <summary>
/// One billing attempt against a membership. The ledger is append-only so a member's charge
/// history survives retries and status changes.
/// </summary>
public class MembershipInvoice
{
    public int Id { get; set; }
    public int MembershipId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    /// <summary>The month this invoice covers.</summary>
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public int Attempt { get; set; } = 1;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SettledAt { get; set; }

    public UserMembership? Membership { get; set; }
}

public enum InvoiceStatus
{
    Pending,
    Paid,
    Failed,
}

/// <summary>
/// A membership tier offered on the storefront.
/// </summary>
public class MembershipPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }

    /// <summary>Percentage taken off catalogue prices for members on this tier, 0-100.</summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>Marks the tier rendered as "Most Popular".</summary>
    public bool IsHighlighted { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MembershipPlanFeature> Features { get; set; } = new List<MembershipPlanFeature>();
}

/// <summary>
/// A discount granted to one member, overriding whatever their tier would give them.
/// </summary>
public class MemberDiscount
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>Percentage taken off catalogue prices, 0-100.</summary>
    public decimal Percentage { get; set; }

    /// <summary>Why the override was granted. Shown to administrators, never to the shopper.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>When the override stops applying. Null means it runs until revoked.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Id of the administrator who last set it.</summary>
    public string GrantedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User? User { get; set; }

    public bool IsLive(DateTime asOf) => ExpiresAt is null || ExpiresAt > asOf;
}

/// <summary>
/// A single benefit line listed under a membership tier.
/// </summary>
public class MembershipPlanFeature
{
    public int Id { get; set; }
    public string PlanId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public MembershipPlan? Plan { get; set; }
}

/// <summary>
/// Membership status enumeration
/// </summary>
public enum MembershipStatus
{
    Active,
    Inactive,
    Paused,
    Cancelled,
    Expired,

    /// <summary>Inside the free introductory period; entitled to benefits but not yet billed.</summary>
    Trialing,

    /// <summary>Billing failed repeatedly; benefits withdrawn until the member resubscribes.</summary>
    Lapsed,
}

/// <summary>
/// DTO for user registration
/// </summary>
public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// DTO for user login
/// </summary>
public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO for authentication response
/// </summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public UserDto? User { get; set; }
}

/// <summary>
/// DTO for user profile
/// </summary>
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Avatar { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for updating user profile
/// </summary>
public class UpdateProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Avatar { get; set; }
}

/// <summary>Sets a member's authorisation role. Administrators only.</summary>
public class SetRoleDto
{
    public string Role { get; set; } = string.Empty;
}

/// <summary>Activates or deactivates a member's account. Administrators only.</summary>
public class SetActiveDto
{
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for membership response
/// </summary>
public class UserMembershipDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public MembershipStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool AutoRenew { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public decimal MonthlyPrice { get; set; }
    public bool IsInTrial { get; set; }
}

/// <summary>A member's billing history entry.</summary>
public class MembershipInvoiceDto
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public InvoiceStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SettledAt { get; set; }
}

/// <summary>
/// DTO for a membership tier on the storefront
/// </summary>
public class MembershipPlanDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public bool IsHighlighted { get; set; }
    public IReadOnlyList<string> Features { get; set; } = [];
}

/// <summary>Where a shopper's discount came from.</summary>
public enum DiscountSource
{
    None,
    Tier,
    Override,
}

/// <summary>
/// The discount that actually applies to a shopper right now, after the override and tier
/// rules have been resolved. This is the only number the storefront and checkout should use.
/// </summary>
public class EffectiveDiscountDto
{
    public string UserId { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public DiscountSource Source { get; set; }

    /// <summary>Shopper-facing wording, e.g. "The AVRA Club member discount".</summary>
    public string Label { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }
}

/// <summary>An administrator's view of one member's discount override.</summary>
public class MemberDiscountDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PlanName { get; set; }
    public decimal Percentage { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Sets the rate every member on a tier receives.</summary>
public class SetPlanDiscountDto
{
    public decimal DiscountPercentage { get; set; }
}

/// <summary>Grants or updates one member's override.</summary>
public class GrantMemberDiscountDto
{
    public decimal Percentage { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}
