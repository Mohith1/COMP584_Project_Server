using FleetManagement.Data;
using FleetManagement.Data.Entities;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Fleets;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Services.Fleets;

public class FleetService : IFleetService
{
    private readonly FleetDbContext _context;

    public FleetService(FleetDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FleetDto>> GetAllFleetsAsync(Guid? ownerId = null)
    {
        var query = _context.Fleets
            .Where(f => !f.IsDeleted)
            .Include(f => f.Owner)
            .AsQueryable();

        if (ownerId.HasValue)
        {
            query = query.Where(f => f.OwnerId == ownerId.Value);
        }

        var fleets = await query.ToListAsync();

        return fleets.Select(f => new FleetDto
        {
            Id = f.Id,
            Name = f.Name,
            Description = f.Description,
            OwnerId = f.OwnerId,
            OwnerName = f.Owner.CompanyName,
            VehicleCount = f.Vehicles.Count(v => !v.IsDeleted),
            CreatedAtUtc = f.CreatedAtUtc,
            UpdatedAtUtc = f.UpdatedAtUtc
        });
    }

    public async Task<FleetDto?> GetFleetByIdAsync(Guid id)
    {
        var fleet = await _context.Fleets
            .Include(f => f.Owner)
            .Include(f => f.Vehicles)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (fleet == null) return null;

        return new FleetDto
        {
            Id = fleet.Id,
            Name = fleet.Name,
            Description = fleet.Description,
            OwnerId = fleet.OwnerId,
            OwnerName = fleet.Owner.CompanyName,
            VehicleCount = fleet.Vehicles.Count(v => !v.IsDeleted),
            CreatedAtUtc = fleet.CreatedAtUtc,
            UpdatedAtUtc = fleet.UpdatedAtUtc
        };
    }

    public async Task<FleetDto> CreateFleetAsync(CreateFleetDto createDto)
    {
        var fleet = new Fleet
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Description = createDto.Description,
            OwnerId = createDto.OwnerId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false
        };

        _context.Fleets.Add(fleet);
        await _context.SaveChangesAsync();

        return await GetFleetByIdAsync(fleet.Id) ?? throw new InvalidOperationException("Failed to retrieve created fleet");
    }

    public async Task<FleetDto?> UpdateFleetAsync(Guid id, UpdateFleetDto updateDto)
    {
        var fleet = await _context.Fleets
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (fleet == null) return null;

        fleet.Name = updateDto.Name;
        fleet.Description = updateDto.Description;
        fleet.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return await GetFleetByIdAsync(id);
    }

    public async Task<bool> DeleteFleetAsync(Guid id)
    {
        var fleet = await _context.Fleets
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (fleet == null) return false;

        fleet.IsDeleted = true;
        fleet.DeletedAtUtc = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }
}

