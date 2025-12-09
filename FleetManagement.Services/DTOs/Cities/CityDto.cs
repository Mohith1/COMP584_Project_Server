namespace FleetManagement.Services.DTOs.Cities;

public class CityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public decimal PopulationMillions { get; set; }
    public Guid CountryId { get; set; }
    public string? CountryName { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class CreateCityDto
{
    public string Name { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public decimal PopulationMillions { get; set; }
    public Guid CountryId { get; set; }
}

public class UpdateCityDto
{
    public string Name { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public decimal PopulationMillions { get; set; }
    public Guid CountryId { get; set; }
}

