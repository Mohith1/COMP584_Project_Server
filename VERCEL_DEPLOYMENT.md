# Vercel Deployment Guide for .NET API

## ⚠️ Important Limitation

**Vercel does NOT natively support .NET runtime.** You cannot directly deploy a .NET application to Vercel. However, there are **two viable workarounds**:

---

## Option 1: Proxy to External .NET API (Recommended) ✅

This approach deploys your .NET API to a .NET-compatible platform (Railway, Render, Azure, etc.) and uses Vercel as a proxy/gateway.

### Architecture
```
Client → Vercel (Proxy) → .NET API (Railway/Render/Azure)
```

### Steps

#### Step 1: Deploy .NET API to a .NET-Compatible Platform

Choose one:
- **Railway** (easiest): https://railway.app
- **Render**: https://render.com  
- **Azure App Service**: https://azure.microsoft.com
- **AWS Elastic Beanstalk**: https://aws.amazon.com/elasticbeanstalk/
- **Fly.io**: https://fly.io

Follow the deployment instructions in `DEPLOYMENT.md`.

#### Step 2: Get Your API URL

After deployment, you'll get a URL like:
- `https://your-api.railway.app`
- `https://your-api.onrender.com`
- `https://your-api.azurewebsites.net`
- `https://your-env.REGION.elasticbeanstalk.com` (AWS)

#### Step 3: Configure Vercel Proxy

1. Copy `vercel.proxy.example.json` to `vercel.json`
2. Replace `YOUR-DOTNET-API-URL` with your actual API URL:

```json
{
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://your-api.railway.app/api/:path*"
    }
  ]
}
```

#### Step 4: Deploy to Vercel

```bash
# Install Vercel CLI
npm i -g vercel

# Deploy
vercel

# Or connect your GitHub repo to Vercel dashboard
```

#### Step 5: Set Environment Variables in Vercel

In Vercel dashboard, set:
- `NEXT_PUBLIC_API_URL=https://your-api.railway.app` (if using frontend)

### Benefits
- ✅ Use Vercel's CDN and edge network
- ✅ Keep .NET API on a compatible platform
- ✅ Single domain for frontend + API
- ✅ Automatic HTTPS and SSL

### Example Flow
```
GET https://your-app.vercel.app/api/weatherforecast
  ↓ (Vercel rewrites)
GET https://your-api.railway.app/api/weatherforecast
  ↓ (.NET API responds)
Response returned to client
```

---

## Option 2: Next.js API Routes Wrapper

Create a Next.js application that wraps your .NET API calls using Next.js API routes.

### Steps

#### Step 1: Create Next.js Project

```bash
npx create-next-app@latest vercel-wrapper
cd vercel-wrapper
```

#### Step 2: Create API Route Wrapper

Create `pages/api/weatherforecast.js` (or `app/api/weatherforecast/route.js` for App Router):

```javascript
// pages/api/weatherforecast.js
export default async function handler(req, res) {
  const apiUrl = process.env.DOTNET_API_URL || 'https://your-api.railway.app';
  
  try {
    const response = await fetch(`${apiUrl}/weatherforecast`, {
      method: req.method,
      headers: {
        'Content-Type': 'application/json',
        ...req.headers.authorization && { 'Authorization': req.headers.authorization }
      },
      body: req.method !== 'GET' ? JSON.stringify(req.body) : undefined
    });
    
    const data = await response.json();
    res.status(response.status).json(data);
  } catch (error) {
    res.status(500).json({ error: 'Failed to fetch from .NET API' });
  }
}
```

#### Step 3: Configure Environment Variables

Create `.env.local`:
```
DOTNET_API_URL=https://your-api.railway.app
```

#### Step 4: Deploy to Vercel

```bash
vercel
```

### Benefits
- ✅ Full Vercel integration
- ✅ Can add caching, rate limiting, etc.
- ✅ Type-safe with TypeScript

### Drawbacks
- ❌ Requires maintaining Next.js wrapper
- ❌ Extra hop adds latency

---

## Option 3: Custom Runtime (Advanced - Not Recommended)

Vercel supports custom runtimes, but creating a .NET runtime is:
- Complex and undocumented
- Requires significant expertise
- Not officially supported
- May break with Vercel updates

**Not recommended for production use.**

---

## Recommended Setup

### For Production:

1. **Deploy .NET API to Railway** (easiest, free tier available)
2. **Use Vercel Proxy** (Option 1) to route requests
3. **Deploy frontend to Vercel** (if you have one)

### Configuration Files

- `vercel.json` - Main Vercel configuration (proxy setup)
- `vercel.proxy.example.json` - Example proxy configuration
- `Dockerfile` - For deploying .NET API to Railway/Render/etc.

---

## Testing the Proxy Setup

After deploying:

1. **Test direct API:**
   ```bash
   curl https://your-api.railway.app/weatherforecast
   ```

2. **Test through Vercel proxy:**
   ```bash
   curl https://your-app.vercel.app/api/weatherforecast
   ```

Both should return the same response.

---

## Troubleshooting

### CORS Issues
If you see CORS errors, ensure your .NET API has CORS configured:

```csharp
// In Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercel", policy =>
    {
        policy.WithOrigins("https://your-app.vercel.app")
             .AllowAnyMethod()
             .AllowAnyHeader();
    });
});

app.UseCors("AllowVercel");
```

### 404 Errors
- Check that your API routes match the rewrite patterns
- Verify your .NET API URL is correct
- Ensure your .NET API is running and accessible

### Timeout Issues
- Vercel has a 10-second timeout for serverless functions
- For longer operations, consider using Vercel's Edge Functions or increasing timeout in plan settings

---

## Cost Considerations

- **Vercel**: Free tier available, then pay-as-you-go
- **Railway**: Free tier available, then $5/month
- **Render**: Free tier available, then $7/month

Total: Can run both for free on hobby tier, or ~$12-20/month for production.

---

## Next Steps

1. ✅ Deploy .NET API to Railway/Render (see `DEPLOYMENT.md`)
2. ✅ Configure `vercel.json` with your API URL
3. ✅ Deploy to Vercel
4. ✅ Test endpoints
5. ✅ Set up environment variables
6. ✅ Configure CORS if needed

For questions or issues, refer to:
- Vercel Docs: https://vercel.com/docs
- Railway Docs: https://docs.railway.app
- Render Docs: https://render.com/docs

