using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using FleetManagement.Data.Entities;
using FleetManagement.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;

namespace FleetManagement.Services.Seed;

public class SeedService : ISeedService
{
    private readonly FleetManagement.Data.FleetDbContext _dbContext;
    private readonly ILogger<SeedService> _logger;

    public SeedService(FleetManagement.Data.FleetDbContext dbContext, ILogger<SeedService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> SeedCitiesAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(csvStream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null
        };

        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<CityCsvRow>();

        var countries = await _dbContext.Countries.ToDictionaryAsync(country => country.IsoCode, cancellationToken);
        var existingCityKeys = (await _dbContext.Cities
            .AsNoTracking()
            .Select(city => city.Country!.IsoCode + "|" + city.Name)
            .ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var citiesToAdd = new List<City>();
        var duplicates = 0;

        foreach (var record in records)
        {
            if (!countries.TryGetValue(record.CountryIsoCode, out var country))
            {
                country = new Country
                {
                    IsoCode = record.CountryIsoCode,
                    Name = record.CountryName,
                    Continent = record.Continent
                };
                countries[record.CountryIsoCode] = country;
                await _dbContext.Countries.AddAsync(country, cancellationToken);
            }

            var cacheKey = $"{country.IsoCode}|{record.CityName}";
            if (existingCityKeys.Contains(cacheKey))
            {
                duplicates++;
                continue;
            }

            existingCityKeys.Add(cacheKey);

            citiesToAdd.Add(new City
            {
                Country = country,
                Name = record.CityName,
                PostalCode = record.PostalCode,
                PopulationMillions = record.PopulationMillions
            });
        }

        await _dbContext.Cities.AddRangeAsync(citiesToAdd, cancellationToken);
        var inserted = await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} cities ({Duplicates} duplicates skipped).", inserted, duplicates);
        return inserted;
    }

    private sealed record CityCsvRow
    {
        public string CountryName { get; init; } = string.Empty;
        public string CountryIsoCode { get; init; } = string.Empty;
        public string CityName { get; init; } = string.Empty;
        public string PostalCode { get; init; } = "NA";
        public decimal PopulationMillions { get; init; }
        public string Continent { get; init; } = string.Empty;
    }
}

