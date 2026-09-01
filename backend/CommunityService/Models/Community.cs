namespace CommunityService.Models;

/// <summary>
/// Represents a community circle
/// </summary>
public class Community
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CreatorId { get; set; } = string.Empty;
    public int MemberCount { get; set; } = 1;
    public string? ImageUrl { get; set; }
    public bool IsPublic { get; set; } = true;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CommunityMember> Members { get; set; } = new List<CommunityMember>();
    public ICollection<CommunityPost> Posts { get; set; } = new List<CommunityPost>();
}

/// <summary>
/// Represents community membership
/// </summary>
public class CommunityMember
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CommunityId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public MemberRole Role { get; set; } = MemberRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public Community? Community { get; set; }
}

/// <summary>
/// Represents a community post/discussion
/// </summary>
public class CommunityPost
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CommunityId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Community? Community { get; set; }
}

/// <summary>
/// Member role enumeration
/// </summary>
public enum MemberRole
{
    Member,
    Moderator,
    Admin
}

/// <summary>
/// DTO for creating community
/// </summary>
public class CreateCommunityDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CreatorId { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsPublic { get; set; } = true;
}

/// <summary>
/// DTO for community response
/// </summary>
public class CommunityDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CreatorId { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsPublic { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for community member response
/// </summary>
public class CommunityMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string CommunityId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public MemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}

/// <summary>
/// DTO for creating community post
/// </summary>
public class CreatePostDto
{
    public string CommunityId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// DTO for community post response
/// </summary>
public class CommunityPostDto
{
    public string Id { get; set; } = string.Empty;
    public string CommunityId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
