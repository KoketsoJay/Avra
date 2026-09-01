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
    private readonly ILogger<CommunitiesController> _logger;

    public CommunitiesController(ICommunityService communityService, ILogger<CommunitiesController> logger)
    {
        _communityService = communityService;
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
    public async Task<ActionResult<CommunityDto>> CreateCommunity(CreateCommunityDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var community = await _communityService.CreateCommunityAsync(createDto);
            return CreatedAtAction(nameof(GetCommunity), new { communityId = community.Id }, community);
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
    public async Task<ActionResult<CommunityDto>> UpdateCommunity(string communityId, CreateCommunityDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var community = await _communityService.UpdateCommunityAsync(communityId, updateDto);
            return Ok(community);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Community not found with id {CommunityId}", communityId);
            return NotFound(new { message = ex.Message });
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
    public async Task<IActionResult> DeleteCommunity(string communityId)
    {
        try
        {
            var deleted = await _communityService.DeleteCommunityAsync(communityId);
            if (!deleted)
            {
                return NotFound(new { message = $"Community with id {communityId} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting community with id {CommunityId}", communityId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error deleting community" });
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
    private readonly ILogger<MembersController> _logger;

    public MembersController(IMemberService memberService, ILogger<MembersController> logger)
    {
        _memberService = memberService;
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
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(userName))
            {
                return BadRequest(new { message = "userId and userName are required" });
            }

            var member = await _memberService.AddMemberAsync(communityId, userId, userName);
            return CreatedAtAction(nameof(GetMember), new { memberId = member.Id }, member);
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
    /// Get member by ID
    /// </summary>
    /// <param name="memberId">Member ID</param>
    /// <returns>Member details</returns>
    [HttpGet("{memberId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityMemberDto>> GetMember(string memberId)
    {
        try
        {
            var member = await _memberService.GetMemberByIdAsync(memberId);
            if (member == null)
            {
                return NotFound(new { message = $"Member with id {memberId} not found" });
            }
            return Ok(member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching member with id {MemberId}", memberId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching member" });
        }
    }

    /// <summary>
    /// Remove member from community
    /// </summary>
    /// <param name="memberId">Member ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{memberId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(string memberId)
    {
        try
        {
            var removed = await _memberService.RemoveMemberAsync(memberId);
            if (!removed)
            {
                return NotFound(new { message = $"Member with id {memberId} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member with id {MemberId}", memberId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error removing member" });
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
    private readonly ILogger<PostsController> _logger;

    public PostsController(IPostService postService, ILogger<PostsController> logger)
    {
        _postService = postService;
        _logger = logger;
    }

    /// <summary>
    /// Get all posts in a community
    /// </summary>
    /// <param name="communityId">Community ID</param>
    /// <returns>List of community posts</returns>
    [HttpGet("community/{communityId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommunityPostDto>>> GetCommunityPosts(string communityId)
    {
        try
        {
            var posts = await _postService.GetCommunityPostsAsync(communityId);
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
            var post = await _postService.GetPostByIdAsync(postId);
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
    public async Task<ActionResult<CommunityPostDto>> CreatePost(CreatePostDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var post = await _postService.CreatePostAsync(createDto);
            return CreatedAtAction(nameof(GetPost), new { postId = post.Id }, post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating post" });
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
    public async Task<ActionResult<CommunityPostDto>> LikePost(string postId)
    {
        try
        {
            var post = await _postService.LikePostAsync(postId);
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
    public async Task<IActionResult> DeletePost(string postId)
    {
        try
        {
            var deleted = await _postService.DeletePostAsync(postId);
            if (!deleted)
            {
                return NotFound(new { message = $"Post with id {postId} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post with id {PostId}", postId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error deleting post" });
        }
    }
}
