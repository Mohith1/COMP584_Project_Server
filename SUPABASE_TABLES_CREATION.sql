-- =====================================================
-- Fleet Management Database Schema for Supabase
-- PostgreSQL Script
-- =====================================================
-- Run this script in Supabase SQL Editor
-- =====================================================

-- Enable UUID extension (if not already enabled)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =====================================================
-- 1. COUNTRIES TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS "Countries" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Name" VARCHAR(255) NOT NULL,
    "IsoCode" VARCHAR(10) NOT NULL,
    "Continent" VARCHAR(100),
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ,
    "DeletedAtUtc" TIMESTAMPTZ,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS "IX_Countries_IsDeleted" ON "Countries" ("IsDeleted");
CREATE INDEX IF NOT EXISTS "IX_Countries_IsoCode" ON "Countries" ("IsoCode");

-- =====================================================
-- 2. CITIES TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS "Cities" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Name" VARCHAR(255) NOT NULL,
    "PostalCode" VARCHAR(20) NOT NULL,
    "PopulationMillions" DECIMAL(10, 2) NOT NULL,
    "CountryId" UUID NOT NULL,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ,
    "DeletedAtUtc" TIMESTAMPTZ,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_Cities_Countries_CountryId" FOREIGN KEY ("CountryId") 
        REFERENCES "Countries" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Cities_CountryId" ON "Cities" ("CountryId");
CREATE INDEX IF NOT EXISTS "IX_Cities_IsDeleted" ON "Cities" ("IsDeleted");

-- =====================================================
-- 3. ASP.NET IDENTITY TABLES
-- =====================================================

-- AppRoles Table
CREATE TABLE IF NOT EXISTS "AppRoles" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Name" VARCHAR(256),
    "NormalizedName" VARCHAR(256),
    "ConcurrencyStamp" TEXT,
    "Description" TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS "RoleNameIndex" ON "AppRoles" ("NormalizedName");

-- AppUsers Table
CREATE TABLE IF NOT EXISTS "AppUsers" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "UserName" VARCHAR(256),
    "NormalizedUserName" VARCHAR(256),
    "Email" VARCHAR(256),
    "NormalizedEmail" VARCHAR(256),
    "EmailConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "PasswordHash" TEXT,
    "SecurityStamp" TEXT,
    "ConcurrencyStamp" TEXT,
    "PhoneNumber" TEXT,
    "PhoneNumberConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "TwoFactorEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEnd" TIMESTAMPTZ,
    "LockoutEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "AccessFailedCount" INTEGER NOT NULL DEFAULT 0,
    "OktaUserId" VARCHAR(255),
    "LastLoginUtc" TIMESTAMPTZ
);

CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndex" ON "AppUsers" ("NormalizedUserName");
CREATE INDEX IF NOT EXISTS "EmailIndex" ON "AppUsers" ("NormalizedEmail");

-- Identity Junction Tables
CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    CONSTRAINT "FK_AspNetUserClaims_AppUsers_UserId" FOREIGN KEY ("UserId") 
        REFERENCES "AppUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
    "LoginProvider" VARCHAR(128) NOT NULL,
    "ProviderKey" VARCHAR(128) NOT NULL,
    "ProviderDisplayName" TEXT,
    "UserId" UUID NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AppUsers_UserId" FOREIGN KEY ("UserId") 
        REFERENCES "AppUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
    "UserId" UUID NOT NULL,
    "RoleId" UUID NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AppUsers_UserId" FOREIGN KEY ("UserId") 
        REFERENCES "AppUsers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AppRoles_RoleId" FOREIGN KEY ("RoleId") 
        REFERENCES "AppRoles" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
    "UserId" UUID NOT NULL,
    "LoginProvider" VARCHAR(128) NOT NULL,
    "Name" VARCHAR(128) NOT NULL,
    "Value" TEXT,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AppUsers_UserId" FOREIGN KEY ("UserId") 
        REFERENCES "AppUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
    "Id" SERIAL PRIMARY KEY,
    "RoleId" UUID NOT NULL,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    CONSTRAINT "FK_AspNetRoleClaims_AppRoles_RoleId" FOREIGN KEY ("RoleId") 
        REFERENCES "AppRoles" ("Id") ON DELETE CASCADE
);

