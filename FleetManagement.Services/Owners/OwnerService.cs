using FleetManagement.Data;
using FleetManagement.Data.Entities;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Owners;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Services.Owners;

public class OwnerService : IOwnerService
{
    private readonly FleetDbContext _context;

    public OwnerService(FleetDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OwnerDto>> GetAllOwnersAsync()
    {
        var owners = await _context.Owners
            .Where(o => !o.IsDeleted)
            .Include(o => o.City)
            .ThenInclude(c => c.Country)
            .ToListAsync();

        return owners.Select(o => new OwnerDto
        {
            Id = o.Id,
            CompanyName = o.CompanyName,
            ContactEmail = o.ContactEmail,
            ContactPhone = o.ContactPhone,
            PrimaryContactName = o.PrimaryContactName,
            CityId = o.CityId,
            CityName = o.City.Name,
            CountryName = o.City.Country.Name,
            TimeZone = o.TimeZone,
            FleetCount = o.FleetCount,
            CreatedAtUtc = o.CreatedAtUtc,
            UpdatedAtUtc = o.UpdatedAtUtc
        });
    }

    public async Task<OwnerDto?> GetOwnerByIdAsync(Guid id)
    {
        var owner = await _context.Owners
            .Include(o => o.City)
            .ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (owner == null) return null;

        return new OwnerDto
        {
            Id = owner.Id,
            CompanyName = owner.CompanyName,
            ContactEmail = owner.ContactEmail,
            ContactPhone = owner.ContactPhone,
            PrimaryContactName = owner.PrimaryContactName,
            CityId = owner.CityId,
            CityName = owner.City.Name,
            CountryName = owner.City.Country.Name,
            TimeZone = owner.TimeZone,
            FleetCount = owner.FleetCount,
            CreatedAtUtc = owner.CreatedAtUtc,
            UpdatedAtUtc = owner.UpdatedAtUtc
        };
    }

    public async Task<OwnerDto?> GetOwnerByIdentityUserIdAsync(Guid identityUserId)
    {
        var owner = await _context.Owners
            .Include(o => o.City)
            .ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(o => o.IdentityUserId == identityUserId && !o.IsDeleted);

        if (owner == null) return null;

        return new OwnerDto
        {
            Id = owner.Id,
            CompanyName = owner.CompanyName,
            ContactEmail = owner.ContactEmail,
            ContactPhone = owner.ContactPhone,
            PrimaryContactName = owner.PrimaryContactName,
            CityId = owner.CityId,
            CityName = owner.City.Name,
            CountryName = owner.City.Country.Name,
            TimeZone = owner.TimeZone,
            FleetCount = owner.FleetCount,
            CreatedAtUtc = owner.CreatedAtUtc,
            UpdatedAtUtc = owner.UpdatedAtUtc
        };
    }

    public async Task<OwnerDto> CreateOwnerAsync(CreateOwnerDto createDto)
    {
        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            CompanyName = createDto.CompanyName,
            ContactEmail = createDto.ContactEmail,
            ContactPhone = createDto.ContactPhone,
            PrimaryContactName = createDto.PrimaryContactName,
            CityId = createDto.CityId,
            TimeZone = createDto.TimeZone,
            FleetCount = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false
        };

        _context.Owners.Add(owner);
        await _context.SaveChangesAsync();

        return await GetOwnerByIdAsync(owner.Id) ?? throw new InvalidOperationException("Failed to retrieve created owner");
    }

    public async Task<OwnerDto?> UpdateOwnerAsync(Guid id, UpdateOwnerDto updateDto)
    {
        var owner = await _context.Owners
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (owner == null) return null;

        owner.CompanyName = updateDto.CompanyName;
        owner.ContactEmail = updateDto.ContactEmail;
        owner.ContactPhone = updateDto.ContactPhone;
        owner.PrimaryContactName = updateDto.PrimaryContactName;
        owner.CityId = updateDto.CityId;
        owner.TimeZone = updateDto.TimeZone;
        owner.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return await GetOwnerByIdAsync(id);
    }

    public async Task<bool> DeleteOwnerAsync(Guid id)
    {
        var owner = await _context.Owners
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (owner == null) return false;

        owner.IsDeleted = true;
        owner.DeletedAtUtc = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }
}
