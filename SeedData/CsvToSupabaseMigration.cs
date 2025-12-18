using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using FleetManagement.Data;
using FleetManagement.Data.Entities;

namespace FleetManagement.Seed;

/// <summary>
/// CSV Migration Tool for Cities and Countries
/// Usage: Place your CSV file in SeedData folder and update the connection string
/// </summary>
public class CsvToSupabaseMigration
{
    private readonly FleetDbContext _context;

    public CsvToSupabaseMigration(FleetDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Migrates cities from CSV file to Supabase
    /// CSV Format: CityName,CountryIsoCode,PostalCode,PopulationMillions,CountryName,Continent
    /// </summary>
    public async Task MigrateCitiesFromCsvAsync(string csvFilePath)
    {
        if (!File.Exists(csvFilePath))
        {
            throw new FileNotFoundException($"CSV file not found: {csvFilePath}");
        }

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null
        };

        var citiesData = new List<CityCsvRecord>();
        var countriesMap = new Dictionary<string, Guid>(); // ISO Code -> Country ID

        // Read CSV file
        using (var reader = new StreamReader(csvFilePath))
        using (var csv = new CsvReader(reader, csvConfig))
        {
            citiesData = csv.GetRecords<CityCsvRecord>().ToList();
        }

        Console.WriteLine($"Found {citiesData.Count} cities in CSV file");

        // Step 1: Create/Get Countries
        var uniqueCountries = citiesData
            .Where(c => !string.IsNullOrWhiteSpace(c.CountryIsoCode))
            .GroupBy(c => c.CountryIsoCode)
            .Select(g => new
            {
                IsoCode = g.Key,
                Name = g.First().CountryName ?? g.Key,
                Continent = g.First().Continent
            })
            .ToList();

        Console.WriteLine($"Processing {uniqueCountries.Count} unique countries...");

        foreach (var countryData in uniqueCountries)
        {
            var existingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.IsoCode == countryData.IsoCode && !c.IsDeleted);

            if (existingCountry == null)
            {
                var country = new Country
                {
                    Id = Guid.NewGuid(),
                    Name = countryData.Name,
                    IsoCode = countryData.IsoCode,
                    Continent = countryData.Continent,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    IsDeleted = false
                };

                _context.Countries.Add(country);
                await _context.SaveChangesAsync();
                countriesMap[countryData.IsoCode] = country.Id;
                Console.WriteLine($"Created country: {country.Name} ({country.IsoCode})");
            }
            else
            {
                countriesMap[countryData.IsoCode] = existingCountry.Id;
                Console.WriteLine($"Using existing country: {existingCountry.Name} ({existingCountry.IsoCode})");
            }
        }

        // Step 2: Create Cities
        Console.WriteLine($"Processing {citiesData.Count} cities...");
        int successCount = 0;
        int skipCount = 0;

        foreach (var cityData in citiesData)
        {
            if (string.IsNullOrWhiteSpace(cityData.CityName) || 
                string.IsNullOrWhiteSpace(cityData.PostalCode) ||
                string.IsNullOrWhiteSpace(cityData.CountryIsoCode))
            {
                Console.WriteLine($"Skipping invalid city record: {cityData.CityName}");
                skipCount++;
                continue;
            }

            if (!countriesMap.ContainsKey(cityData.CountryIsoCode))
            {
                Console.WriteLine($"Skipping city {cityData.CityName}: Country {cityData.CountryIsoCode} not found");
                skipCount++;
                continue;
            }

            // Check if city already exists
            var existingCity = await _context.Cities
                .FirstOrDefaultAsync(c => 
                    c.Name == cityData.CityName.Trim() &&
                    c.PostalCode == cityData.PostalCode.Trim() &&
                    c.CountryId == countriesMap[cityData.CountryIsoCode] &&
                    !c.IsDeleted);

            if (existingCity != null)
            {
                Console.WriteLine($"City already exists: {cityData.CityName}, {cityData.PostalCode}");
                skipCount++;
                continue;
            }

            var city = new City
            {
                Id = Guid.NewGuid(),
                Name = cityData.CityName.Trim(),
                PostalCode = cityData.PostalCode.Trim(),
                PopulationMillions = cityData.PopulationMillions,
                CountryId = countriesMap[cityData.CountryIsoCode],
                CreatedAtUtc = DateTimeOffset.UtcNow,
                IsDeleted = false
            };

            _context.Cities.Add(city);
            successCount++;

            // Batch insert every 100 records
            if (successCount % 100 == 0)
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"Inserted {successCount} cities...");
            }
        }

        // Save remaining cities
        if (successCount % 100 != 0)
        {
            await _context.SaveChangesAsync();
        }

        Console.WriteLine($"\nMigration Complete!");
        Console.WriteLine($"✅ Successfully imported: {successCount} cities");
        Console.WriteLine($"⏭️  Skipped: {skipCount} cities");
        Console.WriteLine($"🌍 Countries processed: {uniqueCountries.Count}");
    }

    /// <summary>
    /// CSV Record Model
    /// </summary>
    private class CityCsvRecord
    {
        public string CityName { get; set; } = string.Empty;
        public string CountryIsoCode { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public decimal PopulationMillions { get; set; }
        public string? CountryName { get; set; }
        public string? Continent { get; set; }
    }
}

// =====================================================
// USAGE INSTRUCTIONS
// =====================================================
// 1. Create a console app or add this to your existing project
// 2. Update connection string in Program.cs
// 3. Place your CSV file in SeedData folder
// 4. Run the migration
//
// Example Program.cs:
// ```
// var connectionString = "Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require;";
// var options = new DbContextOptionsBuilder<FleetDbContext>()
//     .UseNpgsql(connectionString)
//     .Options;
// var context = new FleetDbContext(options);
// var migrator = new CsvToSupabaseMigration(context);
// await migrator.MigrateCitiesFromCsvAsync("SeedData/cities.csv");
// ```
// =====================================================









