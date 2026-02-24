using BusinessDirectory.Application.Dtos.Reviews;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api/reviews")]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewService _service;

    public ReviewsController(IReviewService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] GetReviewsQueryDto query, CancellationToken ct)
    {
        var result = await _service.GetAsync(query, ct);
        return Ok(result);
    }
}
