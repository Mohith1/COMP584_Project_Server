namespace FleetManagement.Services.DTOs.Cities;

public class CountryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IsoCode { get; set; } = string.Empty;
    public string? Continent { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class CreateCountryDto
{
    public string Name { get; set; } = string.Empty;
    public string IsoCode { get; set; } = string.Empty;
    public string? Continent { get; set; }
}

public class UpdateCountryDto
{
    public string Name { get; set; } = string.Empty;
    public string IsoCode { get; set; } = string.Empty;
    public string? Continent { get; set; }
}











