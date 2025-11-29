using System.ComponentModel.DataAnnotations;
using FleetManagement.Data.Enums;

namespace FleetManagement.Services.DTOs.Vehicles;

public sealed record CreateVehicleRequest
{
    [Required]
    public Guid FleetId { get; init; }

    [Required]
    [StringLength(17, MinimumLength = 11)]
    public required string Vin { get; init; }

    [Required]
    [MaxLength(16)]
    public required string PlateNumber { get; init; }

    [MaxLength(64)]
    public string? Make { get; init; }

    [MaxLength(64)]
    public string? Model { get; init; }

    public int ModelYear { get; init; }

    public VehicleStatus Status { get; init; } = VehicleStatus.Available;
}

