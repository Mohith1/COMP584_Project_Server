using FleetManagement.Services.DTOs.Owners;

namespace FleetManagement.Services.DTOs.Auth;

public sealed record AuthResponse
{
    public required string AccessToken { get; init; }

    public required DateTimeOffset ExpiresAtUtc { get; init; }

    public required string RefreshToken { get; init; }

    public OwnerSummaryResponse? Owner { get; init; }
}

