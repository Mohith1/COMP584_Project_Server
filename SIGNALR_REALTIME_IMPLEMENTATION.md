# SignalR Real-Time Implementation Guide

## ✅ Implementation Complete

Real-time updates for Fleet and Vehicle management have been implemented using **ASP.NET Core SignalR**. All CRUD operations now broadcast changes to connected clients instantly.

---

## 🔗 SignalR Hub Endpoints

### Hub URLs:
- **Fleet Hub**: `wss://your-api-domain/hub/fleets`
- **Vehicle Hub**: `wss://your-api-domain/hub/vehicles`
- **Telemetry Hub**: `wss://your-api-domain/hub/telemetry`

**Production URL Example:**
```
wss://fleetmanagement-api-production.up.railway.app/hub/fleets
```

---

## 🔐 Authentication

SignalR connections **require JWT authentication**. The same JWT token used for REST API calls is used for SignalR.

### Connection Method:
1. **WebSocket with Query String** (Recommended):
   ```
   wss://api-domain/hub/fleets?access_token=YOUR_JWT_TOKEN
   ```

2. **Authorization Header** (Alternative):
   - Some SignalR clients support `Authorization: Bearer {token}` header

### Token Format:
- Token must be a valid JWT issued by `/api/auth/login`
- Token must contain `ownerId` claim (set automatically on login)
- Token expires after 1 hour (use refresh endpoint)

---

## 📡 Message Formats

### Fleet Messages

#### `FleetCreated`
Broadcasted when a new fleet is created.

```typescript
{
  id: string;              // GUID
  name: string;
  description?: string;
  ownerId: string;         // GUID
  ownerName?: string;
  vehicleCount: number;
  createdAtUtc: string;    // ISO 8601
  updatedAtUtc?: string;
}
```

#### `FleetUpdated`
Broadcasted when a fleet is updated.

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

#### `FleetDeleted`
Broadcasted when a fleet is deleted.

```typescript
{
  fleetId: string;        // GUID
  ownerId: string;        // GUID
}
```

---

### Vehicle Messages

#### `VehicleCreated`
Broadcasted when a new vehicle is created.

```typescript
{
  id: string;              // GUID
  vin: string;
  plateNumber: string;
  make?: string;
  model?: string;
  modelYear: number;
  status: number;         // VehicleStatus enum
  fleetId: string;        // GUID
  fleetName?: string;
  ownerId?: string;        // GUID
  createdAtUtc: string;
  updatedAtUtc?: string;
}
```

#### `VehicleUpdated`
Broadcasted when a vehicle is updated.

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

#### `VehicleDeleted`
Broadcasted when a vehicle is deleted.

```typescript
{
  vehicleId: string;       // GUID
  fleetId: string;        // GUID
}
```

---

## 🎯 Client Connection Example (TypeScript/Angular)

### Install SignalR Client:
```bash
npm install @microsoft/signalr
```

### Connect to Fleet Hub:
```typescript
import * as signalR from '@microsoft/signalr';

export class FleetRealtimeService {
  private hubConnection: signalR.HubConnection;

  constructor(private authService: AuthService) {
    const token = this.authService.getAccessToken();
    const apiUrl = environment.apiUrl; // e.g., 'https://fleetmanagement-api-production.up.railway.app'
    
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiUrl}/hub/fleets`, {
        accessTokenFactory: () => token,
        // Or use query string:
        // transport: signalR.HttpTransportType.WebSockets,
        // skipNegotiation: true
      })
      .withAutomaticReconnect()
      .build();
  }

  async start(): Promise<void> {
    try {
      await this.hubConnection.start();
      console.log('Connected to Fleet Hub');
      
      // Subscribe to messages
      this.hubConnection.on('FleetCreated', (fleet) => {
        console.log('Fleet created:', fleet);
        // Update your UI
      });
      
      this.hubConnection.on('FleetUpdated', (fleet) => {
        console.log('Fleet updated:', fleet);
        // Update your UI
      });
      
      this.hubConnection.on('FleetDeleted', (data) => {
        console.log('Fleet deleted:', data.fleetId);
        // Remove from UI
      });
      
      this.hubConnection.on('Connected', (data) => {
        console.log('Connected to owner group:', data.ownerId);
      });
    } catch (error) {
      console.error('Error connecting to SignalR:', error);
    }
  }

  async stop(): Promise<void> {
    await this.hubConnection.stop();
  }
}
```

### Alternative: Query String Authentication
```typescript
const token = this.authService.getAccessToken();
this.hubConnection = new signalR.HubConnectionBuilder()
  .withUrl(`${apiUrl}/hub/fleets?access_token=${token}`)
  .withAutomaticReconnect()
  .build();
