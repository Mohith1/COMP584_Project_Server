# Okta Authentication Configuration Guide

## Overview

Your application already has Okta packages installed (`Okta.AspNetCore` v4.6.8). This guide will help you configure Okta authentication for the "User" persona (the "Owner" persona uses JWT authentication).

---

## Step 1: Create Okta Developer Account

1. Go to [https://developer.okta.com/](https://developer.okta.com/)
2. Sign up for a free developer account
3. You'll get a free Okta domain (e.g., `dev-123456.okta.com`)

---

## Step 2: Create Okta Application

1. **Log into Okta Admin Console**
2. **Navigate to Applications → Applications**
3. **Click "Create App Integration"**
4. **Select Sign-in method**: OIDC - OpenID Connect
5. **Select Application type**: Single-Page App (SPA)
6. **Click Next**

### Application Settings:

**App integration name**: `Fleet Management User Portal`

**Grant types**: Check these:
- ✅ Authorization Code
- ✅ Refresh Token

**Sign-in redirect URIs**: Add your client URLs:
```
http://localhost:4200/login/callback
https://your-app.vercel.app/login/callback
```

**Sign-out redirect URIs**: Add:
```
http://localhost:4200
https://your-app.vercel.app
```

**Controlled access**: 
- Select "Allow everyone in your organization to access" (or configure groups)

7. **Click "Save"**

### Copy Your Credentials:

After saving, you'll see:
- **Client ID**: Copy this
- **Client Secret**: Not needed for SPA (PKCE flow)
- **Okta Domain**: Your domain (e.g., `dev-123456.okta.com`)

---

## Step 3: Configure Authorization Server

1. **Navigate to Security → API → Authorization Servers**
2. **Select "default"** (or create a custom one)
3. **Note the "Issuer URI"**: Usually `https://{your-domain}.okta.com/oauth2/default`

---

## Step 4: Configure Backend (.NET API)

### 4.1 Update appsettings.json

Update your `FleetManagement.Api/appsettings.json`:

```json
{
  "Okta": {
    "Domain": "https://dev-123456.okta.com",
    "AuthorizationServerId": "default",
    "Audience": "api://default",
    "ClientId": "your-client-id-here",
    "ApiToken": "your-api-token-here"
  }
}
```

**For Production (AWS Elastic Beanstalk)**, set these as environment variables:
- `Okta__Domain`
- `Okta__AuthorizationServerId`
- `Okta__Audience`
- `Okta__ClientId`
- `Okta__ApiToken`

### 4.2 Get Okta API Token (for backend operations)

1. **Navigate to Security → API → Tokens**
2. **Click "Create Token"**
3. **Name**: `Fleet Management API`
4. **Copy the token** (you won't see it again!)
5. **Add to environment variables**: `Okta__ApiToken`

---

## Step 5: Configure Program.cs

Add Okta authentication to your `FleetManagement.Api/Program.cs`:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Okta.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// Configure Okta Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // JWT for Owner authentication (existing)
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        // ... your JWT config
    };
})
.AddOktaJwtBearer(new OktaJwtBearerOptions
{
    // Okta for User authentication
    OktaDomain = builder.Configuration["Okta:Domain"],
    AuthorizationServerId = builder.Configuration["Okta:AuthorizationServerId"],
    Audience = builder.Configuration["Okta:Audience"]
});

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireUser", policy => 
        policy.RequireAuthenticatedUser());
    
    options.AddPolicy("RequireOwner", policy => 
        policy.RequireRole("Owner"));
});

var app = builder.Build();

// Configure pipeline
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

---

## Step 6: Create Okta Service (Optional - for user sync)

If you want to sync Okta users with your database, create:

**File**: `FleetManagement.Services/Okta/IOktaService.cs`

```csharp
using FleetManagement.Data.Entities;

namespace FleetManagement.Services.Okta;

public interface IOktaService
{
    Task<ApplicationUser?> GetUserByOktaIdAsync(string oktaUserId);
    Task<ApplicationUser> SyncUserFromOktaAsync(string oktaUserId, string email, string firstName, string lastName);
    Task<bool> ValidateOktaTokenAsync(string token);
}
```

**File**: `FleetManagement.Services/Okta/OktaService.cs`

