using System.ComponentModel.DataAnnotations;
using FleetManagement.Data.Common;
using FleetManagement.Data.Enums;

namespace FleetManagement.Data.Entities;

public class MaintenanceTicket : BaseEntity
{
    [Required]
    [MaxLength(128)]
    public required string Title { get; set; }

    [MaxLength(1024)]
    public string? Description { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public Guid VehicleId { get; set; }

    public Vehicle? Vehicle { get; set; }

    public DateTimeOffset? DueAtUtc { get; set; }
}

