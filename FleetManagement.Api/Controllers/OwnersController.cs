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
    [AllowAnonymous]
    public ActionResult<OwnerDto?> GetCurrentOwner()
    {
        // Simplified for class project:
        // - Do NOT block the UI based on server-side owner profile
        // - Frontend will handle showing registration or dashboard
        //
        // Always return 200 OK with null so the client never sees 401/404 here.
        return Ok(null);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<OwnerDto>> UpdateCurrentOwner(UpdateOwnerDto updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("nameid")?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { error = "Unable to identify the current user from token" });
        }

        OwnerDto? currentOwner = null;

        // Try Auth0 string ID first
        if (userIdClaim.Contains('|') || !Guid.TryParse(userIdClaim, out _))
        {
            currentOwner = await _ownerService.GetOwnerByAuth0UserIdAsync(userIdClaim);
        }
        
        // Fallback to internal GUID
        if (currentOwner == null && Guid.TryParse(userIdClaim, out var guidUserId))
        {
            currentOwner = await _ownerService.GetOwnerByIdentityUserIdAsync(guidUserId);
        }

        if (currentOwner == null)
        {
            return NotFound(new { 
                error = "Owner profile not found", 
                message = "Please complete registration first." 
            });
        }

        try
        {
            var owner = await _ownerService.UpdateOwnerAsync(currentOwner.Id, updateDto);
            if (owner == null)
            {
                return NotFound();
            }
            return Ok(owner);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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
        // Validate model state first
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = "Invalid request data", errors = ModelState });
        }

        try
        {
            // If user is authenticated, automatically link the Auth0 ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("nameid")?.Value;

            // If authenticated and no Auth0UserId provided, use the claim
            if (!string.IsNullOrEmpty(userIdClaim) && string.IsNullOrEmpty(createDto.Auth0UserId))
            {
                createDto.Auth0UserId = userIdClaim;
            }

            var owner = await _ownerService.CreateOwnerAsync(createDto);
            return CreatedAtAction(nameof(GetOwner), new { id = owner.Id }, owner);
        }
        catch (ArgumentException ex)
        {
            // Validation error (e.g., invalid CityId)
            return BadRequest(new { error = ex.Message });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            // Database-specific errors
            var errorMsg = dbEx.Message;
            var innerMsg = dbEx.InnerException?.Message ?? "";
            
            Console.WriteLine($"[OwnersController] Database error: {errorMsg}");
            if (dbEx.InnerException != null)
                Console.WriteLine($"[OwnersController] Inner: {innerMsg}");
            
            // Check for specific constraint violations
            if (errorMsg.Contains("duplicate") || errorMsg.Contains("unique") || errorMsg.Contains("violates unique constraint"))
            {
                return Conflict(new { 
                    error = "Duplicate entry", 
                    message = "An owner with this information already exists" 
                });
            }
            
            if (errorMsg.Contains("foreign key") || errorMsg.Contains("constraint"))
            {
                return BadRequest(new { 
                    error = "Invalid data", 
                    message = "Referenced data does not exist (e.g., invalid CityId)" 
                });
            }
            
            return StatusCode(500, new { 
                error = "Database error", 
                message = errorMsg,
                details = innerMsg
            });
        }
        catch (Npgsql.NpgsqlException npgsqlEx)
        {
            // PostgreSQL connection errors
            Console.WriteLine($"[OwnersController] PostgreSQL error: {npgsqlEx.Message}");
            
            return StatusCode(500, new { 
                error = "Database connection error", 
                message = "Cannot connect to database. Please check database configuration." 
            });
        }
        catch (Exception ex)
        {
            // Log full error details
            Console.WriteLine($"[OwnersController] CreateOwner error: {ex.Message}");
            Console.WriteLine($"[OwnersController] Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[OwnersController] Inner: {ex.InnerException.Message}");
                Console.WriteLine($"[OwnersController] Inner stack: {ex.InnerException.StackTrace}");
            }
            
            // Return 500 with more details for debugging
            return StatusCode(500, new { 
                error = "Failed to create owner", 
                message = ex.Message,
                type = ex.GetType().Name
            });
        }
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

}
