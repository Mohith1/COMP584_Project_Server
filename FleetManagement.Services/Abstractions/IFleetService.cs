using FleetManagement.Services.DTOs.Fleets;

namespace FleetManagement.Services.Abstractions;

public interface IFleetService
{
    Task<IEnumerable<FleetDto>> GetAllFleetsAsync(Guid? ownerId = null);
    Task<FleetDto?> GetFleetByIdAsync(Guid id);
    Task<FleetDto> CreateFleetAsync(CreateFleetDto createDto);
    Task<FleetDto?> UpdateFleetAsync(Guid id, UpdateFleetDto updateDto);
    Task<bool> DeleteFleetAsync(Guid id);
}

