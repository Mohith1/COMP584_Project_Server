# Supabase Database Configuration Guide

## Overview

This guide will help you configure your .NET API to use Supabase (PostgreSQL) instead of SQL Server.

---

## Step 1: Create Supabase Project

1. Go to [https://supabase.com](https://supabase.com)
2. Sign up for a free account
3. **Create a new project**:
   - Project name: `Fleet Management`
   - Database password: **Save this password!**
   - Region: Choose closest to your AWS deployment
4. Wait for project to initialize (~2 minutes)

---

## Step 2: Get Supabase Connection String

1. **Navigate to**: Project Settings → Database
2. **Find "Connection string"** section
3. **Select "URI"** tab
4. **Copy the connection string** (it looks like):
   ```
   postgresql://postgres:[YOUR-PASSWORD]@db.xxxxx.supabase.co:5432/postgres
   ```

**Or use the connection parameters**:
- **Host**: `db.xxxxx.supabase.co`
- **Port**: `5432`
- **Database**: `postgres`
- **User**: `postgres`
- **Password**: Your database password

---

## Step 3: Install PostgreSQL Provider for Entity Framework

### Add Npgsql Package

Run this command in your `FleetManagement.Data` project:

```bash
cd FleetManagement.Data
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

Or manually edit `FleetManagement.Data/FleetManagement.Data.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.7" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.7">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <!-- Add PostgreSQL provider -->
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.7" />
    <!-- Keep SQLite for local dev if needed -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.7" />
    <!-- Remove SQL Server if not needed -->
    <!-- <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.7" /> -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.7">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
  </ItemGroup>
</Project>
```

---

## Step 4: Find and Update DbContext Configuration

You need to find where your `FleetDbContext` is configured. Let me search for it:

**Check these locations**:
- `FleetManagement.Data/DependencyInjection/` (likely location)
- `FleetManagement.Data/Common/`
- Or it might be in `FleetManagement.Api/Program.cs`

### Update DbContext to Use PostgreSQL

Wherever your DbContext is configured, change from SQL Server to PostgreSQL:

**Before (SQL Server)**:
```csharp
services.AddDbContext<FleetDbContext>(options =>
    options.UseSqlServer(connectionString));
```

**After (PostgreSQL/Supabase)**:
```csharp
services.AddDbContext<FleetDbContext>(options =>
    options.UseNpgsql(connectionString));
```

**Example in Program.cs** (if configured there):

```csharp
using Microsoft.EntityFrameworkCore;
using Npgsql; // Add this using

var builder = WebApplication.CreateBuilder(args);

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL") 
    ?? throw new InvalidOperationException("Connection string 'PostgreSQL' not found.");

// Configure DbContext with PostgreSQL
builder.Services.AddDbContext<FleetDbContext>(options =>
    options.UseNpgsql(connectionString));
```

---

## Step 5: Update Connection Strings

### Update appsettings.json

**File**: `FleetManagement.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-password-here",
    "Sqlite": "Data Source=FleetManagement.db"
  }
}
```

### Update appsettings.Development.json

**File**: `FleetManagement.Api/appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-password-here",
    "Sqlite": "Data Source=FleetManagement.db"
  }
}
```

---

## Step 6: Create New Migration for PostgreSQL

Since you're switching database providers, you may need to create a new migration:

```bash
# Navigate to API project
cd FleetManagement.Api

# Create new migration (PostgreSQL will be used)
dotnet ef migrations add InitialPostgreSQLMigration --project ../FleetManagement.Data

# Apply migration to Supabase
dotnet ef database update --project ../FleetManagement.Data
```

**Note**: Your existing migration (`20251129021544_InitialSchema.cs`) was created for SQLite. PostgreSQL has some differences:
- `TEXT` → `text` or `varchar(n)`
- `INTEGER` → `integer`
- `decimal` → `numeric` or `decimal`

Entity Framework should handle most conversions automatically, but you may need to adjust some data types.

---

## Step 7: Update Program.cs to Use PostgreSQL Connection

**File**: `FleetManagement.Api/Program.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using FleetManagement.Data;
using Npgsql; // Add this

var builder = WebApplication.CreateBuilder(args);

// Get PostgreSQL connection string
var postgresConnection = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException("PostgreSQL connection string not found");

// Configure DbContext with PostgreSQL
builder.Services.AddDbContext<FleetDbContext>(options =>
    options.UseNpgsql(postgresConnection));