-- =====================================================
-- 4. OWNERS TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS "Owners" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "CompanyName" VARCHAR(255) NOT NULL,
    "ContactEmail" VARCHAR(255) NOT NULL,
    "ContactPhone" VARCHAR(50),
    "PrimaryContactName" VARCHAR(255),
    "CityId" UUID NOT NULL,
    "TimeZone" VARCHAR(100),
    "FleetCount" INTEGER NOT NULL DEFAULT 0,
    "OktaGroupId" VARCHAR(255),
    "IdentityUserId" UUID,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ,
    "DeletedAtUtc" TIMESTAMPTZ,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_Owners_Cities_CityId" FOREIGN KEY ("CityId") 
        REFERENCES "Cities" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Owners_AppUsers_IdentityUserId" FOREIGN KEY ("IdentityUserId") 
        REFERENCES "AppUsers" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_Owners_CityId" ON "Owners" ("CityId");
CREATE INDEX IF NOT EXISTS "IX_Owners_IdentityUserId" ON "Owners" ("IdentityUserId");
CREATE INDEX IF NOT EXISTS "IX_Owners_IsDeleted" ON "Owners" ("IsDeleted");

-- =====================================================
-- 5. FLEETS TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS "Fleets" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Name" VARCHAR(255) NOT NULL,
    "Description" TEXT,
    "OwnerId" UUID NOT NULL,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ,
    "DeletedAtUtc" TIMESTAMPTZ,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_Fleets_Owners_OwnerId" FOREIGN KEY ("OwnerId") 
        REFERENCES "Owners" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Fleets_OwnerId" ON "Fleets" ("OwnerId");
CREATE INDEX IF NOT EXISTS "IX_Fleets_IsDeleted" ON "Fleets" ("IsDeleted");

-- =====================================================
-- 6. VEHICLES TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS "Vehicles" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Vin" VARCHAR(50) NOT NULL,
    "PlateNumber" VARCHAR(50) NOT NULL,
    "Make" VARCHAR(100),
    "Model" VARCHAR(100),
    "ModelYear" INTEGER NOT NULL,
    "Status" INTEGER NOT NULL,
    "FleetId" UUID NOT NULL,
    "OwnerId" UUID,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ,
    "DeletedAtUtc" TIMESTAMPTZ,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_Vehicles_Fleets_FleetId" FOREIGN KEY ("FleetId") 
        REFERENCES "Fleets" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Vehicles_Owners_OwnerId" FOREIGN KEY ("OwnerId") 
        REFERENCES "Owners" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_Vehicles_FleetId" ON "Vehicles" ("FleetId");
CREATE INDEX IF NOT EXISTS "IX_Vehicles_OwnerId" ON "Vehicles" ("OwnerId");
CREATE INDEX IF NOT EXISTS "IX_Vehicles_IsDeleted" ON "Vehicles" ("IsDeleted");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Vehicles_Vin" ON "Vehicles" ("Vin") WHERE "IsDeleted" = FALSE;

-- =====================================================
-- 7. FLEET USERS TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS "FleetUsers" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "FirstName" VARCHAR(100) NOT NULL,
    "LastName" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(255) NOT NULL,
    "PhoneNumber" VARCHAR(50),
    "OktaUserId" VARCHAR(255),
    "Role" INTEGER NOT NULL,
    "OwnerId" UUID NOT NULL,
    "AssignedVehicleId" UUID,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ,
    "DeletedAtUtc" TIMESTAMPTZ,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_FleetUsers_Owners_OwnerId" FOREIGN KEY ("OwnerId") 
        REFERENCES "Owners" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_FleetUsers_Vehicles_AssignedVehicleId" FOREIGN KEY ("AssignedVehicleId") 
        REFERENCES "Vehicles" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_FleetUsers_OwnerId" ON "FleetUsers" ("OwnerId");
CREATE INDEX IF NOT EXISTS "IX_FleetUsers_AssignedVehicleId" ON "FleetUsers" ("AssignedVehicleId");
CREATE INDEX IF NOT EXISTS "IX_FleetUsers_IsDeleted" ON "FleetUsers" ("IsDeleted");

-- =====================================================
-- 8. TELEMATICS DEVICES TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS "TelematicsDevices" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "SerialNumber" VARCHAR(100) NOT NULL,
    "Iccid" VARCHAR(50),
    "Imei" VARCHAR(50),
    "FirmwareVersion" VARCHAR(50),
    "VehicleId" UUID NOT NULL,
    "LastSyncUtc" TIMESTAMPTZ,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ,
    "DeletedAtUtc" TIMESTAMPTZ,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_TelematicsDevices_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") 
        REFERENCES "Vehicles" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_TelematicsDevices_VehicleId" ON "TelematicsDevices" ("VehicleId");
