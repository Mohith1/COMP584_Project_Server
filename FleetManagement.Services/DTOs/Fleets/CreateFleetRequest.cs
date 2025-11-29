using System.ComponentModel.DataAnnotations;

namespace FleetManagement.Services.DTOs.Fleets;

public sealed record CreateFleetRequest
{
    [Required]
    public Guid OwnerId { get; init; }

    [Required]
    [MaxLength(128)]
    public required string Name { get; init; }

    [MaxLength(512)]
    public string? Description { get; init; }
}

