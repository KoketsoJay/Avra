using Microsoft.EntityFrameworkCore;
using CommunityService.Data;
using CommunityService.Models;

namespace CommunityService.Services;

/// <summary>
/// Service for managing communities
/// </summary>
public interface ICommunityService
{
    Task<IReadOnlyList<CommunityDto>> GetAllCommunitiesAsync(bool includeInactive = false, bool includePrivate = false);
    Task<IReadOnlyList<CommunityDto>> GetCommunitiesByCategoryAsync(string category);
    Task<CommunityDto?> GetCommunityByIdAsync(string communityId, bool includeInactive = true);
    Task<CommunityDto> CreateCommunityAsync(CreateCommunityDto createDto, string creatorId);
    Task<CommunityDto> UpdateCommunityAsync(string communityId, UpdateCommunityDto updateDto, string actorUserId, bool isAdministrator);
    Task<bool> DeleteCommunityAsync(string communityId, string actorUserId, bool isAdministrator);
    Task<CommunityDto> SetStatusAsync(string communityId, string status);
}

/// <summary>
/// Service for managing community members
/// </summary>
public interface IMemberService
{
    Task<CommunityMemberDto> JoinCommunityAsync(string communityId, string userId, string userName, string? userAvatar = null);
    Task<bool> LeaveCommunityAsync(string communityId, string userId);
    Task<IReadOnlyList<CommunityMemberDto>> GetCommunityMembersAsync(string communityId);
    Task<IReadOnlyList<CommunityMembershipDto>> GetMembershipsAsync(string userId);
    Task<CommunityMemberDto?> GetMemberAsync(string communityId, string userId);
    Task<CommunityMemberDto> UpdateMemberRoleAsync(string communityId, string targetUserId, MemberRole newRole, string actorUserId, bool isAdministrator);
    Task<bool> CanModerateAsync(string communityId, string actorUserId, bool isAdministrator);
    Task<bool> IsActiveMemberAsync(string communityId, string userId);
}

/// <summary>
/// Service for managing community posts
/// </summary>
public interface IPostService
{
    Task<IReadOnlyList<CommunityPostDto>> GetCommunityPostsAsync(string communityId, string? currentUserId = null, bool includeHidden = false);
    Task<CommunityPostDto?> GetPostByIdAsync(string postId, string? currentUserId = null);
    Task<CommunityPostDto> CreatePostAsync(CreatePostDto createDto, string authorId, string authorName);
    Task<CommunityPostDto> UpdatePostAsync(string postId, UpdatePostDto updateDto, string actorUserId, bool canModerate);
    Task<bool> DeletePostAsync(string postId, string actorUserId, bool canModerate);
    Task<CommunityPostDto> ToggleLikeAsync(string postId, string userId);
    Task<IReadOnlyList<CommunityCommentDto>> GetPostCommentsAsync(string postId, bool includeHidden = false);
    Task<CommunityCommentDto> CreateCommentAsync(CreateCommentDto createDto, string authorId, string authorName);
    Task<CommunityCommentDto> UpdateCommentAsync(string commentId, UpdateCommentDto updateDto, string actorUserId, bool canModerate);
    Task<bool> DeleteCommentAsync(string commentId, string actorUserId, bool canModerate);
    Task<CommunityPostDto> SetPostHiddenAsync(string postId, bool hidden, string actorUserId, string? reason = null);
    Task<CommunityCommentDto> SetCommentHiddenAsync(string commentId, bool hidden, string actorUserId, string? reason = null);
    Task<CommunityReportDto> ReportContentAsync(CreateReportDto createDto, string reporterUserId, string reporterName);
    Task<IReadOnlyList<CommunityReportDto>> GetReportsAsync(ReportStatus? status = null);
    Task<CommunityReportDto> ResolveReportAsync(string reportId, ResolveReportDto resolveDto, string reviewerUserId);
}

/// <summary>
/// Implementation of ICommunityService
/// </summary>
public class CommunityService : ICommunityService
{
    private readonly CommunityDbContext _context;
    private readonly ILogger<CommunityService> _logger;

