# Client-Server Integration Guide
## Keeping Angular Client and .NET Server in Sync

**Last Updated:** 2024  
**API Base URL:** `https://fleetmanagement-api-production.up.railway.app`  
**API Version:** v1.0.0

---

## 📋 Table of Contents

1. [Authentication](#authentication)
2. [API Endpoints](#api-endpoints)
3. [Data Models (DTOs)](#data-models-dtos)
4. [SignalR Real-Time Updates](#signalr-real-time-updates)
5. [Error Handling](#error-handling)
6. [Testing Checklist](#testing-checklist)
7. [Common Issues & Solutions](#common-issues--solutions)

---

## 🔐 Authentication

### Base Configuration

**API Base URL:**
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://fleetmanagement-api-production.up.railway.app',
  // ... other config
};
```

### Login Flow

**Endpoint:** `POST /api/auth/login`

**Request:**
```typescript
interface LoginRequest {
  email: string;
  password: string;
}

// Example
const loginRequest: LoginRequest = {
  email: 'owner@example.com',
  password: 'SecurePassword123!'
};
```

**Response (200 OK):**
```typescript
interface LoginResponse {
  accessToken: string;      // JWT token - use for all API calls
  refreshToken: string;     // Use for token refresh
  expiresIn: number;        // Seconds (3600 = 1 hour)
  tokenType: string;        // "Bearer"
  userId: string;           // GUID
  email: string;
  ownerId: string | null;   // GUID - null if not an owner
}
```

**Error Responses:**
- `400 Bad Request` - Missing email/password
- `401 Unauthorized` - Invalid credentials

**Implementation:**
```typescript
async login(email: string, password: string): Promise<LoginResponse> {
  const response = await fetch(`${this.apiUrl}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });
  
  if (!response.ok) {
    throw new Error('Login failed');
  }
  
  const data: LoginResponse = await response.json();
  
  // Store tokens
  localStorage.setItem('accessToken', data.accessToken);
  localStorage.setItem('refreshToken', data.refreshToken);
  localStorage.setItem('ownerId', data.ownerId || '');
  
  return data;
}
```

### Register Owner Flow

**Endpoint:** `POST /api/auth/register-owner`

**Request:**
```typescript
interface RegisterOwnerRequest {
  email: string;
  password: string;
  companyName: string;
  contactPhone?: string;
  primaryContactName?: string;
  cityId: string;           // GUID - must exist in database
  timeZone?: string;
}
```

**Response:** Same as `LoginResponse`

### Token Refresh

**Endpoint:** `POST /api/auth/refresh`

**Request:**
```typescript
interface RefreshTokenRequest {
  refreshToken: string;
}
```

**Response:** Same as `LoginResponse` (new tokens)

**Implementation:**
```typescript
async refreshToken(): Promise<LoginResponse> {
  const refreshToken = localStorage.getItem('refreshToken');
  if (!refreshToken) throw new Error('No refresh token');
  
  const response = await fetch(`${this.apiUrl}/api/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken })
  });
  
  if (!response.ok) {
    // Refresh failed - redirect to login
    this.router.navigate(['/login']);
    throw new Error('Token refresh failed');
  }
  
  const data: LoginResponse = await response.json();
  localStorage.setItem('accessToken', data.accessToken);
  localStorage.setItem('refreshToken', data.refreshToken);
  
  return data;
}
```

### Get Current User

**Endpoint:** `GET /api/auth/me`

**Headers:** `Authorization: Bearer {accessToken}`

**Response:**
```typescript
interface CurrentUser {
  userId: string;
  email: string;
  ownerId: string | null;
  companyName?: string;
  cityName?: string;
  countryName?: string;
}
```

---

## 📡 API Endpoints

### All endpoints require authentication except:
- `POST /api/auth/login`
- `POST /api/auth/register-owner`
- `GET /api/Cities` (public)
- `GET /api/Countries` (public)

### Headers Required:
```typescript
{
  'Authorization': `Bearer ${accessToken}`,
  'Content-Type': 'application/json'
}
```

---

### Owner Endpoints

#### Get Current Owner
**Endpoint:** `GET /api/Owners/me`  
**Auth:** Required  
**Response:** `OwnerDto` (see Data Models)

#### Update Current Owner
**Endpoint:** `PUT /api/Owners/me`  
**Auth:** Required  
**Request:** `UpdateOwnerDto`  
**Response:** `OwnerDto`

#### Get Owner by ID
**Endpoint:** `GET /api/Owners/{id}`  
**Auth:** Required  
**Response:** `OwnerDto`

---

### Fleet Endpoints

#### List Fleets
**Endpoint:** `GET /api/Fleets?ownerId={ownerId}` (optional)  
**Auth:** Required  
**Response:** `FleetDto[]`

#### Get Fleet
**Endpoint:** `GET /api/Fleets/{id}`  
**Auth:** Required  
**Response:** `FleetDto`

#### Create Fleet
**Endpoint:** `POST /api/Fleets`  
**Auth:** Required  
**Request:** `CreateFleetDto`  
**Response:** `FleetDto`  
**SignalR:** Broadcasts `FleetCreated` event

**Alternative Route:** `POST /api/owners/{ownerId}/fleets`  
(Same functionality, different route pattern)

#### Update Fleet
**Endpoint:** `PUT /api/Fleets/{id}`  
**Auth:** Required  
**Request:** `UpdateFleetDto`  
**Response:** `FleetDto`  
**SignalR:** Broadcasts `FleetUpdated` event

#### Delete Fleet
**Endpoint:** `DELETE /api/Fleets/{id}`  
**Auth:** Required  
**Response:** `204 No Content`  
**SignalR:** Broadcasts `FleetDeleted` event

#### Get Fleet Vehicles
**Endpoint:** `GET /api/Fleets/{fleetId}/vehicles`  
**Auth:** Required  
**Response:** `VehicleDto[]`

#### Create Vehicle in Fleet
**Endpoint:** `POST /api/Fleets/{fleetId}/vehicles`  
**Auth:** Required  
**Request:** `CreateVehicleDto` (FleetId is auto-set from route)  
**Response:** `VehicleDto`  
**SignalR:** Broadcasts `VehicleCreated` event

---

### Vehicle Endpoints

#### List Vehicles
**Endpoint:** `GET /api/Vehicles?fleetId={fleetId}` (optional)  
**Auth:** Required  
**Response:** `VehicleDto[]`

#### Get Vehicle
**Endpoint:** `GET /api/Vehicles/{id}`  
**Auth:** Required  
**Response:** `VehicleDto`

#### Create Vehicle
**Endpoint:** `POST /api/Vehicles`  
**Auth:** Required  
**Request:** `CreateVehicleDto`  
**Response:** `VehicleDto`  
**SignalR:** Broadcasts `VehicleCreated` event

#### Update Vehicle
**Endpoint:** `PUT /api/Vehicles/{id}`  
**Auth:** Required  
**Request:** `UpdateVehicleDto`  
**Response:** `VehicleDto`  
**SignalR:** Broadcasts `VehicleUpdated` event

#### Delete Vehicle
**Endpoint:** `DELETE /api/Vehicles/{id}`  
**Auth:** Required  
**Response:** `204 No Content`  
**SignalR:** Broadcasts `VehicleDeleted` event

#### Get Vehicle Telemetry
**Endpoint:** `GET /api/Vehicles/{id}/telemetry`  
**Auth:** Required  
**Response:** `TelemetryDto[]`

---

### Owner Nested Routes

#### Get Owner's Fleets
**Endpoint:** `GET /api/owners/{ownerId}/fleets`  
**Auth:** Required  
**Response:** `FleetDto[]`

#### Create Owner's Fleet
**Endpoint:** `POST /api/owners/{ownerId}/fleets`  
**Auth:** Required  
**Request:** `CreateFleetDto` (OwnerId is auto-set from route)  
**Response:** `FleetDto`  
**SignalR:** Broadcasts `FleetCreated` event

#### Get Owner's Vehicles
**Endpoint:** `GET /api/owners/{ownerId}/vehicles`  
**Auth:** Required  
**Response:** `VehicleDto[]` (all vehicles across all fleets)

#### Get Owner's Vehicles Telemetry
**Endpoint:** `GET /api/owners/{ownerId}/vehicles/telemetry`  
**Auth:** Required  
**Response:** `TelemetryDto[]` (latest telemetry for all vehicles)

---

### City/Country Endpoints (Public)

#### List Cities
**Endpoint:** `GET /api/Cities?countryId={countryId}` (optional)  
**Auth:** Not required  
**Response:** `CityDto[]`

#### List Countries
**Endpoint:** `GET /api/Countries`  
**Auth:** Not required  
**Response:** `CountryDto[]`

---

## 📦 Data Models (DTOs)

### OwnerDto
```typescript
interface OwnerDto {
  id: string;                    // GUID
  companyName: string;
  contactEmail: string;
  contactPhone?: string;
  primaryContactName?: string;
  cityId: string;                // GUID
  cityName?: string;
  countryName?: string;
  timeZone?: string;
  fleetCount: number;
  createdAtUtc: string;          // ISO 8601
  updatedAtUtc?: string;         // ISO 8601
}
```

### CreateOwnerDto
```typescript
interface CreateOwnerDto {
  companyName: string;
  contactEmail: string;
  contactPhone?: string;
  primaryContactName?: string;
  cityId: string;                // GUID - must exist
  timeZone?: string;
}
```

### UpdateOwnerDto
```typescript
interface UpdateOwnerDto {
  companyName: string;
  contactEmail: string;
  contactPhone?: string;
  primaryContactName?: string;
  cityId: string;                // GUID
  timeZone?: string;
}
```

---

### FleetDto
```typescript
interface FleetDto {
  id: string;                    // GUID
  name: string;
  description?: string;
  ownerId: string;               // GUID
  ownerName?: string;
  vehicleCount: number;
  createdAtUtc: string;         // ISO 8601
  updatedAtUtc?: string;         // ISO 8601
}
```

### CreateFleetDto
```typescript
interface CreateFleetDto {
  name: string;
  description?: string;
  ownerId: string;               // GUID - must exist
}
```

### UpdateFleetDto
```typescript
interface UpdateFleetDto {
  name: string;
  description?: string;
}
```

---

### VehicleDto
```typescript
interface VehicleDto {
  id: string;                    // GUID
  vin: string;                   // Vehicle Identification Number
  plateNumber: string;
  make?: string;
  model?: string;
  modelYear: number;
  status: number;                // VehicleStatus enum (0=Active, 1=Inactive, etc.)
  fleetId: string;              // GUID
  fleetName?: string;
  ownerId?: string;              // GUID
  createdAtUtc: string;         // ISO 8601
  updatedAtUtc?: string;         // ISO 8601
}
```

### CreateVehicleDto
```typescript
interface CreateVehicleDto {
  vin: string;
  plateNumber: string;
  make?: string;
  model?: string;
  modelYear: number;
  status: number;                // VehicleStatus enum
  fleetId: string;               // GUID - must exist
  ownerId?: string;              // GUID (optional)
}
```

### UpdateVehicleDto
```typescript
interface UpdateVehicleDto {
  vin: string;
  plateNumber: string;
  make?: string;
  model?: string;
  modelYear: number;
  status: number;                // VehicleStatus enum
}
```

---

### TelemetryDto
```typescript
interface TelemetryDto {
  id: string;                    // GUID
  vehicleId: string;             // GUID
  vehicleVin?: string;
  latitude: number;               // Decimal
  longitude: number;             // Decimal
  speedKph: number;              // Decimal
  fuelLevelPercentage: number;   // Decimal (0-100)
  capturedAtUtc: string;         // ISO 8601
}
```

---

### CityDto
```typescript
interface CityDto {
  id: string;                    // GUID
  name: string;
  postalCode: string;
  populationMillions: number;    // Decimal
  countryId: string;             // GUID
  countryName?: string;
  createdAtUtc: string;         // ISO 8601
}
```

### CountryDto
```typescript
interface CountryDto {
  id: string;                    // GUID
  name: string;
  isoCode: string;               // e.g., "US", "CA"
  continent?: string;
  createdAtUtc: string;         // ISO 8601
}
```

---

## 🔄 SignalR Real-Time Updates

### Connection Setup

**Install SignalR Client:**
```bash
npm install @microsoft/signalr
```

### Fleet Hub Connection

```typescript
import * as signalR from '@microsoft/signalr';

export class FleetRealtimeService {
  private hubConnection: signalR.HubConnection;
  private apiUrl = 'https://fleetmanagement-api-production.up.railway.app';

  constructor(private authService: AuthService) {
    this.initializeConnection();
  }

  private initializeConnection(): void {
    const token = this.authService.getAccessToken();
    
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiUrl}/hub/fleets?access_token=${token}`, {
        accessTokenFactory: () => this.authService.getAccessToken()
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0s, 2s, 10s, 30s
          if (retryContext.elapsedMilliseconds < 60000) {
            return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
          }
          return null; // Stop retrying after 60 seconds
        }
      })
      .build();
  }

  async start(): Promise<void> {
    try {
      await this.hubConnection.start();
      console.log('Connected to Fleet Hub');
      
      this.setupMessageHandlers();
    } catch (error) {
      console.error('Error connecting to SignalR:', error);
      throw error;
    }
  }

  private setupMessageHandlers(): void {
    // Fleet Created
    this.hubConnection.on('FleetCreated', (fleet: FleetDto) => {
      console.log('Fleet created:', fleet);
      // Emit event or update state
      this.fleetCreatedSubject.next(fleet);
    });

    // Fleet Updated
    this.hubConnection.on('FleetUpdated', (fleet: FleetDto) => {
      console.log('Fleet updated:', fleet);
      this.fleetUpdatedSubject.next(fleet);
    });

    // Fleet Deleted
    this.hubConnection.on('FleetDeleted', (data: { fleetId: string; ownerId: string }) => {
      console.log('Fleet deleted:', data.fleetId);
      this.fleetDeletedSubject.next(data.fleetId);
    });

    // Connection Status
    this.hubConnection.on('Connected', (data: { ownerId: string }) => {
      console.log('Connected to owner group:', data.ownerId);
    });

    // Reconnection events
    this.hubConnection.onreconnecting((error) => {
      console.log('SignalR reconnecting...', error);
    });

    this.hubConnection.onreconnected((connectionId) => {
      console.log('SignalR reconnected:', connectionId);
    });

    this.hubConnection.onclose((error) => {
      console.log('SignalR connection closed:', error);
      // Optionally attempt to reconnect
    });
  }

  async stop(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
    }
  }

  // Join a specific fleet group (optional)
  async joinFleetGroup(fleetId: string): Promise<void> {
    await this.hubConnection.invoke('JoinFleetGroup', fleetId);
  }
}
```

### Vehicle Hub Connection

```typescript
export class VehicleRealtimeService {
  private hubConnection: signalR.HubConnection;
  private apiUrl = 'https://fleetmanagement-api-production.up.railway.app';

  constructor(private authService: AuthService) {
    const token = this.authService.getAccessToken();
    
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiUrl}/hub/vehicles?access_token=${token}`)
      .withAutomaticReconnect()
      .build();
  }

  async start(): Promise<void> {
    await this.hubConnection.start();
    
    this.hubConnection.on('VehicleCreated', (vehicle: VehicleDto) => {
      // Handle vehicle created
    });

    this.hubConnection.on('VehicleUpdated', (vehicle: VehicleDto) => {
      // Handle vehicle updated
    });

    this.hubConnection.on('VehicleDeleted', (data: { vehicleId: string; fleetId: string }) => {
      // Handle vehicle deleted
    });
  }

  // Join a fleet group to receive vehicle updates
  async joinFleetGroup(fleetId: string): Promise<void> {
    await this.hubConnection.invoke('JoinFleetGroup', fleetId);
  }
}
```

### SignalR Message Formats

**FleetCreated:**
```typescript
{
  id: string;
  name: string;
  description?: string;
  ownerId: string;
  ownerName?: string;
  vehicleCount: number;
  createdAtUtc: string;
  updatedAtUtc?: string;
}
```

**FleetUpdated:** Same as FleetCreated

**FleetDeleted:**
```typescript
{
  fleetId: string;
  ownerId: string;
}
```

**VehicleCreated:**
```typescript
{
  id: string;
  vin: string;
  plateNumber: string;
  make?: string;
  model?: string;
  modelYear: number;
  status: number;
  fleetId: string;
  fleetName?: string;
  ownerId?: string;
  createdAtUtc: string;
  updatedAtUtc?: string;
}
```

**VehicleUpdated:** Same as VehicleCreated

**VehicleDeleted:**
```typescript
{
  vehicleId: string;
  fleetId: string;
}
```

---

## ⚠️ Error Handling

### HTTP Status Codes

| Code | Meaning | Action |
|------|---------|--------|
| 200 | Success | Process response |
| 201 | Created | Process response, update UI |
| 204 | No Content | Operation successful (DELETE) |
| 400 | Bad Request | Show validation errors |
| 401 | Unauthorized | Refresh token or redirect to login |
| 403 | Forbidden | Show access denied message |
| 404 | Not Found | Show "not found" message |
| 409 | Conflict | Show conflict message (e.g., duplicate email) |
| 500 | Server Error | Show generic error, log details |

### Error Response Format

```typescript
interface ErrorResponse {
  error: string;
  message?: string;
  errors?: { [key: string]: string[] }; // Validation errors
}
```

### Implementation

```typescript
async handleApiCall<T>(
  request: RequestInfo,
  init?: RequestInit
): Promise<T> {
  const token = this.getAccessToken();
  
  const response = await fetch(request, {
    ...init,
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
      ...init?.headers
    }
  });

  if (response.status === 401) {
    // Try to refresh token
    try {
      await this.refreshToken();
      // Retry request with new token
      return this.handleApiCall<T>(request, init);
    } catch {
      // Refresh failed - redirect to login
      this.router.navigate(['/login']);
      throw new Error('Session expired');
    }
  }

  if (!response.ok) {
    const error: ErrorResponse = await response.json().catch(() => ({
      error: 'An error occurred'
    }));
    throw new Error(error.error || error.message || 'Request failed');
  }

  if (response.status === 204) {
    return null as T; // No content
  }

  return response.json();
}
```

---

## ✅ Testing Checklist

### Authentication
- [ ] Login with valid credentials returns tokens
- [ ] Login with invalid credentials returns 401
- [ ] Register owner creates account and returns tokens
- [ ] Token refresh works before expiration
- [ ] Token refresh fails after expiration
- [ ] `/api/Owners/me` returns current owner with valid token
- [ ] `/api/Owners/me` returns 401 without token

### Fleet CRUD
- [ ] `GET /api/Fleets` returns list of fleets
- [ ] `GET /api/Fleets/{id}` returns single fleet
- [ ] `POST /api/Fleets` creates fleet and broadcasts SignalR event
- [ ] `PUT /api/Fleets/{id}` updates fleet and broadcasts SignalR event
- [ ] `DELETE /api/Fleets/{id}` deletes fleet and broadcasts SignalR event
- [ ] `GET /api/owners/{ownerId}/fleets` returns owner's fleets
- [ ] `POST /api/owners/{ownerId}/fleets` creates fleet for owner

### Vehicle CRUD
- [ ] `GET /api/Vehicles` returns list of vehicles
- [ ] `GET /api/Vehicles?fleetId={id}` filters by fleet
- [ ] `GET /api/Vehicles/{id}` returns single vehicle
- [ ] `POST /api/Vehicles` creates vehicle and broadcasts SignalR event
- [ ] `PUT /api/Vehicles/{id}` updates vehicle and broadcasts SignalR event
- [ ] `DELETE /api/Vehicles/{id}` deletes vehicle and broadcasts SignalR event
- [ ] `GET /api/Fleets/{fleetId}/vehicles` returns fleet's vehicles
- [ ] `POST /api/Fleets/{fleetId}/vehicles` creates vehicle in fleet

### SignalR Real-Time
- [ ] Connect to `/hub/fleets` with valid token
- [ ] Receive `FleetCreated` when fleet is created
- [ ] Receive `FleetUpdated` when fleet is updated
- [ ] Receive `FleetDeleted` when fleet is deleted
- [ ] Connect to `/hub/vehicles` with valid token
- [ ] Receive `VehicleCreated` when vehicle is created
- [ ] Receive `VehicleUpdated` when vehicle is updated
- [ ] Receive `VehicleDeleted` when vehicle is deleted
- [ ] Multiple browser tabs stay in sync
- [ ] Reconnection works after connection loss

### Data Validation
- [ ] Create fleet with missing required fields returns 400
- [ ] Create vehicle with invalid VIN returns 400
- [ ] Update non-existent fleet returns 404
- [ ] Delete non-existent vehicle returns 404

---

## 🔧 Common Issues & Solutions

### Issue: 401 Unauthorized on all requests
**Solution:**
- Check token is being sent: `Authorization: Bearer {token}`
- Verify token hasn't expired (1 hour lifetime)
- Try refreshing token
- Check token format (should be JWT string)

### Issue: SignalR connection fails
**Solution:**
- Verify token is passed: `?access_token={token}`
- Check WebSocket support in browser
- Verify CORS allows your origin
- Check browser console for connection errors

### Issue: SignalR messages not received
**Solution:**
- Verify connection is established (`connection.state === 'Connected'`)
- Check you're in the correct group (`owner-{ownerId}`)
- Verify the operation actually succeeded (check API response)
- Check browser console for SignalR errors

### Issue: CORS errors
**Solution:**
- Verify your origin is allowed (Vercel, Railway, localhost)
- Check `Access-Control-Allow-Credentials` header
- Ensure credentials are included: `credentials: 'include'`

### Issue: 400 Bad Request on create/update
**Solution:**
- Verify all required fields are present
- Check data types match (GUIDs are strings, numbers are numbers)
- Verify foreign keys exist (cityId, fleetId, ownerId)
- Check request body is valid JSON

### Issue: 404 Not Found
**Solution:**
- Verify GUID format is correct
- Check resource actually exists in database
- Verify you have access to that resource (ownerId matches)

---

## 📞 Support

**API Documentation:** `https://fleetmanagement-api-production.up.railway.app/swagger`  
**Health Check:** `https://fleetmanagement-api-production.up.railway.app/health`

**For issues:**
1. Check Swagger documentation for endpoint details
2. Verify request/response formats match this guide
3. Check server logs in Railway dashboard
4. Test with Postman/curl to isolate client vs server issues

---

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2024 | Initial release with SignalR support |

---

**Keep this document updated when API changes are made!**

