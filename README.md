# Fleet Management Server

Modern fleet-operations backend designed for COMP584. The server follows a strict three-tier architecture (API ➜ Services ➜ Data) and targets .NET 8 with SQL Server, Okta-powered authentication, and Entity Framework Core code-first migrations.

---

## 1. Project Foundation & Structure

```
COMP584_Project_Server/
├─ FleetManagement.Api/          # Presentation layer (controllers, middleware, Program.cs)
├─ FleetManagement.Services/     # Application layer (DTOs, business services, auth, Okta)
├─ FleetManagement.Data/         # Data layer (DbContext, entities, repositories, migrations)
├─ SeedData/                     # CSV seed files (e.g., cities.sample.csv)
├─ FleetManagement.sln
└─ README.md
```

- **Target Frameworks:** All projects run on `net8.0`.
- **Dependencies:** EF Core 8 (SQL Server provider), ASP.NET Core Identity, Serilog, CsvHelper, System.IdentityModel.Tokens.Jwt, HttpClient factory.
- **3-tier enforcement:** API only depends on Services; Services depends on Data; Data is persistence-only.

---

## 2. Entity Framework Core & Data Models

- **DbContext:** `FleetDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`
- **Base entity pattern:** `BaseEntity` enforces `Id`, `CreatedAtUtc`, `UpdatedAtUtc`, `IsDeleted`, `DeletedAtUtc`.
- **Soft delete:** Implemented in `FleetDbContext` via global query filters and metadata stamping.
- **Relationships:**
  - `Country 1:N City`
  - `Owner 1:N Fleet / FleetUser / Vehicle`
  - `Fleet 1:N Vehicle`
  - `Vehicle 1:N VehicleTelemetrySnapshot / MaintenanceTicket`
  - `Vehicle 1:1 TelematicsDevice`
  - `ApplicationUser 1:1 Owner`
  - `Owner 1:N FleetUsers` with `OwnerId` FK.
- **Identity extensions:** `ApplicationUser` stores `OktaUserId`, `LastLoginUtc`, `OwnerProfile`.
- **Data annotations & SQL types:** Configurations enforce `varchar`, `char`, decimal precision, unique indexes, and required constraints.
- **Repositories:** Generic repository + unit of work pattern in `FleetManagement.Data.Repositories`.

---

## 3. Migrations Strategy (Code-First)

| Order | Migration Name            | Purpose                                                      |
|-------|---------------------------|--------------------------------------------------------------|
| 01    | `InitialCountriesCities`  | Country/City tables with FK and unique ISO constraints       |
| 02    | `GoldenFleetTables`       | Owners, Fleets, Vehicles, Telemetry, Maintenance, Soft delete|
| 03    | `IdentitySetup`           | ASP.NET Identity tables, refresh tokens, owner-user linkage  |

Commands:

```bash
# Add a migration
dotnet ef migrations add <Name> \
  --project FleetManagement.Data/FleetManagement.Data.csproj \
  --startup-project FleetManagement.Api/FleetManagement.Api.csproj

# Apply to SQL Server
dotnet ef database update \
  --project FleetManagement.Data/FleetManagement.Data.csproj \
  --startup-project FleetManagement.Api/FleetManagement.Api.csproj

# Remove last migration (if not applied)
dotnet ef migrations remove \
  --project FleetManagement.Data/FleetManagement.Data.csproj
```

---

## 4. Authentication & JWT Implementation

- **Identity:** ASP.NET Core Identity with password + lockout policies (12+ chars, symbol & uppercase, lockout after 5 attempts).
- **JWT:** `JwtTokenService` issues access+refresh tokens with owner and Okta claims.
- **Refresh tokens:** Stored hashed in `RefreshTokens` table and rotated.
- **AuthService:** Handles register/login/refresh/revoke with password hygiene checks and owner creation.
- **Okta integration:** `OktaIntegrationService` provisions users & groups through Okta REST API (uses SSWS token).
- **JWT Settings (appsettings):**

```json
"Jwt": {
  "Issuer": "FleetManagement.Api",
  "Audience": "FleetManagement.Client",
  "SigningKey": "<64+ char secret>",
  "AccessTokenMinutes": 30,
  "RefreshTokenDays": 14
}
```

Configure Okta via `Okta:*` settings (Domain, AuthorizationServerId, Audience, ApiToken).

---

## 5. Program.cs Highlights

- Serilog logging with console sink.
- DbContext registration with SQL Server retry + migrations assembly.
- Identity configuration & policy wiring in `AddFleetManagementCore`.
- Authentication:
  - JWT bearer (internal tokens)
  - Okta Web API scheme (external tokens)
- Authorization policies:
  - `OwnerOrAdmin`: either Owner or Administrator role.
  - `AdminOnly`: Administrator role (used for seeding endpoints).
- Middleware pipeline order: HTTPS ➜ CORS ➜ Exception middleware ➜ Authentication ➜ Authorization ➜ Controllers.
- Swagger includes JWT security definition with lock icon & Authorize button.

---

## 6. API Controllers & Endpoints

| Controller | Route                             | Methods (HTTP)                        | Notes                                  |
|-----------|-----------------------------------|---------------------------------------|----------------------------------------|
| `AuthController`   | `/api/auth/*`                | POST register/login/refresh/revoke    | Anonymous + Okta integration           |
| `OwnersController` | `/api/owners`               | GET/PUT `me`, GET `{id}`              | Requires Owner/Admin                   |
| `FleetsController` | `/api/owners/{id}/fleets`   | GET/POST fleets, PUT/DELETE fleet, vehicle CRUD, telemetry | Owner/Admin + full CRUD |
| `SeedController`   | `/api/seed/cities`          | POST CSV                               | Admin only (uploads CSV via form-data) |

