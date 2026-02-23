using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Exceptions;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting("auth-register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] UserCreateDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(Register), result);
        }
        catch (DuplicateResourceException ex)
        {
            return Conflict(new ErrorResponseDto { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(dto, cancellationToken);
            return result is null
                ? Unauthorized(new ErrorResponseDto { Message = "Email ose fjalekalim i gabuar." })
                : Ok(result);
        }
        catch (EmailNotVerifiedException ex)
        {
            return Unauthorized(new ErrorResponseDto { Message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponseDto>> VerifyEmail([FromBody] VerifyEmailRequestDto request, CancellationToken cancellationToken)
    {
        var ok = await _authService.VerifyEmailAsync(request.Token, cancellationToken);
        if (!ok)
            return BadRequest(new ErrorResponseDto { Message = "Token verifikimi i pavlefshem ose i skaduar." });

        return Ok(new MessageResponseDto { Message = "Email u verifikua me sukses." });
    }

    [AllowAnonymous]
    [HttpPost("resend-verification")]
    [EnableRateLimiting("auth-register")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageResponseDto>> ResendVerification([FromBody] ResendVerificationRequestDto request, CancellationToken cancellationToken)
    {
        await _authService.ResendVerificationEmailAsync(request.Email, cancellationToken);
        return Ok(new MessageResponseDto { Message = "Nese email ekziston dhe nuk eshte verifikuar, u dergua link i ri." });
    }
}
