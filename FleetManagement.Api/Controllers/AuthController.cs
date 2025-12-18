using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FleetManagement.Data;
using FleetManagement.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly FleetDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        FleetDbContext context,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { error = "Email and password are required" });
        }

        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent user: {Email}", request.Email);
            return Unauthorized(new { error = "Invalid email or password" });
        }

        // Verify password (using Identity's password hasher)
        var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? "", request.Password);

        if (result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Failed login attempt for user: {Email}", request.Email);
            return Unauthorized(new { error = "Invalid email or password" });
        }

        // Get owner info if exists
        var owner = await _context.Owners
            .FirstOrDefaultAsync(o => o.IdentityUserId == user.Id && !o.IsDeleted);

        // Generate tokens
        var accessToken = GenerateAccessToken(user, owner);
        var refreshToken = await GenerateRefreshToken(user.Id);

        // Update last login
        user.LastLoginUtc = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User logged in successfully: {Email}", request.Email);

        return Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 3600, // 1 hour
            TokenType = "Bearer",
            UserId = user.Id,
            Email = user.Email ?? "",
            OwnerId = owner?.Id
        });
    }

    /// <summary>
    /// Register a new owner account
    /// </summary>
    [HttpPost("register-owner")]
    public async Task<IActionResult> RegisterOwner([FromBody] RegisterOwnerRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { error = "Email and password are required" });
        }

        if (string.IsNullOrEmpty(request.CompanyName))
        {
            return BadRequest(new { error = "Company name is required" });
        }

        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            return Conflict(new { error = "An account with this email already exists" });
        }

        // Validate city exists
        var city = await _context.Cities.FirstOrDefaultAsync(c => c.Id == request.CityId && !c.IsDeleted);
        if (city == null)
        {
            return BadRequest(new { error = "Invalid city ID" });
        }

        // Create user
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            NormalizedUserName = request.Email.ToUpper(),
            Email = request.Email,
            NormalizedEmail = request.Email.ToUpper(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        // Hash password
        var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);

        // Create owner
        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            CompanyName = request.CompanyName,
            ContactEmail = request.Email,
            ContactPhone = request.ContactPhone,
            PrimaryContactName = request.PrimaryContactName,
            CityId = request.CityId,
            TimeZone = request.TimeZone,
            IdentityUserId = user.Id,
            FleetCount = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false
        };

        _context.Owners.Add(owner);
        await _context.SaveChangesAsync();

        // Generate tokens
        var accessToken = GenerateAccessToken(user, owner);
        var refreshToken = await GenerateRefreshToken(user.Id);

        _logger.LogInformation("New owner registered: {Email}, Company: {Company}", request.Email, request.CompanyName);

        return CreatedAtAction(nameof(Login), new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 3600,
            TokenType = "Bearer",
            UserId = user.Id,
            Email = user.Email,
            OwnerId = owner.Id
        });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new { error = "Refresh token is required" });
        }

        // Find the refresh token
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsDeleted);

        if (storedToken == null)
        {
            return Unauthorized(new { error = "Invalid refresh token" });
        }

        if (storedToken.RevokedAtUtc.HasValue)
        {
            return Unauthorized(new { error = "Refresh token has been revoked" });
        }

        if (storedToken.ExpiresAtUtc < DateTimeOffset.UtcNow)
        {
            return Unauthorized(new { error = "Refresh token has expired" });
        }

        var user = storedToken.User;
        var owner = await _context.Owners
            .FirstOrDefaultAsync(o => o.IdentityUserId == user.Id && !o.IsDeleted);

        // Revoke old refresh token
        storedToken.RevokedAtUtc = DateTimeOffset.UtcNow;

        // Generate new tokens
        var accessToken = GenerateAccessToken(user, owner);
        var newRefreshToken = await GenerateRefreshToken(user.Id);

        storedToken.ReplacedByToken = newRefreshToken;
        await _context.SaveChangesAsync();

        return Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = 3600,
            TokenType = "Bearer",
            UserId = user.Id,
            Email = user.Email ?? "",
            OwnerId = owner?.Id
        });
    }

    /// <summary>
    /// Revoke refresh token (logout)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request)
    {
        if (request != null && !string.IsNullOrEmpty(request.RefreshToken))
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsDeleted);

            if (storedToken != null)
            {
                storedToken.RevokedAtUtc = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        var owner = await _context.Owners
            .Include(o => o.City)
            .ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(o => o.IdentityUserId == userId && !o.IsDeleted);

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            ownerId = owner?.Id,
            companyName = owner?.CompanyName,
            cityName = owner?.City?.Name,
            countryName = owner?.City?.Country?.Name
        });
    }

    private string GenerateAccessToken(ApplicationUser user, Owner? owner)
    {
        var secret = _configuration["Jwt:Secret"] 
            ?? Environment.GetEnvironmentVariable("JWT_SECRET") 
            ?? "FleetManagement_SuperSecretKey_ChangeInProduction_AtLeast32Chars!";
        var issuer = _configuration["Jwt:Issuer"] 
            ?? Environment.GetEnvironmentVariable("JWT_ISSUER") 
            ?? "FleetManagementAPI";
        var audience = _configuration["Jwt:Audience"] 
            ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
            ?? "FleetManagementClient";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (owner != null)
        {
            claims.Add(new Claim("ownerId", owner.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, "Owner"));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> GenerateRefreshToken(Guid userId)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return token;
    }
}

// Request/Response DTOs
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterOwnerRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? PrimaryContactName { get; set; }
    public Guid CityId { get; set; }
    public string? TimeZone { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
}



