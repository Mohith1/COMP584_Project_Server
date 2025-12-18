# Quick Integration Guide: Client + Server on Vercel

## 🎯 Quick Overview

**Architecture:**
- **Frontend (Client)**: Deployed on Vercel
- **Backend (.NET API)**: Deployed on Railway/Render/Azure
- **Connection**: Vercel rewrites proxy `/api/*` to your backend

## 📋 3-Step Integration

### Step 1: Deploy Backend
```bash
# Deploy .NET API to Railway (or Render/Azure)
# Get your API URL: https://your-api.railway.app
```

### Step 2: Configure Frontend

**In your frontend code, use `/api` for API calls:**

```javascript
// Example: React/Next.js
const API_URL = '/api';  // Vercel will proxy this

fetch(`${API_URL}/weatherforecast`)
  .then(res => res.json())
  .then(data => console.log(data));
```

### Step 3: Update vercel.json

**For Next.js:**
```json
{
  "version": 2,
  "framework": "nextjs",
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://your-api.railway.app/api/:path*"
    }
  ]
}
```

**For React (Vite/CRA):**
```json
{
  "version": 2,
  "buildCommand": "npm run build",
  "outputDirectory": "dist",
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://your-api.railway.app/api/:path*"
    }
  ]
}
```

**For Angular:**
```json
{
  "version": 2,
  "buildCommand": "npm run build",
  "outputDirectory": "dist/your-app-name",
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://your-api.railway.app/api/:path*"
    }
  ]
}
```

## 🔧 Environment Variables

In Vercel Dashboard → Settings → Environment Variables:

```
NEXT_PUBLIC_API_URL=/api
```

Or for direct connection (not recommended):
```
NEXT_PUBLIC_API_URL=https://your-api.railway.app
```

## ✅ Testing

1. **Frontend loads**: `https://your-app.vercel.app`
2. **API proxy works**: `https://your-app.vercel.app/api/weatherforecast`
3. **Backend direct**: `https://your-api.railway.app/weatherforecast`

All three should return the same data!

## 🚨 Common Issues

### CORS Errors
✅ Already fixed! CORS is configured in `Program.cs` to allow `*.vercel.app`

### 404 on API Calls
- Check `vercel.json` rewrites are correct
- Verify API URL in destination
- Test backend directly first

### Environment Variables Not Working
- Use `NEXT_PUBLIC_*` for Next.js
- Use `VITE_*` for Vite
- Restart deployment after adding vars

## 📚 Full Documentation

See `INTEGRATE_CLIENT_SERVER.md` for:
- Detailed setup instructions
- Framework-specific examples
- Complete code samples
- Troubleshooting guide

## 🎉 That's It!

Your client and server are now integrated on Vercel! 🚀



















