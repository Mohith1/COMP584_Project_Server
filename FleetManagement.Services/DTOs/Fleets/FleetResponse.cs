namespace FleetManagement.Services.DTOs.Fleets;

public sealed record FleetResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public int VehicleCount { get; init; }
}

