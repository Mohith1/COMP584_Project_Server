using FleetManagement.Services.DTOs.Owners;

namespace FleetManagement.Services.Abstractions;

public interface IOwnerService
{
    Task<OwnerDetailResponse> GetAsync(Guid ownerId, CancellationToken cancellationToken = default);

    Task<OwnerDetailResponse> UpdateAsync(UpdateOwnerRequest request, CancellationToken cancellationToken = default);
}

