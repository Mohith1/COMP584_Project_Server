-- =====================================================
-- COMPLETE SUPABASE SEED SCRIPT
-- =====================================================
-- Run this script in Supabase SQL Editor
-- This will:
-- 1. Add Auth0UserId column to Owners table
-- 2. Make CityId nullable in Owners table
-- 3. Seed Countries
-- 4. Seed Cities
-- 5. Seed Roles
-- =====================================================

-- Enable UUID extension (if not already enabled)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =====================================================
-- STEP 1: ALTER OWNERS TABLE FOR NEW COLUMNS
-- =====================================================

-- Add Auth0UserId column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Owners' AND column_name = 'Auth0UserId'
    ) THEN
        ALTER TABLE "Owners" ADD COLUMN "Auth0UserId" VARCHAR(255) NULL;
        CREATE INDEX IF NOT EXISTS "IX_Owners_Auth0UserId" ON "Owners" ("Auth0UserId");
        RAISE NOTICE 'Added Auth0UserId column to Owners table';
    ELSE
        RAISE NOTICE 'Auth0UserId column already exists';
    END IF;
END $$;

-- Make CityId nullable if it's not already
DO $$ 
BEGIN
    -- Drop foreign key constraint first
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_Owners_Cities_CityId' AND table_name = 'Owners'
    ) THEN
        ALTER TABLE "Owners" DROP CONSTRAINT "FK_Owners_Cities_CityId";
    END IF;
    
    -- Make column nullable
    ALTER TABLE "Owners" ALTER COLUMN "CityId" DROP NOT NULL;
    
    -- Re-add foreign key constraint with ON DELETE SET NULL
    ALTER TABLE "Owners" ADD CONSTRAINT "FK_Owners_Cities_CityId" 
        FOREIGN KEY ("CityId") REFERENCES "Cities" ("Id") ON DELETE SET NULL;
    
    RAISE NOTICE 'Made CityId nullable in Owners table';
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'CityId modification: %', SQLERRM;
END $$;

-- =====================================================
-- STEP 2: CREATE HELPER FUNCTION FOR CITY IMPORT
-- =====================================================
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
    END IF;

    RETURN v_city_id;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- STEP 3: SEED COUNTRIES
-- =====================================================
INSERT INTO "Countries" ("Id", "Name", "IsoCode", "Continent", "CreatedAtUtc", "IsDeleted")
SELECT gen_random_uuid(), name, iso_code, continent, NOW(), FALSE
FROM (VALUES
    ('United States', 'US', 'North America'),
    ('Canada', 'CA', 'North America'),
    ('Mexico', 'MX', 'North America'),
    ('United Kingdom', 'GB', 'Europe'),
    ('Germany', 'DE', 'Europe'),
    ('France', 'FR', 'Europe'),
    ('Spain', 'ES', 'Europe'),
    ('Italy', 'IT', 'Europe'),
    ('Australia', 'AU', 'Oceania'),
    ('Japan', 'JP', 'Asia'),
    ('China', 'CN', 'Asia'),
    ('India', 'IN', 'Asia'),
    ('Brazil', 'BR', 'South America'),
    ('Argentina', 'AR', 'South America')
) AS v(name, iso_code, continent)
WHERE NOT EXISTS (
    SELECT 1 FROM "Countries" WHERE "IsoCode" = v.iso_code AND "IsDeleted" = FALSE
);

