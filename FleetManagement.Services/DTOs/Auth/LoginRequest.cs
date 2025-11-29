using System.ComponentModel.DataAnnotations;

namespace FleetManagement.Services.DTOs.Auth;

public sealed record LoginRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }

    public bool RememberMe { get; init; }
}

