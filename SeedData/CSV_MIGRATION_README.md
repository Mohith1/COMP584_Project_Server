# CSV Data Migration to Supabase

## CSV File Format

Your CSV file should have one of these formats:

### Option 1: Cities with Country Name
```csv
CityName,CountryName,PostalCode,PopulationMillions,CountryIsoCode,Continent
New York,United States,10001,8.4,US,North America
Los Angeles,United States,90001,3.9,US,North America
London,United Kingdom,SW1A 1AA,9.0,GB,Europe
```

### Option 2: Cities with Country ISO Code Only
```csv
CityName,CountryIsoCode,PostalCode,PopulationMillions,Continent
New York,US,10001,8.4,North America
Los Angeles,US,90001,3.9,North America
London,GB,SW1A 1AA,9.0,Europe
```

### Required Columns:
- **CityName** (required) - Name of the city
- **PostalCode** (required) - Postal/ZIP code
- **PopulationMillions** (required) - Population in millions (decimal)
- **CountryIsoCode** (required) - ISO country code (e.g., US, GB, CA)
- **CountryName** (optional) - Full country name (if not provided, will use ISO code)
- **Continent** (optional) - Continent name

## Migration Methods

### Method 1: Using .NET Migration Tool (Recommended)
See `CsvToSupabaseMigration.cs` - Run this console app to import your CSV.

### Method 2: Using SQL Script
See `SUPABASE_CSV_IMPORT.sql` - Use Supabase's built-in CSV import feature.

### Method 3: Using Supabase Dashboard
1. Go to Table Editor → Cities
2. Click "Insert" → "Import data from CSV"
3. Upload your CSV file
4. Map columns correctly

## Next Steps

1. Place your CSV file in the `SeedData` folder
2. Update the connection string in the migration tool
3. Run the migration
4. Verify data in Supabase








