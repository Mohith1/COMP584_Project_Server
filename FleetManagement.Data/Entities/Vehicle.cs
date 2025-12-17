namespace FleetManagement.Data.Entities;

public class Vehicle
{
    public Guid Id { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int ModelYear { get; set; }
    public int Status { get; set; }
    public Guid FleetId { get; set; }
    public Guid? OwnerId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public Fleet Fleet { get; set; } = null!;
    public Owner? Owner { get; set; }
    public ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();
    public ICollection<TelematicsDevice> TelematicsDevices { get; set; } = new List<TelematicsDevice>();
    public ICollection<VehicleTelemetrySnapshot> TelemetrySnapshots { get; set; } = new List<VehicleTelemetrySnapshot>();
    public ICollection<FleetUser> AssignedFleetUsers { get; set; } = new List<FleetUser>();
}












