namespace FleetManagement.Services.DTOs.Vehicles;

public sealed record VehicleTelemetryResponse
{
    public Guid SnapshotId { get; init; }

    public Guid VehicleId { get; init; }

    public decimal Latitude { get; init; }

    public decimal Longitude { get; init; }

    public decimal SpeedKph { get; init; }

    public decimal FuelLevelPercentage { get; init; }

    public DateTimeOffset CapturedAtUtc { get; init; }
}

