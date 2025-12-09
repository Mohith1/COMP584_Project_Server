using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Vehicles;
using Microsoft.AspNetCore.Mvc;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public TelemetryController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TelemetryDto>>> GetTelemetry([FromQuery] string? vehicleIds)
    {
        if (string.IsNullOrWhiteSpace(vehicleIds))
        {
            return BadRequest("vehicleIds query parameter is required");
        }

        var ids = vehicleIds.Split(',')
            .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        if (ids.Count == 0)
        {
            return BadRequest("Invalid vehicleIds format");
        }

        var telemetry = await _vehicleService.GetLatestTelemetryAsync(ids);
        return Ok(telemetry);
    }

    [HttpGet("{vehicleId}/latest")]
    public async Task<ActionResult<TelemetryDto>> GetLatestTelemetry(Guid vehicleId)
    {
        var telemetry = await _vehicleService.GetVehicleTelemetryAsync(vehicleId);
        var latest = telemetry.OrderByDescending(t => t.CapturedAtUtc).FirstOrDefault();
        
        if (latest == null)
        {
            return NotFound();
        }
        
        return Ok(latest);
    }
}