    public CommunityService(CommunityDbContext context, ILogger<CommunityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CommunityDto>> GetAllCommunitiesAsync(bool includeInactive = false, bool includePrivate = false)
    {
        _logger.LogInformation("Fetching communities. IncludeInactive={IncludeInactive} IncludePrivate={IncludePrivate}", includeInactive, includePrivate);

        var query = _context.Communities.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(c => c.Status == "Active");
        }

        if (!includePrivate)
        {
            query = query.Where(c => c.IsPublic);
        }

        var communities = await query
            .OrderByDescending(c => c.MemberCount)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();

        return communities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<CommunityDto>> GetCommunitiesByCategoryAsync(string category)
    {
        _logger.LogInformation("Fetching communities by category: {Category}", category);

        var trimmed = category.Trim();
        var communities = await _context.Communities
            .AsNoTracking()
            .Where(c => c.IsPublic
                && c.Status == "Active"
                && EF.Functions.Like(c.Category, $"%{trimmed}%"))
            .OrderByDescending(c => c.MemberCount)
            .ToListAsync();

        return communities.Select(MapToDto).ToList();
    }

    public async Task<CommunityDto?> GetCommunityByIdAsync(string communityId, bool includeInactive = true)
    {
        _logger.LogInformation("Fetching community with id: {CommunityId}", communityId);

        var query = _context.Communities.AsNoTracking().Where(c => c.Id == communityId);
        if (!includeInactive)
        {
            query = query.Where(c => c.Status == "Active");
        }

        var community = await query.FirstOrDefaultAsync();

        return community == null ? null : MapToDto(community);
    }

    public async Task<CommunityDto> CreateCommunityAsync(CreateCommunityDto createDto, string creatorId)
    {
        _logger.LogInformation("Creating new community: {Name}", createDto.Name);

        if (string.IsNullOrWhiteSpace(createDto.Name) || string.IsNullOrWhiteSpace(createDto.Description) || string.IsNullOrWhiteSpace(createDto.Category))
        {
            throw new InvalidOperationException("Name, description and category are required");
        }

        var ownerId = string.IsNullOrWhiteSpace(creatorId) ? createDto.CreatorId : creatorId;
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new InvalidOperationException("A creator user id is required");
        }

        var community = new Community
        {
            Name = createDto.Name.Trim(),
            Description = createDto.Description.Trim(),
            Category = createDto.Category.Trim(),
            CreatorId = ownerId,
            MemberCount = 1,
            ImageUrl = createDto.ImageUrl,
            IsPublic = createDto.IsPublic,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        var ownerMembership = new CommunityMember
        {
            CommunityId = community.Id,
            UserId = ownerId,
            UserName = "Community Owner",
            Role = MemberRole.Admin,
            JoinedAt = DateTime.UtcNow,
            IsActive = true,
        };

        _context.Communities.Add(community);
        _context.CommunityMembers.Add(ownerMembership);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Community created successfully with id: {CommunityId}", community.Id);

        return MapToDto(community);
    }

    public async Task<CommunityDto> UpdateCommunityAsync(string communityId, UpdateCommunityDto updateDto, string actorUserId, bool isAdministrator)
    {
        _logger.LogInformation("Updating community with id: {CommunityId}", communityId);

        var community = await _context.Communities.FindAsync(communityId);
        if (community == null)
        {
            throw new KeyNotFoundException($"Community with id {communityId} not found");
        }

        var canManage = isAdministrator || string.Equals(community.CreatorId, actorUserId, StringComparison.Ordinal);
        if (!canManage)
        {
            throw new UnauthorizedAccessException("You are not allowed to edit this community");
        }

        if (string.IsNullOrWhiteSpace(updateDto.Name) || string.IsNullOrWhiteSpace(updateDto.Description) || string.IsNullOrWhiteSpace(updateDto.Category))
        {
            throw new InvalidOperationException("Name, description and category are required");
        }

        community.Name = updateDto.Name.Trim();
        community.Description = updateDto.Description.Trim();
        community.Category = updateDto.Category.Trim();
        community.ImageUrl = updateDto.ImageUrl;
        community.IsPublic = updateDto.IsPublic;
        community.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Community updated successfully with id: {CommunityId}", communityId);

        return MapToDto(community);
    }

    public async Task<bool> DeleteCommunityAsync(string communityId, string actorUserId, bool isAdministrator)
    {
        _logger.LogInformation("Deleting community with id: {CommunityId}", communityId);

        var community = await _context.Communities.FindAsync(communityId);
        if (community == null)
        {
            return false;
        }

        var canManage = isAdministrator || string.Equals(community.CreatorId, actorUserId, StringComparison.Ordinal);
        if (!canManage)
        {
            throw new UnauthorizedAccessException("You are not allowed to delete this community");
        }

        community.Status = "Inactive";
        community.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Community deleted successfully with id: {CommunityId}", communityId);

        return true;
    }

    public async Task<CommunityDto> SetStatusAsync(string communityId, string status)
    {
        var allowed = new[] { "Active", "Inactive", "Archived" };
        var normalized = allowed.FirstOrDefault(s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase));
        if (normalized is null)
        {
            throw new InvalidOperationException("Status must be Active, Inactive, or Archived");
        }

        var community = await _context.Communities.FindAsync(communityId)
            ?? throw new KeyNotFoundException($"Community with id {communityId} not found");

        community.Status = normalized;
        community.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(community);
    }

