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
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public bool AutoRenew { get; set; } = true;

    public User? User { get; set; }
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
    Expired
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
