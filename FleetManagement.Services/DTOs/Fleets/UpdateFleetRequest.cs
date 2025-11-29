using System.ComponentModel.DataAnnotations;

namespace FleetManagement.Services.DTOs.Fleets;

public sealed record UpdateFleetRequest
{
    [Required]
    public Guid FleetId { get; init; }

    [MaxLength(128)]
    public string? Name { get; init; }

    [MaxLength(512)]
    public string? Description { get; init; }
}

