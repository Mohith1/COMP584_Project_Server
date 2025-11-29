using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FleetManagement.Data.Entities;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Auth;
using FleetManagement.Services.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FleetManagement.Services.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtTokenService(IOptions<JwtSettings> options, UserManager<ApplicationUser> userManager, ILogger<JwtTokenService> logger)
    {
        _settings = options.Value;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<TokenPair> CreateTokenPairAsync(ApplicationUser user, Owner? owner, CancellationToken cancellationToken = default)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("okta_sub", user.OktaUserId ?? string.Empty)
        };

        if (owner != null)
        {
            claims.Add(new Claim("ownerId", owner.Id.ToString()));
            claims.Add(new Claim("ownerName", owner.CompanyName));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expires = DateTimeOffset.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = _tokenHandler.WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        _logger.LogInformation("Generated JWT for user {UserId} expiring at {Expiry}", user.Id, expires);

        return new TokenPair(accessToken, refreshToken, expires);
    }

    public string HashRefreshToken(string plainToken)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToBase64String(bytes);
    }

    public string GenerateRefreshToken()
    {
        var buffer = new byte[64];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer);
    }
}

