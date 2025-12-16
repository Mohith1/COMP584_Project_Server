# Quick Setup: Vercel Proxy for .NET API

## Prerequisites
- .NET API deployed to Railway/Render/Azure (get the URL)
- Vercel account (free tier works)

## Step-by-Step Setup

### 1. Deploy .NET API First

Deploy your .NET API to one of these platforms:
- **Railway**: https://railway.app (easiest)
- **Render**: https://render.com
- **Azure**: https://azure.microsoft.com

**Get your API URL**, e.g.:
- `https://fleetmanagement-api.railway.app`
- `https://fleetmanagement-api.onrender.com`

### 2. Update vercel.json

Edit `vercel.json` and replace `YOUR-DOTNET-API-URL` with your actual API URL:

```json
{
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://fleetmanagement-api.railway.app/api/:path*"
    }
  ]
}
```

**Important**: Remove `.railway.app` from the destination if your URL is different!

### 3. Deploy to Vercel

#### Option A: Via Vercel CLI
```bash
# Install Vercel CLI
npm i -g vercel

# Login
vercel login

# Deploy
vercel

# Follow prompts
```

#### Option B: Via GitHub (Recommended)
1. Push your code to GitHub
2. Go to https://vercel.com
3. Click "New Project"
4. Import your GitHub repository
5. Vercel will auto-detect and deploy

### 4. Test Your Setup

After deployment, test the proxy:

```bash
# Your Vercel URL (e.g., https://your-app.vercel.app)
curl https://your-app.vercel.app/api/weatherforecast

# Should return the same as:
curl https://your-api.railway.app/weatherforecast
```

### 5. Configure CORS (If Needed)

If you see CORS errors, your .NET API already has CORS configured in `Program.cs` to allow `*.vercel.app` domains.

To add a specific domain, update `Program.cs`:

```csharp
policy.WithOrigins(
    "https://your-specific-app.vercel.app",
    "http://localhost:3000"
)
```

## Troubleshooting

### ❌ 404 Not Found
- Check that your API URL in `vercel.json` is correct
- Verify your .NET API is running and accessible
- Test the API URL directly in browser

### ❌ CORS Errors
- Ensure CORS is configured in `Program.cs` (already done)
- Check that your Vercel domain is allowed
- Verify headers in `vercel.json`

### ❌ Timeout Errors
- Vercel has 10s timeout on free tier
- Consider upgrading or optimizing API response times

### ❌ 502 Bad Gateway
- Your .NET API might be down
- Check Railway/Render/Azure dashboard
- Verify API is accessible directly

## Example vercel.json

```json
{
  "version": 2,
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://your-api.railway.app/api/:path*"
    },
    {
      "source": "/swagger/:path*",
      "destination": "https://your-api.railway.app/swagger/:path*"
    },
    {
      "source": "/weatherforecast",
      "destination": "https://your-api.railway.app/weatherforecast"
    }
  ]
}
```

## Next Steps

- ✅ API deployed to Railway/Render
- ✅ vercel.json configured
- ✅ Deployed to Vercel
- ✅ Tested endpoints
- 🎉 Done!

For detailed information, see `VERCEL_DEPLOYMENT.md`.
















