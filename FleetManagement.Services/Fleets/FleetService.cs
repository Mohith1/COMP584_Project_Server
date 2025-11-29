using System.Linq;
using FleetManagement.Data.Entities;
using FleetManagement.Data.Enums;
using FleetManagement.Data.Repositories;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.Common;
using FleetManagement.Services.DTOs.Fleets;
using FleetManagement.Services.DTOs.Vehicles;
using FleetManagement.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FleetManagement.Services.Fleets;

public class FleetService : IFleetService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FleetService> _logger;

    public FleetService(IUnitOfWork unitOfWork, ILogger<FleetService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FleetResponse> CreateAsync(CreateFleetRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureOwnerExists(request.OwnerId, cancellationToken);

        var fleet = new Fleet
        {
            OwnerId = request.OwnerId,
            Name = request.Name,
            Description = request.Description
        };

        await _unitOfWork.Repository<Fleet>().AddAsync(fleet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Fleet {FleetName} created for owner {OwnerId}", fleet.Name, fleet.OwnerId);

        return MapFleet(fleet, 0);
    }

    public async Task<FleetResponse> UpdateAsync(UpdateFleetRequest request, CancellationToken cancellationToken = default)
    {
        var fleet = await _unitOfWork.Repository<Fleet>()
            .Queryable
            .Include(f => f.Vehicles)
            .FirstOrDefaultAsync(f => f.Id == request.FleetId, cancellationToken);

        if (fleet is null)
        {
            throw new NotFoundException(nameof(Fleet), request.FleetId.ToString());
        }

        fleet.Name = request.Name ?? fleet.Name;
        fleet.Description = request.Description ?? fleet.Description;
        _unitOfWork.Repository<Fleet>().Update(fleet);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapFleet(fleet, fleet.Vehicles.Count);
    }

    public async Task DeleteAsync(Guid fleetId, CancellationToken cancellationToken = default)
    {
        var fleet = await _unitOfWork.Repository<Fleet>().GetByIdAsync(fleetId, cancellationToken);
        if (fleet is null)
        {
            throw new NotFoundException(nameof(Fleet), fleetId.ToString());
        }

        _unitOfWork.Repository<Fleet>().Remove(fleet);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<FleetResponse>> GetByOwnerAsync(Guid ownerId, int page, int size, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<Fleet>().Queryable
            .Where(fleet => fleet.OwnerId == ownerId)
            .Include(fleet => fleet.Vehicles);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(fleet => MapFleet(fleet, fleet.Vehicles.Count))
            .ToListAsync(cancellationToken);

        return new PagedResult<FleetResponse>(items, page, size, total);
    }

    public async Task<VehicleResponse> AddVehicleAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var fleet = await _unitOfWork.Repository<Fleet>().GetByIdAsync(request.FleetId, cancellationToken);
        if (fleet is null)
        {
            throw new NotFoundException(nameof(Fleet), request.FleetId.ToString());
        }

        var vehicle = new Vehicle
        {
            FleetId = request.FleetId,
            Vin = request.Vin,
            PlateNumber = request.PlateNumber,
            Make = request.Make,
            Model = request.Model,
            ModelYear = request.ModelYear,
            Status = request.Status
        };

        await _unitOfWork.Repository<Vehicle>().AddAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapVehicle(vehicle);
    }

    public async Task<VehicleResponse> UpdateVehicleAsync(UpdateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var vehicle = await _unitOfWork.Repository<Vehicle>().GetByIdAsync(request.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            throw new NotFoundException(nameof(Vehicle), request.VehicleId.ToString());
        }

        vehicle.PlateNumber = request.PlateNumber ?? vehicle.PlateNumber;
        vehicle.Make = request.Make ?? vehicle.Make;
        vehicle.Model = request.Model ?? vehicle.Model;
        vehicle.Status = request.Status ?? vehicle.Status;

        _unitOfWork.Repository<Vehicle>().Update(vehicle);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapVehicle(vehicle);
    }

    public async Task DeleteVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var vehicle = await _unitOfWork.Repository<Vehicle>().GetByIdAsync(vehicleId, cancellationToken);
        if (vehicle is null)
        {
            throw new NotFoundException(nameof(Vehicle), vehicleId.ToString());
        }

        _unitOfWork.Repository<Vehicle>().Remove(vehicle);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<VehicleTelemetryResponse>> GetLatestTelemetryAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var vehicles = await _unitOfWork.Repository<Vehicle>()
            .Queryable
            .Where(vehicle => vehicle.Fleet != null && vehicle.Fleet.OwnerId == ownerId)
            .Select(vehicle => new
            {
                vehicle.Id,
                vehicle.Vin,
                LatestTelemetry = vehicle.TelemetrySnapshots!
                    .OrderByDescending(snapshot => snapshot.CapturedAtUtc)
                    .Select(snapshot => new VehicleTelemetryResponse
                    {
                        SnapshotId = snapshot.Id,
                        VehicleId = vehicle.Id,
                        Latitude = snapshot.Latitude,
                        Longitude = snapshot.Longitude,
                        SpeedKph = snapshot.SpeedKph,
                        FuelLevelPercentage = snapshot.FuelLevelPercentage,
                        CapturedAtUtc = snapshot.CapturedAtUtc
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return vehicles
            .Where(vehicle => vehicle.LatestTelemetry != null)
            .Select(vehicle => vehicle.LatestTelemetry!)
            .ToList();
    }

    private static FleetResponse MapFleet(Fleet fleet, int vehicleCount) =>
        new()
        {
            Id = fleet.Id,
            Name = fleet.Name,
            Description = fleet.Description,
            VehicleCount = vehicleCount
        };

    private static VehicleResponse MapVehicle(Vehicle vehicle) =>
        new()
        {
            Id = vehicle.Id,
            Vin = vehicle.Vin,
            PlateNumber = vehicle.PlateNumber,
            Make = vehicle.Make,
            Model = vehicle.Model,
            ModelYear = vehicle.ModelYear,
            Status = vehicle.Status
        };

    private async Task EnsureOwnerExists(Guid ownerId, CancellationToken cancellationToken)
    {
        var exists = await _unitOfWork.Repository<Owner>().Queryable.AnyAsync(owner => owner.Id == ownerId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException(nameof(Owner), ownerId.ToString());
        }
    }
}

