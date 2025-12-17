# PowerShell Script to Migrate Cities CSV to Supabase
# Usage: .\MigrateCities.ps1 -CsvPath "cities.csv" -ConnectionString "Host=..."

param(
    [Parameter(Mandatory=$true)]
    [string]$CsvPath,
    
    [Parameter(Mandatory=$true)]
    [string]$ConnectionString
)

# Install required module if not present
if (-not (Get-Module -ListAvailable -Name "Npgsql")) {
    Write-Host "Installing Npgsql module..." -ForegroundColor Yellow
    Install-Package -Name Npgsql -ProviderName NuGet -Scope CurrentUser -Force
}

# Add Npgsql assembly
Add-Type -Path "$env:USERPROFILE\.nuget\packages\npgsql\*\lib\netstandard2.0\Npgsql.dll"

# Read CSV file
Write-Host "Reading CSV file: $CsvPath" -ForegroundColor Cyan
$cities = Import-Csv -Path $CsvPath

Write-Host "Found $($cities.Count) cities in CSV" -ForegroundColor Green

# Connect to database
$conn = New-Object Npgsql.NpgsqlConnection($ConnectionString)
$conn.Open()

try {
    $countriesMap = @{}
    $successCount = 0
    $skipCount = 0

    foreach ($city in $cities) {
        $countryIsoCode = $city.CountryIsoCode.Trim()
        $cityName = $city.CityName.Trim()
        $postalCode = $city.PostalCode.Trim()
        $population = [decimal]$city.PopulationMillions
        $countryName = if ($city.CountryName) { $city.CountryName.Trim() } else { $countryIsoCode }
        $continent = if ($city.Continent) { $city.Continent.Trim() } else { $null }

        # Get or create country
        if (-not $countriesMap.ContainsKey($countryIsoCode)) {
            $countryCmd = $conn.CreateCommand()
            $countryCmd.CommandText = @"
                INSERT INTO "Countries" ("Id", "Name", "IsoCode", "Continent", "CreatedAtUtc", "IsDeleted")
                VALUES (gen_random_uuid(), @name, @isoCode, @continent, NOW(), FALSE)
                ON CONFLICT DO NOTHING
                RETURNING "Id";
"@
            $countryCmd.Parameters.AddWithValue("@name", $countryName) | Out-Null
            $countryCmd.Parameters.AddWithValue("@isoCode", $countryIsoCode) | Out-Null
            $countryCmd.Parameters.AddWithValue("@continent", [DBNull]::Value) | Out-Null
            if ($continent) {
                $countryCmd.Parameters["@continent"].Value = $continent
            }

            $countryId = $countryCmd.ExecuteScalar()
            
            if (-not $countryId) {
                # Country already exists, get its ID
                $getCountryCmd = $conn.CreateCommand()
                $getCountryCmd.CommandText = 'SELECT "Id" FROM "Countries" WHERE "IsoCode" = @isoCode AND "IsDeleted" = FALSE LIMIT 1'
                $getCountryCmd.Parameters.AddWithValue("@isoCode", $countryIsoCode) | Out-Null
                $countryId = $getCountryCmd.ExecuteScalar()
            }

            $countriesMap[$countryIsoCode] = $countryId
            Write-Host "Country: $countryName ($countryIsoCode)" -ForegroundColor Yellow
        }

        $countryId = $countriesMap[$countryIsoCode]

        # Check if city exists
        $checkCmd = $conn.CreateCommand()
        $checkCmd.CommandText = @"
            SELECT COUNT(*) FROM "Cities" 
            WHERE "Name" = @name 
            AND "PostalCode" = @postalCode 
            AND "CountryId" = @countryId 
            AND "IsDeleted" = FALSE
"@
        $checkCmd.Parameters.AddWithValue("@name", $cityName) | Out-Null
        $checkCmd.Parameters.AddWithValue("@postalCode", $postalCode) | Out-Null
        $checkCmd.Parameters.AddWithValue("@countryId", $countryId) | Out-Null

        $exists = [int]$checkCmd.ExecuteScalar()

        if ($exists -gt 0) {
            Write-Host "Skipping existing city: $cityName, $postalCode" -ForegroundColor Gray
            $skipCount++
            continue
        }

        # Insert city
        $insertCmd = $conn.CreateCommand()
        $insertCmd.CommandText = @"
            INSERT INTO "Cities" ("Id", "Name", "PostalCode", "PopulationMillions", "CountryId", "CreatedAtUtc", "IsDeleted")
            VALUES (gen_random_uuid(), @name, @postalCode, @population, @countryId, NOW(), FALSE)
"@
        $insertCmd.Parameters.AddWithValue("@name", $cityName) | Out-Null
        $insertCmd.Parameters.AddWithValue("@postalCode", $postalCode) | Out-Null
        $insertCmd.Parameters.AddWithValue("@population", $population) | Out-Null
        $insertCmd.Parameters.AddWithValue("@countryId", $countryId) | Out-Null

        $insertCmd.ExecuteNonQuery() | Out-Null
        $successCount++

        if ($successCount % 100 -eq 0) {
            Write-Host "Imported $successCount cities..." -ForegroundColor Green
        }
    }

    Write-Host "`nMigration Complete!" -ForegroundColor Green
    Write-Host "Successfully imported: $successCount cities" -ForegroundColor Green
    Write-Host "Skipped: $skipCount cities" -ForegroundColor Yellow
    Write-Host "Countries processed: $($countriesMap.Count)" -ForegroundColor Cyan

} finally {
    $conn.Close()
}








