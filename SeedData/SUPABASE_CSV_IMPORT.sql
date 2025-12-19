-- =====================================================
-- Supabase CSV Import Script
-- =====================================================
-- This script provides SQL commands to import CSV data
-- Use Supabase's Table Editor → Import CSV feature for easier import
-- =====================================================

-- =====================================================
-- METHOD 1: Using Supabase Table Editor (EASIEST)
-- =====================================================
-- 1. Go to Supabase Dashboard → Table Editor → Cities
-- 2. Click "Insert" → "Import data from CSV"
-- 3. Upload your CSV file
-- 4. Map columns:
--    - CityName → Name
--    - PostalCode → PostalCode
--    - PopulationMillions → PopulationMillions
--    - CountryIsoCode → (you'll need to look up CountryId)
--
-- NOTE: You'll need to import Countries first, then Cities
-- =====================================================

-- =====================================================
-- METHOD 2: Using COPY command (if CSV is on server)
-- =====================================================
-- First, ensure you have countries in the database
-- Then use COPY command to import cities

-- Example: Import cities from CSV (adjust path and columns)
/*
COPY "Cities"("Name", "PostalCode", "PopulationMillions", "CountryId", "CreatedAtUtc", "IsDeleted")
FROM '/path/to/cities.csv'
WITH (FORMAT csv, HEADER true, DELIMITER ',');
*/

-- =====================================================
-- METHOD 3: Manual INSERT statements (for small datasets)
-- =====================================================

-- First, insert countries if not exists
INSERT INTO "Countries" ("Id", "Name", "IsoCode", "Continent", "CreatedAtUtc", "IsDeleted")
VALUES 
    (uuid_generate_v4(), 'United States', 'US', 'North America', NOW(), FALSE),
    (uuid_generate_v4(), 'United Kingdom', 'GB', 'Europe', NOW(), FALSE),
    (uuid_generate_v4(), 'Canada', 'CA', 'North America', NOW(), FALSE)
ON CONFLICT DO NOTHING;

-- Then insert cities (replace CountryId with actual UUID from Countries table)
-- Get country IDs first:
-- SELECT "Id", "IsoCode" FROM "Countries" WHERE "IsoCode" = 'US';

-- Example city insert:
/*
INSERT INTO "Cities" ("Id", "Name", "PostalCode", "PopulationMillions", "CountryId", "CreatedAtUtc", "IsDeleted")
SELECT 
    uuid_generate_v4(),
    'New York',
    '10001',
    8.4,
    (SELECT "Id" FROM "Countries" WHERE "IsoCode" = 'US' LIMIT 1),
    NOW(),
    FALSE
WHERE NOT EXISTS (
    SELECT 1 FROM "Cities" 
    WHERE "Name" = 'New York' 
    AND "PostalCode" = '10001'
    AND "CountryId" = (SELECT "Id" FROM "Countries" WHERE "IsoCode" = 'US' LIMIT 1)
);
*/

-- =====================================================
-- HELPER FUNCTION: Import City with Country Lookup
-- =====================================================
CREATE OR REPLACE FUNCTION import_city(
    p_city_name VARCHAR(255),
    p_postal_code VARCHAR(20),
    p_population_millions DECIMAL(10,2),
    p_country_iso_code VARCHAR(10),
    p_country_name VARCHAR(255) DEFAULT NULL,
    p_continent VARCHAR(100) DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
    v_country_id UUID;
    v_city_id UUID;
BEGIN
    -- Get or create country
    SELECT "Id" INTO v_country_id
    FROM "Countries"
    WHERE "IsoCode" = p_country_iso_code AND "IsDeleted" = FALSE
    LIMIT 1;

    -- If country doesn't exist, create it
    IF v_country_id IS NULL THEN
        INSERT INTO "Countries" ("Id", "Name", "IsoCode", "Continent", "CreatedAtUtc", "IsDeleted")
        VALUES (uuid_generate_v4(), COALESCE(p_country_name, p_country_iso_code), p_country_iso_code, p_continent, NOW(), FALSE)
        RETURNING "Id" INTO v_country_id;
    END IF;

    -- Check if city already exists
    SELECT "Id" INTO v_city_id
    FROM "Cities"
    WHERE "Name" = p_city_name 
    AND "PostalCode" = p_postal_code
    AND "CountryId" = v_country_id
    AND "IsDeleted" = FALSE
    LIMIT 1;

    -- If city doesn't exist, create it
    IF v_city_id IS NULL THEN
        INSERT INTO "Cities" ("Id", "Name", "PostalCode", "PopulationMillions", "CountryId", "CreatedAtUtc", "IsDeleted")
        VALUES (uuid_generate_v4(), p_city_name, p_postal_code, p_population_millions, v_country_id, NOW(), FALSE)
        RETURNING "Id" INTO v_city_id;
    END IF;

    RETURN v_city_id;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- USAGE EXAMPLE:
-- =====================================================
-- SELECT import_city('New York', '10001', 8.4, 'US', 'United States', 'North America');
-- SELECT import_city('Los Angeles', '90001', 3.9, 'US', 'United States', 'North America');
-- SELECT import_city('London', 'SW1A 1AA', 9.0, 'GB', 'United Kingdom', 'Europe');

-- =====================================================
-- BULK IMPORT FROM CSV (using the function)
-- =====================================================
-- You can create a temporary table, import CSV into it, then use the function:
/*
CREATE TEMP TABLE temp_cities (
    city_name VARCHAR(255),
    postal_code VARCHAR(20),
    population_millions DECIMAL(10,2),
    country_iso_code VARCHAR(10),
    country_name VARCHAR(255),
    continent VARCHAR(100)
);

-- Import CSV into temp table (adjust path)
COPY temp_cities FROM '/path/to/cities.csv' WITH (FORMAT csv, HEADER true);

-- Import using the function
SELECT import_city(city_name, postal_code, population_millions, country_iso_code, country_name, continent)
FROM temp_cities;

DROP TABLE temp_cities;
*/











