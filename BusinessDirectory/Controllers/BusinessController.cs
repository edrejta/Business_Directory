using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.API.Controllers
{
    [ApiController]
    [Route("businesses")]
    public class BusinessController : ControllerBase
    {
        private readonly IBusinessService _service;

        public BusinessController(IBusinessService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BusinessCreateDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var id = await _service.CreateAsync(dto, dto.OwnerId);

            return Created($"/businesses/{id}", new { id });
        }
    }
}
