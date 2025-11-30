# COMP584_Project_Server

A .NET 8.0 ASP.NET Core Web API for Fleet Management.

## Deployment

### ⚠️ Important: Vercel Limitation

**Vercel does not natively support .NET applications.** Vercel is optimized for serverless functions in Node.js, Python, Go, and Ruby, and does not provide runtime support for .NET Core/ASP.NET Core applications.

**However, you CAN use Vercel as a proxy to your .NET API!** See [VERCEL_DEPLOYMENT.md](./VERCEL_DEPLOYMENT.md) for detailed instructions on setting up a hybrid deployment (Vercel proxy → .NET API on Railway/Render/Azure).

**Quick Setup**: See [SETUP_VERCEL.md](./SETUP_VERCEL.md) for a step-by-step guide.

### Recommended Deployment Platforms for .NET

This application can be deployed to the following platforms that support .NET:

#### 1. **Railway** (Recommended - Easiest)
Railway provides excellent .NET support with minimal configuration.

**Steps:**
1. Sign up at [railway.app](https://railway.app)
2. Create a new project
3. Connect your GitHub repository
4. Railway will automatically detect the Dockerfile and deploy
5. Set environment variables in the Railway dashboard

**Environment Variables to Set:**
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__SqlServer` (your production database connection string)
- Any other configuration from `appsettings.json`

#### 2. **Render**
Render supports .NET applications via Docker.

**Steps:**
1. Sign up at [render.com](https://render.com)
2. Create a new Web Service
3. Connect your GitHub repository
4. Select "Docker" as the runtime
5. Render will use the provided `render.yaml` configuration
6. Set environment variables in the Render dashboard

#### 3. **Azure App Service** (Microsoft's Platform)
Best for enterprise deployments with full .NET ecosystem support.

**Steps:**
1. Create an Azure App Service in the Azure Portal
2. Select .NET 8.0 as the runtime stack
3. Deploy via:
   - Visual Studio Publish
   - GitHub Actions CI/CD
   - Azure CLI
   - Git deployment

#### 4. **Fly.io**
Good for global distribution with edge computing.

**Steps:**
1. Install Fly CLI: `iwr https://fly.io/install.ps1 -useb | iex`
2. Run `fly launch` in the project root
3. Follow the prompts to configure and deploy

### Local Development

```bash
# Navigate to the API project
cd FleetManagement.Api

# Restore dependencies
dotnet restore

# Run the application
dotnet run

# Or use the executable
cd bin/Debug/net8.0
.\FleetManagement.Api.exe
```

The API will be available at:
- HTTP: `http://localhost:5224`
- HTTPS: `https://localhost:7211`
- Swagger UI: `http://localhost:5224/swagger`

### Docker Deployment

The project includes a `Dockerfile` for containerized deployment:

```bash
# Build the Docker image
docker build -t fleetmanagement-api .

# Run the container
docker run -p 8080:8080 fleetmanagement-api
```

### Environment Configuration

Make sure to configure the following in your production environment:

- Database connection strings
- JWT signing keys (use secure, randomly generated keys)
- Okta configuration (if using Okta authentication)
- CORS allowed origins
- Admin user credentials

### Project Structure

```
FleetManagement.Api/          # Main API project
FleetManagement.Services/      # Business logic layer
FleetManagement.Data/          # Data access layer
```

### Technologies

- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core
- Swagger/OpenAPI
- Serilog for logging
- Okta for authentication (optional)
