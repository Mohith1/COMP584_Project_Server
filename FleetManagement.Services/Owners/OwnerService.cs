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
            .ThenInclude(c => c!.Country)
            .ToListAsync();

        return owners.Select(MapToDto);
    }

    public async Task<OwnerDto?> GetOwnerByIdAsync(Guid id)
    {
        var owner = await _context.Owners
            .Include(o => o.City)
            .ThenInclude(c => c!.Country)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        return owner == null ? null : MapToDto(owner);
    }

    public async Task<OwnerDto?> GetOwnerByIdentityUserIdAsync(Guid identityUserId)
    {
        var owner = await _context.Owners
            .Include(o => o.City)
            .ThenInclude(c => c!.Country)
            .FirstOrDefaultAsync(o => o.IdentityUserId == identityUserId && !o.IsDeleted);

        return owner == null ? null : MapToDto(owner);
    }

    /// <summary>
    /// Get owner by Auth0 user ID (string like "auth0|123456789")
    /// </summary>
    public async Task<OwnerDto?> GetOwnerByAuth0UserIdAsync(string auth0UserId)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return null;

        var owner = await _context.Owners
            .Include(o => o.City)
            .ThenInclude(c => c!.Country)
            .FirstOrDefaultAsync(o => o.Auth0UserId == auth0UserId && !o.IsDeleted);

        return owner == null ? null : MapToDto(owner);
    }

    public async Task<OwnerDto> CreateOwnerAsync(CreateOwnerDto createDto)
    {
        // Validate CityId only if provided
        if (createDto.CityId.HasValue)
        {
            var cityExists = await _context.Cities.AnyAsync(c => c.Id == createDto.CityId.Value);
            if (!cityExists)
            {
                throw new ArgumentException($"City with ID {createDto.CityId} does not exist");
            }
        }

        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            CompanyName = createDto.CompanyName,
            ContactEmail = createDto.ContactEmail,
            ContactPhone = createDto.ContactPhone,
            PrimaryContactName = createDto.PrimaryContactName,
            CityId = createDto.CityId,  // Now nullable - can be null
            TimeZone = createDto.TimeZone,
            Auth0UserId = createDto.Auth0UserId,  // Link Auth0 account
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

        // Validate CityId only if provided
        if (updateDto.CityId.HasValue)
        {
            var cityExists = await _context.Cities.AnyAsync(c => c.Id == updateDto.CityId.Value);
            if (!cityExists)
            {
                throw new ArgumentException($"City with ID {updateDto.CityId} does not exist");
            }
        }

        owner.CompanyName = updateDto.CompanyName;
        owner.ContactEmail = updateDto.ContactEmail;
        owner.ContactPhone = updateDto.ContactPhone;
        owner.PrimaryContactName = updateDto.PrimaryContactName;
        owner.CityId = updateDto.CityId;  // Now nullable - can be null
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

    /// <summary>
    /// Maps Owner entity to OwnerDto, handling nullable City
    /// </summary>
    private static OwnerDto MapToDto(Owner owner)
    {
        return new OwnerDto
        {
            Id = owner.Id,
            CompanyName = owner.CompanyName,
            ContactEmail = owner.ContactEmail,
            ContactPhone = owner.ContactPhone,
            PrimaryContactName = owner.PrimaryContactName,
            CityId = owner.CityId,
            CityName = owner.City?.Name,  // Handle nullable
            CountryName = owner.City?.Country?.Name,  // Handle nullable
            TimeZone = owner.TimeZone,
            FleetCount = owner.FleetCount,
            Auth0UserId = owner.Auth0UserId,
            CreatedAtUtc = owner.CreatedAtUtc,
            UpdatedAtUtc = owner.UpdatedAtUtc
        };
    }
}
