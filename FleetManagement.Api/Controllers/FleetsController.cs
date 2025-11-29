using FleetManagement.Api.Authorization;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Fleets;
using FleetManagement.Services.DTOs.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Okta.AspNetCore;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(
    AuthenticationSchemes = $"{Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme},{OktaDefaults.ApiAuthenticationScheme}",
    Policy = AuthorizationPolicies.OwnerOrAdmin)]
public class FleetsController(IFleetService fleetService) : ControllerBase
{
    private readonly IFleetService _fleetService = fleetService;

    [HttpGet("owners/{ownerId:guid}/fleets")]
    public async Task<ActionResult> GetFleets(Guid ownerId, [FromQuery] int page = 1, [FromQuery] int size = 10, CancellationToken cancellationToken = default)
    {
        var response = await _fleetService.GetByOwnerAsync(ownerId, page, size, cancellationToken);
        return Ok(response);
    }

    [HttpPost("owners/{ownerId:guid}/fleets")]
    public async Task<ActionResult> CreateFleet(Guid ownerId, [FromBody] CreateFleetRequest request, CancellationToken cancellationToken)
    {
        var response = await _fleetService.CreateAsync(request with { OwnerId = ownerId }, cancellationToken);
        return CreatedAtAction(nameof(GetFleets), new { ownerId, page = 1, size = 10 }, response);
    }

    [HttpPut("fleets/{fleetId:guid}")]
    public async Task<ActionResult> UpdateFleet(Guid fleetId, [FromBody] UpdateFleetRequest request, CancellationToken cancellationToken)
    {
        var response = await _fleetService.UpdateAsync(request with { FleetId = fleetId }, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("fleets/{fleetId:guid}")]
    public async Task<IActionResult> DeleteFleet(Guid fleetId, CancellationToken cancellationToken)
    {
        await _fleetService.DeleteAsync(fleetId, cancellationToken);
        return NoContent();
    }

    [HttpPost("fleets/{fleetId:guid}/vehicles")]
    public async Task<ActionResult<VehicleResponse>> AddVehicle(Guid fleetId, [FromBody] CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        var response = await _fleetService.AddVehicleAsync(request with { FleetId = fleetId }, cancellationToken);
        return Ok(response);
    }

    [HttpPut("vehicles/{vehicleId:guid}")]
    public async Task<ActionResult<VehicleResponse>> UpdateVehicle(Guid vehicleId, [FromBody] UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        var response = await _fleetService.UpdateVehicleAsync(request with { VehicleId = vehicleId }, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("vehicles/{vehicleId:guid}")]
    public async Task<IActionResult> DeleteVehicle(Guid vehicleId, CancellationToken cancellationToken)
    {
        await _fleetService.DeleteVehicleAsync(vehicleId, cancellationToken);
        return NoContent();
    }

    [HttpGet("owners/{ownerId:guid}/vehicles/telemetry")]
    public async Task<ActionResult<IReadOnlyCollection<VehicleTelemetryResponse>>> GetLatestTelemetry(Guid ownerId, CancellationToken cancellationToken)
    {
        var response = await _fleetService.GetLatestTelemetryAsync(ownerId, cancellationToken);
        return Ok(response);
    }
}

