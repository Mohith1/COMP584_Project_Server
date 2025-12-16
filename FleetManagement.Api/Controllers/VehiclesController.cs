using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Vehicles;
using Microsoft.AspNetCore.Mvc;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetVehicles([FromQuery] Guid? fleetId)
    {
        var vehicles = await _vehicleService.GetAllVehiclesAsync(fleetId);
        return Ok(vehicles);
    }

    [HttpGet("{id}")]
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
    public async Task<ActionResult<VehicleDto>> CreateVehicle(CreateVehicleDto createDto)
    {
        var vehicle = await _vehicleService.CreateVehicleAsync(createDto);
        return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<VehicleDto>> UpdateVehicle(Guid id, UpdateVehicleDto updateDto)
    {
        var vehicle = await _vehicleService.UpdateVehicleAsync(id, updateDto);
        if (vehicle == null)
        {
            return NotFound();
        }
        return Ok(vehicle);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVehicle(Guid id)
    {
        var result = await _vehicleService.DeleteVehicleAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpGet("{id}/telemetry")]
    public async Task<ActionResult<IEnumerable<TelemetryDto>>> GetVehicleTelemetry(Guid id)
    {
        var telemetry = await _vehicleService.GetVehicleTelemetryAsync(id);
        return Ok(telemetry);
    }
}







