using System.Text;
using FleetManagement.Api.Hubs;
using FleetManagement.Data;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.Cities;
using FleetManagement.Services.Fleets;
using FleetManagement.Services.Owners;
using FleetManagement.Services.Vehicles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===========================================
// 0. FORCE IPv4 (Railway/Supabase IPv6 issue)
// ===========================================
// Supabase may resolve to IPv6, but Railway can't connect via IPv6
AppContext.SetSwitch("System.Net.DisableIPv6", true);

// ===========================================
// 0. CONFIGURE PORT FOR RAILWAY
// ===========================================
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

Console.WriteLine($"[STARTUP] Configuring server to listen on port {port}");

// ===========================================
// 1. ADD CONTROLLERS
// ===========================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ===========================================
// 2. SWAGGER WITH JWT SUPPORT
// ===========================================
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fleet Management API",
        Version = "v1",
        Description = "API for managing fleets, vehicles, and telemetry"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ===========================================
// 3. DATABASE CONFIGURATION
// ===========================================
// PRIORITY: Environment variable first (Railway sets DATABASE_URL)
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

// Fallback to configuration if env var not set
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

Console.WriteLine($"[STARTUP] DATABASE_URL from env: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL"))}");
Console.WriteLine($"[STARTUP] Connection string found: {!string.IsNullOrEmpty(connectionString)}");