-- =====================================================
-- STEP 4: SEED CITIES (Major US Cities)
-- =====================================================
SELECT import_city_from_csv('New York', '10001', 8.4, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Los Angeles', '90001', 3.9, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Chicago', '60601', 2.7, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Houston', '77001', 2.3, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Phoenix', '85001', 1.6, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Philadelphia', '19101', 1.6, 'US', 'United States', 'North America');
SELECT import_city_from_csv('San Antonio', '78201', 1.5, 'US', 'United States', 'North America');
SELECT import_city_from_csv('San Diego', '92101', 1.4, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Dallas', '75201', 1.3, 'US', 'United States', 'North America');
SELECT import_city_from_csv('San Jose', '95101', 1.0, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Austin', '78701', 0.96, 'US', 'United States', 'North America');
SELECT import_city_from_csv('San Francisco', '94101', 0.87, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Seattle', '98101', 0.73, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Denver', '80201', 0.72, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Washington', '20001', 0.71, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Boston', '02101', 0.69, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Nashville', '37201', 0.68, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Detroit', '48201', 0.67, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Portland', '97201', 0.65, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Las Vegas', '89101', 0.64, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Atlanta', '30301', 0.50, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Miami', '33101', 0.44, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Minneapolis', '55401', 0.43, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Cleveland', '44101', 0.38, 'US', 'United States', 'North America');
SELECT import_city_from_csv('New Orleans', '70112', 0.38, 'US', 'United States', 'North America');

-- Canadian Cities
SELECT import_city_from_csv('Toronto', 'M5H 2N2', 2.9, 'CA', 'Canada', 'North America');
SELECT import_city_from_csv('Vancouver', 'V6B 1A1', 2.5, 'CA', 'Canada', 'North America');
SELECT import_city_from_csv('Montreal', 'H2Y 1A6', 1.8, 'CA', 'Canada', 'North America');
SELECT import_city_from_csv('Calgary', 'T2P 1J9', 1.3, 'CA', 'Canada', 'North America');
SELECT import_city_from_csv('Ottawa', 'K1P 1J1', 1.0, 'CA', 'Canada', 'North America');

-- UK Cities
SELECT import_city_from_csv('London', 'SW1A 1AA', 9.0, 'GB', 'United Kingdom', 'Europe');
SELECT import_city_from_csv('Manchester', 'M1 1AD', 2.8, 'GB', 'United Kingdom', 'Europe');
SELECT import_city_from_csv('Birmingham', 'B1 1AA', 2.6, 'GB', 'United Kingdom', 'Europe');
SELECT import_city_from_csv('Glasgow', 'G1 1AA', 1.7, 'GB', 'United Kingdom', 'Europe');
SELECT import_city_from_csv('Liverpool', 'L1 1AA', 0.9, 'GB', 'United Kingdom', 'Europe');

-- German Cities
SELECT import_city_from_csv('Berlin', '10115', 3.6, 'DE', 'Germany', 'Europe');
SELECT import_city_from_csv('Munich', '80331', 1.5, 'DE', 'Germany', 'Europe');
SELECT import_city_from_csv('Frankfurt', '60311', 0.75, 'DE', 'Germany', 'Europe');
SELECT import_city_from_csv('Hamburg', '20095', 1.9, 'DE', 'Germany', 'Europe');

-- Australian Cities
SELECT import_city_from_csv('Sydney', '2000', 5.3, 'AU', 'Australia', 'Oceania');
SELECT import_city_from_csv('Melbourne', '3000', 5.0, 'AU', 'Australia', 'Oceania');
SELECT import_city_from_csv('Brisbane', '4000', 2.5, 'AU', 'Australia', 'Oceania');
SELECT import_city_from_csv('Perth', '6000', 2.1, 'AU', 'Australia', 'Oceania');

-- =====================================================
-- STEP 5: SEED ROLES
-- =====================================================
INSERT INTO "AppRoles" ("Id", "Name", "NormalizedName", "Description", "ConcurrencyStamp")
SELECT gen_random_uuid(), name, UPPER(name), description, gen_random_uuid()::text
FROM (VALUES
    ('Admin', 'System administrator with full access'),
    ('Owner', 'Fleet owner with management access'),
    ('Manager', 'Fleet manager with operational access'),
    ('Driver', 'Vehicle driver with limited access'),
    ('Viewer', 'Read-only access user')
) AS v(name, description)
WHERE NOT EXISTS (
    SELECT 1 FROM "AppRoles" WHERE "NormalizedName" = UPPER(v.name)
);

-- =====================================================
-- STEP 6: ENABLE REALTIME (Optional but recommended)
-- =====================================================
-- Enable realtime for key tables
DO $$
BEGIN
    -- These may fail if realtime is not enabled on your Supabase plan
    -- That's okay - they're optional
    ALTER PUBLICATION supabase_realtime ADD TABLE "Fleets";
    ALTER PUBLICATION supabase_realtime ADD TABLE "Vehicles";
    ALTER PUBLICATION supabase_realtime ADD TABLE "VehicleTelemetrySnapshots";
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'Realtime publication skipped: %', SQLERRM;
END $$;

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================

-- Check Countries
SELECT 'Countries' as table_name, COUNT(*) as row_count FROM "Countries" WHERE "IsDeleted" = FALSE
UNION ALL
SELECT 'Cities', COUNT(*) FROM "Cities" WHERE "IsDeleted" = FALSE
UNION ALL
SELECT 'AppRoles', COUNT(*) FROM "AppRoles"
UNION ALL
SELECT 'Owners', COUNT(*) FROM "Owners" WHERE "IsDeleted" = FALSE
UNION ALL
SELECT 'Fleets', COUNT(*) FROM "Fleets" WHERE "IsDeleted" = FALSE
UNION ALL
SELECT 'Vehicles', COUNT(*) FROM "Vehicles" WHERE "IsDeleted" = FALSE;

-- Show sample cities
SELECT 
    ci."Name" as city,
    ci."PostalCode",
    c."Name" as country,
    c."IsoCode"
FROM "Cities" ci
JOIN "Countries" c ON ci."CountryId" = c."Id"
WHERE ci."IsDeleted" = FALSE
ORDER BY c."Name", ci."Name"
LIMIT 20;

-- Show roles
SELECT "Name", "Description" FROM "AppRoles" ORDER BY "Name";

-- Check Owners table structure
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'Owners'
ORDER BY ordinal_position;

-- =====================================================
-- SCRIPT COMPLETE!
-- =====================================================
-- Your Supabase database is now seeded with:
-- - 14 Countries
-- - 44+ Cities
-- - 5 Roles (Admin, Owner, Manager, Driver, Viewer)
-- - Auth0UserId column added to Owners
-- - CityId made nullable in Owners
-- =====================================================

