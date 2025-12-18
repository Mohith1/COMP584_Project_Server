# Supabase Database Setup Instructions

## Step 1: Run the SQL Script

1. **Open Supabase Dashboard**
   - Go to [supabase.com](https://supabase.com)
   - Log in to your project

2. **Open SQL Editor**
   - Click on "SQL Editor" in the left sidebar
   - Click "New query"

3. **Copy and Paste the Script**
   - Open `SUPABASE_TABLES_CREATION.sql`
   - Copy the entire contents
   - Paste into the SQL Editor

4. **Run the Script**
   - Click "Run" or press `Ctrl+Enter` (Windows) / `Cmd+Enter` (Mac)
   - Wait for all tables to be created
   - You should see "Success. No rows returned" for each CREATE TABLE statement

## Step 2: Verify Tables Created

1. **Check Tables**
   - Go to "Table Editor" in Supabase dashboard
   - You should see all these tables:
     - Countries
     - Cities
     - AppUsers
     - AppRoles
     - AspNetUserClaims
     - AspNetUserLogins
     - AspNetUserRoles
     - AspNetUserTokens
     - AspNetRoleClaims
     - Owners
     - Fleets
     - Vehicles
     - FleetUsers
     - TelematicsDevices
     - MaintenanceTickets
     - VehicleTelemetrySnapshots
     - RefreshTokens

## Step 3: Get Connection String

1. **Get Database Connection String**
   - Go to "Settings" → "Database"
   - Scroll to "Connection string"
   - Select "URI" tab
   - Copy the connection string (it looks like: `postgresql://postgres:[YOUR-PASSWORD]@db.xxxxx.supabase.co:5432/postgres`)

2. **Format for .NET**
   - The connection string should be in this format:
   ```
   Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-password;SSL Mode=Require;Trust Server Certificate=true;
   ```

## Step 4: Configure Railway Environment Variables

1. **Go to Railway Dashboard**
   - Navigate to your service
   - Click "Variables" tab

2. **Add Connection String**
   - Add a new variable:
     - **Key**: `ConnectionStrings__PostgreSQL`
     - **Value**: Your formatted connection string from Step 3

   OR use the DATABASE_URL format:
   - **Key**: `DATABASE_URL`
   - **Value**: The URI format from Supabase (postgresql://...)

3. **Redeploy**
   - Railway will automatically redeploy when you save the variable
   - Or trigger a manual redeploy

## Step 5: Run Database Migrations (Optional)

If you want to use Entity Framework migrations instead of the SQL script:

1. **SSH into Railway** (or use Railway CLI)
2. **Run migrations**:
   ```bash
   dotnet ef database update --project FleetManagement.Data
   ```

## Step 6: Test the API

1. **Check Health Endpoint**
   ```
   GET https://fleetmanagement-api-production.up.railway.app/health
   ```

2. **Test Creating Data**
   - Use Swagger UI: `https://fleetmanagement-api-production.up.railway.app/swagger`
   - Try creating a Country first
   - Then create a City
   - Then create an Owner
   - Then create a Fleet
   - Then create a Vehicle

## Table Relationships

```
Countries
  └── Cities
       └── Owners
            ├── Fleets
            │    └── Vehicles
            │         ├── TelematicsDevices
            │         ├── MaintenanceTickets
            │         └── VehicleTelemetrySnapshots
            └── FleetUsers
                 └── (assigned to) Vehicles

AppUsers (Identity)
  ├── Owners (via IdentityUserId)
  └── RefreshTokens
```

## Important Notes

- **Soft Deletes**: All tables have `IsDeleted` flag for soft delete functionality
- **Timestamps**: All tables use `TIMESTAMPTZ` (timezone-aware timestamps)
- **UUIDs**: All primary keys use UUID type
- **Indexes**: Indexes are created for foreign keys and commonly queried fields
- **Cascade Deletes**: Foreign keys are set to CASCADE where appropriate

## Troubleshooting

### Connection Issues
- Make sure SSL Mode is set to `Require`
- Add `Trust Server Certificate=true` if needed
- Check that your Supabase project is not paused

### Migration Issues
- If migrations fail, you can use the SQL script directly
- The SQL script creates all tables without migrations

### Table Not Found Errors
- Verify tables were created in Supabase Table Editor
- Check that you're using the correct database name (usually `postgres`)

## Next Steps

After tables are created:
1. ✅ Test API endpoints
2. ✅ Seed initial data (countries, cities)
3. ✅ Configure authentication (JWT/Okta)
4. ✅ Set up Row Level Security (RLS) in Supabase if needed