- All endpoints return proper status codes, log context, and rely on DTOs.

---

## 7. DTO Guidance

- **Naming:** `<Entity><Action>Request/Response` (e.g., `CreateFleetRequest`, `OwnerDetailResponse`).
- **Validation:** `[Required]`, `[EmailAddress]`, `[MaxLength]`, `[Compare]`, etc. Model state enforced automatically.
- **Mapping:** Services map entities to DTOs (no direct entity exposure).
- **Auth DTOs:** `OwnerRegisterRequest`, `LoginRequest`, `AuthResponse`, `RefreshTokenRequest`, `RevokeTokenRequest`.

---

## 8. Error Handling & Validation

- `GlobalExceptionMiddleware` catches validation, forbidden, not found, unauthorized, and unknown errors.
- Returns JSON payloads for validation errors (`{ message, errors }`).
- ModelState errors automatically produce `400`.
- Domain-level validation lives in services (`AuthService`, `FleetService`, etc.).

---

## 9. Best Practices Checklist

- [x] Three-tier architecture with DI enforcement
- [x] Soft delete & base auditing (UTC)
- [x] Repository + Unit-of-Work pattern
- [x] JWT + Okta dual authentication
- [x] Serilog structured logging
- [x] Swagger with JWT security
- [x] CORS & HTTPS enforced
- [x] Migrations versioned & documented
- [x] CSV seeding with duplicate prevention
- [x] Async/await everywhere (no sync I/O)

---

## 10. C# Modern Features

- `required` members in DTOs and entities.
- Nullable reference types enabled solution-wide.
- `record` types for DTOs (`OwnerSummaryResponse`, `TokenPair`, etc.).
- Collection expressions (`[]`) in config sections.
- Async LINQ queries via EF Core.

---

## Data Seeding

- Upload CSV via `/api/seed/cities` (admin only). Example file: `SeedData/cities.sample.csv`.
- Uses CsvHelper + dictionaries to avoid duplicates (country ISO + city name).
- Logs inserted count and skipped duplicates.

---

## Performance & Optimization

- EF Core tracking disabled for read-heavy queries (`AsNoTracking` where applicable).
- SQL indexes on VIN, ISO codes, emails, composite owner/fleet names.
- Query-level pagination for fleets to avoid large payloads.
- HttpClient factory for Okta integration (connection pooling).

---

## Deployment Requirements

1. SQL Server (Microsoft SQL) accessible from API.
2. Okta tenant with API token and custom authorization server (configure in `appsettings`).
3. Environment variables / Key Vault recommended for secrets (`Jwt:SigningKey`, `Okta:ApiToken`, `ConnectionStrings`).
4. Run `dotnet ef database update ...` before first launch.
5. Optional: configure Serilog sinks (Seq, Application Insights) for production.

---

## Common Patterns

- **Async/await** for all I/O.
- **Dependency Injection** for every service/repository.
- **Repository pattern** via `IGenericRepository<T>` & `IUnitOfWork`.
- **DTO mapping** centralized in services (no entity leakage).
- **Policy-based authorization** for role separation (Owners vs Users vs Admins).

---

## Testing Considerations

- Suggested unit tests:
  - `JwtTokenServiceTests` verifying claims + expiration
  - `AuthServiceTests` (register/login/refresh flows using InMemoryDb or mocked UserManager)
  - `FleetServiceTests` for CRUD and telemetry retrieval
  - `SeedServiceTests` using in-memory streams (CsvHelper)
- Integration tests can boot `FleetManagement.Api` via `WebApplicationFactory` with test DB (SQLite in-memory).

---

## Documentation Standards

- Controllers & services include XML comments for Swagger (enable `<GenerateDocumentationFile>` if desired).
- README captures architecture, workflow, auth, migrations, deployment.
- Swagger UI documents endpoints + requires JWT via Authorize button.

---

## Quick Start Commands

```bash
# Restore tools & build
dotnet build

# Apply migrations to local dev DB
dotnet ef database update \
  --project FleetManagement.Data/FleetManagement.Data.csproj \
  --startup-project FleetManagement.Api/FleetManagement.Api.csproj

# Run the API (https://localhost:5001 by default)
dotnet run --project FleetManagement.Api/FleetManagement.Api.csproj
```

---

## Key Architectural Principles

1. Separation of concerns (API vs Services vs Data)
2. Dependency Inversion (layers reference abstractions)
3. Security by default (JWT + Okta, HTTPS, lockouts)
4. Configuration-driven (appsettings + options pattern)
5. Observability (Serilog everywhere)
6. Extensibility (repository & service abstractions)
7. Reliability (retry logic, soft delete, migrations)
8. Testability (DI-friendly services)
9. Documentation & automation (README + commands)
10. Performance awareness (indexes, pagination, async)

---

## Development Workflow

1. **Install prerequisites:** .NET 8 SDK, SQL Server (localdb ok), Okta tenant (optional for local testing).
2. **Clone & restore:** `dotnet restore && dotnet build`.
3. **Configure secrets:** update `appsettings.Development.json` or user-secrets for connection strings, JWT key, Okta.
4. **Database:** run `dotnet ef database update ...`.
5. **Seed base data:** (Optional) call POST `/api/seed/cities` with `SeedData/cities.sample.csv`.
6. **Run API:** `dotnet run --project FleetManagement.Api`.
7. **Test endpoints:** use Swagger (`/swagger`) with JWT tokens (login/register) or Okta tokens.
8. **Add migrations when models change.**
9. **Update README / docs if architecture shifts.**

---

FleetManagement Server now satisfies all requested sections, migrations, Okta integration, and operational guidance for a fleet of vehicle truck owners. Happy exploring!
