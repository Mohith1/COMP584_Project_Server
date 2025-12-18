# Integrating Client and Server on Vercel

## Architecture Overview

```
┌─────────────┐
│   Client    │  (Frontend - Deployed on Vercel)
│  (React/    │
│  Next.js/   │
│  Angular)   │
└──────┬──────┘
       │
       │ API Calls
       │
┌──────▼──────┐         ┌──────────────┐
│   Vercel    │ ──────> │  .NET API    │
│  (Proxy)    │         │  (Railway/   │
│             │ <────── │   Render/    │
└─────────────┘         │   Azure)     │
                        └──────────────┘
```

## Step-by-Step Integration

### Step 1: Deploy .NET API Backend

1. **Deploy to Railway/Render/Azure** (see `DEPLOYMENT.md`)
2. **Get your API URL**, e.g.:
   - `https://fleetmanagement-api.railway.app`
   - `https://fleetmanagement-api.onrender.com`

### Step 2: Prepare Your Frontend

Your frontend can be:
- **Next.js** (recommended for Vercel)
- **React** (Create React App, Vite)
- **Angular**
- **Vue.js**
- **Svelte**

### Step 3: Configure Frontend to Use API

#### Option A: Use Vercel Proxy (Recommended)

Configure your frontend to call `/api/*` which Vercel will proxy to your backend.

**Example API Configuration:**

```javascript
// config/api.js or utils/api.js
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || '/api';

export const apiClient = {
  baseURL: API_BASE_URL,
  
  async get(endpoint) {
    const response = await fetch(`${API_BASE_URL}${endpoint}`);
    return response.json();
  },
  
  async post(endpoint, data) {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    return response.json();
  }
};
```

#### Option B: Direct API URL (Alternative)

Point directly to your backend URL:

```javascript
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 
  'https://fleetmanagement-api.railway.app';
```

### Step 4: Update vercel.json for Frontend + Proxy

Update your `vercel.json` to support both frontend and API proxy:

```json
{
  "version": 2,
  "buildCommand": "",
  "outputDirectory": ".next",
  "installCommand": "npm install",
  "framework": "nextjs",
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://YOUR-DOTNET-API-URL.railway.app/api/:path*"
    },
    {
      "source": "/swagger/:path*",
      "destination": "https://YOUR-DOTNET-API-URL.railway.app/swagger/:path*"
    },
    {
      "source": "/weatherforecast",
      "destination": "https://YOUR-DOTNET-API-URL.railway.app/weatherforecast"
    }
  ],
  "headers": [
    {
      "source": "/api/(.*)",
      "headers": [
        {
          "key": "Access-Control-Allow-Origin",
          "value": "*"
        },
        {
          "key": "Access-Control-Allow-Methods",
          "value": "GET, POST, PUT, DELETE, OPTIONS, PATCH"
        },
        {
          "key": "Access-Control-Allow-Headers",
          "value": "Content-Type, Authorization, X-Requested-With"
        }
      ]
    }
  ]
}
```

**Note**: Adjust `framework`, `outputDirectory`, and `buildCommand` based on your frontend framework.

### Step 5: Set Environment Variables in Vercel

In Vercel Dashboard → Your Project → Settings → Environment Variables:

```
NEXT_PUBLIC_API_URL=/api
# OR for direct connection:
# NEXT_PUBLIC_API_URL=https://fleetmanagement-api.railway.app
```

### Step 6: Deploy Frontend to Vercel

#### If Frontend is in Same Repo:

1. Push frontend code to your repository
2. Vercel will auto-detect the framework
3. Configure build settings if needed
4. Deploy!

#### If Frontend is in Separate Repo:

1. Create a new Vercel project
2. Connect the frontend repository
3. Configure build settings
4. Add environment variables
5. Deploy!

## Framework-Specific Examples

### Next.js (Recommended)

**Project Structure:**
```
your-project/
├── app/              # Next.js App Router
│   └── page.tsx
├── components/
├── lib/
│   └── api.ts        # API client
├── vercel.json
└── package.json
```

**lib/api.ts:**
```typescript
const API_URL = process.env.NEXT_PUBLIC_API_URL || '/api';

export async function getWeatherForecast() {
  const res = await fetch(`${API_URL}/weatherforecast`);
  return res.json();
}
```

**app/page.tsx:**
```typescript
import { getWeatherForecast } from '@/lib/api';

export default async function Home() {
  const data = await getWeatherForecast();
  return (
    <div>
      <h1>Weather Forecast</h1>
      <pre>{JSON.stringify(data, null, 2)}</pre>
    </div>
  );
}
```

**vercel.json:**
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

### React (Vite/CRA)

