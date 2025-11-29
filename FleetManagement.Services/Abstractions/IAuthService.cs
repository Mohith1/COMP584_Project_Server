using FleetManagement.Services.DTOs.Auth;

namespace FleetManagement.Services.Abstractions;

public interface IAuthService
{
    Task<AuthResponse> RegisterOwnerAsync(OwnerRegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default);
}

