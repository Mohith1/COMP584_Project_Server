using System.ComponentModel.DataAnnotations;
using FleetManagement.Data.Common;

namespace FleetManagement.Data.Entities;

public class Fleet : BaseEntity
{
    [Required]
    [MaxLength(128)]
    public required string Name { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }

    public Guid OwnerId { get; set; }

    public Owner? Owner { get; set; }

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}

