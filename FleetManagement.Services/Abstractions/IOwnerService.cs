using FleetManagement.Services.DTOs.Owners;

namespace FleetManagement.Services.Abstractions;

public interface IOwnerService
{
    Task<IEnumerable<OwnerDto>> GetAllOwnersAsync();
    Task<OwnerDto?> GetOwnerByIdAsync(Guid id);
    Task<OwnerDto?> GetOwnerByIdentityUserIdAsync(Guid identityUserId);
    /// <summary>
    /// Get owner by Auth0 user ID (string like "auth0|123456789")
    /// </summary>
    Task<OwnerDto?> GetOwnerByAuth0UserIdAsync(string auth0UserId);
    Task<OwnerDto> CreateOwnerAsync(CreateOwnerDto createDto);
    Task<OwnerDto?> UpdateOwnerAsync(Guid id, UpdateOwnerDto updateDto);
    Task<bool> DeleteOwnerAsync(Guid id);
}









