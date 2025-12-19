# CSV Migration Guide - Cities and Countries to Supabase

## Overview

This guide helps you migrate cities data from a CSV file to your Supabase database. This is essential for the Owner registration endpoint to work properly.

## CSV File Format

Your CSV file should have these columns (in any order):

| Column | Required | Description | Example |
|--------|----------|------------|---------|
| CityName | ✅ Yes | Name of the city | "New York" |
| CountryIsoCode | ✅ Yes | ISO country code | "US", "GB", "CA" |
| PostalCode | ✅ Yes | Postal/ZIP code | "10001", "SW1A 1AA" |
| PopulationMillions | ✅ Yes | Population in millions (decimal) | 8.4, 2.7 |
| CountryName | ⚠️ Optional | Full country name | "United States" |
| Continent | ⚠️ Optional | Continent name | "North America" |

### Sample CSV Format

```csv
CityName,CountryIsoCode,PostalCode,PopulationMillions,CountryName,Continent
New York,US,10001,8.4,United States,North America
Los Angeles,US,90001,3.9,United States,North America
London,GB,SW1A 1AA,9.0,United Kingdom,Europe
Toronto,CA,M5H 2N2,2.9,Canada,North America
```

## Migration Methods

### Method 1: Using Supabase Dashboard (Easiest) ⭐ Recommended

1. **Prepare your CSV file**
   - Ensure it follows the format above
   - Save as `cities.csv`

2. **Import Countries first** (if not already imported)
   - Go to Supabase Dashboard → Table Editor → Countries
   - Click "Insert" → "Import data from CSV"
   - Create a CSV with: `CountryName,CountryIsoCode,Continent`
   - Or manually insert a few countries

3. **Import Cities**
   - Go to Table Editor → Cities
   - Click "Insert" → "Import data from CSV"
   - Upload your `cities.csv` file
   - **Important**: You'll need to map `CountryIsoCode` to `CountryId`
   - Use this SQL to get Country IDs:
     ```sql
     SELECT "Id", "IsoCode", "Name" FROM "Countries";
     ```
   - Create a mapping or use the SQL function below

### Method 2: Using SQL Function (Recommended for Large Datasets)

1. **Run the SQL function** (from `SUPABASE_CSV_IMPORT.sql`):
   ```sql
   -- This creates a helper function
   -- See SUPABASE_CSV_IMPORT.sql for the function definition
   ```

2. **Import your CSV** using Supabase's CSV import, but map to a temporary table first

3. **Use the function** to import:
   ```sql
   -- Example
   SELECT import_city('New York', '10001', 8.4, 'US', 'United States', 'North America');
   ```

### Method 3: Using .NET Migration Tool

1. **Place your CSV file** in the `SeedData` folder
   - Example: `SeedData/cities.csv`

2. **Create a console app** or add to existing project:
   ```csharp
   using FleetManagement.Data;
   using FleetManagement.Seed;
   using Microsoft.EntityFrameworkCore;

   var connectionString = "Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require;";
   
   var options = new DbContextOptionsBuilder<FleetDbContext>()
       .UseNpgsql(connectionString)
       .Options;
   
   using var context = new FleetDbContext(options);
   var migrator = new CsvToSupabaseMigration(context);
   await migrator.MigrateCitiesFromCsvAsync("SeedData/cities.csv");
   ```

3. **Run the application**

### Method 4: Using PowerShell Script

1. **Install Npgsql** (if needed):
   ```powershell
   Install-Package -Name Npgsql -ProviderName NuGet
   ```

2. **Run the script**:
   ```powershell
   .\MigrateCities.ps1 -CsvPath "cities.csv" -ConnectionString "Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require;"
   ```

## Step-by-Step: Supabase Dashboard Method

### Step 1: Prepare Countries

If you don't have countries yet, create them first:

1. Go to Supabase → Table Editor → Countries
2. Click "Insert row"
3. Add countries manually or use SQL:
   ```sql
   INSERT INTO "Countries" ("Id", "Name", "IsoCode", "Continent", "CreatedAtUtc", "IsDeleted")
   VALUES 
       (gen_random_uuid(), 'United States', 'US', 'North America', NOW(), FALSE),
       (gen_random_uuid(), 'United Kingdom', 'GB', 'Europe', NOW(), FALSE),
       (gen_random_uuid(), 'Canada', 'CA', 'North America', NOW(), FALSE);
   ```

