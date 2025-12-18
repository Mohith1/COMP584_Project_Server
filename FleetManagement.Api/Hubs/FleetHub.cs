using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FleetManagement.Api.Hubs;

[Authorize]
public class FleetHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Get owner ID from JWT claims
        var ownerId = GetOwnerId();
        if (ownerId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"owner-{ownerId.Value}");
            await Clients.Caller.SendAsync("Connected", new { ownerId = ownerId.Value });
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
    /// Join a specific owner's group (for multi-owner scenarios)
    /// </summary>
    public async Task JoinOwnerGroup(string ownerId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"owner-{ownerId}");
        await Clients.Caller.SendAsync("JoinedGroup", new { group = $"owner-{ownerId}" });
    }

    /// <summary>
    /// Leave a specific owner's group
    /// </summary>
    public async Task LeaveOwnerGroup(string ownerId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"owner-{ownerId}");
        await Clients.Caller.SendAsync("LeftGroup", new { group = $"owner-{ownerId}" });
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


