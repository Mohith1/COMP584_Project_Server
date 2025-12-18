namespace FleetManagement.Services.DTOs.Vehicles;

public class TelemetryDto
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public string? VehicleVin { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal SpeedKph { get; set; }
    public decimal FuelLevelPercentage { get; set; }
    public DateTimeOffset CapturedAtUtc { get; set; }
}










