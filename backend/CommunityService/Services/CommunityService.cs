using Microsoft.EntityFrameworkCore;
using CommunityService.Data;
using CommunityService.Models;

namespace CommunityService.Services;

/// <summary>
/// Service for managing communities
/// </summary>
public interface ICommunityService
{
    Task<IEnumerable<CommunityDto>> GetAllCommunitiesAsync();
    Task<IEnumerable<CommunityDto>> GetCommunitiesByCategoryAsync(string category);
    Task<CommunityDto?> GetCommunityByIdAsync(string communityId);
    Task<CommunityDto> CreateCommunityAsync(CreateCommunityDto createDto);
    Task<CommunityDto> UpdateCommunityAsync(string communityId, CreateCommunityDto updateDto);
    Task<bool> DeleteCommunityAsync(string communityId);
}

/// <summary>
/// Service for managing community members
/// </summary>
public interface IMemberService
{
    Task<CommunityMemberDto> AddMemberAsync(string communityId, string userId, string userName);
    Task<IEnumerable<CommunityMemberDto>> GetCommunityMembersAsync(string communityId);
    Task<CommunityMemberDto?> GetMemberByIdAsync(string memberId);
    Task<bool> RemoveMemberAsync(string memberId);
    Task<CommunityMemberDto> UpdateMemberRoleAsync(string memberId, MemberRole newRole);
}

/// <summary>
/// Service for managing community posts
/// </summary>
public interface IPostService
{
    Task<IEnumerable<CommunityPostDto>> GetCommunityPostsAsync(string communityId);
    Task<CommunityPostDto?> GetPostByIdAsync(string postId);
    Task<CommunityPostDto> CreatePostAsync(CreatePostDto createDto);
    Task<CommunityPostDto> UpdatePostAsync(string postId, string title, string content);
    Task<bool> DeletePostAsync(string postId);
    Task<CommunityPostDto> LikePostAsync(string postId);
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

    public async Task<IEnumerable<CommunityDto>> GetAllCommunitiesAsync()
    {
        _logger.LogInformation("Fetching all public communities");

        var communities = await _context.Communities
            .Where(c => c.IsPublic && c.Status == "Active")
            .OrderByDescending(c => c.MemberCount)
            .ToListAsync();

        return communities.Select(MapToDto);
    }

    public async Task<IEnumerable<CommunityDto>> GetCommunitiesByCategoryAsync(string category)
    {
        _logger.LogInformation("Fetching communities by category: {Category}", category);

        var communities = await _context.Communities
            .Where(c => c.Category.Contains(category) && c.IsPublic && c.Status == "Active")
            .OrderByDescending(c => c.MemberCount)
            .ToListAsync();

        return communities.Select(MapToDto);
    }

    public async Task<CommunityDto?> GetCommunityByIdAsync(string communityId)
    {
        _logger.LogInformation("Fetching community with id: {CommunityId}", communityId);

        var community = await _context.Communities
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == communityId);

