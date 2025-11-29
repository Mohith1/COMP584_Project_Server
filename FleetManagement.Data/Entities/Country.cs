using System.ComponentModel.DataAnnotations;
using FleetManagement.Data.Common;

namespace FleetManagement.Data.Entities;

public class Country : BaseEntity
{
    [Required]
    [MaxLength(128)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(3)]
    public required string IsoCode { get; set; }

    [MaxLength(32)]
    public string? Continent { get; set; }

    public ICollection<City> Cities { get; set; } = new List<City>();
}

