using FleetManagement.Services.DTOs.Cities;

namespace FleetManagement.Services.Abstractions;

public interface ICountryService
{
    Task<IEnumerable<CountryDto>> GetAllCountriesAsync();
    Task<CountryDto?> GetCountryByIdAsync(Guid id);
    Task<CountryDto> CreateCountryAsync(CreateCountryDto createDto);
    Task<CountryDto?> UpdateCountryAsync(Guid id, UpdateCountryDto updateDto);
    Task<bool> DeleteCountryAsync(Guid id);
}

