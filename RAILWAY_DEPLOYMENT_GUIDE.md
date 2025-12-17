# Railway Deployment Guide for .NET API

## Overview

This guide will help you deploy your Fleet Management .NET API to Railway. Railway is an excellent choice for .NET applications with automatic Docker builds and easy environment variable management.

---

## Prerequisites

- [ ] Railway account ([railway.app](https://railway.app))
- [ ] GitHub repository with your code
- [ ] Supabase project created (for database)
- [ ] Okta application created (for authentication)
- [ ] Dockerfile in project root (✅ already exists)

---

## Step 1: Create Railway Account

1. Go to [https://railway.app](https://railway.app)
2. Click **"Start a New Project"**
3. Sign up with GitHub (recommended) or email
4. Verify your email if needed

---

## Step 2: Create New Project

1. **Click "New Project"**
2. **Select "Deploy from GitHub repo"**
3. **Authorize Railway** to access your GitHub repositories
4. **Select your repository**: `COMP584_Project_Server`
5. **Click "Deploy Now"**

Railway will automatically:
- Detect the Dockerfile
- Start building your application
- Deploy it (will fail initially until we configure environment variables)

---

## Step 3: Configure Environment Variables

### 3.1 Access Environment Variables

1. **Click on your project** in Railway dashboard
2. **Click on your service** (the deployed API)
3. **Go to "Variables" tab**
4. **Click "New Variable"** for each variable below

### 3.2 Required Environment Variables

Add these environment variables one by one:

#### Database (Supabase)

```
ConnectionStrings__PostgreSQL=Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-password;SSL Mode=Require;Trust Server Certificate=true;
```

**How to get this**:
1. Go to Supabase dashboard
2. Project Settings → Database
3. Copy connection string (URI format)
4. Convert to connection string format if needed

**Or use individual parameters**:
```
DATABASE_URL=postgresql://postgres:password@db.xxxxx.supabase.co:5432/postgres?sslmode=require
```

#### JWT Configuration (for Owner Authentication)

```
Jwt__Issuer=FleetManagement.Api
Jwt__Audience=FleetManagement.Client
Jwt__SigningKey=your-secure-random-key-minimum-32-characters-long-change-this
Jwt__AccessTokenMinutes=30
Jwt__RefreshTokenDays=14
```

**Generate secure signing key**:
```bash
# On Windows PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))

# Or use online generator: https://www.grc.com/passwords.htm
```

#### Okta Configuration (for User Authentication)

```
Okta__Domain=https://dev-123456.okta.com
Okta__AuthorizationServerId=default
Okta__Audience=api://default
Okta__ClientId=your-okta-client-id
Okta__ApiToken=your-okta-api-token
```

**How to get these**:
1. Okta Domain: Your Okta developer domain (e.g., `dev-123456.okta.com`)
2. Client ID: From Okta Application settings
3. API Token: From Okta → Security → API → Tokens

#### CORS Configuration

```
Cors__AllowedOrigins__0=https://your-app.vercel.app
Cors__AllowedOrigins__1=http://localhost:4200
Cors__AllowedOrigins__2=https://localhost:4200
```

**Replace** `your-app.vercel.app` with your actual Vercel domain.

#### Environment

```
ASPNETCORE_ENVIRONMENT=Production
```

#### Port Configuration (Railway sets this automatically)

Railway automatically sets `PORT` environment variable. Your Dockerfile already uses port 8080, which Railway will map correctly.

---

## Step 4: Update Program.cs for Railway

Railway requires some specific configurations. Update your `Program.cs`:

**File**: `FleetManagement.Api/Program.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using FleetManagement.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL Database (Supabase)
var postgresConnection = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? builder.Configuration["DATABASE_URL"]?.Replace("postgresql://", "postgresql://")
    ?? throw new InvalidOperationException("PostgreSQL connection string not found");

builder.Services.AddDbContext<FleetDbContext>(options =>
    options.UseNpgsql(postgresConnection));

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercelAndLocalhost", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                // Allow localhost for development
                if (origin?.StartsWith("http://localhost:") == true || 
                    origin?.StartsWith("https://localhost:") == true)
                    return true;
                
                // Allow all Vercel deployments
                if (origin?.EndsWith(".vercel.app") == true || 
                    origin?.EndsWith(".vercel.app/") == true)
                    return true;
                
                // Allow configured origins
                var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
                if (allowedOrigins != null && origin != null)
                {
                    return allowedOrigins.Contains(origin);
                }
                
                return false;
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure pipeline
app.UseCors("AllowVercelAndLocalhost");

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fleet Management API v1");
    c.RoutePrefix = "swagger";
});

// Health check endpoint (for Railway)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

// Your existing endpoints
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

---

## Step 5: Configure Database Provider

### 5.1 Add PostgreSQL Package

If not already added, add the PostgreSQL provider:

```bash
cd FleetManagement.Data
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

### 5.2 Update DbContext Registration

The Program.cs update above already includes the DbContext configuration. Make sure your `FleetDbContext` is accessible.

---

## Step 6: Run Database Migrations

### Option A: Run Migrations Automatically (Recommended)

Add migration code to `Program.cs` to run automatically on startup:

```csharp
// After app is built, before app.Run()
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<FleetDbContext>();
        context.Database.Migrate(); // Apply pending migrations
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}
```

### Option B: Run Migrations Manually

1. **Install Railway CLI** (optional):
   ```bash
   npm install -g @railway/cli
   ```

2. **Login to Railway**:
   ```bash
   railway login
   ```

3. **Link to your project**:
   ```bash
   railway link
   ```

4. **Run migrations**:
   ```bash
   railway run dotnet ef database update --project FleetManagement.Data
   ```

---

## Step 7: Verify Deployment

### 7.1 Check Build Logs

1. **Go to Railway dashboard**
2. **Click on your service**
3. **Click "Deployments" tab**
4. **Check build logs** for any errors

### 7.2 Test Health Endpoint

Once deployed, Railway will give you a URL like:
```
https://your-app.up.railway.app
```

Test the health endpoint:
```bash
curl https://your-app.up.railway.app/health
```

Expected response:
```json
{"status":"healthy","timestamp":"2024-01-01T00:00:00Z"}
```

### 7.3 Test Swagger UI

Visit:
```
https://your-app.up.railway.app/swagger
```

You should see the Swagger UI with your API endpoints.

### 7.4 Test Weather Forecast Endpoint

```bash
curl https://your-app.up.railway.app/weatherforecast
```

---

## Step 8: Configure Custom Domain (Optional)

1. **Go to Railway dashboard**
2. **Click on your service**
3. **Go to "Settings" tab**
4. **Scroll to "Domains"**
5. **Click "Generate Domain"** or **"Add Custom Domain"**
6. **Copy the domain** (e.g., `fleetmanagement-api.up.railway.app`)

---

## Step 9: Update Client Configuration

### Update Angular Environment

**File**: `src/environments/environment.prod.ts` (in your Angular app)

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://your-app.up.railway.app/api',
  okta: {
    issuer: 'https://dev-123456.okta.com/oauth2/default',
    clientId: 'your-okta-client-id',
    redirectUri: window.location.origin + '/login/callback',
    scopes: ['openid', 'profile', 'email']
  }
};
```

### Update Vercel Configuration (if using proxy)

**File**: `vercel.json` (in your Angular app)

```json
{
  "version": 2,
  "framework": "angular",
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://your-app.up.railway.app/api/:path*"
    }
  ]
}
```

---

## Step 10: Monitor and Debug

### View Logs

1. **Go to Railway dashboard**
2. **Click on your service**
3. **Click "Logs" tab**
4. **View real-time logs**

### Common Issues

#### Issue: Build Fails
- **Check**: Dockerfile is in project root
- **Check**: All project references are correct
- **Check**: Build logs for specific errors

#### Issue: Database Connection Fails
- **Check**: Connection string is correct
- **Check**: Supabase project is active (not paused)
- **Check**: SSL mode is set to `Require`
- **Check**: Database password is correct

#### Issue: CORS Errors
- **Check**: Vercel domain is in `Cors__AllowedOrigins`
- **Check**: CORS middleware is before other middleware
- **Check**: Origin matches exactly (including protocol)

#### Issue: Application Crashes
- **Check**: Logs tab in Railway
- **Check**: All required environment variables are set
- **Check**: Database migrations have run

---

## Railway-Specific Features

### Automatic Deployments

Railway automatically deploys when you push to your main branch.

### Environment Variables

Railway makes it easy to manage environment variables:
- Set in dashboard
- Can be different per environment
- Supports secrets management

### Scaling

Railway can automatically scale your application based on traffic.

### Metrics

Railway provides:
- CPU usage
- Memory usage
- Network traffic
- Request logs

---

## Cost

### Railway Pricing

- **Free Tier**: $5 credit per month
- **Hobby Plan**: $5/month (after free credit)
- **Pro Plan**: $20/month

**Estimated Cost**: $0-5/month for small applications

---

## Railway.json Configuration

Your existing `railway.json` should work. If you need to customize:

```json
{
  "$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "DOCKERFILE",
    "dockerfilePath": "Dockerfile"
  },
  "deploy": {
    "startCommand": "dotnet FleetManagement.Api.dll",
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10
  }
}
```

---

## Quick Deployment Checklist

- [ ] Railway account created
- [ ] Project created from GitHub repo
- [ ] Environment variables configured:
  - [ ] PostgreSQL connection string
  - [ ] JWT configuration
  - [ ] Okta configuration
  - [ ] CORS origins
  - [ ] ASPNETCORE_ENVIRONMENT
- [ ] Program.cs updated for PostgreSQL
- [ ] Database migrations run
- [ ] Health endpoint tested
- [ ] Swagger UI accessible
- [ ] Client updated with Railway URL
- [ ] End-to-end testing completed

---

## Next Steps After Deployment

1. **Implement API Controllers** (see `SERVER_TASKS.md`)
2. **Configure Okta Authentication** (see `OKTA_CONFIGURATION_GUIDE.md`)
3. **Test Integration** with Angular client
4. **Set up Monitoring** in Railway dashboard
5. **Configure Custom Domain** (optional)

---

## Support Resources

- **Railway Docs**: https://docs.railway.app
- **Railway Discord**: https://discord.gg/railway
- **Railway Status**: https://status.railway.app

---

## Summary

Railway is an excellent choice for deploying .NET applications because:
- ✅ Automatic Docker builds
- ✅ Easy environment variable management
- ✅ Automatic HTTPS
- ✅ Simple scaling
- ✅ Great developer experience
- ✅ Free tier available

**Your application will be live at**: `https://your-app.up.railway.app`

Good luck with your deployment! 🚀












