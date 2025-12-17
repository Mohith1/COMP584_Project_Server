using System.Security.Claims;
using FleetManagement.Api.Hubs;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Fleets;
using FleetManagement.Services.DTOs.Owners;
using FleetManagement.Services.DTOs.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OwnersController : ControllerBase
{
    private readonly IOwnerService _ownerService;
    private readonly IFleetService _fleetService;
    private readonly IVehicleService _vehicleService;
    private readonly IHubContext<FleetHub> _fleetHub;
    private readonly IHubContext<VehicleHub> _vehicleHub;

    public OwnersController(
        IOwnerService ownerService,
        IFleetService fleetService,
        IVehicleService vehicleService,
        IHubContext<FleetHub> fleetHub,
        IHubContext<VehicleHub> vehicleHub)
    {
        _ownerService = ownerService;
        _fleetService = fleetService;
        _vehicleService = vehicleService;
        _fleetHub = fleetHub;
        _vehicleHub = vehicleHub;
    }

    // ===================================
    // OWNER CRUD ENDPOINTS
    // ===================================

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<OwnerDto>>> GetOwners()
    {
        var owners = await _ownerService.GetAllOwnersAsync();
        return Ok(owners);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<OwnerDto>> GetCurrentOwner()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return BadRequest(new { error = "Unable to identify the current user" });
        }

        var owner = await _ownerService.GetOwnerByIdentityUserIdAsync(userId.Value);
        if (owner == null)
        {
            return NotFound(new { error = "Owner record not found for the current user" });
        }

        return Ok(owner);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<OwnerDto>> UpdateCurrentOwner(UpdateOwnerDto updateDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return BadRequest(new { error = "Unable to identify the current user" });
        }

        var currentOwner = await _ownerService.GetOwnerByIdentityUserIdAsync(userId.Value);
        if (currentOwner == null)
        {
            return NotFound(new { error = "Owner record not found for the current user" });
        }

        var owner = await _ownerService.UpdateOwnerAsync(currentOwner.Id, updateDto);
        if (owner == null)
        {
            return NotFound();
        }

        return Ok(owner);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<OwnerDto>> GetOwner(Guid id)
    {
        var owner = await _ownerService.GetOwnerByIdAsync(id);
        if (owner == null)
        {
            return NotFound();
        }
        return Ok(owner);
    }

    [HttpPost]
    public async Task<ActionResult<OwnerDto>> CreateOwner(CreateOwnerDto createDto)
    {
        var owner = await _ownerService.CreateOwnerAsync(createDto);
        return CreatedAtAction(nameof(GetOwner), new { id = owner.Id }, owner);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<OwnerDto>> UpdateOwner(Guid id, UpdateOwnerDto updateDto)
    {
        var owner = await _ownerService.UpdateOwnerAsync(id, updateDto);
        if (owner == null)
        {
            return NotFound();
        }
        return Ok(owner);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteOwner(Guid id)
    {
        var result = await _ownerService.DeleteOwnerAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    // ===================================
    // NESTED FLEET ROUTES: /api/owners/{ownerId}/fleets
    // ===================================

    [HttpGet("{ownerId}/fleets")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<FleetDto>>> GetOwnerFleets(Guid ownerId)
    {
        // Verify owner exists
        var owner = await _ownerService.GetOwnerByIdAsync(ownerId);
        if (owner == null)
        {
            return NotFound(new { error = "Owner not found" });
        }

        var fleets = await _fleetService.GetAllFleetsAsync(ownerId);
        return Ok(fleets);
    }

    [HttpPost("{ownerId}/fleets")]
    [Authorize]
    public async Task<ActionResult<FleetDto>> CreateOwnerFleet(Guid ownerId, CreateFleetDto createDto)
    {
        // Verify owner exists
        var owner = await _ownerService.GetOwnerByIdAsync(ownerId);
        if (owner == null)
        {
            return NotFound(new { error = "Owner not found" });
        }

        // Ensure OwnerId matches the route
        createDto.OwnerId = ownerId;

        var fleet = await _fleetService.CreateFleetAsync(createDto);
        
        // Broadcast to all clients subscribed to this owner
        await _fleetHub.Clients
            .Group($"owner-{ownerId}")
            .SendAsync("FleetCreated", fleet);
        
        return CreatedAtAction(nameof(GetOwnerFleets), new { ownerId = ownerId }, fleet);
    }

    // ===================================
    // NESTED VEHICLE ROUTES: /api/owners/{ownerId}/vehicles
    // ===================================

    [HttpGet("{ownerId}/vehicles")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetOwnerVehicles(Guid ownerId)
    {
        // Verify owner exists
        var owner = await _ownerService.GetOwnerByIdAsync(ownerId);
        if (owner == null)
        {
            return NotFound(new { error = "Owner not found" });
        }

        // Get all fleets for this owner, then get all vehicles
        var fleets = await _fleetService.GetAllFleetsAsync(ownerId);
        var allVehicles = new List<VehicleDto>();

        foreach (var fleet in fleets)
        {
            var vehicles = await _vehicleService.GetAllVehiclesAsync(fleet.Id);
            allVehicles.AddRange(vehicles);
        }

        return Ok(allVehicles);
    }

    [HttpGet("{ownerId}/vehicles/telemetry")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<TelemetryDto>>> GetOwnerVehiclesTelemetry(Guid ownerId)
    {
        // Verify owner exists
        var owner = await _ownerService.GetOwnerByIdAsync(ownerId);
        if (owner == null)
        {
            return NotFound(new { error = "Owner not found" });
        }

        // Get all fleets for this owner
        var fleets = await _fleetService.GetAllFleetsAsync(ownerId);
        var vehicleIds = new List<Guid>();

        foreach (var fleet in fleets)
        {
            var vehicles = await _vehicleService.GetAllVehiclesAsync(fleet.Id);
            vehicleIds.AddRange(vehicles.Select(v => v.Id));
        }

        if (vehicleIds.Count == 0)
        {
            return Ok(new List<TelemetryDto>());
        }

        var telemetry = await _vehicleService.GetLatestTelemetryAsync(vehicleIds);
        return Ok(telemetry);
    }

    // ===================================
    // HELPER METHODS
    // ===================================

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("nameid")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}
