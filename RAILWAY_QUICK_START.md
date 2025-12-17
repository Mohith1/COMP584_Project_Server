# Railway Deployment - Quick Start

## 🚀 5-Minute Quick Start

### Step 1: Create Railway Project (2 minutes)

1. Go to [railway.app](https://railway.app) and sign up
2. Click **"New Project"** → **"Deploy from GitHub repo"**
3. Select your repository: `COMP584_Project_Server`
4. Click **"Deploy Now"**

Railway will start building (it will fail until we add environment variables - that's normal!)

---

### Step 2: Add Environment Variables (3 minutes)

Go to your service → **Variables** tab → Add these:

#### Database (Supabase)
```
ConnectionStrings__PostgreSQL=Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-password;SSL Mode=Require;
```

#### JWT (Owner Auth)
```
Jwt__Issuer=FleetManagement.Api
Jwt__Audience=FleetManagement.Client
Jwt__SigningKey=generate-a-secure-32-character-key-here
Jwt__AccessTokenMinutes=30
Jwt__RefreshTokenDays=14
```

#### Okta (User Auth)
```
Okta__Domain=https://dev-123456.okta.com
Okta__AuthorizationServerId=default
Okta__Audience=api://default
Okta__ClientId=your-client-id
Okta__ApiToken=your-api-token
```

#### CORS
```
Cors__AllowedOrigins__0=https://your-app.vercel.app
ASPNETCORE_ENVIRONMENT=Production
```

---

### Step 3: Update Program.cs

Add database configuration to `FleetManagement.Api/Program.cs`:

```csharp
// Add after builder creation
var postgresConnection = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException("PostgreSQL connection string not found");

builder.Services.AddDbContext<FleetDbContext>(options =>
    options.UseNpgsql(postgresConnection));
```

**Commit and push** - Railway will auto-deploy!

---

### Step 4: Test Deployment

1. **Get your Railway URL**: `https://your-app.up.railway.app`
2. **Test health**: `https://your-app.up.railway.app/health`
3. **Test Swagger**: `https://your-app.up.railway.app/swagger`

---

## ✅ Done!

Your API is now live on Railway! 🎉

**Next**: Update your Angular client with the Railway URL.

---

## 📚 Full Guide

See `RAILWAY_DEPLOYMENT_GUIDE.md` for complete instructions.












