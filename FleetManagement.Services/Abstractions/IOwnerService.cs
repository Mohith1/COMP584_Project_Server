using FleetManagement.Services.DTOs.Owners;

namespace FleetManagement.Services.Abstractions;

public interface IOwnerService
{
    Task<IEnumerable<OwnerDto>> GetAllOwnersAsync();
    Task<OwnerDto?> GetOwnerByIdAsync(Guid id);
    Task<OwnerDto> CreateOwnerAsync(CreateOwnerDto createDto);
    Task<OwnerDto?> UpdateOwnerAsync(Guid id, UpdateOwnerDto updateDto);
    Task<bool> DeleteOwnerAsync(Guid id);
}

