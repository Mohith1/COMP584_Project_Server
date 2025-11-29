using System.ComponentModel.DataAnnotations;
using FleetManagement.Data.Common;
using FleetManagement.Data.Enums;

namespace FleetManagement.Data.Entities;

public class FleetUser : BaseEntity
{
    [Required]
    [MaxLength(64)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(64)]
    public required string LastName { get; set; }

    [Required]
    [MaxLength(256)]
    public required string Email { get; set; }

    [MaxLength(32)]
    public string? PhoneNumber { get; set; }

    [MaxLength(64)]
    public string? OktaUserId { get; set; }

    [Required]
    public FleetUserRole Role { get; set; }

    public Guid OwnerId { get; set; }

    public Owner? Owner { get; set; }

    public Guid? AssignedVehicleId { get; set; }

    public Vehicle? AssignedVehicle { get; set; }
}