### Step 2: Prepare CSV File

1. Create `cities.csv` with your data
2. Ensure all required columns are present
3. Use UTF-8 encoding

### Step 3: Import Cities

**Option A: Using Supabase Import (Requires CountryId mapping)**

1. Go to Table Editor → Cities
2. Click "Insert" → "Import data from CSV"
3. Upload your CSV
4. **Problem**: CSV has `CountryIsoCode` but table needs `CountryId`
5. **Solution**: Use SQL import function (see Method 2)

**Option B: Using SQL with Temporary Table**

1. Create a temporary table:
   ```sql
   CREATE TEMP TABLE temp_cities (
       city_name VARCHAR(255),
       postal_code VARCHAR(20),
       population_millions DECIMAL(10,2),
       country_iso_code VARCHAR(10),
       country_name VARCHAR(255),
       continent VARCHAR(100)
   );
   ```

2. Import CSV into temp table (use Supabase import feature)

3. Run import:
   ```sql
   INSERT INTO "Cities" ("Id", "Name", "PostalCode", "PopulationMillions", "CountryId", "CreatedAtUtc", "IsDeleted")
   SELECT 
       gen_random_uuid(),
       city_name,
       postal_code,
       population_millions,
       (SELECT "Id" FROM "Countries" WHERE "IsoCode" = country_iso_code LIMIT 1),
       NOW(),
       FALSE
   FROM temp_cities
   WHERE NOT EXISTS (
       SELECT 1 FROM "Cities" 
       WHERE "Name" = temp_cities.city_name 
       AND "PostalCode" = temp_cities.postal_code
       AND "CountryId" = (SELECT "Id" FROM "Countries" WHERE "IsoCode" = temp_cities.country_iso_code LIMIT 1)
   );
   ```

## Verification

After migration, verify the data:

```sql
-- Check countries
SELECT COUNT(*) FROM "Countries" WHERE "IsDeleted" = FALSE;

-- Check cities
SELECT COUNT(*) FROM "Cities" WHERE "IsDeleted" = FALSE;

-- Check cities by country
SELECT c."Name" as Country, COUNT(ci."Id") as CityCount
FROM "Countries" c
LEFT JOIN "Cities" ci ON ci."CountryId" = c."Id" AND ci."IsDeleted" = FALSE
WHERE c."IsDeleted" = FALSE
GROUP BY c."Name"
ORDER BY CityCount DESC;

-- Sample cities
SELECT ci."Name", ci."PostalCode", c."Name" as Country
FROM "Cities" ci
JOIN "Countries" c ON ci."CountryId" = c."Id"
WHERE ci."IsDeleted" = FALSE
LIMIT 10;
```

## Testing the API

After migration, test the cities endpoint:

```bash
# Get all cities
curl https://fleetmanagement-api-production.up.railway.app/api/cities

# Get cities by country (first get country ID)
curl https://fleetmanagement-api-production.up.railway.app/api/cities?countryId=<country-guid>
```

## Troubleshooting

### Issue: "Country not found" errors
- **Solution**: Ensure countries are imported before cities
- Check that CountryIsoCode in CSV matches IsoCode in Countries table

### Issue: Duplicate cities
- The migration scripts check for duplicates
- Existing cities with same Name + PostalCode + CountryId will be skipped

### Issue: CSV encoding problems
- Save CSV as UTF-8 encoding
- Check for special characters in city names

### Issue: Population format errors
- Ensure PopulationMillions is a decimal number (e.g., 8.4, not "8.4 million")
- Use dot (.) as decimal separator

## Next Steps

After successful migration:
1. ✅ Verify cities are visible in Supabase Table Editor
2. ✅ Test `/api/cities` endpoint returns data
3. ✅ Test `/api/cities?countryId=xxx` filtering works
4. ✅ Test Owner registration can select cities

## Files Provided

- `cities_sample.csv` - Sample CSV format
- `CsvToSupabaseMigration.cs` - .NET migration tool
- `MigrateCities.ps1` - PowerShell migration script
- `SUPABASE_CSV_IMPORT.sql` - SQL functions and import scripts
- `CSV_MIGRATION_GUIDE.md` - This guide











