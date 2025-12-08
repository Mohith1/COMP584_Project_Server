namespace FleetManagement.Data.Entities;

public class MaintenanceTicket
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; }
    public Guid VehicleId { get; set; }
    public DateTimeOffset? DueAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public Vehicle Vehicle { get; set; } = null!;
}