    private static CommunityDto MapToDto(Community community)
    {
        return new CommunityDto
        {
            Id = community.Id,
            Name = community.Name,
            Description = community.Description,
            Category = community.Category,
            CreatorId = community.CreatorId,
            MemberCount = community.MemberCount,
            ImageUrl = community.ImageUrl,
            IsPublic = community.IsPublic,
            Status = community.Status,
            CreatedAt = community.CreatedAt
        };
    }
}

/// <summary>
/// Implementation of IMemberService
/// </summary>
public class MemberService : IMemberService
{
    private readonly CommunityDbContext _context;
    private readonly ILogger<MemberService> _logger;

    public MemberService(CommunityDbContext context, ILogger<MemberService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CommunityMemberDto> JoinCommunityAsync(string communityId, string userId, string userName, string? userAvatar = null)
    {
        _logger.LogInformation("Adding member {UserId} to community {CommunityId}", userId, communityId);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(userName))
        {
            throw new InvalidOperationException("A valid user id and user name are required");
        }

        var community = await _context.Communities.FirstOrDefaultAsync(c => c.Id == communityId && c.Status == "Active");
        if (community == null)
        {
            throw new KeyNotFoundException($"Community with id {communityId} not found");
        }

        var existingActive = await _context.CommunityMembers
            .FirstOrDefaultAsync(m => m.CommunityId == communityId && m.UserId == userId && m.IsActive);

        if (existingActive is not null)
        {
            return MapToDto(existingActive);
        }

        var existing = await _context.CommunityMembers
            .OrderByDescending(m => m.JoinedAt)
            .FirstOrDefaultAsync(m => m.CommunityId == communityId && m.UserId == userId);

        CommunityMember member;
        if (existing is null)
        {
            member = new CommunityMember
            {
                CommunityId = communityId,
                UserId = userId,
                UserName = userName,
                UserAvatar = userAvatar,
                Role = MemberRole.Member,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.CommunityMembers.Add(member);
        }
        else
        {
            existing.IsActive = true;
            existing.LeftAt = null;
            existing.UserName = userName;
            existing.UserAvatar = userAvatar;
            member = existing;
        }

        await _context.SaveChangesAsync();
        await RefreshMemberCountAsync(communityId);

        _logger.LogInformation("Member added successfully with id: {MemberId}", member.Id);

        return MapToDto(member);
    }

    public async Task<IReadOnlyList<CommunityMemberDto>> GetCommunityMembersAsync(string communityId)
    {
        _logger.LogInformation("Fetching members for community: {CommunityId}", communityId);

        var members = await _context.CommunityMembers
            .AsNoTracking()
            .Where(m => m.CommunityId == communityId && m.IsActive)
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .ToListAsync();

        return members.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<CommunityMembershipDto>> GetMembershipsAsync(string userId)
    {
        var memberships = await _context.CommunityMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.IsActive)
            .Join(
                _context.Communities.AsNoTracking().Where(c => c.Status == "Active"),
                member => member.CommunityId,
                community => community.Id,
                (member, community) => new CommunityMembershipDto
                {
                    CommunityId = community.Id,
                    CommunityName = community.Name,
                    CommunityCategory = community.Category,
                    CommunityImageUrl = community.ImageUrl,
                    Role = member.Role,
                    JoinedAt = member.JoinedAt,
                })
            .OrderByDescending(x => x.JoinedAt)
            .ToListAsync();

        return memberships;
    }

    public async Task<CommunityMemberDto?> GetMemberAsync(string communityId, string userId)
    {
        var member = await _context.CommunityMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.CommunityId == communityId && m.UserId == userId && m.IsActive);

        return member == null ? null : MapToDto(member);
    }

