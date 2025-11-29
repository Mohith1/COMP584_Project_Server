using FleetManagement.Services.Common;
using FleetManagement.Services.DTOs.Fleets;
using FleetManagement.Services.DTOs.Vehicles;

namespace FleetManagement.Services.Abstractions;

public interface IFleetService
{
    Task<FleetResponse> CreateAsync(CreateFleetRequest request, CancellationToken cancellationToken = default);

    Task<FleetResponse> UpdateAsync(UpdateFleetRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid fleetId, CancellationToken cancellationToken = default);

    Task<PagedResult<FleetResponse>> GetByOwnerAsync(Guid ownerId, int page, int size, CancellationToken cancellationToken = default);

    Task<VehicleResponse> AddVehicleAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default);

    Task<VehicleResponse> UpdateVehicleAsync(UpdateVehicleRequest request, CancellationToken cancellationToken = default);

    Task DeleteVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<VehicleTelemetryResponse>> GetLatestTelemetryAsync(Guid ownerId, CancellationToken cancellationToken = default);
}

