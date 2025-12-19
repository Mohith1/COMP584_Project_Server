-- =====================================================
-- Supabase Cities Import Script
-- =====================================================
-- This script helps import cities from CSV
-- Run this in Supabase SQL Editor
-- =====================================================

-- Step 1: Create helper function (run this first)
CREATE OR REPLACE FUNCTION import_city_from_csv(
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
        VALUES (
            gen_random_uuid(), 
            COALESCE(p_country_name, p_country_iso_code), 
            p_country_iso_code, 
            p_continent, 
            NOW(), 
            FALSE
        )
        RETURNING "Id" INTO v_country_id;
        
        RAISE NOTICE 'Created country: % (%)', COALESCE(p_country_name, p_country_iso_code), p_country_iso_code;
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
        VALUES (gen_random_uuid(), p_city_name, p_postal_code, p_population_millions, v_country_id, NOW(), FALSE)
        RETURNING "Id" INTO v_city_id;
        
        RAISE NOTICE 'Created city: %, %', p_city_name, p_postal_code;
    ELSE
        RAISE NOTICE 'City already exists: %, %', p_city_name, p_postal_code;
    END IF;

    RETURN v_city_id;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- Step 2: Import Sample Cities (Example)
-- =====================================================
-- Replace these with your actual CSV data
-- You can copy-paste rows from your CSV here

-- United States Cities
SELECT import_city_from_csv('New York', '10001', 8.4, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Los Angeles', '90001', 3.9, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Chicago', '60601', 2.7, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Houston', '77001', 2.3, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Phoenix', '85001', 1.6, 'US', 'United States', 'North America');

-- United Kingdom Cities
SELECT import_city_from_csv('London', 'SW1A 1AA', 9.0, 'GB', 'United Kingdom', 'Europe');
SELECT import_city_from_csv('Manchester', 'M1 1AD', 2.8, 'GB', 'United Kingdom', 'Europe');
SELECT import_city_from_csv('Birmingham', 'B1 1AA', 2.6, 'GB', 'United Kingdom', 'Europe');

-- Canada Cities
SELECT import_city_from_csv('Toronto', 'M5H 2N2', 2.9, 'CA', 'Canada', 'North America');
SELECT import_city_from_csv('Vancouver', 'V6B 1A1', 2.5, 'CA', 'Canada', 'North America');
SELECT import_city_from_csv('Montreal', 'H2Y 1A6', 1.8, 'CA', 'Canada', 'North America');

-- =====================================================
-- Step 3: Bulk Import from CSV (Alternative Method)
-- =====================================================
-- If you have many cities, use this method:

-- 1. First, create a temporary table
CREATE TEMP TABLE IF NOT EXISTS temp_cities_import (
    city_name VARCHAR(255),
    postal_code VARCHAR(20),
    population_millions DECIMAL(10,2),
    country_iso_code VARCHAR(10),
    country_name VARCHAR(255),
    continent VARCHAR(100)
);

-- 2. Import your CSV into the temp table using Supabase's CSV import feature:
--    - Go to Table Editor
--    - Create a new table (or use temp table)
--    - Import CSV
--    - OR manually insert:
/*
INSERT INTO temp_cities_import VALUES
    ('New York', '10001', 8.4, 'US', 'United States', 'North America'),
    ('Los Angeles', '90001', 3.9, 'US', 'United States', 'North America');
-- Add all your cities here
*/

-- 3. Run the import function for all rows
/*
SELECT import_city_from_csv(
    city_name, 
    postal_code, 
    population_millions, 
    country_iso_code, 
    country_name, 
    continent
)
FROM temp_cities_import;
*/

-- 4. Clean up
-- DROP TABLE temp_cities_import;

-- =====================================================
-- Verification Queries
-- =====================================================

-- Check total cities imported
SELECT COUNT(*) as total_cities FROM "Cities" WHERE "IsDeleted" = FALSE;

-- Check cities by country
SELECT 
    c."Name" as country,
    c."IsoCode" as iso_code,
    COUNT(ci."Id") as city_count
FROM "Countries" c
LEFT JOIN "Cities" ci ON ci."CountryId" = c."Id" AND ci."IsDeleted" = FALSE
WHERE c."IsDeleted" = FALSE
GROUP BY c."Name", c."IsoCode"
ORDER BY city_count DESC;

-- Sample cities
SELECT 
    ci."Name" as city,
    ci."PostalCode" as postal_code,
    ci."PopulationMillions" as population_millions,
    c."Name" as country
FROM "Cities" ci
JOIN "Countries" c ON ci."CountryId" = c."Id"
WHERE ci."IsDeleted" = FALSE
ORDER BY ci."Name"
LIMIT 20;











