namespace CommunityService.Models;

/// <summary>
/// Represents a community circle.
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
/// Represents community membership.
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
    public DateTime? LeftAt { get; set; }
    public bool IsActive { get; set; } = true;

    public Community? Community { get; set; }
}

/// <summary>
/// Represents a community post/discussion.
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
    public bool IsPinned { get; set; }
    public bool IsHidden { get; set; }
    public string? HiddenReason { get; set; }
    public DateTime? HiddenAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Community? Community { get; set; }
    public ICollection<CommunityPostReaction> Reactions { get; set; } = new List<CommunityPostReaction>();
    public ICollection<CommunityComment> Comments { get; set; } = new List<CommunityComment>();
}

/// <summary>
/// Represents a per-user reaction to a post.
/// </summary>
public class CommunityPostReaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PostId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CommunityPost? Post { get; set; }
}

/// <summary>
/// Represents a comment inside a community post.
/// </summary>
public class CommunityComment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PostId { get; set; } = string.Empty;
    public string CommunityId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public string? HiddenReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public CommunityPost? Post { get; set; }
}

/// <summary>
/// A moderation report against post/comment content.
/// </summary>
public class CommunityReport
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CommunityId { get; set; } = string.Empty;
    public string? PostId { get; set; }
    public string? CommentId { get; set; }
    public string ReportedByUserId { get; set; } = string.Empty;
    public string ReportedByUserName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Open;
    public string? ResolutionNotes { get; set; }
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Member role enumeration.
/// </summary>
public enum MemberRole
{
    Member,
    Moderator,
    Admin
}

/// <summary>
/// Report status enumeration.
/// </summary>
public enum ReportStatus
{
    Open,
    Dismissed,
    Actioned
}

/// <summary>
/// DTO for creating community.
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
/// DTO for updating community.
/// </summary>
public class UpdateCommunityDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsPublic { get; set; } = true;
}

/// <summary>
/// DTO for community response.
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
/// DTO for updating status.
/// </summary>
public class UpdateCommunityStatusDto
{
    public string Status { get; set; } = "Active";
}

/// <summary>
/// DTO for community member response.
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
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for listing memberships for one user.
/// </summary>
public class CommunityMembershipDto
{
    public string CommunityId { get; set; } = string.Empty;
    public string CommunityName { get; set; } = string.Empty;
    public string CommunityCategory { get; set; } = string.Empty;
    public string? CommunityImageUrl { get; set; }
    public MemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}

/// <summary>
/// DTO for changing a member's role.
/// </summary>
public class UpdateMemberRoleDto
{
    public MemberRole Role { get; set; } = MemberRole.Member;
}

/// <summary>
/// DTO for creating community post.
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
/// DTO for editing community post.
/// </summary>
public class UpdatePostDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// DTO for community post response.
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
    public bool IsPinned { get; set; }
    public bool IsHidden { get; set; }
    public string? HiddenReason { get; set; }
    public bool LikedByCurrentUser { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating comment.
/// </summary>
public class CreateCommentDto
{
    public string PostId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// DTO for updating comment.
/// </summary>
public class UpdateCommentDto
{
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// DTO for comment response.
/// </summary>
public class CommunityCommentDto
{
    public string Id { get; set; } = string.Empty;
    public string PostId { get; set; } = string.Empty;
    public string CommunityId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public string? HiddenReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for changing content visibility.
/// </summary>
public class SetContentVisibilityDto
{
    public bool Hidden { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for creating a report.
/// </summary>
public class CreateReportDto
{
    public string CommunityId { get; set; } = string.Empty;
    public string? PostId { get; set; }
    public string? CommentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
}

/// <summary>
/// DTO for report response.
/// </summary>
public class CommunityReportDto
{
    public string Id { get; set; } = string.Empty;
    public string CommunityId { get; set; } = string.Empty;
    public string? PostId { get; set; }
    public string? CommentId { get; set; }
    public string ReportedByUserId { get; set; } = string.Empty;
    public string ReportedByUserName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public ReportStatus Status { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for resolving a report.
/// </summary>
public class ResolveReportDto
{
    public ReportStatus Status { get; set; } = ReportStatus.Actioned;
    public string? ResolutionNotes { get; set; }
}
