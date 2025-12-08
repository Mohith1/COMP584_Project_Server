namespace FleetManagement.Data.Entities;

public class Owner
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? PrimaryContactName { get; set; }
    public Guid CityId { get; set; }
    public string? TimeZone { get; set; }
    public int FleetCount { get; set; }
    public string? OktaGroupId { get; set; }
    public Guid? IdentityUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public City City { get; set; } = null!;
    public ApplicationUser? IdentityUser { get; set; }
    public ICollection<Fleet> Fleets { get; set; } = new List<Fleet>();
    public ICollection<FleetUser> FleetUsers { get; set; } = new List<FleetUser>();
}

