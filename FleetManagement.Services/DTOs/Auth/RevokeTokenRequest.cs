using System.ComponentModel.DataAnnotations;

namespace FleetManagement.Services.DTOs.Auth;

public sealed record RevokeTokenRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}