CREATE INDEX IF NOT EXISTS "IX_TelematicsDevices_IsDeleted" ON "TelematicsDevices" ("IsDeleted");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_TelematicsDevices_SerialNumber" ON "TelematicsDevices" ("SerialNumber") WHERE "IsDeleted" = FALSE;

-- =====================================================
-- 9. MAINTENANCE TICKETS TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS "MaintenanceTickets" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Title" VARCHAR(255) NOT NULL,
    "Description" TEXT,
    "Status" INTEGER NOT NULL,
    "VehicleId" UUID NOT NULL,
    "DueAtUtc" TIMESTAMPTZ,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ,
    "DeletedAtUtc" TIMESTAMPTZ,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_MaintenanceTickets_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") 
        REFERENCES "Vehicles" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_MaintenanceTickets_VehicleId" ON "MaintenanceTickets" ("VehicleId");
CREATE INDEX IF NOT EXISTS "IX_MaintenanceTickets_IsDeleted" ON "MaintenanceTickets" ("IsDeleted");
CREATE INDEX IF NOT EXISTS "IX_MaintenanceTickets_Status" ON "MaintenanceTickets" ("Status");

-- =====================================================
-- 10. VEHICLE TELEMETRY SNAPSHOTS TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS "VehicleTelemetrySnapshots" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "VehicleId" UUID NOT NULL,
    "Latitude" DECIMAL(10, 8) NOT NULL,
    "Longitude" DECIMAL(11, 8) NOT NULL,
    "SpeedKph" DECIMAL(10, 2) NOT NULL,
    "FuelLevelPercentage" DECIMAL(5, 2) NOT NULL,
    "CapturedAtUtc" TIMESTAMPTZ NOT NULL,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ,
    "DeletedAtUtc" TIMESTAMPTZ,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_VehicleTelemetrySnapshots_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") 
        REFERENCES "Vehicles" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_VehicleTelemetrySnapshots_VehicleId" ON "VehicleTelemetrySnapshots" ("VehicleId");
CREATE INDEX IF NOT EXISTS "IX_VehicleTelemetrySnapshots_CapturedAtUtc" ON "VehicleTelemetrySnapshots" ("CapturedAtUtc" DESC);
CREATE INDEX IF NOT EXISTS "IX_VehicleTelemetrySnapshots_IsDeleted" ON "VehicleTelemetrySnapshots" ("IsDeleted");

-- =====================================================
-- 11. REFRESH TOKENS TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS "RefreshTokens" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "UserId" UUID NOT NULL,
    "Token" TEXT NOT NULL,
    "ExpiresAtUtc" TIMESTAMPTZ NOT NULL,
    "RevokedAtUtc" TIMESTAMPTZ,
    "ReplacedByToken" TEXT,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ,
    "DeletedAtUtc" TIMESTAMPTZ,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_RefreshTokens_AppUsers_UserId" FOREIGN KEY ("UserId") 
        REFERENCES "AppUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_Token" ON "RefreshTokens" ("Token");
CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_IsDeleted" ON "RefreshTokens" ("IsDeleted");

-- =====================================================
-- COMMENTS FOR DOCUMENTATION
-- =====================================================
COMMENT ON TABLE "Countries" IS 'Geographic countries reference data';
COMMENT ON TABLE "Cities" IS 'Cities within countries';
COMMENT ON TABLE "AppUsers" IS 'ASP.NET Identity users (Owners and Fleet Users)';
COMMENT ON TABLE "AppRoles" IS 'ASP.NET Identity roles';
COMMENT ON TABLE "Owners" IS 'Fleet management company owners';
COMMENT ON TABLE "Fleets" IS 'Vehicle fleets owned by owners';
COMMENT ON TABLE "Vehicles" IS 'Individual vehicles in fleets';
COMMENT ON TABLE "FleetUsers" IS 'Users assigned to fleets (Okta integration)';
COMMENT ON TABLE "TelematicsDevices" IS 'GPS/telematics devices installed in vehicles';
COMMENT ON TABLE "MaintenanceTickets" IS 'Vehicle maintenance requests and tickets';
COMMENT ON TABLE "VehicleTelemetrySnapshots" IS 'Real-time vehicle location and telemetry data';
COMMENT ON TABLE "RefreshTokens" IS 'JWT refresh tokens for authentication';

-- =====================================================
-- SCRIPT COMPLETE
-- =====================================================
-- All tables created successfully!
-- Next steps:
-- 1. Run this script in Supabase SQL Editor
-- 2. Update your connection string in Railway environment variables
-- 3. Test the API endpoints
-- =====================================================









