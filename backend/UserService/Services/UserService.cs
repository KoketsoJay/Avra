using Avra.ServiceDefaults.Security;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;

namespace UserService.Services;

/// <summary>
/// Service for user authentication and account management
/// </summary>
public interface IUserService
{
    Task<AuthResponse?> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponse?> LoginAsync(LoginDto loginDto);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<IReadOnlyList<UserDto>> SearchUsersAsync(string? term, int limit = 20);
    Task<UserDto> UpdateProfileAsync(string userId, UpdateProfileDto updateDto);
    Task<bool> DeleteUserAsync(string userId);
}

/// <summary>
/// Service for managing user memberships
/// </summary>
public interface IMembershipService
{
    Task<IReadOnlyList<MembershipPlanDto>> GetPlansAsync();
    Task<MembershipPlanDto?> SetPlanDiscountAsync(string planId, decimal discountPercentage);
    Task<EffectiveDiscountDto> GetEffectiveDiscountAsync(string userId);
    Task<IReadOnlyList<MemberDiscountDto>> GetMemberDiscountsAsync();
    Task<MemberDiscountDto?> GrantMemberDiscountAsync(string userId, GrantMemberDiscountDto grant, string grantedBy);
    Task<bool> RevokeMemberDiscountAsync(string userId);
    Task<UserMembershipDto> CreateMembershipAsync(string userId, string planName);
    Task<UserMembershipDto?> GetMembershipByUserIdAsync(string userId);
    Task<UserMembershipDto> UpdateMembershipStatusAsync(int membershipId, MembershipStatus status);
    Task<bool> CancelMembershipAsync(int membershipId);
}

