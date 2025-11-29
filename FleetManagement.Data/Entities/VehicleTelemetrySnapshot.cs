using System.ComponentModel.DataAnnotations.Schema;
using FleetManagement.Data.Common;

namespace FleetManagement.Data.Entities;

public class VehicleTelemetrySnapshot : BaseEntity
{
    public Guid VehicleId { get; set; }

    public Vehicle? Vehicle { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal Latitude { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal Longitude { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal SpeedKph { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal FuelLevelPercentage { get; set; }

    public DateTimeOffset CapturedAtUtc { get; set; }
}

