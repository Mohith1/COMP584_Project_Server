# Deployment Guide

## ⚠️ Vercel Deployment Limitation

**Vercel does not support .NET applications.** Vercel's platform is designed for:
- Node.js/TypeScript
- Python
- Go
- Ruby
- Static sites

ASP.NET Core applications require a .NET runtime, which Vercel does not provide.

## Recommended Alternatives

### Option 1: Railway (Easiest) ⭐ Recommended

**Why Railway:**
- Automatic Docker detection
- Free tier available
- Simple setup
- Great .NET support

**Steps:**
1. Push your code to GitHub
2. Go to [railway.app](https://railway.app) and sign up
3. Click "New Project" → "Deploy from GitHub repo"
4. Select your repository
5. Railway will automatically detect the `Dockerfile` and deploy
6. Add environment variables in the Railway dashboard:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `ConnectionStrings__SqlServer=<your-db-connection>`
   - Any other settings from `appsettings.json`

**Cost:** Free tier available, then pay-as-you-go

---

### Option 2: Render

**Why Render:**
- Good free tier
- Simple configuration
- Docker support

**Steps:**
1. Push your code to GitHub
2. Go to [render.com](https://render.com) and sign up
3. Click "New" → "Web Service"
4. Connect your GitHub repository
5. Render will use the `render.yaml` configuration file
6. Set environment variables in the Render dashboard

**Cost:** Free tier available, then $7/month for basic web service

---

### Option 3: Azure App Service

**Why Azure:**
- Native .NET support
- Enterprise-grade
- Integrated with Microsoft ecosystem
- Free tier available

**Steps:**
1. Create an Azure account at [azure.microsoft.com](https://azure.microsoft.com)
2. Create a new App Service:
   - Runtime stack: .NET 8
   - Operating System: Linux or Windows
3. Deploy via:
   - **Visual Studio:** Right-click project → Publish → Azure App Service
   - **GitHub Actions:** Set up CI/CD pipeline
   - **Azure CLI:** `az webapp up --name <app-name> --runtime "DOTNET|8.0"`

**Cost:** Free tier available (F1), then pay-as-you-go

---

### Option 4: Fly.io

**Why Fly.io:**
- Global edge deployment
- Good for low latency
- Docker-based

**Steps:**
1. Install Fly CLI: `iwr https://fly.io/install.ps1 -useb | iex`
2. Run `fly launch` in project root
3. Follow prompts to configure
4. Deploy with `fly deploy`

**Cost:** Free tier available, then pay-as-you-go

---

## Docker Deployment (Any Platform)

All platforms above support Docker. The project includes a `Dockerfile` that you can use:

```bash
# Build
docker build -t fleetmanagement-api .

# Run locally
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Production fleetmanagement-api
```

## Environment Variables

Set these in your deployment platform:

### Required
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__SqlServer=<your-production-database-connection>`

### Optional (if using)
- `Jwt__SigningKey=<secure-random-key>`
- `Okta__Domain=<your-okta-domain>`
- `Okta__ApiToken=<your-okta-token>`
- `Cors__AllowedOrigins=<comma-separated-origins>`

## Database Setup

You'll need to set up a production database. Options:
- **Azure SQL Database** (if using Azure)
- **PostgreSQL** (Railway, Render, Fly.io all support)
- **SQL Server** (Azure, AWS RDS)
- **SQLite** (not recommended for production)

Update your connection string accordingly.

## Post-Deployment Checklist

- [ ] Database migrations applied
- [ ] Environment variables configured
- [ ] CORS origins updated for production domain
- [ ] JWT signing key changed from default
- [ ] HTTPS/SSL certificate configured (handled by platform)
- [ ] Health check endpoint working (`/swagger`)
- [ ] API endpoints tested

## Monitoring

Consider adding:
- Application Insights (Azure)
- Log aggregation (Serilog → Seq, DataDog, etc.)
- Health checks endpoint
- Error tracking (Sentry, etc.)

