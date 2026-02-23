using BusinessDirectory.Application.Dtos.City;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class CitiesController : ControllerBase
    {
        private readonly ICityService _cityService;

        public CitiesController(ICityService cityService)
        {
            _cityService = cityService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CityDto>>> GetAll(CancellationToken ct)
        {
            var results = await _cityService.GetAllAsync(ct);
            return Ok(results);
        }

        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<ActionResult<IReadOnlyList<CityDto>>> Search(
            [FromQuery] string? query,
            [FromQuery] int take = 20,
            CancellationToken ct = default)
        {
            var results = await _cityService.SearchAsync(query, take, ct);
            return Ok(results);
        }
    }
}
