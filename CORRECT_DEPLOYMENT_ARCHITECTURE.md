# Correct Deployment Architecture

## ⚠️ Important: Server Cannot Run on Vercel!

**You should ONLY deploy the CLIENT (frontend) to Vercel.**

**The SERVER (.NET API) must be deployed to a different platform** that supports .NET.

## ✅ Correct Architecture

```
┌─────────────────┐
│   CLIENT        │  ← Deploy THIS to Vercel
│  (Frontend)     │
│  React/Next.js/  │
│  Angular/etc.   │
└────────┬────────┘
         │
         │ API Calls to /api/*
         │
┌────────▼────────┐
│   VERCEL        │  ← Proxy/Gateway (already deployed)
│   (Proxy)       │
└────────┬────────┘
         │
         │ Proxies to backend
         │
┌────────▼────────┐
│   SERVER        │  ← Deploy THIS to Railway/Render/Azure
│  (.NET API)     │     NOT to Vercel!
└─────────────────┘
```

## 🚫 What NOT to Do

❌ **DO NOT deploy the .NET server to Vercel**
- Vercel doesn't support .NET runtime
- It will fail or not work properly
- The server needs to run on a .NET-compatible platform

## ✅ What TO Do

### Step 1: Deploy Server Separately

Deploy your .NET API to one of these platforms:

1. **Railway** (Recommended - Easiest)
   - Go to https://railway.app
   - Create new project
   - Connect your repository
   - Railway auto-detects Dockerfile
   - Get your API URL: `https://your-api.railway.app`

2. **Render**
   - Go to https://render.com
   - Create new Web Service
   - Use Docker deployment
   - Get your API URL: `https://your-api.onrender.com`

3. **Azure App Service**
   - Deploy via Azure Portal
   - Get your API URL: `https://your-api.azurewebsites.net`

4. **AWS Elastic Beanstalk**
   - Deploy via AWS Toolkit for Visual Studio or AWS CLI
   - Get your API URL: `https://your-env.REGION.elasticbeanstalk.com`
   - See [AWS_DEPLOYMENT.md](./AWS_DEPLOYMENT.md) for details

### Step 2: Deploy Only Client to Vercel

1. **If client is in same repo:**
   - Make sure `vercel.json` is configured for your frontend framework
   - Vercel will only build/deploy the frontend
   - The rewrites in `vercel.json` proxy API calls to your backend

2. **If client is in separate repo:**
   - Create a new Vercel project
   - Connect the frontend repository
   - Configure build settings for your framework
   - Add rewrites to proxy to backend

### Step 3: Configure vercel.json

Your `vercel.json` should look like this:

```json
{
  "version": 2,
  "framework": "nextjs",  // or "react", "angular", etc.
  "buildCommand": "npm run build",
  "outputDirectory": ".next",  // adjust for your framework
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://your-api.railway.app/api/:path*"
    }
  ]
}
```

**Key Points:**
- `framework` and `buildCommand` are for the **frontend only**
- `rewrites` proxy API calls to your **backend on Railway/Render/Azure**
- The backend URL should point to your Railway/Render/Azure deployment

## 📋 Deployment Checklist

### Backend (.NET API)
- [ ] Deployed to Railway/Render/Azure ✅
- [ ] API URL obtained (e.g., `https://api.railway.app`)
- [ ] API tested and working
- [ ] CORS configured (already done in `Program.cs`)
- [ ] Environment variables set

### Frontend (Client)
- [ ] Deployed to Vercel ✅
- [ ] `vercel.json` configured with framework settings
- [ ] Rewrites configured to point to backend
- [ ] Environment variables set in Vercel
- [ ] Frontend code uses `/api` for API calls

## 🔍 How to Verify Your Setup

### Check 1: Backend is on Railway/Render/Azure
```bash
# Test backend directly
curl https://your-api.railway.app/weatherforecast
# Should return JSON data
```

### Check 2: Frontend is on Vercel
```bash
# Visit your Vercel URL
https://your-app.vercel.app
# Should show your frontend
```

### Check 3: Proxy Works
```bash
# Test API through Vercel proxy
curl https://your-app.vercel.app/api/weatherforecast
# Should return same JSON as direct backend
```

## 🛠️ If You Already Deployed Server to Vercel

If you mistakenly tried to deploy the server to Vercel:

1. **Remove server deployment from Vercel**
   - Delete the project or remove server files
   - Keep only frontend files

2. **Deploy server to Railway/Render/Azure**
   - Follow Step 1 above
   - Get your backend URL

3. **Update vercel.json**
   - Update `destination` in rewrites to point to your new backend URL

4. **Redeploy frontend to Vercel**
   - Only frontend should be deployed
   - Vercel will proxy API calls to your backend

## 📁 Recommended Project Structure

### Option A: Monorepo (Both in Same Repo)
```
your-project/
├── client/              # Frontend code
│   ├── src/
│   ├── package.json
│   └── vercel.json     # Configured for frontend
├── FleetManagement.Api/ # Backend code
│   ├── Program.cs
│   └── Dockerfile     # For Railway/Render
└── vercel.json         # Root vercel.json for frontend
```

**Deploy:**
- Frontend: Vercel (uses `client/` directory or root)
- Backend: Railway/Render (uses `FleetManagement.Api/` with Dockerfile)

### Option B: Separate Repos (Recommended)
```
repo-client/            # Frontend repository
├── src/
├── package.json
└── vercel.json        # Deploy to Vercel

repo-server/           # Backend repository  
├── FleetManagement.Api/
├── Dockerfile
└── railway.json       # Deploy to Railway
```

**Deploy:**
- Frontend repo → Vercel
- Backend repo → Railway/Render/Azure

## 🎯 Summary

| Component | Platform | Why |
|-----------|----------|-----|
| **Client (Frontend)** | ✅ Vercel | Vercel excels at frontend hosting |
| **Server (Backend)** | ✅ Railway/Render/Azure | These support .NET runtime |
| **Proxy** | ✅ Vercel | Vercel rewrites proxy API calls |

## ❓ Common Questions

### Q: Can I deploy the server to Vercel?
**A:** No, Vercel doesn't support .NET. Deploy to Railway/Render/Azure.

### Q: What if I have both in the same repo?
**A:** Configure Vercel to only build/deploy the frontend. Use `.vercelignore` to exclude backend files.

### Q: How does the frontend connect to backend?
**A:** Frontend calls `/api/*`, Vercel rewrites proxy to your backend URL.

### Q: Do I need two Vercel projects?
**A:** No, one Vercel project for frontend. Backend goes to Railway/Render/Azure.

## ✅ Correct Setup Example

**Backend (Railway):**
- URL: `https://fleetmanagement-api.railway.app`
- Deployed via Railway dashboard
- Running .NET 8.0

**Frontend (Vercel):**
- URL: `https://fleetmanagement-app.vercel.app`
- Deployed via Vercel
- Framework: Next.js/React/etc.

**vercel.json:**
```json
{
  "version": 2,
  "framework": "nextjs",
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://fleetmanagement-api.railway.app/api/:path*"
    }
  ]
}
```

**Result:**
- Frontend at: `https://fleetmanagement-app.vercel.app`
- API calls: `https://fleetmanagement-app.vercel.app/api/weatherforecast`
- Vercel proxies to: `https://fleetmanagement-api.railway.app/api/weatherforecast`

---

**Remember: Client on Vercel ✅ | Server on Railway/Render/Azure ✅**

