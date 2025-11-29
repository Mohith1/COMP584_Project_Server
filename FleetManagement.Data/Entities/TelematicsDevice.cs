using System.ComponentModel.DataAnnotations;
using FleetManagement.Data.Common;

namespace FleetManagement.Data.Entities;

public class TelematicsDevice : BaseEntity
{
    [Required]
    [MaxLength(32)]
    public required string SerialNumber { get; set; }

    [MaxLength(32)]
    public string? Iccid { get; set; }

    [MaxLength(32)]
    public string? Imei { get; set; }

    [MaxLength(32)]
    public string? FirmwareVersion { get; set; }

    public Guid VehicleId { get; set; }

    public Vehicle? Vehicle { get; set; }

    public DateTimeOffset? LastSyncUtc { get; set; }
}