```csharp
using FleetManagement.Data;
using FleetManagement.Data.Entities;
using FleetManagement.Services.Options;
using Microsoft.Extensions.Options;
using Okta.Sdk;
using Okta.Sdk.Configuration;

namespace FleetManagement.Services.Okta;

public class OktaService : IOktaService
{
    private readonly FleetDbContext _context;
    private readonly OktaClient _oktaClient;
    private readonly OktaOptions _options;

    public OktaService(
        FleetDbContext context,
        IOptions<OktaOptions> options)
    {
        _context = context;
        _options = options.Value;
        
        _oktaClient = new OktaClient(new OktaClientConfiguration
        {
            OktaDomain = _options.Domain,
            Token = _options.ApiToken
        });
    }

    public async Task<ApplicationUser?> GetUserByOktaIdAsync(string oktaUserId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.OktaUserId == oktaUserId);
    }

    public async Task<ApplicationUser> SyncUserFromOktaAsync(
        string oktaUserId, 
        string email, 
        string firstName, 
        string lastName)
    {
        var existingUser = await GetUserByOktaIdAsync(oktaUserId);
        
        if (existingUser != null)
        {
            // Update existing user
            existingUser.Email = email;
            existingUser.UserName = email;
            existingUser.NormalizedEmail = email.ToUpperInvariant();
            existingUser.NormalizedUserName = email.ToUpperInvariant();
            existingUser.LastLoginUtc = DateTimeOffset.UtcNow;
            
            await _context.SaveChangesAsync();
            return existingUser;
        }

        // Create new user
        var newUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            OktaUserId = oktaUserId,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            EmailConfirmed = true,
            LastLoginUtc = DateTimeOffset.UtcNow
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
        
        return newUser;
    }

    public async Task<bool> ValidateOktaTokenAsync(string token)
    {
        try
        {
            // Token validation is handled by Okta middleware
            // This method can be used for additional validation if needed
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

---

## Step 7: Create Okta Options Class

**File**: `FleetManagement.Services/Options/OktaOptions.cs`

```csharp
namespace FleetManagement.Services.Options;

public class OktaOptions
{
    public string Domain { get; set; } = string.Empty;
    public string AuthorizationServerId { get; set; } = "default";
    public string Audience { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
}
```

**Register in Program.cs**:

```csharp
builder.Services.Configure<OktaOptions>(
    builder.Configuration.GetSection("Okta"));
```

---

## Step 8: Protect API Endpoints

In your controllers, use `[Authorize]` attribute:

```csharp
[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    [HttpGet]
    [Authorize] // Requires Okta authentication
    public async Task<IActionResult> GetTelemetry()
    {
        // Get user from Okta token
        var oktaUserId = User.FindFirst("sub")?.Value;
        
        // Your logic here
        return Ok();
    }
}
```

---

## Step 9: Client Configuration (Already Done)

Your Angular client already has Okta configured. Just update the environment:

**File**: `src/environments/environment.prod.ts` (in your Angular app)

```typescript
export const environment = {
  production: true,
  okta: {
    issuer: 'https://dev-123456.okta.com/oauth2/default',
    clientId: 'your-client-id',
    redirectUri: window.location.origin + '/login/callback',
    scopes: ['openid', 'profile', 'email']
  }
};
```

---

## Step 10: Test Okta Authentication

### Test Flow:

1. **User clicks "Login"** in Angular app
2. **Redirects to Okta login page**
3. **User enters credentials**
4. **Okta redirects back** with authorization code
5. **Angular exchanges code for tokens**
6. **Angular sends token** to API in `Authorization: Bearer {token}` header
7. **API validates token** using Okta middleware
8. **API returns data**

### Test Endpoint:

```bash
# Get Okta token from Angular app (check browser DevTools → Application → Session Storage)
curl -X GET https://your-api.elasticbeanstalk.com/api/telemetry \
  -H "Authorization: Bearer {okta-access-token}"
```

---

## AWS Elastic Beanstalk Environment Variables

Set these in AWS Elastic Beanstalk → Configuration → Software → Environment Properties:

```
Okta__Domain=https://dev-123456.okta.com
Okta__AuthorizationServerId=default
Okta__Audience=api://default
Okta__ClientId=your-client-id
Okta__ApiToken=your-api-token
```

---

## Troubleshooting

### Issue: "Invalid token"
- **Check**: Token expiration (Okta tokens expire after 1 hour)
- **Solution**: Client should refresh token automatically

### Issue: "CORS error"
- **Check**: CORS configuration in `Program.cs`
- **Solution**: Add your Vercel domain to allowed origins

### Issue: "Unauthorized"
- **Check**: Token is being sent in Authorization header
- **Check**: Okta domain and audience match
- **Solution**: Verify Okta configuration matches between client and server

---

## Summary

✅ **Okta Setup Complete When**:
1. Okta application created
2. Backend configured with Okta middleware
3. Environment variables set in AWS
4. Client already configured (Angular)
5. API endpoints protected with `[Authorize]`

**Next Steps**: Implement API endpoints that use Okta authentication for the "User" persona.













