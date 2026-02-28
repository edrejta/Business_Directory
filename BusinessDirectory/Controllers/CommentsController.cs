using BusinessDirectory.Application.Dtos.Comment;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CommentsController : ControllerBase
{
    private readonly ICommentService _service;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(ICommentService service, ILogger<CommentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET /api/comments?businessId={id}&limit=50
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetByBusiness(
        [FromQuery] Guid businessId,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        if (businessId == Guid.Empty)
            return BadRequest(new { message = "businessId is required." });

        try
        {
            var comments = await _service.GetByBusinessAsync(businessId, limit, ct);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByBusiness failed. businessId={BusinessId}, limit={Limit}", businessId, limit);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ndodhi një gabim në server." });
        }
    }

    // POST /api/comments
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CommentCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var created = await _service.CreateAsync(userId.Value, dto, ct);
            return StatusCode(StatusCodes.Status201Created, created);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Create comment failed (not found). userId={UserId}", userId);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Create comment failed (bad request). userId={UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create comment failed. userId={UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ndodhi një gabim në server." });
        }
    }

    // PUT /api/comments/{id}
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] CommentUpdateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var (result, notFound, forbid, error) = await _service.UpdateAsync(id, userId.Value, dto, ct);

            if (notFound) return NotFound();
            if (forbid) return Forbid();
            if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update comment failed. id={CommentId}, userId={UserId}", id, userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ndodhi një gabim në server." });
        }
    }

    // DELETE /api/comments/{id}
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var (notFound, forbid, error) = await _service.DeleteAsync(id, userId.Value, ct);

            if (notFound) return NotFound();
            if (forbid) return Forbid();
            if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete comment failed. id={CommentId}, userId={UserId}", id, userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ndodhi një gabim në server." });
        }
    }

    private Guid? GetUserId()
    {
        return User.GetActorUserId();
    }
}