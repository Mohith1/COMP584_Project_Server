using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Fleets;
using FleetManagement.Services.DTOs.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FleetsController : ControllerBase
{
    private readonly IFleetService _fleetService;
    private readonly IVehicleService _vehicleService;

    public FleetsController(IFleetService fleetService, IVehicleService vehicleService)
    {
        _fleetService = fleetService;
        _vehicleService = vehicleService;
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
        return Ok(fleet);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteFleet(Guid id)
    {
        var result = await _fleetService.DeleteFleetAsync(id);
        if (!result)
        {
            return NotFound();
        }
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
        return CreatedAtAction(nameof(GetFleetVehicles), new { fleetId = fleetId }, vehicle);
    }
}
