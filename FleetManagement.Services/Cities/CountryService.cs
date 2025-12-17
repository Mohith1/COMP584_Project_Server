using FleetManagement.Data;
using FleetManagement.Data.Entities;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Cities;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Services.Cities;

public class CountryService : ICountryService
{
    private readonly FleetDbContext _context;

    public CountryService(FleetDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CountryDto>> GetAllCountriesAsync()
    {
        var countries = await _context.Countries
            .Where(c => !c.IsDeleted)
            .ToListAsync();

        return countries.Select(c => new CountryDto
        {
            Id = c.Id,
            Name = c.Name,
            IsoCode = c.IsoCode,
            Continent = c.Continent,
            CreatedAtUtc = c.CreatedAtUtc
        });
    }

    public async Task<CountryDto?> GetCountryByIdAsync(Guid id)
    {
        var country = await _context.Countries
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (country == null) return null;

        return new CountryDto
        {
            Id = country.Id,
            Name = country.Name,
            IsoCode = country.IsoCode,
            Continent = country.Continent,
            CreatedAtUtc = country.CreatedAtUtc
        };
    }

    public async Task<CountryDto> CreateCountryAsync(CreateCountryDto createDto)
    {
        var country = new Country
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            IsoCode = createDto.IsoCode,
            Continent = createDto.Continent,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false
        };

        _context.Countries.Add(country);
        await _context.SaveChangesAsync();

        return await GetCountryByIdAsync(country.Id) ?? throw new InvalidOperationException("Failed to retrieve created country");
    }

    public async Task<CountryDto?> UpdateCountryAsync(Guid id, UpdateCountryDto updateDto)
    {
        var country = await _context.Countries
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (country == null) return null;

        country.Name = updateDto.Name;
        country.IsoCode = updateDto.IsoCode;
        country.Continent = updateDto.Continent;
        country.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return await GetCountryByIdAsync(id);
    }

    public async Task<bool> DeleteCountryAsync(Guid id)
    {
        var country = await _context.Countries
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (country == null) return false;

        country.IsDeleted = true;
        country.DeletedAtUtc = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }
}









