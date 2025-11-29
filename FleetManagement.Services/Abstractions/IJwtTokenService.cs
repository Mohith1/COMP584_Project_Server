using FleetManagement.Data.Entities;
using FleetManagement.Services.DTOs.Auth;

namespace FleetManagement.Services.Abstractions;

public interface IJwtTokenService
{
    Task<TokenPair> CreateTokenPairAsync(ApplicationUser user, Owner? owner, CancellationToken cancellationToken = default);

    string HashRefreshToken(string plainToken);

    string GenerateRefreshToken();
}

