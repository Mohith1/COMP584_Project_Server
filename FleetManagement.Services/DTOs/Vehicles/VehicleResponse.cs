using FleetManagement.Data.Enums;

namespace FleetManagement.Services.DTOs.Vehicles;

public sealed record VehicleResponse
{
    public required Guid Id { get; init; }

    public required string Vin { get; init; }

    public required string PlateNumber { get; init; }

    public string? Make { get; init; }

    public string? Model { get; init; }

    public int ModelYear { get; init; }

    public VehicleStatus Status { get; init; }
}