        return community == null ? null : MapToDto(community);
    }

    public async Task<CommunityDto> CreateCommunityAsync(CreateCommunityDto createDto)
    {
        _logger.LogInformation("Creating new community: {Name}", createDto.Name);

        var community = new Community
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Category = createDto.Category,
            CreatorId = createDto.CreatorId,
            ImageUrl = createDto.ImageUrl,
            IsPublic = createDto.IsPublic,
            CreatedAt = DateTime.UtcNow
        };

        _context.Communities.Add(community);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Community created successfully with id: {CommunityId}", community.Id);

        return MapToDto(community);
    }

    public async Task<CommunityDto> UpdateCommunityAsync(string communityId, CreateCommunityDto updateDto)
    {
        _logger.LogInformation("Updating community with id: {CommunityId}", communityId);

        var community = await _context.Communities.FindAsync(communityId);
        if (community == null)
        {
            throw new KeyNotFoundException($"Community with id {communityId} not found");
        }

        community.Name = updateDto.Name;
        community.Description = updateDto.Description;
        community.Category = updateDto.Category;
        community.ImageUrl = updateDto.ImageUrl;
        community.IsPublic = updateDto.IsPublic;
        community.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Community updated successfully with id: {CommunityId}", communityId);

        return MapToDto(community);
    }

    public async Task<bool> DeleteCommunityAsync(string communityId)
    {
        _logger.LogInformation("Deleting community with id: {CommunityId}", communityId);

        var community = await _context.Communities.FindAsync(communityId);
        if (community == null)
        {
            return false;
        }

        community.Status = "Inactive";
        community.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Community deleted successfully with id: {CommunityId}", communityId);

        return true;
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

    public async Task<CommunityMemberDto> AddMemberAsync(string communityId, string userId, string userName)
    {
        _logger.LogInformation("Adding member {UserId} to community {CommunityId}", userId, communityId);

        var community = await _context.Communities.FindAsync(communityId);
        if (community == null)
        {
            throw new KeyNotFoundException($"Community with id {communityId} not found");
        }

        var member = new CommunityMember
        {
            CommunityId = communityId,
            UserId = userId,
            UserName = userName,
            Role = MemberRole.Member,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.CommunityMembers.Add(member);
        community.MemberCount++;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Member added successfully with id: {MemberId}", member.Id);

        return MapToDto(member);
    }

    public async Task<IEnumerable<CommunityMemberDto>> GetCommunityMembersAsync(string communityId)
    {
        _logger.LogInformation("Fetching members for community: {CommunityId}", communityId);

        var members = await _context.CommunityMembers
            .Where(m => m.CommunityId == communityId && m.IsActive)
            .OrderByDescending(m => m.Role)
            .ToListAsync();

        return members.Select(MapToDto);
    }

    public async Task<CommunityMemberDto?> GetMemberByIdAsync(string memberId)
    {
        _logger.LogInformation("Fetching member with id: {MemberId}", memberId);

        var member = await _context.CommunityMembers.FindAsync(memberId);
        return member == null ? null : MapToDto(member);
    }

    public async Task<bool> RemoveMemberAsync(string memberId)
    {
        _logger.LogInformation("Removing member with id: {MemberId}", memberId);

        var member = await _context.CommunityMembers.FindAsync(memberId);
        if (member == null)
        {
            return false;
        }

        member.IsActive = false;
        var community = await _context.Communities.FindAsync(member.CommunityId);
        if (community != null)
        {
            community.MemberCount--;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Member removed successfully with id: {MemberId}", memberId);

        return true;
    }

    public async Task<CommunityMemberDto> UpdateMemberRoleAsync(string memberId, MemberRole newRole)
    {
        _logger.LogInformation("Updating member role for id: {MemberId}", memberId);

        var member = await _context.CommunityMembers.FindAsync(memberId);
        if (member == null)
        {
            throw new KeyNotFoundException($"Member with id {memberId} not found");
        }

        member.Role = newRole;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Member role updated successfully for id: {MemberId}", memberId);

        return MapToDto(member);
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
            JoinedAt = member.JoinedAt
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

    public async Task<IEnumerable<CommunityPostDto>> GetCommunityPostsAsync(string communityId)
    {
        _logger.LogInformation("Fetching posts for community: {CommunityId}", communityId);

        var posts = await _context.CommunityPosts
            .Where(p => p.CommunityId == communityId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return posts.Select(MapToDto);
    }

    public async Task<CommunityPostDto?> GetPostByIdAsync(string postId)
    {
        _logger.LogInformation("Fetching post with id: {PostId}", postId);

        var post = await _context.CommunityPosts.FindAsync(postId);
        return post == null ? null : MapToDto(post);
    }

    public async Task<CommunityPostDto> CreatePostAsync(CreatePostDto createDto)
    {
        _logger.LogInformation("Creating new post in community: {CommunityId}", createDto.CommunityId);

        var post = new CommunityPost
        {
            CommunityId = createDto.CommunityId,
            UserId = createDto.UserId,
            UserName = createDto.UserName,
            Title = createDto.Title,
            Content = createDto.Content,
            CreatedAt = DateTime.UtcNow
        };

        _context.CommunityPosts.Add(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Post created successfully with id: {PostId}", post.Id);

        return MapToDto(post);
    }

    public async Task<CommunityPostDto> UpdatePostAsync(string postId, string title, string content)
    {
        _logger.LogInformation("Updating post with id: {PostId}", postId);

        var post = await _context.CommunityPosts.FindAsync(postId);
        if (post == null)
        {
            throw new KeyNotFoundException($"Post with id {postId} not found");
        }

        post.Title = title;
        post.Content = content;
        post.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Post updated successfully with id: {PostId}", postId);

        return MapToDto(post);
    }

    public async Task<bool> DeletePostAsync(string postId)
    {
        _logger.LogInformation("Deleting post with id: {PostId}", postId);

        var post = await _context.CommunityPosts.FindAsync(postId);
        if (post == null)
        {
            return false;
        }

        _context.CommunityPosts.Remove(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Post deleted successfully with id: {PostId}", postId);

        return true;
    }

    public async Task<CommunityPostDto> LikePostAsync(string postId)
    {
        _logger.LogInformation("Liking post with id: {PostId}", postId);

        var post = await _context.CommunityPosts.FindAsync(postId);
        if (post == null)
        {
            throw new KeyNotFoundException($"Post with id {postId} not found");
        }

        post.LikeCount++;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Post liked successfully with id: {PostId}", postId);

        return MapToDto(post);
    }

    private static CommunityPostDto MapToDto(CommunityPost post)
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
            CreatedAt = post.CreatedAt
        };
    }
}
