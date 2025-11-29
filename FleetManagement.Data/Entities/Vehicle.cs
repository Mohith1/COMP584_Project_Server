using System.ComponentModel.DataAnnotations;
using FleetManagement.Data.Common;
using FleetManagement.Data.Enums;

namespace FleetManagement.Data.Entities;

public class Vehicle : BaseEntity
{
    [Required]
    [MaxLength(17)]
    public required string Vin { get; set; }

    [Required]
    [MaxLength(16)]
    public required string PlateNumber { get; set; }

    [MaxLength(64)]
    public string? Make { get; set; }

    [MaxLength(64)]
    public string? Model { get; set; }

    public int ModelYear { get; set; }

    public VehicleStatus Status { get; set; } = VehicleStatus.Available;

    public Guid FleetId { get; set; }

    public Fleet? Fleet { get; set; }

    public ICollection<VehicleTelemetrySnapshot> TelemetrySnapshots { get; set; } = new List<VehicleTelemetrySnapshot>();

    public ICollection<FleetUser> AssignedDrivers { get; set; } = new List<FleetUser>();

    public TelematicsDevice? TelematicsDevice { get; set; }

    public ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();
}