```

---

## 📋 Group Subscriptions

### Automatic Groups:
- **Owner Group**: `owner-{ownerId}` - Automatically joined on connection
  - Receives all fleet/vehicle updates for that owner

### Manual Groups:
- **Fleet Group**: `fleet-{fleetId}` - Join via `JoinFleetGroup(fleetId)`
  - Receives vehicle updates for specific fleet

### Join Fleet Group (from client):
```typescript
// After connection is established
await this.hubConnection.invoke('JoinFleetGroup', fleetId);
this.hubConnection.on('JoinedGroup', (data) => {
  console.log('Joined group:', data.group);
});
```

---

## ✅ CRUD Endpoints Status

All endpoints are working and broadcasting changes:

### Fleet Endpoints:
- ✅ `POST /api/owners/{ownerId}/fleets` - Creates fleet, broadcasts `FleetCreated`
- ✅ `GET /api/owners/{ownerId}/fleets` - Lists fleets
- ✅ `PUT /api/Fleets/{fleetId}` - Updates fleet, broadcasts `FleetUpdated`
- ✅ `DELETE /api/Fleets/{fleetId}` - Deletes fleet, broadcasts `FleetDeleted`
- ✅ `GET /api/Fleets/{fleetId}` - Gets fleet details
- ✅ `POST /api/Fleets/{fleetId}/vehicles` - Creates vehicle, broadcasts `VehicleCreated`

### Vehicle Endpoints:
- ✅ `POST /api/Vehicles` - Creates vehicle, broadcasts `VehicleCreated`
- ✅ `PUT /api/Vehicles/{vehicleId}` - Updates vehicle, broadcasts `VehicleUpdated`
- ✅ `DELETE /api/Vehicles/{vehicleId}` - Deletes vehicle, broadcasts `VehicleDeleted`
- ✅ `GET /api/Vehicles` - Lists vehicles
- ✅ `GET /api/Vehicles/{id}` - Gets vehicle details

---

## 🔍 Testing SignalR

### 1. Test Connection:
```bash
# Using wscat (install: npm install -g wscat)
wscat -c "wss://fleetmanagement-api-production.up.railway.app/hub/fleets?access_token=YOUR_JWT_TOKEN"
```

### 2. Test Broadcast:
1. Connect to SignalR hub (from client or test tool)
2. Create a fleet via `POST /api/owners/{ownerId}/fleets`
3. You should receive `FleetCreated` message within 1 second

### 3. Test Multiple Clients:
1. Open two browser tabs
2. Connect both to SignalR hub
3. Create/update/delete from one tab
4. Both tabs should receive updates instantly

---

## 🛠️ Supabase Realtime (Optional)

**Note:** The current implementation broadcasts directly from controllers when CRUD operations occur. This is **simpler and more reliable** than using Supabase Realtime.

If you want to enable Supabase Realtime for database-level change detection:

1. **Enable in Supabase Dashboard:**
   - Go to Database → Replication
   - Enable replication for: `Fleets`, `Vehicles`, `VehicleTelemetrySnapshots`

2. **Configure RLS Policies:**
   ```sql
   -- Allow owners to receive real-time updates for their fleets
   CREATE POLICY "Owners can receive fleet updates"
   ON "Fleets" FOR SELECT
   USING (auth.uid() = "OwnerId");
   ```

3. **Install Supabase Client** (optional):
   ```bash
   dotnet add package Supabase
   ```

4. **Create Bridge Service** (see `SupabaseRealtimeBridgeService.cs` example below)

---

## 📝 Supabase Realtime Bridge Service (Optional)

If you want to use Supabase Realtime instead of controller broadcasts:

```csharp
public class SupabaseRealtimeBridgeService : BackgroundService
{
    private readonly IHubContext<FleetHub> _fleetHub;
    private readonly IHubContext<VehicleHub> _vehicleHub;
    private readonly Supabase.Client _supabase;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribe to Fleet changes
        var fleetChannel = _supabase
            .Realtime
            .Channel("fleets")
            .OnPostgresChange(PostgresChangesFilter.Event.Insert, (sender, response) =>
            {
                var fleet = response.NewRecord.ToObject<FleetDto>();
                _fleetHub.Clients
                    .Group($"owner-{fleet.OwnerId}")
                    .SendAsync("FleetCreated", fleet);
            })
            .OnPostgresChange(PostgresChangesFilter.Event.Update, (sender, response) =>
            {
                var fleet = response.NewRecord.ToObject<FleetDto>();
                _fleetHub.Clients
                    .Group($"owner-{fleet.OwnerId}")
                    .SendAsync("FleetUpdated", fleet);
            })
            .OnPostgresChange(PostgresChangesFilter.Event.Delete, (sender, response) =>
            {
                var fleetId = response.OldRecord["Id"].ToString();
                var ownerId = response.OldRecord["OwnerId"].ToString();
                _fleetHub.Clients
                    .Group($"owner-{ownerId}")
                    .SendAsync("FleetDeleted", new { fleetId, ownerId });
            });
            
        await fleetChannel.Subscribe();
        
        // Similar for Vehicles...
    }
}
```

**However, the current controller-based approach is recommended** because:
- ✅ Simpler to maintain
- ✅ No external dependencies
- ✅ Works with any database (not just Supabase)
- ✅ Immediate broadcasts (no database polling delay)

---

## 🚀 Deployment Notes

### Railway:
- SignalR WebSocket connections work automatically
- No additional configuration needed
- CORS is already configured for SignalR

### Environment Variables:
No additional environment variables needed for SignalR. It uses the same JWT configuration as REST API.

---

## ✅ Acceptance Criteria - ALL MET

1. ✅ When a fleet is created/updated/deleted, all connected clients for that owner receive update within 1 second
2. ✅ When a vehicle is added/updated/deleted, all clients viewing that fleet receive update within 1 second  
3. ✅ Multiple browser tabs/windows stay in sync automatically
4. ✅ No polling required - updates are push-based
5. ✅ Authentication/authorization is enforced on all SignalR connections

---

## 📚 Resources

- [ASP.NET Core SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [SignalR JavaScript Client](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client)
- [Supabase Realtime Documentation](https://supabase.com/docs/guides/realtime)

---

**Implementation Date:** 2024
**Status:** ✅ Production Ready

