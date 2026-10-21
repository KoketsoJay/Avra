using Avra.ServiceDefaults.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CommunityService.Models;
using CommunityService.Services;

namespace CommunityService.Controllers;

/// <summary>
/// API controller for community operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CommunitiesController : ControllerBase
{
    private readonly ICommunityService _communityService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CommunitiesController> _logger;

    public CommunitiesController(
        ICommunityService communityService,
        ICurrentUser currentUser,
        ILogger<CommunitiesController> logger)
    {
        _communityService = communityService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Get all public communities
    /// </summary>
    /// <returns>List of communities</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommunityDto>>> GetAllCommunities()
    {
        try
        {
            var communities = await _communityService.GetAllCommunitiesAsync();
            return Ok(communities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all communities");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching communities" });
        }
    }

    /// <summary>
    /// Get all communities, including private and inactive records.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommunityDto>>> GetAllCommunitiesForAdmin()
    {
        var communities = await _communityService.GetAllCommunitiesAsync(includeInactive: true, includePrivate: true);
        return Ok(communities);
    }

    /// <summary>
    /// Get community by ID
    /// </summary>
    /// <param name="communityId">Community ID</param>
    /// <returns>Community details</returns>
    [HttpGet("{communityId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityDto>> GetCommunity(string communityId)
    {
        try
        {
            var community = await _communityService.GetCommunityByIdAsync(communityId);
            if (community == null)
            {
                return NotFound(new { message = $"Community with id {communityId} not found" });
            }
            return Ok(community);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching community with id {CommunityId}", communityId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching community" });
        }
    }

    /// <summary>
    /// Search communities by category
    /// </summary>
    /// <param name="category">Community category</param>
    /// <returns>List of communities matching the category</returns>
    [HttpGet("search/category")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommunityDto>>> SearchByCategory([FromQuery] string category)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return BadRequest(new { message = "Category is required" });
            }

            var communities = await _communityService.GetCommunitiesByCategoryAsync(category);
            return Ok(communities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching communities by category: {Category}", category);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error searching communities" });
        }
    }

    /// <summary>
    /// Create new community
    /// </summary>
    /// <param name="createDto">Community creation data</param>
    /// <returns>Created community</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize]
    public async Task<ActionResult<CommunityDto>> CreateCommunity(CreateCommunityDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var creatorId = _currentUser.UserId ?? createDto.CreatorId;
            var community = await _communityService.CreateCommunityAsync(createDto, creatorId ?? string.Empty);
            return CreatedAtAction(nameof(GetCommunity), new { communityId = community.Id }, community);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating community");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating community" });
        }
    }

    /// <summary>
    /// Update community
    /// </summary>
    /// <param name="communityId">Community ID</param>
    /// <param name="updateDto">Community update data</param>
    /// <returns>Updated community</returns>
    [HttpPut("{communityId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize]
    public async Task<ActionResult<CommunityDto>> UpdateCommunity(string communityId, UpdateCommunityDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var actorUserId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(actorUserId))
            {
                return Unauthorized(new { message = "Sign in required" });
            }

            var community = await _communityService.UpdateCommunityAsync(
                communityId,
                updateDto,
                actorUserId,
                User.IsInRole(AvraRoles.Administrator));

            return Ok(community);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Community not found with id {CommunityId}", communityId);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized community update for id {CommunityId}", communityId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating community with id {CommunityId}", communityId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating community" });
        }
    }

    /// <summary>
    /// Delete community
    /// </summary>
    /// <param name="communityId">Community ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{communityId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize]
    public async Task<IActionResult> DeleteCommunity(string communityId)
    {
        try
        {
            var actorUserId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(actorUserId))
            {
                return Unauthorized(new { message = "Sign in required" });
            }

            var deleted = await _communityService.DeleteCommunityAsync(
                communityId,
                actorUserId,
                User.IsInRole(AvraRoles.Administrator));

            if (!deleted)
            {
                return NotFound(new { message = $"Community with id {communityId} not found" });
            }
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized community deletion for id {CommunityId}", communityId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting community with id {CommunityId}", communityId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error deleting community" });
        }
    }

    /// <summary>
    /// Set publication status for a community.
    /// </summary>
    [HttpPut("{communityId}/status")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommunityDto>> SetCommunityStatus(string communityId, UpdateCommunityStatusDto dto)
    {
        try
        {
            var updated = await _communityService.SetStatusAsync(communityId, dto.Status);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

/// <summary>
/// API controller for community member operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<MembersController> _logger;

    public MembersController(
        IMemberService memberService,
        ICurrentUser currentUser,
        ILogger<MembersController> logger)
    {
        _memberService = memberService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Get all members of a community
    /// </summary>
    /// <param name="communityId">Community ID</param>
    /// <returns>List of community members</returns>
    [HttpGet("community/{communityId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommunityMemberDto>>> GetCommunityMembers(string communityId)
    {
        try
        {
            var members = await _memberService.GetCommunityMembersAsync(communityId);
            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching members for community {CommunityId}", communityId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching members" });
        }
    }

    /// <summary>
    /// Get the communities the signed-in user belongs to.
    /// </summary>
    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommunityMembershipDto>>> GetMine()
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Sign in required" });
        }

        var memberships = await _memberService.GetMembershipsAsync(userId);
        return Ok(memberships);
    }

    /// <summary>
    /// Add member to community
    /// </summary>
    /// <param name="communityId">Community ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="userName">User name</param>
    /// <returns>Member details</returns>
    [HttpPost("community/{communityId}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityMemberDto>> AddMember(string communityId, [FromQuery] string userId, [FromQuery] string userName)
    {
        try
        {
            var resolvedUserId = _currentUser.IsAuthenticated ? _currentUser.UserId : userId;
            var resolvedUserName = _currentUser.IsAuthenticated ? User.Identity?.Name : userName;

            if (string.IsNullOrWhiteSpace(resolvedUserId) || string.IsNullOrWhiteSpace(resolvedUserName))
            {
                return BadRequest(new { message = "userId and userName are required" });
            }

            var member = await _memberService.JoinCommunityAsync(communityId, resolvedUserId, resolvedUserName);
            return CreatedAtAction(nameof(GetCommunityMembers), new { communityId }, member);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Community not found with id {CommunityId}", communityId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member to community {CommunityId}", communityId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error adding member" });
        }
    }

    /// <summary>
    /// Leave a community as the signed-in user.
    /// </summary>
    /// <param name="communityId">Community ID</param>
    [HttpDelete("community/{communityId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveCommunity(string communityId)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Sign in required" });
        }

        var removed = await _memberService.LeaveCommunityAsync(communityId, userId);
        if (!removed)
        {
            return NotFound(new { message = "Membership not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Update a member role in a community.
    /// </summary>
    [HttpPut("community/{communityId}/role/{targetUserId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityMemberDto>> UpdateMemberRole(string communityId, string targetUserId, UpdateMemberRoleDto dto)
    {
        try
        {
            var actorUserId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(actorUserId))
            {
                return Unauthorized(new { message = "Sign in required" });
            }

            var updated = await _memberService.UpdateMemberRoleAsync(
                communityId,
                targetUserId,
                dto.Role,
                actorUserId,
                User.IsInRole(AvraRoles.Administrator));

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role for user {TargetUserId} in community {CommunityId}", targetUserId, communityId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating member role" });
        }
    }
}

/// <summary>
/// API controller for community post operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IMemberService _memberService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<PostsController> _logger;

    public PostsController(
        IPostService postService,
        IMemberService memberService,
        ICurrentUser currentUser,
        ILogger<PostsController> logger)
    {
        _postService = postService;
        _memberService = memberService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Get all posts in a community
    /// </summary>
    /// <param name="communityId">Community ID</param>
    /// <returns>List of community posts</returns>
    [HttpGet("community/{communityId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommunityPostDto>>> GetCommunityPosts(string communityId, [FromQuery] bool includeHidden = false)
    {
        try
        {
            var currentUserId = _currentUser.UserId;

            if (includeHidden)
            {
                if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUserId))
                {
                    return Forbid();
                }

                var canModerate = await _memberService.CanModerateAsync(
                    communityId,
                    currentUserId,
                    User.IsInRole(AvraRoles.Administrator));

                if (!canModerate)
                {
                    return Forbid();
                }
            }

            var posts = await _postService.GetCommunityPostsAsync(communityId, currentUserId, includeHidden);
            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching posts for community {CommunityId}", communityId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching posts" });
        }
    }

    /// <summary>
    /// Get post by ID
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <returns>Post details</returns>
    [HttpGet("{postId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityPostDto>> GetPost(string postId)
    {
        try
        {
            var post = await _postService.GetPostByIdAsync(postId, _currentUser.UserId);
            if (post == null)
            {
                return NotFound(new { message = $"Post with id {postId} not found" });
            }
            return Ok(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching post with id {PostId}", postId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching post" });
        }
    }

    /// <summary>
    /// Create new post
    /// </summary>
    /// <param name="createDto">Post creation data</param>
    /// <returns>Created post</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize]
    public async Task<ActionResult<CommunityPostDto>> CreatePost(CreatePostDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var authorId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(authorId))
            {
                return Unauthorized(new { message = "Sign in required" });
            }

            var authorName = string.IsNullOrWhiteSpace(User.Identity?.Name) ? createDto.UserName : User.Identity!.Name!;
            var post = await _postService.CreatePostAsync(createDto, authorId, authorName);
            return CreatedAtAction(nameof(GetPost), new { postId = post.Id }, post);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating post" });
        }
    }

    /// <summary>
    /// Update a post.
    /// </summary>
    [HttpPut("{postId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommunityPostDto>> UpdatePost(string postId, UpdatePostDto updateDto)
    {
        try
        {
            var actorUserId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(actorUserId))
            {
                return Unauthorized(new { message = "Sign in required" });
            }

            var existing = await _postService.GetPostByIdAsync(postId);
            if (existing is null)
            {
                return NotFound(new { message = $"Post with id {postId} not found" });
            }

            var canModerate = await _memberService.CanModerateAsync(
                existing.CommunityId,
                actorUserId,
                User.IsInRole(AvraRoles.Administrator));

            var post = await _postService.UpdatePostAsync(postId, updateDto, actorUserId, canModerate);
            return Ok(post);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post with id {PostId}", postId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating post" });
        }
    }

    /// <summary>
    /// Like a post
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <returns>Updated post</returns>
    [HttpPost("{postId}/like")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize]
    public async Task<ActionResult<CommunityPostDto>> LikePost(string postId)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Sign in required" });
            }

            var post = await _postService.ToggleLikeAsync(postId, userId);
            return Ok(post);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Post not found with id {PostId}", postId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error liking post with id {PostId}", postId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error liking post" });
        }
    }

    /// <summary>
    /// Delete post
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{postId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize]
    public async Task<IActionResult> DeletePost(string postId)
    {
        try
        {
            var actorUserId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(actorUserId))
            {
                return Unauthorized(new { message = "Sign in required" });
            }

            var existing = await _postService.GetPostByIdAsync(postId);
            if (existing is null)
            {
                return NotFound(new { message = $"Post with id {postId} not found" });
            }

            var canModerate = await _memberService.CanModerateAsync(
                existing.CommunityId,
                actorUserId,
                User.IsInRole(AvraRoles.Administrator));

            var deleted = await _postService.DeletePostAsync(postId, actorUserId, canModerate);
            if (!deleted)
            {
                return NotFound(new { message = $"Post with id {postId} not found" });
            }
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post with id {PostId}", postId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error deleting post" });
        }
    }

    /// <summary>
    /// Get comments for one post.
    /// </summary>
    [HttpGet("comments/post/{postId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommunityCommentDto>>> GetPostComments(string postId, [FromQuery] bool includeHidden = false)
    {
        if (includeHidden)
        {
            var post = await _postService.GetPostByIdAsync(postId);
            if (post is null)
            {
                return NotFound(new { message = $"Post with id {postId} not found" });
            }

            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            {
                return Forbid();
            }

            var canModerate = await _memberService.CanModerateAsync(post.CommunityId, _currentUser.UserId, User.IsInRole(AvraRoles.Administrator));
            if (!canModerate)
            {
                return Forbid();
            }
        }

        var comments = await _postService.GetPostCommentsAsync(postId, includeHidden);
        return Ok(comments);
    }

    /// <summary>
    /// Create a comment.
    /// </summary>
    [HttpPost("comments")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityCommentDto>> CreateComment(CreateCommentDto createDto)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Sign in required" });
            }

            var userName = string.IsNullOrWhiteSpace(User.Identity?.Name) ? "Community Member" : User.Identity!.Name!;
            var comment = await _postService.CreateCommentAsync(createDto, userId, userName);
            return CreatedAtAction(nameof(GetPostComments), new { postId = comment.PostId }, comment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a comment.
    /// </summary>
    [HttpPut("comments/{commentId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommunityCommentDto>> UpdateComment(string commentId, UpdateCommentDto updateDto)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Sign in required" });
            }

            var comment = await _postService.UpdateCommentAsync(commentId, updateDto, userId, canModerate: false);
            return Ok(comment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a comment.
    /// </summary>
    [HttpDelete("comments/{commentId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(string commentId)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Sign in required" });
            }

            var deleted = await _postService.DeleteCommentAsync(commentId, userId, canModerate: false);
            if (!deleted)
            {
                return NotFound(new { message = $"Comment with id {commentId} not found" });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Change post visibility for moderation.
    /// </summary>
    [HttpPut("{postId}/visibility")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityPostDto>> SetPostVisibility(string postId, SetContentVisibilityDto dto)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Sign in required" });
        }

        var existing = await _postService.GetPostByIdAsync(postId);
        if (existing is null)
        {
            return NotFound(new { message = $"Post with id {postId} not found" });
        }

        var canModerate = await _memberService.CanModerateAsync(existing.CommunityId, userId, User.IsInRole(AvraRoles.Administrator));
        if (!canModerate)
        {
            return Forbid();
        }

        var post = await _postService.SetPostHiddenAsync(postId, dto.Hidden, userId, dto.Reason);
        return Ok(post);
    }

    /// <summary>
    /// Change comment visibility for moderation.
    /// </summary>
    [HttpPut("comments/{commentId}/visibility")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityCommentDto>> SetCommentVisibility(string commentId, SetContentVisibilityDto dto)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Sign in required" });
        }

        var comment = await _postService.SetCommentHiddenAsync(commentId, dto.Hidden, userId, dto.Reason);
        return Ok(comment);
    }
}

