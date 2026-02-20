using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Regjistron një përdorues të ri (Signup).
    /// Inputet (username, email, password, role) vijnë nga request body i frontend-it, jo nga databaza.
    /// </summary>
    /// <param name="dto">Inputet nga formularët e klientit – FromBody.</param>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] UserCreateDto? dto, CancellationToken cancellationToken)
    {
        if (dto is null)
            return BadRequest(new { message = "Trupi i kërkesës duhet të jetë JSON me: username, email, password, role (0 ose 1)." });

        try
        {
            var result = await _authService.RegisterAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(Register), result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Identifikon përdoruesin dhe kthen JWT token (Login).
    /// Inputet (email, password) vijnë nga request body i frontend-it, jo nga databaza.
    /// </summary>
    /// <param name="dto">Inputet nga formularët e klientit – FromBody.</param>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto? dto, CancellationToken cancellationToken)
    {
        if (dto is null)
            return BadRequest(new { message = "Trupi i kërkesës duhet të jetë JSON me: email, password." });

        var result = await _authService.LoginAsync(dto, cancellationToken);
        return result is null
            ? Unauthorized(new { message = "Email ose fjalëkalim i gabuar." })
            : Ok(result);
    }
}
