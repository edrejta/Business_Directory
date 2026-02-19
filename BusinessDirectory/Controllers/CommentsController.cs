using System.Security.Claims;
using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CommentsController : ControllerBase
{
    private readonly ICommentService _service;

    public CommentsController(ICommentService service)
    {
        _service = service;
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

        var created = await _service.CreateAsync(userId.Value, dto, ct);

        return StatusCode(StatusCodes.Status201Created, created);
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

        var (result, notFound, forbid, error) = await _service.UpdateAsync(id, userId.Value, dto, ct);

        if (notFound) return NotFound();
        if (forbid) return Forbid();
        if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

        return Ok(result);
    }

    // DELETE /api/comments/{id}
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var (notFound, forbid, error) = await _service.DeleteAsync(id, userId.Value, ct);

        if (notFound) return NotFound();
        if (forbid) return Forbid();
        if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

        return NoContent();
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
