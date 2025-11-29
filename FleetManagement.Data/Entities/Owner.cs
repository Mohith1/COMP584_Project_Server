using System.ComponentModel.DataAnnotations;
using FleetManagement.Data.Common;

namespace FleetManagement.Data.Entities;

public class Owner : BaseEntity
{
    [Required]
    [MaxLength(128)]
    public required string CompanyName { get; set; }

    [Required]
    [MaxLength(256)]
    public required string ContactEmail { get; set; }

    [MaxLength(32)]
    public string? ContactPhone { get; set; }

    [MaxLength(128)]
    public string? PrimaryContactName { get; set; }

    [Required]
    public Guid CityId { get; set; }

    public City? City { get; set; }

    [MaxLength(64)]
    public string? TimeZone { get; set; }

    public int FleetCount { get; set; }

    [MaxLength(64)]
    public string? OktaGroupId { get; set; }

    public Guid? IdentityUserId { get; set; }

    public ApplicationUser? IdentityUser { get; set; }

    public ICollection<Fleet> Fleets { get; set; } = new List<Fleet>();

    public ICollection<FleetUser> FleetUsers { get; set; } = new List<FleetUser>();

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}

