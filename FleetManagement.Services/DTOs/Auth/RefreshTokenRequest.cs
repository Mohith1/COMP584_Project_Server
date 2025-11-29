using System.ComponentModel.DataAnnotations;

namespace FleetManagement.Services.DTOs.Auth;

public sealed record RefreshTokenRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}