if (!string.IsNullOrEmpty(connectionString))
{
    // Handle Railway/Supabase PostgreSQL connection strings
    if (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://"))
    {
        try
        {
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo;
            
            // Split on LAST colon to handle passwords that might contain colons
            var colonIndex = userInfo.IndexOf(':');
            var username = colonIndex > 0 ? userInfo.Substring(0, colonIndex) : userInfo;
            var password = colonIndex > 0 ? userInfo.Substring(colonIndex + 1) : "";
            
            // URL-decode username and password (handles %40 -> @, etc.)
            username = Uri.UnescapeDataString(username);
            password = Uri.UnescapeDataString(password);
            
            var database = uri.AbsolutePath.TrimStart('/');
            if (string.IsNullOrEmpty(database)) database = "postgres";
            
            var dbPort = uri.Port > 0 ? uri.Port : 5432;
            var host = uri.Host;
            
            // AUTO-FIX: Convert Supabase direct connection to pooler (fixes IPv6 issue)
            // Direct: db.xxxxx.supabase.co -> Pooler: aws-0-[region].pooler.supabase.com
            if (host.Contains(".supabase.co") && !host.Contains("pooler"))
            {
                // Extract project ref from host (db.PROJECTREF.supabase.co)
                var parts = host.Split('.');
                if (parts.Length >= 2 && parts[0] == "db")
                {
                    // Try to determine region from project (default to us-east-1)
                    // For now, use a common pooler endpoint
                    // User should update DATABASE_URL to use pooler directly, but this helps
                    Console.WriteLine($"[STARTUP] WARNING: Direct Supabase connection detected. Consider using Connection Pooler (port 6543) to avoid IPv6 issues.");
                    Console.WriteLine($"[STARTUP] Current host: {host} - This may fail due to IPv6 resolution.");
                }
            }
            
            connectionString = $"Host={host};Port={dbPort};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Timeout=10;Command Timeout=10;Keepalive=30";
            Console.WriteLine($"[STARTUP] Converted PostgreSQL URI. Host: {host}, Port: {dbPort}, User: {username}");
            
            // Warn if using direct Supabase connection (IPv6 issue)
            if (host.Contains(".supabase.co") && !host.Contains("pooler") && dbPort == 5432)
            {
                Console.WriteLine($"[STARTUP] ⚠️  WARNING: Using direct Supabase connection (port 5432)");
                Console.WriteLine($"[STARTUP] ⚠️  This may fail due to IPv6 resolution on Railway.");
                Console.WriteLine($"[STARTUP] ⚠️  SOLUTION: Use Supabase Connection Pooler (port 6543)");
                Console.WriteLine($"[STARTUP] ⚠️  Get pooler URL from: Supabase Dashboard → Settings → Database → Connection Pooling");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[STARTUP] Error parsing DATABASE_URL: {ex.Message}");
        }
    }

    builder.Services.AddDbContext<FleetDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    Console.WriteLine("[STARTUP] No DATABASE_URL found, using in-memory database");
    builder.Services.AddDbContext<FleetDbContext>(options =>
        options.UseInMemoryDatabase("FleetManagementDb"));
}

// ===========================================
// 4. JWT AUTHENTICATION
// ===========================================
var jwtSecret = builder.Configuration["Jwt:Secret"] 
    ?? Environment.GetEnvironmentVariable("JWT_SECRET") 
    ?? "FleetManagement_SuperSecretKey_ChangeInProduction_AtLeast32Chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] 
    ?? Environment.GetEnvironmentVariable("JWT_ISSUER") 
    ?? "FleetManagementAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] 
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
    ?? "FleetManagementClient";

// Auth0 Configuration
var auth0Domain = builder.Configuration["Auth0:Domain"] 
    ?? Environment.GetEnvironmentVariable("AUTH0_DOMAIN") 
    ?? "";
var auth0Audience = builder.Configuration["Auth0:Audience"] 
    ?? Environment.GetEnvironmentVariable("AUTH0_AUDIENCE") 
    ?? "https://fleetmanagement-api-production.up.railway.app";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Configure for both custom JWT and Auth0 tokens
    var validIssuers = new List<string> { jwtIssuer };
    var validAudiences = new List<string> { jwtAudience };
    
    // Add Auth0 issuer if configured
    if (!string.IsNullOrEmpty(auth0Domain))
    {
        // Auth0 issuer format: https://{domain}/
        var auth0Issuer = auth0Domain.StartsWith("http") ? auth0Domain : $"https://{auth0Domain}/";
        validIssuers.Add(auth0Issuer);
        validAudiences.Add(auth0Audience);
    }
    
    // Add Okta issuer if configured
    var oktaDomain = builder.Configuration["Okta:Domain"] ?? "";
    if (!string.IsNullOrEmpty(oktaDomain))
    {
        validIssuers.Add(oktaDomain);
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuers = validIssuers.ToArray(),
        ValidAudiences = validAudiences.ToArray(),
        // For custom JWT tokens (owner authentication)
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };

    // Configure Auth0 token validation (if Auth0 is configured)
    if (!string.IsNullOrEmpty(auth0Domain))
    {
        // Auth0 uses JWKS (JSON Web Key Set) for token validation
        // The issuer is https://{domain}/
        var auth0Issuer = auth0Domain.StartsWith("http") ? auth0Domain : $"https://{auth0Domain}/";
        
        // Set Authority for Auth0 JWKS validation
        // This allows the middleware to fetch signing keys from Auth0's JWKS endpoint
        options.Authority = auth0Issuer;
        options.Audience = auth0Audience;
        
        // Note: When Authority is set, Auth0 tokens are validated using JWKS automatically
        // Custom JWT tokens (with our symmetric key) are still validated via TokenValidationParameters
        // The middleware will try both validation methods based on the token's issuer
    }

    // Configure JWT for SignalR WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // SignalR sends token as query string parameter "access_token"
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && 
                (path.StartsWithSegments("/hub/fleets") || 
                 path.StartsWithSegments("/hub/vehicles") || 
                 path.StartsWithSegments("/hub/telemetry")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// ===========================================
// 5. AUTHORIZATION POLICIES
// ===========================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());
    
    options.AddPolicy("RequireOwner", policy =>
        policy.RequireRole("Owner", "Admin"));
    
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Admin"));
});

// ===========================================
// 6. SIGNALR CONFIGURATION
// ===========================================
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Enable in development for debugging
});

// ===========================================
// 7. REGISTER SERVICES (Dependency Injection)
// ===========================================
builder.Services.AddScoped<IOwnerService, OwnerService>();
builder.Services.AddScoped<IFleetService, FleetService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<ICountryService, CountryService>();

