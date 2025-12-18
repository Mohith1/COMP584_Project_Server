using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FleetManagement.Api.Hubs;

[Authorize]
public class VehicleHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Get owner ID from JWT claims
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
    /// Join a specific fleet's group to receive vehicle updates for that fleet
    /// </summary>
    public async Task JoinFleetGroup(string fleetId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"fleet-{fleetId}");
        await Clients.Caller.SendAsync("JoinedGroup", new { group = $"fleet-{fleetId}" });
    }

    /// <summary>
    /// Leave a specific fleet's group
    /// </summary>
    public async Task LeaveFleetGroup(string fleetId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"fleet-{fleetId}");
        await Clients.Caller.SendAsync("LeftGroup", new { group = $"fleet-{fleetId}" });
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


