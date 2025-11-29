using System.ComponentModel.DataAnnotations;

namespace FleetManagement.Services.DTOs.Auth;

public sealed record OwnerRegisterRequest
{
    [Required]
    [MaxLength(128)]
    public required string CompanyName { get; init; }

    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Phone]
    public string? PhoneNumber { get; init; }

    [Required]
    [MaxLength(64)]
    public required string PrimaryContactName { get; init; }

    [Required]
    public Guid CityId { get; init; }

    [Required]
    [MinLength(12)]
    public required string Password { get; init; }

    [Compare(nameof(Password))]
    public required string ConfirmPassword { get; init; }
}

