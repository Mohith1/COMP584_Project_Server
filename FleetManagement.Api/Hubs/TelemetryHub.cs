using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FleetManagement.Api.Hubs;

[Authorize]
public class TelemetryHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var ownerId = GetOwnerId();
        if (ownerId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"owner-{ownerId.Value}");
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var ownerId = GetOwnerId();
        if (ownerId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"owner-{ownerId.Value}");
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to telemetry updates for specific vehicles
    /// </summary>
    public async Task SubscribeToVehicles(string[] vehicleIds)
    {
        foreach (var vehicleId in vehicleIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"vehicle-{vehicleId}");
        }
        await Clients.Caller.SendAsync("Subscribed", new { vehicleIds });
    }

    /// <summary>
    /// Unsubscribe from telemetry updates for specific vehicles
    /// </summary>
    public async Task UnsubscribeFromVehicles(string[] vehicleIds)
    {
        foreach (var vehicleId in vehicleIds)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"vehicle-{vehicleId}");
        }
        await Clients.Caller.SendAsync("Unsubscribed", new { vehicleIds });
    }

    private Guid? GetOwnerId()
    {
        var ownerIdClaim = Context.User?.FindFirst("ownerId")?.Value
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(ownerIdClaim) || !Guid.TryParse(ownerIdClaim, out var ownerId))
        {
            return null;
        }

        return ownerId;
    }
}


