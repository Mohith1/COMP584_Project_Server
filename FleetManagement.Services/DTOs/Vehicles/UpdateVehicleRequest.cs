using System.ComponentModel.DataAnnotations;
using FleetManagement.Data.Enums;

namespace FleetManagement.Services.DTOs.Vehicles;

public sealed record UpdateVehicleRequest
{
    [Required]
    public Guid VehicleId { get; init; }

    [MaxLength(16)]
    public string? PlateNumber { get; init; }

    [MaxLength(64)]
    public string? Make { get; init; }

    [MaxLength(64)]
    public string? Model { get; init; }

    public VehicleStatus? Status { get; init; }
}

