using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FleetManagement.Data.Common;

namespace FleetManagement.Data.Entities;

public class City : BaseEntity
{
    [Required]
    [MaxLength(128)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(8)]
    public required string PostalCode { get; set; }

    [Column(TypeName = "decimal(11,2)")]
    public decimal PopulationMillions { get; set; }

    [Required]
    public Guid CountryId { get; set; }

    public Country? Country { get; set; }
}

