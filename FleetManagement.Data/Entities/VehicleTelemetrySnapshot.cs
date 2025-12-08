namespace FleetManagement.Data.Entities;

public class VehicleTelemetrySnapshot
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal SpeedKph { get; set; }
    public decimal FuelLevelPercentage { get; set; }
    public DateTimeOffset CapturedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public Vehicle Vehicle { get; set; } = null!;
}

