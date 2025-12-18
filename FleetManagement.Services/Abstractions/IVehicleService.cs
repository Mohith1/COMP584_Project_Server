using FleetManagement.Services.DTOs.Vehicles;

namespace FleetManagement.Services.Abstractions;

public interface IVehicleService
{
    Task<IEnumerable<VehicleDto>> GetAllVehiclesAsync(Guid? fleetId = null);
    Task<VehicleDto?> GetVehicleByIdAsync(Guid id);
    Task<VehicleDto> CreateVehicleAsync(CreateVehicleDto createDto);
    Task<VehicleDto?> UpdateVehicleAsync(Guid id, UpdateVehicleDto updateDto);
    Task<bool> DeleteVehicleAsync(Guid id);
    Task<IEnumerable<TelemetryDto>> GetVehicleTelemetryAsync(Guid vehicleId);
    Task<IEnumerable<TelemetryDto>> GetLatestTelemetryAsync(IEnumerable<Guid> vehicleIds);
}











