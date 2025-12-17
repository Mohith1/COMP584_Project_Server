using FleetManagement.Data;
using FleetManagement.Data.Entities;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Cities;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Services.Cities;

public class CityService : ICityService
{
    private readonly FleetDbContext _context;

    public CityService(FleetDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CityDto>> GetAllCitiesAsync(Guid? countryId = null)
    {
        var query = _context.Cities
            .Where(c => !c.IsDeleted)
            .Include(c => c.Country)
            .AsQueryable();

        if (countryId.HasValue)
        {
            query = query.Where(c => c.CountryId == countryId.Value);
        }

        var cities = await query.ToListAsync();

        return cities.Select(c => new CityDto
        {
            Id = c.Id,
            Name = c.Name,
            PostalCode = c.PostalCode,
            PopulationMillions = c.PopulationMillions,
            CountryId = c.CountryId,
            CountryName = c.Country.Name,
            CreatedAtUtc = c.CreatedAtUtc
        });
    }

    public async Task<CityDto?> GetCityByIdAsync(Guid id)
    {
        var city = await _context.Cities
            .Include(c => c.Country)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (city == null) return null;

        return new CityDto
        {
            Id = city.Id,
            Name = city.Name,
            PostalCode = city.PostalCode,
            PopulationMillions = city.PopulationMillions,
            CountryId = city.CountryId,
            CountryName = city.Country.Name,
            CreatedAtUtc = city.CreatedAtUtc
        };
    }

    public async Task<CityDto> CreateCityAsync(CreateCityDto createDto)
    {
        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            PostalCode = createDto.PostalCode,
            PopulationMillions = createDto.PopulationMillions,
            CountryId = createDto.CountryId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false
        };

        _context.Cities.Add(city);
        await _context.SaveChangesAsync();

        return await GetCityByIdAsync(city.Id) ?? throw new InvalidOperationException("Failed to retrieve created city");
    }

    public async Task<CityDto?> UpdateCityAsync(Guid id, UpdateCityDto updateDto)
    {
        var city = await _context.Cities
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (city == null) return null;

        city.Name = updateDto.Name;
        city.PostalCode = updateDto.PostalCode;
        city.PopulationMillions = updateDto.PopulationMillions;
        city.CountryId = updateDto.CountryId;
        city.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return await GetCityByIdAsync(id);
    }

    public async Task<bool> DeleteCityAsync(Guid id)
    {
        var city = await _context.Cities
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (city == null) return false;

        city.IsDeleted = true;
        city.DeletedAtUtc = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }
}