// ... rest of your configuration
```

---

## Step 8: Configure AWS Elastic Beanstalk Environment Variables

In AWS Elastic Beanstalk → Configuration → Software → Environment Properties:

**Add**:
```
ConnectionStrings__PostgreSQL=Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-password-here
```

**Or use AWS Secrets Manager** (recommended for production):

1. **Store connection string in AWS Secrets Manager**
2. **Reference in environment variable**:
   ```
   ConnectionStrings__PostgreSQL={{resolve:secretsmanager:supabase-connection:SecretString}}
   ```

---

## Step 9: Test Connection Locally

```bash
# Test connection
cd FleetManagement.Api
dotnet run

# Check if database connection works
# Look for any connection errors in console
```

**Verify in Supabase Dashboard**:
1. Go to Supabase → Table Editor
2. You should see your tables after running migrations

---

## Step 10: Handle PostgreSQL-Specific Data Types

PostgreSQL has some differences from SQL Server/SQLite. Check your migration file and update if needed:

### Common Conversions:

| SQL Server/SQLite | PostgreSQL |
|------------------|------------|
| `TEXT` | `text` or `varchar(n)` |
| `INTEGER` | `integer` |
| `char(n)` | `char(n)` (same) |
| `varchar(n)` | `varchar(n)` (same) |
| `decimal(p,s)` | `numeric(p,s)` or `decimal(p,s)` |

Your existing migration should work, but verify:

**File**: `FleetManagement.Data/Migrations/20251129021544_InitialSchema.cs`

The migration uses `TEXT` and `INTEGER` which EF Core will convert automatically. However, for better PostgreSQL compatibility, you might want to create a new migration.

---

## Step 11: Update Dockerfile (if using Docker)

If your Dockerfile has any database-specific commands, ensure they work with PostgreSQL.

**Example Dockerfile** (should work as-is):
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FleetManagement.Api/FleetManagement.Api.csproj", "FleetManagement.Api/"]
COPY ["FleetManagement.Services/FleetManagement.Services.csproj", "FleetManagement.Services/"]
COPY ["FleetManagement.Data/FleetManagement.Data.csproj", "FleetManagement.Data/"]
RUN dotnet restore "FleetManagement.Api/FleetManagement.Api.csproj"
COPY . .
WORKDIR "/src/FleetManagement.Api"
RUN dotnet build "FleetManagement.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FleetManagement.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FleetManagement.Api.dll"]
```

---

## Step 12: Run Migrations on Supabase

After deploying to AWS, run migrations:

### Option A: Run migrations during deployment

Add to your deployment script or `Program.cs`:

```csharp
// In Program.cs (after app is built)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<FleetDbContext>();
    context.Database.Migrate(); // Apply pending migrations
}
```

### Option B: Run migrations manually

```bash
# From your local machine (with AWS credentials configured)
dotnet ef database update --project FleetManagement.Data --connection "your-supabase-connection-string"
```

---

## Troubleshooting

### Issue: "NpgsqlException: Connection refused"
- **Check**: Supabase project is active (not paused)
- **Check**: Connection string is correct
- **Check**: IP address is allowed (Supabase allows all by default)

### Issue: "Migration failed"
- **Check**: Database user has CREATE TABLE permissions
- **Solution**: Supabase postgres user has full permissions by default

### Issue: "Data type mismatch"
- **Check**: Migration file data types
- **Solution**: Create new migration after switching to PostgreSQL

### Issue: "SSL required"
- **Solution**: Add `SSL Mode=Require;` to connection string:
  ```
  Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require;
  ```

---

## Supabase Connection String Format

**Full format**:
```
Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-password;SSL Mode=Require;Trust Server Certificate=true;
```

**Or using URI format**:
```
postgresql://postgres:password@db.xxxxx.supabase.co:5432/postgres?sslmode=require
```

---

## Summary Checklist

- [ ] Supabase project created
- [ ] Connection string copied
- [ ] Npgsql.EntityFrameworkCore.PostgreSQL package added
- [ ] DbContext configured to use `UseNpgsql()`
- [ ] Connection string added to appsettings.json
- [ ] Connection string added to AWS environment variables
- [ ] Migrations run on Supabase
- [ ] Connection tested locally
- [ ] Connection tested in AWS

---

## Next Steps

1. **Test locally** with Supabase connection
2. **Run migrations** to create tables
3. **Deploy to AWS** with connection string in environment variables
4. **Verify** tables exist in Supabase dashboard

Your database is now ready to use with Supabase! 🎉













