using FleetManagement.Data;
using FleetManagement.Data.Entities;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Services.Vehicles;

public class VehicleService : IVehicleService
{
    private readonly FleetDbContext _context;

    public VehicleService(FleetDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<VehicleDto>> GetAllVehiclesAsync(Guid? fleetId = null)
    {
        var query = _context.Vehicles
            .Where(v => !v.IsDeleted)
            .Include(v => v.Fleet)
            .AsQueryable();

        if (fleetId.HasValue)
        {
            query = query.Where(v => v.FleetId == fleetId.Value);
        }

        var vehicles = await query.ToListAsync();

        return vehicles.Select(v => new VehicleDto
        {
            Id = v.Id,
            Vin = v.Vin,
            PlateNumber = v.PlateNumber,
            Make = v.Make,
            Model = v.Model,
            ModelYear = v.ModelYear,
            Status = v.Status,
            FleetId = v.FleetId,
            FleetName = v.Fleet?.Name,
            OwnerId = v.OwnerId,
            CreatedAtUtc = v.CreatedAtUtc,
            UpdatedAtUtc = v.UpdatedAtUtc
        });
    }

    public async Task<VehicleDto?> GetVehicleByIdAsync(Guid id)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Fleet)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null) return null;

        return new VehicleDto
        {
            Id = vehicle.Id,
            Vin = vehicle.Vin,
            PlateNumber = vehicle.PlateNumber,
            Make = vehicle.Make,
            Model = vehicle.Model,
            ModelYear = vehicle.ModelYear,
            Status = vehicle.Status,
            FleetId = vehicle.FleetId,
            FleetName = vehicle.Fleet?.Name,
            OwnerId = vehicle.OwnerId,
            CreatedAtUtc = vehicle.CreatedAtUtc,
            UpdatedAtUtc = vehicle.UpdatedAtUtc
        };
    }

    public async Task<VehicleDto> CreateVehicleAsync(CreateVehicleDto createDto)
    {
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Vin = createDto.Vin,
            PlateNumber = createDto.PlateNumber,
            Make = createDto.Make,
            Model = createDto.Model,
            ModelYear = createDto.ModelYear,
            Status = createDto.Status,
            FleetId = createDto.FleetId,
            OwnerId = createDto.OwnerId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        return await GetVehicleByIdAsync(vehicle.Id) ?? throw new InvalidOperationException("Failed to retrieve created vehicle");
    }

    public async Task<VehicleDto?> UpdateVehicleAsync(Guid id, UpdateVehicleDto updateDto)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null) return null;

        vehicle.Vin = updateDto.Vin;
        vehicle.PlateNumber = updateDto.PlateNumber;
        vehicle.Make = updateDto.Make;
        vehicle.Model = updateDto.Model;
        vehicle.ModelYear = updateDto.ModelYear;
        vehicle.Status = updateDto.Status;
        vehicle.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return await GetVehicleByIdAsync(id);
    }

    public async Task<bool> DeleteVehicleAsync(Guid id)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null) return false;

        vehicle.IsDeleted = true;
        vehicle.DeletedAtUtc = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<TelemetryDto>> GetVehicleTelemetryAsync(Guid vehicleId)
    {
        var telemetry = await _context.VehicleTelemetrySnapshots
            .Where(t => t.VehicleId == vehicleId && !t.IsDeleted)
            .Include(t => t.Vehicle)
            .OrderByDescending(t => t.CapturedAtUtc)
            .Take(100) // Limit to last 100 records
            .ToListAsync();

        return telemetry.Select(t => new TelemetryDto
        {
            Id = t.Id,
            VehicleId = t.VehicleId,
            VehicleVin = t.Vehicle?.Vin,
            Latitude = t.Latitude,
            Longitude = t.Longitude,
            SpeedKph = t.SpeedKph,
            FuelLevelPercentage = t.FuelLevelPercentage,
            CapturedAtUtc = t.CapturedAtUtc
        });
    }

    public async Task<IEnumerable<TelemetryDto>> GetLatestTelemetryAsync(IEnumerable<Guid> vehicleIds)
    {
        var vehicleIdList = vehicleIds.ToList();

        // Get latest telemetry for each vehicle
        var latestTelemetry = await _context.VehicleTelemetrySnapshots
            .Where(t => vehicleIdList.Contains(t.VehicleId) && !t.IsDeleted)
            .Include(t => t.Vehicle)
            .GroupBy(t => t.VehicleId)
            .Select(g => g.OrderByDescending(t => t.CapturedAtUtc).First())
            .ToListAsync();

        return latestTelemetry.Select(t => new TelemetryDto
        {
            Id = t.Id,
            VehicleId = t.VehicleId,
            VehicleVin = t.Vehicle?.Vin,
            Latitude = t.Latitude,
            Longitude = t.Longitude,
            SpeedKph = t.SpeedKph,
            FuelLevelPercentage = t.FuelLevelPercentage,
            CapturedAtUtc = t.CapturedAtUtc
        });
    }
}
