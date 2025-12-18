namespace FleetManagement.Services.DTOs.Owners;

public class OwnerDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? PrimaryContactName { get; set; }
    public Guid? CityId { get; set; }  // Made nullable - cityId is optional
    public string? CityName { get; set; }
    public string? CountryName { get; set; }
    public string? TimeZone { get; set; }
    public int FleetCount { get; set; }
    public string? Auth0UserId { get; set; }  // Auth0 user ID for reference
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}

public class CreateOwnerDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? PrimaryContactName { get; set; }
    public Guid? CityId { get; set; }  // Made nullable - cityId is optional
    public string? TimeZone { get; set; }
    public string? Auth0UserId { get; set; }  // Auth0 user ID to link account
}

public class UpdateOwnerDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? PrimaryContactName { get; set; }
    public Guid? CityId { get; set; }  // Made nullable - cityId is optional
    public string? TimeZone { get; set; }
}