    public async Task<bool> LeaveCommunityAsync(string communityId, string userId)
    {
        _logger.LogInformation("Removing member {UserId} from community {CommunityId}", userId, communityId);

        var member = await _context.CommunityMembers
            .FirstOrDefaultAsync(m => m.CommunityId == communityId && m.UserId == userId && m.IsActive);

        if (member == null)
        {
            return false;
        }

        member.IsActive = false;
        member.LeftAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await RefreshMemberCountAsync(communityId);

        _logger.LogInformation("Member removed successfully for user {UserId} in community {CommunityId}", userId, communityId);

        return true;
    }

    public async Task<CommunityMemberDto> UpdateMemberRoleAsync(string communityId, string targetUserId, MemberRole newRole, string actorUserId, bool isAdministrator)
    {
        _logger.LogInformation("Updating member role for user {TargetUserId} in community {CommunityId}", targetUserId, communityId);

        var community = await _context.Communities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == communityId)
            ?? throw new KeyNotFoundException($"Community with id {communityId} not found");

        var canManage = await CanModerateAsync(communityId, actorUserId, isAdministrator);
        if (!canManage)
        {
            throw new UnauthorizedAccessException("You are not allowed to manage roles in this community");
        }

        if (!isAdministrator && string.Equals(community.CreatorId, targetUserId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only an administrator can change the creator role");
        }

        var member = await _context.CommunityMembers
            .FirstOrDefaultAsync(m => m.CommunityId == communityId && m.UserId == targetUserId && m.IsActive);

        if (member == null)
        {
            throw new KeyNotFoundException("Member not found in this community");
        }

        member.Role = newRole;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Member role updated successfully for user {TargetUserId} in community {CommunityId}", targetUserId, communityId);

        return MapToDto(member);
    }

    public async Task<bool> CanModerateAsync(string communityId, string actorUserId, bool isAdministrator)
    {
        if (isAdministrator)
        {
            return true;
        }

        var community = await _context.Communities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == communityId);
        if (community is not null && string.Equals(community.CreatorId, actorUserId, StringComparison.Ordinal))
        {
            return true;
        }

