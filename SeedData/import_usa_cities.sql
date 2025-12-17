-- =====================================================
-- Import USA Metro Cities to Supabase
-- =====================================================
-- Run this script in Supabase SQL Editor
-- Make sure the import_city_from_csv function exists first
-- (Run SUPABASE_IMPORT_CITIES.sql first to create the function)
-- =====================================================

-- Import all USA metro cities from the CSV data
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
SELECT import_city_from_csv('Jacksonville', '32201', 0.95, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Fort Worth', '76101', 0.92, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Columbus', '43201', 0.91, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Charlotte', '28201', 0.88, 'US', 'United States', 'North America');
SELECT import_city_from_csv('San Francisco', '94101', 0.87, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Indianapolis', '46201', 0.87, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Seattle', '98101', 0.73, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Denver', '80201', 0.72, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Washington', '20001', 0.71, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Boston', '02101', 0.69, 'US', 'United States', 'North America');
SELECT import_city_from_csv('El Paso', '79901', 0.68, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Nashville', '37201', 0.68, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Detroit', '48201', 0.67, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Oklahoma City', '73101', 0.65, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Portland', '97201', 0.65, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Las Vegas', '89101', 0.64, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Memphis', '38101', 0.63, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Louisville', '40201', 0.63, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Baltimore', '21201', 0.58, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Milwaukee', '53201', 0.58, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Albuquerque', '87101', 0.56, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Tucson', '85701', 0.54, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Fresno', '93701', 0.54, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Sacramento', '95814', 0.52, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Kansas City', '64101', 0.51, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Mesa', '85201', 0.50, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Atlanta', '30301', 0.50, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Omaha', '68101', 0.49, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Colorado Springs', '80901', 0.48, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Raleigh', '27601', 0.47, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Virginia Beach', '23451', 0.45, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Miami', '33101', 0.44, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Oakland', '94601', 0.43, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Minneapolis', '55401', 0.43, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Tulsa', '74101', 0.41, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Cleveland', '44101', 0.38, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Wichita', '67201', 0.39, 'US', 'United States', 'North America');
SELECT import_city_from_csv('Arlington', '76001', 0.39, 'US', 'United States', 'North America');
SELECT import_city_from_csv('New Orleans', '70112', 0.38, 'US', 'United States', 'North America');

-- Verification
SELECT 
    COUNT(*) as total_cities_imported,
    'United States' as country
FROM "Cities" ci
JOIN "Countries" c ON ci."CountryId" = c."Id"
WHERE c."IsoCode" = 'US' 
AND ci."IsDeleted" = FALSE
AND c."IsDeleted" = FALSE;








