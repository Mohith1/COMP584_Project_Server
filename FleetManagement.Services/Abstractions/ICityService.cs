using FleetManagement.Services.DTOs.Cities;

namespace FleetManagement.Services.Abstractions;

public interface ICityService
{
    Task<IEnumerable<CityDto>> GetAllCitiesAsync(Guid? countryId = null);
    Task<CityDto?> GetCityByIdAsync(Guid id);
    Task<CityDto> CreateCityAsync(CreateCityDto createDto);
    Task<CityDto?> UpdateCityAsync(Guid id, UpdateCityDto updateDto);
    Task<bool> DeleteCityAsync(Guid id);
}

