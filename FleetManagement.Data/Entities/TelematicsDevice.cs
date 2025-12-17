namespace FleetManagement.Data.Entities;

public class TelematicsDevice
{
    public Guid Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string? Iccid { get; set; }
    public string? Imei { get; set; }
    public string? FirmwareVersion { get; set; }
    public Guid VehicleId { get; set; }
    public DateTimeOffset? LastSyncUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public Vehicle Vehicle { get; set; } = null!;
}