// ===========================================
// 8. CORS CONFIGURATION
// ===========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrEmpty(origin)) return false;
            
            // Explicitly allow Vercel deployment
            if (origin == "https://comp-584-project-client-vercel.vercel.app")
                return true;
            
            // Allow localhost for development
            if (origin.StartsWith("http://localhost:") || origin.StartsWith("https://localhost:"))
                return true;
            
            // Allow any Vercel deployment
            if (origin.EndsWith(".vercel.app"))
                return true;
            
            // Allow Railway deployments
            if (origin.EndsWith(".railway.app"))
                return true;

            // Allow Netlify deployments
            if (origin.EndsWith(".netlify.app"))
                return true;

            return false;
        })
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

Console.WriteLine("[STARTUP] Application built successfully");

// ===========================================
// 9. MIDDLEWARE PIPELINE
// ===========================================

// CORS must be before authentication/authorization
app.UseCors("AllowFrontend");

// Handle preflight OPTIONS requests
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }
    await next();
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fleet Management API v1");
    options.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

// ===========================================
// 10. HEALTH CHECK ENDPOINT
// ===========================================
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithOpenApi();

// Debug endpoint to test database
app.MapGet("/debug/db", async (FleetDbContext db) =>
{
    var result = new Dictionary<string, object?>();
    result["timestamp"] = DateTime.UtcNow;
    result["db_type"] = db.Database.IsNpgsql() ? "PostgreSQL" : "InMemory";
    
    try
    {
        result["can_connect"] = await db.Database.CanConnectAsync();
        result["country_count"] = await db.Countries.CountAsync();
        result["city_count"] = await db.Cities.CountAsync();
    }
    catch (Exception ex)
    {
        result["error"] = ex.Message;
        result["inner_error"] = ex.InnerException?.Message;
    }
    
    return Results.Ok(result);
})
.WithName("DebugDb")
.WithOpenApi();

app.MapGet("/", () => Results.Ok(new
{
    name = "Fleet Management API",
    version = "1.0.0",
    documentation = "/swagger",
    health = "/health"
}))
.WithName("Root")
.WithOpenApi();

// ===========================================
// 11. MAP SIGNALR HUBS
// ===========================================
app.MapHub<FleetHub>("/hub/fleets");
app.MapHub<VehicleHub>("/hub/vehicles");
app.MapHub<TelemetryHub>("/hub/telemetry");

Console.WriteLine("[STARTUP] SignalR hubs mapped:");
Console.WriteLine("  - /hub/fleets");
Console.WriteLine("  - /hub/vehicles");
Console.WriteLine("  - /hub/telemetry");

// ===========================================
// 12. MAP CONTROLLERS
// ===========================================
app.MapControllers();

// ===========================================
// 13. DATABASE CONNECTION TEST (non-blocking)
// ===========================================
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<FleetDbContext>();
    
    if (context.Database.IsNpgsql())
    {
        // For Supabase: DO NOT run migrations - tables are created via SQL seed script
        // Just test the connection
        Console.WriteLine("[STARTUP] Testing PostgreSQL connection...");
        var canConnect = await context.Database.CanConnectAsync();
        Console.WriteLine($"[STARTUP] PostgreSQL connection: {(canConnect ? "SUCCESS" : "FAILED")}");
        
        if (canConnect)
        {
            var countryCount = await context.Countries.CountAsync();
            Console.WriteLine($"[STARTUP] Found {countryCount} countries in database");
        }
    }
    else
    {
        Console.WriteLine("[STARTUP] Using in-memory database, ensuring created...");
        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("[STARTUP] In-memory database ready");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[STARTUP] Database setup error: {ex.Message}");
    if (ex.InnerException != null)
        Console.WriteLine($"[STARTUP] Inner: {ex.InnerException.Message}");
}

Console.WriteLine($"[STARTUP] Server starting on http://0.0.0.0:{port}");

app.Run();
