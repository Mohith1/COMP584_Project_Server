namespace FleetManagement.Data.Entities;

public class City
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public decimal PopulationMillions { get; set; }
    public Guid CountryId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public Country Country { get; set; } = null!;
    public ICollection<Owner> Owners { get; set; } = new List<Owner>();
}