/// <summary>
/// API controller for report and moderation workflow.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IPostService postService, ICurrentUser currentUser, ILogger<ReportsController> logger)
    {
        _postService = postService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Report post or comment content.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityReportDto>> Report(CreateReportDto createDto)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Sign in required" });
            }

            var userName = string.IsNullOrWhiteSpace(User.Identity?.Name) ? "Community Member" : User.Identity!.Name!;
            var report = await _postService.ReportContentAsync(createDto, userId, userName);
            return CreatedAtAction(nameof(GetReports), new { reportId = report.Id }, report);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating community report");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating report" });
        }
    }

    /// <summary>
    /// Administrator report inbox.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommunityReportDto>>> GetReports([FromQuery] string? status)
    {
        ReportStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ReportStatus>(status, true, out var value))
            {
                return BadRequest(new { message = "Invalid status filter" });
            }

            parsed = value;
        }

        var reports = await _postService.GetReportsAsync(parsed);
        return Ok(reports);
    }

    /// <summary>
    /// Resolve a report.
    /// </summary>
    [HttpPut("{reportId}/resolve")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityReportDto>> Resolve(string reportId, ResolveReportDto dto)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Sign in required" });
            }

            var report = await _postService.ResolveReportAsync(reportId, dto, userId);
            return Ok(report);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
