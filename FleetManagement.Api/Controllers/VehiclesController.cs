using FleetManagement.Api.Hubs;
using FleetManagement.Data;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;
    private readonly IHubContext<VehicleHub> _vehicleHub;
    private readonly IHubContext<TelemetryHub> _telemetryHub;
    private readonly FleetDbContext _context;

    public VehiclesController(
        IVehicleService vehicleService,
        IHubContext<VehicleHub> vehicleHub,
        IHubContext<TelemetryHub> telemetryHub,
        FleetDbContext context)
    {
        _vehicleService = vehicleService;
        _vehicleHub = vehicleHub;
        _telemetryHub = telemetryHub;
        _context = context;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetVehicles([FromQuery] Guid? fleetId)
    {
        var vehicles = await _vehicleService.GetAllVehiclesAsync(fleetId);
        return Ok(vehicles);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<VehicleDto>> GetVehicle(Guid id)
    {
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }
        return Ok(vehicle);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<VehicleDto>> CreateVehicle(CreateVehicleDto createDto)
    {
        var vehicle = await _vehicleService.CreateVehicleAsync(createDto);
        
        // Get fleet to find owner
        var fleet = await _context.Fleets.FindAsync(vehicle.FleetId);
        if (fleet != null)
        {
            // Broadcast to fleet group and owner group
            await _vehicleHub.Clients
                .Group($"fleet-{vehicle.FleetId}")
                .SendAsync("VehicleCreated", vehicle);
            
            await _vehicleHub.Clients
                .Group($"owner-{fleet.OwnerId}")
                .SendAsync("VehicleCreated", vehicle);
        }
        
        return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<VehicleDto>> UpdateVehicle(Guid id, UpdateVehicleDto updateDto)
    {
        var vehicle = await _vehicleService.UpdateVehicleAsync(id, updateDto);
        if (vehicle == null)
        {
            return NotFound();
        }
        
        // Get fleet to find owner
        var fleet = await _context.Fleets.FindAsync(vehicle.FleetId);
        if (fleet != null)
        {
            // Broadcast update to fleet group and owner group
            await _vehicleHub.Clients
                .Group($"fleet-{vehicle.FleetId}")
                .SendAsync("VehicleUpdated", vehicle);
            
            await _vehicleHub.Clients
                .Group($"owner-{fleet.OwnerId}")
                .SendAsync("VehicleUpdated", vehicle);
        }
        
        return Ok(vehicle);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteVehicle(Guid id)
    {
        // Get vehicle info before deletion
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }
        
        var fleet = await _context.Fleets.FindAsync(vehicle.FleetId);
        var result = await _vehicleService.DeleteVehicleAsync(id);
        if (!result)
        {
            return NotFound();
        }
        
        // Broadcast deletion
        if (fleet != null)
        {
            await _vehicleHub.Clients
                .Group($"fleet-{vehicle.FleetId}")
                .SendAsync("VehicleDeleted", new { vehicleId = id, fleetId = vehicle.FleetId });
            
            await _vehicleHub.Clients
                .Group($"owner-{fleet.OwnerId}")
                .SendAsync("VehicleDeleted", new { vehicleId = id, fleetId = vehicle.FleetId });
        }
        
        return NoContent();
    }

    [HttpGet("{id}/telemetry")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<TelemetryDto>>> GetVehicleTelemetry(Guid id)
    {
        var telemetry = await _vehicleService.GetVehicleTelemetryAsync(id);
        return Ok(telemetry);
    }
}