        var actorMember = await _context.CommunityMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.CommunityId == communityId && m.UserId == actorUserId && m.IsActive);

        return actorMember is not null && (actorMember.Role == MemberRole.Admin || actorMember.Role == MemberRole.Moderator);
    }

    public async Task<bool> IsActiveMemberAsync(string communityId, string userId)
    {
        return await _context.CommunityMembers
            .AsNoTracking()
            .AnyAsync(m => m.CommunityId == communityId && m.UserId == userId && m.IsActive);
    }

    private async Task RefreshMemberCountAsync(string communityId)
    {
        var count = await _context.CommunityMembers.CountAsync(m => m.CommunityId == communityId && m.IsActive);
        var community = await _context.Communities.FindAsync(communityId);
        if (community is null)
        {
            return;
        }

        if (community.MemberCount != count)
        {
            community.MemberCount = count;
            community.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private static CommunityMemberDto MapToDto(CommunityMember member)
    {
        return new CommunityMemberDto
        {
            Id = member.Id,
            CommunityId = member.CommunityId,
            UserId = member.UserId,
            UserName = member.UserName,
            UserAvatar = member.UserAvatar,
            Role = member.Role,
            JoinedAt = member.JoinedAt,
            IsActive = member.IsActive,
        };
    }
}

/// <summary>
/// Implementation of IPostService
/// </summary>
public class PostService : IPostService
{
    private readonly CommunityDbContext _context;
    private readonly ILogger<PostService> _logger;

    public PostService(CommunityDbContext context, ILogger<PostService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CommunityPostDto>> GetCommunityPostsAsync(string communityId, string? currentUserId = null, bool includeHidden = false)
    {
        _logger.LogInformation("Fetching posts for community: {CommunityId}", communityId);

        var posts = await _context.CommunityPosts
            .AsNoTracking()
            .Where(p => p.CommunityId == communityId && (includeHidden || !p.IsHidden))
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync();

        HashSet<string> liked = new(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(currentUserId) && posts.Count > 0)
        {
            var postIds = posts.Select(p => p.Id).ToList();
            var likedPostIds = await _context.CommunityPostReactions
                .AsNoTracking()
                .Where(r => r.UserId == currentUserId && postIds.Contains(r.PostId))
                .Select(r => r.PostId)
                .ToListAsync();

            liked = new HashSet<string>(likedPostIds, StringComparer.Ordinal);
        }

        return posts.Select(post => MapPostToDto(post, liked.Contains(post.Id))).ToList();
    }

    public async Task<CommunityPostDto?> GetPostByIdAsync(string postId, string? currentUserId = null)
    {
        _logger.LogInformation("Fetching post with id: {PostId}", postId);

        var post = await _context.CommunityPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == postId);
        if (post is null)
        {
            return null;
        }

        var likedByCurrentUser = !string.IsNullOrWhiteSpace(currentUserId)
            && await _context.CommunityPostReactions.AsNoTracking().AnyAsync(r => r.PostId == postId && r.UserId == currentUserId);

        return MapPostToDto(post, likedByCurrentUser);
    }

    public async Task<CommunityPostDto> CreatePostAsync(CreatePostDto createDto, string authorId, string authorName)
    {
        _logger.LogInformation("Creating new post in community: {CommunityId}", createDto.CommunityId);

        if (string.IsNullOrWhiteSpace(createDto.CommunityId)
            || string.IsNullOrWhiteSpace(createDto.Title)
            || string.IsNullOrWhiteSpace(createDto.Content))
        {
            throw new InvalidOperationException("Community, title and content are required");
        }

        if (string.IsNullOrWhiteSpace(authorId) || string.IsNullOrWhiteSpace(authorName))
        {
            throw new InvalidOperationException("A valid authenticated author is required");
        }

        var community = await _context.Communities.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == createDto.CommunityId && c.Status == "Active");

        if (community is null)
        {
            throw new KeyNotFoundException($"Community with id {createDto.CommunityId} not found");
        }

        var isMember = await _context.CommunityMembers.AsNoTracking()
            .AnyAsync(m => m.CommunityId == createDto.CommunityId && m.UserId == authorId && m.IsActive);

        if (!isMember)
        {
            throw new UnauthorizedAccessException("Join this community before creating a post");
        }

        var post = new CommunityPost
        {
            CommunityId = createDto.CommunityId,
            UserId = authorId,
            UserName = authorName,
            Title = createDto.Title.Trim(),
            Content = createDto.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.CommunityPosts.Add(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Post created successfully with id: {PostId}", post.Id);

        return MapPostToDto(post, likedByCurrentUser: false);
    }

    public async Task<CommunityPostDto> UpdatePostAsync(string postId, UpdatePostDto updateDto, string actorUserId, bool canModerate)
    {
        _logger.LogInformation("Updating post with id: {PostId}", postId);

        var post = await _context.CommunityPosts.FindAsync(postId);
        if (post == null)
        {
            throw new KeyNotFoundException($"Post with id {postId} not found");
        }

        if (!string.Equals(post.UserId, actorUserId, StringComparison.Ordinal) && !canModerate)
        {
            throw new UnauthorizedAccessException("You are not allowed to edit this post");
        }

        if (string.IsNullOrWhiteSpace(updateDto.Title) || string.IsNullOrWhiteSpace(updateDto.Content))
        {
            throw new InvalidOperationException("Title and content are required");
        }

        post.Title = updateDto.Title.Trim();
        post.Content = updateDto.Content.Trim();
        post.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Post updated successfully with id: {PostId}", postId);

        return MapPostToDto(post, likedByCurrentUser: false);
    }

    public async Task<bool> DeletePostAsync(string postId, string actorUserId, bool canModerate)
    {
        _logger.LogInformation("Deleting post with id: {PostId}", postId);

        var post = await _context.CommunityPosts.FindAsync(postId);
        if (post == null)
        {
            return false;
        }

        if (!string.Equals(post.UserId, actorUserId, StringComparison.Ordinal) && !canModerate)
        {
            throw new UnauthorizedAccessException("You are not allowed to delete this post");
        }

        _context.CommunityPosts.Remove(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Post deleted successfully with id: {PostId}", postId);

        return true;
    }

    public async Task<CommunityPostDto> ToggleLikeAsync(string postId, string userId)
    {
        _logger.LogInformation("Toggling like for post {PostId} by user {UserId}", postId, userId);

        var post = await _context.CommunityPosts.FindAsync(postId);
        if (post == null)
        {
            throw new KeyNotFoundException($"Post with id {postId} not found");
        }

        var reaction = await _context.CommunityPostReactions
            .FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId);

        var likedByCurrentUser = reaction is null;

        if (reaction is null)
        {
            _context.CommunityPostReactions.Add(new CommunityPostReaction
            {
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
            });
            post.LikeCount++;
        }
        else
        {
            _context.CommunityPostReactions.Remove(reaction);
            post.LikeCount = Math.Max(0, post.LikeCount - 1);
        }

        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Post like toggled successfully with id: {PostId}", postId);

        return MapPostToDto(post, likedByCurrentUser);
    }

    public async Task<IReadOnlyList<CommunityCommentDto>> GetPostCommentsAsync(string postId, bool includeHidden = false)
    {
        var comments = await _context.CommunityComments
            .AsNoTracking()
            .Where(c => c.PostId == postId && (includeHidden || !c.IsHidden))
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(MapCommentToDto).ToList();
    }

    public async Task<CommunityCommentDto> CreateCommentAsync(CreateCommentDto createDto, string authorId, string authorName)
    {
        if (string.IsNullOrWhiteSpace(createDto.PostId) || string.IsNullOrWhiteSpace(createDto.Content))
        {
            throw new InvalidOperationException("Post and content are required");
        }

        var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.Id == createDto.PostId)
            ?? throw new KeyNotFoundException($"Post with id {createDto.PostId} not found");

        var isMember = await _context.CommunityMembers
            .AsNoTracking()
            .AnyAsync(m => m.CommunityId == post.CommunityId && m.UserId == authorId && m.IsActive);

        if (!isMember)
        {
            throw new UnauthorizedAccessException("Join this community before commenting");
        }

        var comment = new CommunityComment
        {
            PostId = post.Id,
            CommunityId = post.CommunityId,
            UserId = authorId,
            UserName = authorName,
            Content = createDto.Content.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        _context.CommunityComments.Add(comment);
        post.CommentCount++;
        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapCommentToDto(comment);
    }

    public async Task<CommunityCommentDto> UpdateCommentAsync(string commentId, UpdateCommentDto updateDto, string actorUserId, bool canModerate)
    {
        var comment = await _context.CommunityComments.FindAsync(commentId)
            ?? throw new KeyNotFoundException($"Comment with id {commentId} not found");

        if (!string.Equals(comment.UserId, actorUserId, StringComparison.Ordinal) && !canModerate)
        {
            throw new UnauthorizedAccessException("You are not allowed to edit this comment");
        }

        if (string.IsNullOrWhiteSpace(updateDto.Content))
        {
            throw new InvalidOperationException("Content is required");
        }

        comment.Content = updateDto.Content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapCommentToDto(comment);
    }

    public async Task<bool> DeleteCommentAsync(string commentId, string actorUserId, bool canModerate)
    {
        var comment = await _context.CommunityComments.FindAsync(commentId);
        if (comment is null)
        {
            return false;
        }

        if (!string.Equals(comment.UserId, actorUserId, StringComparison.Ordinal) && !canModerate)
        {
            throw new UnauthorizedAccessException("You are not allowed to delete this comment");
        }

        var post = await _context.CommunityPosts.FindAsync(comment.PostId);
        if (post is not null)
        {
            post.CommentCount = Math.Max(0, post.CommentCount - 1);
            post.UpdatedAt = DateTime.UtcNow;
        }

        _context.CommunityComments.Remove(comment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<CommunityPostDto> SetPostHiddenAsync(string postId, bool hidden, string actorUserId, string? reason = null)
    {
        var post = await _context.CommunityPosts.FindAsync(postId)
            ?? throw new KeyNotFoundException($"Post with id {postId} not found");

        post.IsHidden = hidden;
        post.HiddenReason = hidden ? reason?.Trim() : null;
        post.HiddenAt = hidden ? DateTime.UtcNow : null;
        post.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Post visibility changed by {ActorUserId} for post {PostId}. Hidden={Hidden}", actorUserId, postId, hidden);

        return MapPostToDto(post, likedByCurrentUser: false);
    }

    public async Task<CommunityCommentDto> SetCommentHiddenAsync(string commentId, bool hidden, string actorUserId, string? reason = null)
    {
        var comment = await _context.CommunityComments.FindAsync(commentId)
            ?? throw new KeyNotFoundException($"Comment with id {commentId} not found");

        comment.IsHidden = hidden;
        comment.HiddenReason = hidden ? reason?.Trim() : null;
        comment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Comment visibility changed by {ActorUserId} for comment {CommentId}. Hidden={Hidden}", actorUserId, commentId, hidden);

        return MapCommentToDto(comment);
    }

    public async Task<CommunityReportDto> ReportContentAsync(CreateReportDto createDto, string reporterUserId, string reporterName)
    {
        if (string.IsNullOrWhiteSpace(createDto.CommunityId)
            || string.IsNullOrWhiteSpace(createDto.Reason)
            || (string.IsNullOrWhiteSpace(createDto.PostId) && string.IsNullOrWhiteSpace(createDto.CommentId)))
        {
            throw new InvalidOperationException("Community, reason, and either post or comment are required");
        }

        if (!string.IsNullOrWhiteSpace(createDto.PostId) && !string.IsNullOrWhiteSpace(createDto.CommentId))
        {
            throw new InvalidOperationException("Report either a post or a comment, not both");
        }

        if (!string.IsNullOrWhiteSpace(createDto.PostId))
        {
            var post = await _context.CommunityPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == createDto.PostId && p.CommunityId == createDto.CommunityId);
            if (post is null)
            {
                throw new KeyNotFoundException("Reported post was not found in this community");
            }
        }

        if (!string.IsNullOrWhiteSpace(createDto.CommentId))
        {
            var comment = await _context.CommunityComments
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == createDto.CommentId && c.CommunityId == createDto.CommunityId);
            if (comment is null)
            {
                throw new KeyNotFoundException("Reported comment was not found in this community");
            }
        }

        var report = new CommunityReport
        {
            CommunityId = createDto.CommunityId,
            PostId = createDto.PostId,
            CommentId = createDto.CommentId,
            ReportedByUserId = reporterUserId,
            ReportedByUserName = reporterName,
            Reason = createDto.Reason.Trim(),
            Details = createDto.Details?.Trim(),
            Status = ReportStatus.Open,
            CreatedAt = DateTime.UtcNow,
        };

        _context.CommunityReports.Add(report);
        await _context.SaveChangesAsync();

        return MapReportToDto(report);
    }

    public async Task<IReadOnlyList<CommunityReportDto>> GetReportsAsync(ReportStatus? status = null)
    {
        var query = _context.CommunityReports.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var reports = await query
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reports.Select(MapReportToDto).ToList();
    }

    public async Task<CommunityReportDto> ResolveReportAsync(string reportId, ResolveReportDto resolveDto, string reviewerUserId)
    {
        var report = await _context.CommunityReports.FindAsync(reportId)
            ?? throw new KeyNotFoundException($"Report with id {reportId} not found");

        report.Status = resolveDto.Status;
        report.ResolutionNotes = resolveDto.ResolutionNotes?.Trim();
        report.ReviewedByUserId = reviewerUserId;
        report.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapReportToDto(report);
    }

    private static CommunityPostDto MapPostToDto(CommunityPost post, bool likedByCurrentUser)
    {
        return new CommunityPostDto
        {
            Id = post.Id,
            CommunityId = post.CommunityId,
            UserId = post.UserId,
            UserName = post.UserName,
            Title = post.Title,
            Content = post.Content,
            LikeCount = post.LikeCount,
            CommentCount = post.CommentCount,
            IsPinned = post.IsPinned,
            IsHidden = post.IsHidden,
            HiddenReason = post.HiddenReason,
            LikedByCurrentUser = likedByCurrentUser,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
        };
    }

    private static CommunityCommentDto MapCommentToDto(CommunityComment comment)
    {
        return new CommunityCommentDto
        {
            Id = comment.Id,
            PostId = comment.PostId,
            CommunityId = comment.CommunityId,
            UserId = comment.UserId,
            UserName = comment.UserName,
            UserAvatar = comment.UserAvatar,
            Content = comment.Content,
            IsHidden = comment.IsHidden,
            HiddenReason = comment.HiddenReason,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
        };
    }

    private static CommunityReportDto MapReportToDto(CommunityReport report)
    {
        return new CommunityReportDto
        {
            Id = report.Id,
            CommunityId = report.CommunityId,
            PostId = report.PostId,
            CommentId = report.CommentId,
            ReportedByUserId = report.ReportedByUserId,
            ReportedByUserName = report.ReportedByUserName,
            Reason = report.Reason,
            Details = report.Details,
            Status = report.Status,
            ResolutionNotes = report.ResolutionNotes,
            ReviewedByUserId = report.ReviewedByUserId,
            ReviewedAt = report.ReviewedAt,
            CreatedAt = report.CreatedAt,
        };
    }
}
