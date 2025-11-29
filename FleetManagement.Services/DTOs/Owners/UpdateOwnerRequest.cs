using System.ComponentModel.DataAnnotations;

namespace FleetManagement.Services.DTOs.Owners;

public sealed record UpdateOwnerRequest
{
    [Required]
    public Guid OwnerId { get; init; }

    [MaxLength(32)]
    public string? ContactPhone { get; init; }

    [MaxLength(128)]
    public string? TimeZone { get; init; }

    public Guid? CityId { get; init; }
}

