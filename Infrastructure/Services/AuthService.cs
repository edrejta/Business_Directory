using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Exceptions;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Application.Options;
using BusinessDirectory.Domain.Entities;
using BusinessDirectory.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly EmailVerificationSettings _emailVerificationSettings;
    private readonly IEmailSender _emailSender;

    public AuthService(
        ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings,
        IOptions<EmailVerificationSettings> emailVerificationOptions,
        IEmailSender emailSender)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _emailVerificationSettings = emailVerificationOptions.Value;
        _emailSender = emailSender;
    }

    public async Task<AuthResponseDto> RegisterAsync(UserCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Role is not UserRole.User and not UserRole.BusinessOwner)
            throw new InvalidOperationException("Roli i regjistrimit lejohet vetem 0 ose 1.");

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var normalizedUsername = dto.Username.Trim();
        var normalizedPassword = dto.Password.Trim();

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken))
            throw new DuplicateResourceException("Nje perdorues me kete email ekziston tashme.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = normalizedUsername,
            Email = normalizedEmail,
            Password = BCrypt.Net.BCrypt.HashPassword(normalizedPassword),
            EmailVerified = false,
            EmailVerificationToken = Guid.NewGuid().ToString("N"),
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(Math.Max(1, _emailVerificationSettings.TokenExpiryHours)),
            Role = dto.Role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        await SendVerificationEmailAsync(user, cancellationToken);

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var normalizedPassword = dto.Password.Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(normalizedPassword, user.Password))
            return null;

        if (_emailVerificationSettings.RequireVerifiedEmailForLogin && !user.EmailVerified)
            throw new EmailNotVerifiedException("Email nuk eshte verifikuar.");

        return CreateAuthResponse(user);
    }

    public async Task<bool> VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var normalizedToken = token.Trim();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == normalizedToken, cancellationToken);

        if (user is null || user.EmailVerificationTokenExpiresAt is null || user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
            return false;

        user.EmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ResendVerificationEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || user.EmailVerified)
            return false;

        user.EmailVerificationToken = Guid.NewGuid().ToString("N");
        user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(Math.Max(1, _emailVerificationSettings.TokenExpiryHours));
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await SendVerificationEmailAsync(user, cancellationToken);
        return true;
    }

    private AuthResponseDto CreateAuthResponse(User user)
    {
        return new AuthResponseDto
        {
            Token = GenerateJwtToken(user),
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role
        };
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task SendVerificationEmailAsync(User user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.EmailVerificationToken))
            return;

        var baseUrl = string.IsNullOrWhiteSpace(_emailVerificationSettings.VerificationBaseUrl)
            ? "http://localhost:3000/verify-email"
            : _emailVerificationSettings.VerificationBaseUrl.TrimEnd('/');

        var verificationLink = $"{baseUrl}?token={Uri.EscapeDataString(user.EmailVerificationToken)}";
        var subject = "Verifiko email-in tend";
        var body = $"Pershendetje {user.Username},\n\nKliko linkun per verifikim:\n{verificationLink}\n\nKy link skadon brenda {_emailVerificationSettings.TokenExpiryHours} oreve.";

        await _emailSender.SendAsync(user.Email, subject, body, cancellationToken);
    }
}
