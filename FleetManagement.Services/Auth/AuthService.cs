using System.Linq;
using FleetManagement.Data.Entities;
using FleetManagement.Data.Repositories;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.Constants;
using FleetManagement.Services.DTOs.Auth;
using FleetManagement.Services.DTOs.Owners;
using FleetManagement.Services.Exceptions;
using FleetManagement.Services.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FleetManagement.Services.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOktaIntegrationService _oktaIntegrationService;
    private readonly ILogger<AuthService> _logger;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<ApplicationRole> roleManager,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        IOktaIntegrationService oktaIntegrationService,
        ILogger<AuthService> logger,
        IOptions<JwtSettings> jwtOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _oktaIntegrationService = oktaIntegrationService;
        _logger = logger;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterOwnerAsync(OwnerRegisterRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureEmailIsUnique(request.Email, cancellationToken);

        await EnsurePasswordDoesNotContainCompanyName(request);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            throw new ValidationException(createResult.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray()));
        }

        await EnsureRoleExistsAsync(SystemRoles.Owner);
        await _userManager.AddToRoleAsync(user, SystemRoles.Owner);

        var owner = new Owner
        {
            CompanyName = request.CompanyName,
            ContactEmail = request.Email,
            ContactPhone = request.PhoneNumber,
            PrimaryContactName = request.PrimaryContactName,
            CityId = request.CityId,
            IdentityUserId = user.Id,
            FleetCount = 0
        };

        await _unitOfWork.Repository<Owner>().AddAsync(owner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var oktaGroupId = await _oktaIntegrationService.EnsureOwnerGroupAsync(owner.CompanyName, cancellationToken);
        var oktaUserId = await _oktaIntegrationService.ProvisionUserAsync(
            request.Email,
            request.Password,
            ExtractFirstName(request.PrimaryContactName),
            ExtractLastName(request.PrimaryContactName),
            cancellationToken);

        owner.OktaGroupId = oktaGroupId ?? owner.OktaGroupId;
        user.OktaUserId = oktaUserId ?? user.OktaUserId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        owner = await _unitOfWork.Repository<Owner>()
            .Queryable
            .Include(o => o.City)
            .ThenInclude(city => city!.Country)
            .FirstAsync(o => o.Id == owner.Id, cancellationToken);

        var tokenPair = await _jwtTokenService.CreateTokenPairAsync(user, owner, cancellationToken);
        await PersistRefreshTokenAsync(user, tokenPair.RefreshToken, cancellationToken);

        return new AuthResponse
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            ExpiresAtUtc = tokenPair.AccessTokenExpiresAtUtc,
            Owner = MapOwner(owner)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user is null)
        {
            throw ValidationException.FromMessage(nameof(request.Email), "Invalid credentials.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
        if (!result.Succeeded)
        {
            throw ValidationException.FromMessage(nameof(request.Password), "Invalid credentials.");
        }

        user.LastLoginUtc = DateTimeOffset.UtcNow;
        var owner = await GetOwnerByUserAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var tokenPair = await _jwtTokenService.CreateTokenPairAsync(user, owner, cancellationToken);
        await PersistRefreshTokenAsync(user, tokenPair.RefreshToken, cancellationToken);

        return new AuthResponse
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            ExpiresAtUtc = tokenPair.AccessTokenExpiresAtUtc,
            Owner = owner is null ? null : MapOwner(owner)
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenRepository = _unitOfWork.Repository<RefreshToken>();
        var hashedToken = _jwtTokenService.HashRefreshToken(request.RefreshToken);

        var storedToken = await tokenRepository.Queryable
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.Token == hashedToken, cancellationToken);

        if (storedToken is null || storedToken.ExpiresAtUtc < DateTimeOffset.UtcNow || storedToken.RevokedAtUtc != null)
        {
            throw ValidationException.FromMessage(nameof(request.RefreshToken), "Refresh token is invalid or expired.");
        }

        var owner = await GetOwnerByUserAsync(storedToken.User!, cancellationToken);
        var tokenPair = await _jwtTokenService.CreateTokenPairAsync(storedToken.User!, owner, cancellationToken);
        storedToken.RevokedAtUtc = DateTimeOffset.UtcNow;
        storedToken.ReplacedByToken = _jwtTokenService.HashRefreshToken(tokenPair.RefreshToken);
        await PersistRefreshTokenAsync(storedToken.User!, tokenPair.RefreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            ExpiresAtUtc = tokenPair.AccessTokenExpiresAtUtc,
            Owner = owner is null ? null : MapOwner(owner)
        };
    }

    public async Task RevokeRefreshTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default)
    {
        var hashedToken = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var repository = _unitOfWork.Repository<RefreshToken>();
        var storedToken = await repository.Queryable.FirstOrDefaultAsync(token => token.Token == hashedToken, cancellationToken);

        if (storedToken is null)
        {
            throw ValidationException.FromMessage(nameof(request.RefreshToken), "Refresh token not found.");
        }

        storedToken.RevokedAtUtc = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureEmailIsUnique(string email, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            throw ValidationException.FromMessage(nameof(email), "Email address already exists.");
        }
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var result = await _roleManager.CreateAsync(new ApplicationRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            });

            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to create role {Role}. Errors: {Errors}", roleName, string.Join(", ", result.Errors.Select(error => error.Description)));
            }
        }
    }

    private static string ExtractFirstName(string fullName) =>
        fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fullName;

    private static string ExtractLastName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[^1] : parts[0];
    }

    private Task EnsurePasswordDoesNotContainCompanyName(OwnerRegisterRequest request)
    {
        if (request.Password.Contains(request.CompanyName, StringComparison.OrdinalIgnoreCase))
        {
            throw ValidationException.FromMessage(nameof(request.Password), "Password cannot contain company name.");
        }

        return Task.CompletedTask;
    }

    private async Task PersistRefreshTokenAsync(ApplicationUser user, string plainRefreshToken, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Repository<RefreshToken>();
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = _jwtTokenService.HashRefreshToken(plainRefreshToken),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
        };

        await repository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Owner?> GetOwnerByUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Repository<Owner>()
            .Queryable
            .Include(owner => owner.City)
            .ThenInclude(city => city!.Country)
            .FirstOrDefaultAsync(owner => owner.IdentityUserId == user.Id, cancellationToken);
    }

    private static OwnerSummaryResponse MapOwner(Owner owner) =>
        new()
        {
            Id = owner.Id,
            CompanyName = owner.CompanyName,
            ContactEmail = owner.ContactEmail,
            ContactPhone = owner.ContactPhone,
            City = owner.City?.Name,
            Country = owner.City?.Country?.Name
        };
}

