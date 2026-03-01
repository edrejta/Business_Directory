using BusinessDirectory.Application.Dtos.City;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CitiesController : ControllerBase
{
    private readonly ICityService _cityService;
    private readonly ILogger<CitiesController> _logger;

    public CitiesController(ICityService cityService, ILogger<CitiesController> logger)
    {
        _cityService = cityService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CityDto>>> GetAll(CancellationToken ct)
    {
        try
        {
            var results = await _cityService.GetAllAsync(ct);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAll cities failed.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ndodhi një gabim në server." });
        }
    }
}