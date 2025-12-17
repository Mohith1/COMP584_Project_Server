using FleetManagement.Api.Hubs;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Fleets;
using FleetManagement.Services.DTOs.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FleetsController : ControllerBase
{
    private readonly IFleetService _fleetService;
    private readonly IVehicleService _vehicleService;
    private readonly IHubContext<FleetHub> _fleetHub;
    private readonly IHubContext<VehicleHub> _vehicleHub;

    public FleetsController(
        IFleetService fleetService, 
        IVehicleService vehicleService,
        IHubContext<FleetHub> fleetHub,
        IHubContext<VehicleHub> vehicleHub)
    {
        _fleetService = fleetService;
        _vehicleService = vehicleService;
        _fleetHub = fleetHub;
        _vehicleHub = vehicleHub;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<FleetDto>>> GetFleets([FromQuery] Guid? ownerId)
    {
        var fleets = await _fleetService.GetAllFleetsAsync(ownerId);
        return Ok(fleets);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<FleetDto>> GetFleet(Guid id)
    {
        var fleet = await _fleetService.GetFleetByIdAsync(id);
        if (fleet == null)
        {
            return NotFound();
        }
        return Ok(fleet);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<FleetDto>> CreateFleet(CreateFleetDto createDto)
    {
        var fleet = await _fleetService.CreateFleetAsync(createDto);
        
        // Broadcast to all clients subscribed to this owner
        await _fleetHub.Clients
            .Group($"owner-{fleet.OwnerId}")
            .SendAsync("FleetCreated", fleet);
        
        return CreatedAtAction(nameof(GetFleet), new { id = fleet.Id }, fleet);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<FleetDto>> UpdateFleet(Guid id, UpdateFleetDto updateDto)
    {
        var fleet = await _fleetService.UpdateFleetAsync(id, updateDto);
        if (fleet == null)
        {
            return NotFound();
        }
        
        // Broadcast update to all clients subscribed to this owner
        await _fleetHub.Clients
            .Group($"owner-{fleet.OwnerId}")
            .SendAsync("FleetUpdated", fleet);
        
        return Ok(fleet);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteFleet(Guid id)
    {
        // Get fleet info before deletion to broadcast to correct owner
        var fleet = await _fleetService.GetFleetByIdAsync(id);
        if (fleet == null)
        {
            return NotFound();
        }
        
        var result = await _fleetService.DeleteFleetAsync(id);
        if (!result)
        {
            return NotFound();
        }
        
        // Broadcast deletion to all clients subscribed to this owner
        await _fleetHub.Clients
            .Group($"owner-{fleet.OwnerId}")
            .SendAsync("FleetDeleted", new { fleetId = id, ownerId = fleet.OwnerId });
        
        return NoContent();
    }

    // ===================================
    // NESTED VEHICLE ROUTES: /api/Fleets/{fleetId}/vehicles
    // ===================================

    [HttpGet("{fleetId}/vehicles")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetFleetVehicles(Guid fleetId)
    {
        // Verify fleet exists
        var fleet = await _fleetService.GetFleetByIdAsync(fleetId);
        if (fleet == null)
        {
            return NotFound(new { error = "Fleet not found" });
        }

        var vehicles = await _vehicleService.GetAllVehiclesAsync(fleetId);
        return Ok(vehicles);
    }

    [HttpPost("{fleetId}/vehicles")]
    [Authorize]
    public async Task<ActionResult<VehicleDto>> CreateFleetVehicle(Guid fleetId, CreateVehicleDto createDto)
    {
        // Verify fleet exists
        var fleet = await _fleetService.GetFleetByIdAsync(fleetId);
        if (fleet == null)
        {
            return NotFound(new { error = "Fleet not found" });
        }

        // Ensure FleetId matches the route
        createDto.FleetId = fleetId;

        var vehicle = await _vehicleService.CreateVehicleAsync(createDto);
        
        // Broadcast to fleet group and owner group
        await _vehicleHub.Clients
            .Group($"fleet-{fleetId}")
            .SendAsync("VehicleCreated", vehicle);
        
        await _vehicleHub.Clients
            .Group($"owner-{fleet.OwnerId}")
            .SendAsync("VehicleCreated", vehicle);
        
        return CreatedAtAction(nameof(GetFleetVehicles), new { fleetId = fleetId }, vehicle);
    }
}