**src/config/api.js:**
```javascript
const API_URL = import.meta.env.VITE_API_URL || '/api';

export const fetchWeather = async () => {
  const response = await fetch(`${API_URL}/weatherforecast`);
  return response.json();
};
```

**vercel.json:**
```json
{
  "version": 2,
  "buildCommand": "npm run build",
  "outputDirectory": "dist",
  "framework": null,
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://your-api.railway.app/api/:path*"
    }
  ]
}
```

### Angular

**src/environments/environment.ts:**
```typescript
export const environment = {
  production: true,
  apiUrl: '/api'
};
```

**src/app/services/api.service.ts:**
```typescript
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private http: HttpClient) {}
  
  getWeatherForecast() {
    return this.http.get(`${environment.apiUrl}/weatherforecast`);
  }
}
```

**vercel.json:**
```json
{
  "version": 2,
  "buildCommand": "npm run build",
  "outputDirectory": "dist/your-app-name",
  "framework": null,
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://your-api.railway.app/api/:path*"
    }
  ]
}
```

## Complete Integration Checklist

### Backend (.NET API)
- [ ] Deployed to Railway/Render/Azure
- [ ] API URL obtained
- [ ] CORS configured (already done in `Program.cs`)
- [ ] Environment variables set
- [ ] API tested and working

### Frontend
- [ ] Frontend code ready
- [ ] API client configured to use `/api` or direct URL
- [ ] Environment variables configured
- [ ] `vercel.json` updated with rewrites
- [ ] Build command configured

### Vercel Configuration
- [ ] Framework preset set correctly
- [ ] Build settings configured
- [ ] Environment variables added
- [ ] Rewrites configured in `vercel.json`
- [ ] Deployed successfully

### Testing
- [ ] Frontend loads correctly
- [ ] API calls work through proxy
- [ ] CORS errors resolved
- [ ] Authentication works (if applicable)
- [ ] All endpoints accessible

## Common Issues & Solutions

### Issue: CORS Errors

**Solution**: CORS is already configured in `Program.cs` to allow `*.vercel.app` domains. If you still see errors:

1. Check that CORS middleware is before other middleware
2. Verify your Vercel domain is in allowed origins
3. Check browser console for specific error

### Issue: 404 on API Calls

**Solution**:
1. Verify `vercel.json` rewrites are correct
2. Check that API URL in rewrite destination is correct
3. Test API directly (bypass Vercel proxy)
4. Check Vercel deployment logs

### Issue: Environment Variables Not Working

**Solution**:
1. Use `NEXT_PUBLIC_*` prefix for Next.js (exposed to browser)
2. Use `VITE_*` prefix for Vite
3. Restart Vercel deployment after adding env vars
4. Check variable names match in code

### Issue: Build Fails

**Solution**:
1. Check build command in `vercel.json`
2. Verify `package.json` has correct scripts
3. Check Node.js version compatibility
4. Review build logs in Vercel dashboard

## Example: Complete Next.js Integration

### 1. Create Next.js App (if needed)

```bash
npx create-next-app@latest fleet-management-client
cd fleet-management-client
```

### 2. Create API Client

**lib/api.ts:**
```typescript
const API_URL = process.env.NEXT_PUBLIC_API_URL || '/api';

export class ApiClient {
  private baseUrl: string;

  constructor() {
    this.baseUrl = API_URL;
  }

  async get<T>(endpoint: string): Promise<T> {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      headers: {
        'Content-Type': 'application/json',
      },
    });
    
    if (!response.ok) {
      throw new Error(`API Error: ${response.statusText}`);
    }
    
    return response.json();
  }

  async post<T>(endpoint: string, data: any): Promise<T> {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    });
    
    if (!response.ok) {
      throw new Error(`API Error: ${response.statusText}`);
    }
    
    return response.json();
  }
}

export const apiClient = new ApiClient();
```

### 3. Use in Components

**app/weather/page.tsx:**
```typescript
import { apiClient } from '@/lib/api';

export default async function WeatherPage() {
  const forecast = await apiClient.get('/weatherforecast');
  
  return (
    <div>
      <h1>Weather Forecast</h1>
      {forecast.map((item: any) => (
        <div key={item.date}>
          <p>{item.date}: {item.temperatureC}°C</p>
        </div>
      ))}
    </div>
  );
}
```

### 4. Configure vercel.json

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

### 5. Deploy

```bash
vercel
```

## Summary

✅ **Backend**: Deploy .NET API to Railway/Render/Azure  
✅ **Frontend**: Deploy to Vercel with your framework  
✅ **Integration**: Use Vercel rewrites to proxy `/api/*` to backend  
✅ **Configuration**: Set environment variables in Vercel  
✅ **Testing**: Verify both frontend and API work together  

Your application is now fully integrated! 🎉



