/// <summary>
/// Implementation of IUserService
/// </summary>
public class UserService : IUserService
{
    private readonly UserDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        UserDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<UserService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterDto registerDto)
    {
        ArgumentNullException.ThrowIfNull(registerDto);

        _logger.LogInformation("Registering new user with email: {Email}", registerDto.Email);

        if (string.IsNullOrWhiteSpace(registerDto.Password) || registerDto.Password.Length < 8)
        {
            throw new InvalidOperationException("Password must be at least 8 characters long");
        }

        // Check if user already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("User already exists with email: {Email}", registerDto.Email);
            return null;
        }

        var user = new User
        {
            Email = registerDto.Email,
            PasswordHash = _passwordHasher.Hash(registerDto.Password),
            Role = AvraRoles.Customer,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            PhoneNumber = registerDto.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User registered successfully with id: {UserId}", user.Id);

        var (accessToken, expiresIn) = _tokenService.CreateAccessToken(user);
        return new AuthResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            User = MapToDto(user)
        };
    }

    public async Task<AuthResponse?> LoginAsync(LoginDto loginDto)
    {
        ArgumentNullException.ThrowIfNull(loginDto);

        _logger.LogInformation("User login attempt with email: {Email}", loginDto.Email);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        // The same null result covers unknown email, wrong password and disabled account, so a
        // caller cannot use the response to discover which emails are registered.
        if (user is null || !user.IsActive || !_passwordHasher.Verify(loginDto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for email: {Email}", loginDto.Email);
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var (accessToken, expiresIn) = _tokenService.CreateAccessToken(user);
        return new AuthResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            User = MapToDto(user)
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        _logger.LogInformation("Fetching user with id: {UserId}", userId);

        var user = await _context.Users.FindAsync(userId);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        _logger.LogInformation("Fetching user with email: {Email}", email);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        return user == null ? null : MapToDto(user);
    }

    public async Task<IReadOnlyList<UserDto>> SearchUsersAsync(string? term, int limit = 20)
    {
        var query = _context.Users.AsNoTracking().Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var trimmed = term.Trim();
            query = query.Where(u =>
                u.Email.Contains(trimmed) ||
                u.FirstName.Contains(trimmed) ||
                u.LastName.Contains(trimmed));
        }

        var users = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync();

        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto> UpdateProfileAsync(string userId, UpdateProfileDto updateDto)
    {
        _logger.LogInformation("Updating profile for user: {UserId}", userId);

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }

        user.FirstName = updateDto.FirstName;
        user.LastName = updateDto.LastName;
        user.PhoneNumber = updateDto.PhoneNumber;
        user.Avatar = updateDto.Avatar;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Profile updated successfully for user: {UserId}", userId);

        return MapToDto(user);
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        _logger.LogInformation("Deleting user with id: {UserId}", userId);

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User deleted successfully with id: {UserId}", userId);

        return true;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            Avatar = user.Avatar,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}

/// <summary>
/// Implementation of IMembershipService
/// </summary>
public class MembershipService : IMembershipService
{
    private readonly UserDbContext _context;
    private readonly ILogger<MembershipService> _logger;

    public MembershipService(UserDbContext context, ILogger<MembershipService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MembershipPlanDto>> GetPlansAsync()
    {
        return await _context.MembershipPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => new MembershipPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                Tagline = p.Tagline,
                MonthlyPrice = p.MonthlyPrice,
                DiscountPercentage = p.DiscountPercentage,
                IsHighlighted = p.IsHighlighted,
                Features = p.Features.OrderBy(f => f.SortOrder).Select(f => f.Text).ToList()
            })
            .ToListAsync();
    }

    public async Task<MembershipPlanDto?> SetPlanDiscountAsync(string planId, decimal discountPercentage)
    {
        var plan = await _context.MembershipPlans
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.Id == planId);

        if (plan is null)
        {
            return null;
        }

        plan.DiscountPercentage = NormalisePercentage(discountPercentage);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Discount for plan {PlanId} set to {Percentage}%", planId, plan.DiscountPercentage);

        return new MembershipPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Tagline = plan.Tagline,
            MonthlyPrice = plan.MonthlyPrice,
            DiscountPercentage = plan.DiscountPercentage,
            IsHighlighted = plan.IsHighlighted,
            Features = plan.Features.OrderBy(f => f.SortOrder).Select(f => f.Text).ToList()
        };
    }

    public async Task<EffectiveDiscountDto> GetEffectiveDiscountAsync(string userId)
    {
        var now = DateTime.UtcNow;

        // A hand-granted override beats the tier rate, so goodwill can exceed what the plan gives.
        var over = await _context.MemberDiscounts
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (over is not null && over.IsLive(now) && over.Percentage > 0)
        {
            return new EffectiveDiscountDto
            {
                UserId = userId,
                Percentage = over.Percentage,
                Source = DiscountSource.Override,
                Label = "Member discount",
                ExpiresAt = over.ExpiresAt,
            };
        }

        var membership = await _context.UserMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Status == MembershipStatus.Active);

        if (membership is not null)
        {
            var plan = await _context.MembershipPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name == membership.PlanName && p.IsActive);

            if (plan is not null && plan.DiscountPercentage > 0)
            {
                return new EffectiveDiscountDto
                {
                    UserId = userId,
                    Percentage = plan.DiscountPercentage,
                    Source = DiscountSource.Tier,
                    Label = $"{plan.Name} member discount",
                    ExpiresAt = membership.EndDate,
                };
            }
        }

        return new EffectiveDiscountDto { UserId = userId, Percentage = 0m, Source = DiscountSource.None, Label = string.Empty };
    }

    public async Task<IReadOnlyList<MemberDiscountDto>> GetMemberDiscountsAsync()
    {
        var now = DateTime.UtcNow;

        var rows = await _context.MemberDiscounts
            .AsNoTracking()
            .Join(_context.Users.AsNoTracking(), d => d.UserId, u => u.Id, (d, u) => new { Discount = d, User = u })
            .OrderByDescending(x => x.Discount.UpdatedAt ?? x.Discount.CreatedAt)
            .ToListAsync();

        var memberships = await _context.UserMemberships
            .AsNoTracking()
            .Where(m => m.Status == MembershipStatus.Active)
            .ToListAsync();

        return rows.Select(x => new MemberDiscountDto
        {
            UserId = x.User.Id,
            Email = x.User.Email,
            FullName = $"{x.User.FirstName} {x.User.LastName}".Trim(),
            PlanName = memberships.Find(m => m.UserId == x.User.Id)?.PlanName,
            Percentage = x.Discount.Percentage,
            Reason = x.Discount.Reason,
            ExpiresAt = x.Discount.ExpiresAt,
            IsExpired = !x.Discount.IsLive(now),
            CreatedAt = x.Discount.CreatedAt,
            UpdatedAt = x.Discount.UpdatedAt,
        }).ToList();
    }

    public async Task<MemberDiscountDto?> GrantMemberDiscountAsync(string userId, GrantMemberDiscountDto grant, string grantedBy)
    {
        ArgumentNullException.ThrowIfNull(grant);

        var user = await _context.Users.FindAsync(userId);
        if (user is null)
        {
            return null;
        }

        var percentage = NormalisePercentage(grant.Percentage);
        var existing = await _context.MemberDiscounts.FirstOrDefaultAsync(d => d.UserId == userId);

        if (existing is null)
        {
            existing = new MemberDiscount
            {
                UserId = userId,
                Percentage = percentage,
                Reason = grant.Reason,
                ExpiresAt = grant.ExpiresAt,
                GrantedBy = grantedBy,
            };
            _context.MemberDiscounts.Add(existing);
        }
        else
        {
            existing.Percentage = percentage;
            existing.Reason = grant.Reason;
            existing.ExpiresAt = grant.ExpiresAt;
            existing.GrantedBy = grantedBy;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Discount override of {Percentage}% granted to {UserId} by {GrantedBy}", percentage, userId, grantedBy);

        var membership = await _context.UserMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Status == MembershipStatus.Active);

        return new MemberDiscountDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            PlanName = membership?.PlanName,
            Percentage = existing.Percentage,
            Reason = existing.Reason,
            ExpiresAt = existing.ExpiresAt,
            IsExpired = !existing.IsLive(DateTime.UtcNow),
            CreatedAt = existing.CreatedAt,
            UpdatedAt = existing.UpdatedAt,
        };
    }

    public async Task<bool> RevokeMemberDiscountAsync(string userId)
    {
        var existing = await _context.MemberDiscounts.FirstOrDefaultAsync(d => d.UserId == userId);
        if (existing is null)
        {
            return false;
        }

        _context.MemberDiscounts.Remove(existing);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Discount override revoked for {UserId}", userId);

        return true;
    }

    /// <summary>Keeps a stored rate inside 0-100 so a bad input can never produce a negative price.</summary>
    private static decimal NormalisePercentage(decimal percentage) => Math.Round(Math.Clamp(percentage, 0m, 100m), 2);

    public async Task<UserMembershipDto> CreateMembershipAsync(string userId, string planName)
    {
        _logger.LogInformation("Creating membership for user: {UserId}, Plan: {PlanName}", userId, planName);

        var membership = new UserMembership
        {
            UserId = userId,
            PlanName = planName,
            Status = MembershipStatus.Active,
            StartDate = DateTime.UtcNow,
            AutoRenew = true
        };

        _context.UserMemberships.Add(membership);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Membership created successfully with id: {MembershipId}", membership.Id);

        return MapToDto(membership);
    }

    public async Task<UserMembershipDto?> GetMembershipByUserIdAsync(string userId)
    {
        _logger.LogInformation("Fetching membership for user: {UserId}", userId);

        var membership = await _context.UserMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Status == MembershipStatus.Active);

        return membership == null ? null : MapToDto(membership);
    }

    public async Task<UserMembershipDto> UpdateMembershipStatusAsync(int membershipId, MembershipStatus status)
    {
        _logger.LogInformation("Updating membership status for id: {MembershipId}, Status: {Status}", membershipId, status);

        var membership = await _context.UserMemberships.FindAsync(membershipId);
        if (membership == null)
        {
            throw new KeyNotFoundException($"Membership with id {membershipId} not found");
        }

        membership.Status = status;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Membership status updated successfully for id: {MembershipId}", membershipId);

        return MapToDto(membership);
    }

    public async Task<bool> CancelMembershipAsync(int membershipId)
    {
        _logger.LogInformation("Cancelling membership with id: {MembershipId}", membershipId);

        var membership = await _context.UserMemberships.FindAsync(membershipId);
        if (membership == null)
        {
            return false;
        }

        membership.Status = MembershipStatus.Cancelled;
        membership.EndDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Membership cancelled successfully with id: {MembershipId}", membershipId);

        return true;
    }

    private static UserMembershipDto MapToDto(UserMembership membership)
    {
        return new UserMembershipDto
        {
            Id = membership.Id,
            UserId = membership.UserId,
            PlanName = membership.PlanName,
            Status = membership.Status,
            StartDate = membership.StartDate,
            EndDate = membership.EndDate,
            AutoRenew = membership.AutoRenew
        };
    }
}
