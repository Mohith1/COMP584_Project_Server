using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Fleets;
using Microsoft.AspNetCore.Mvc;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FleetsController : ControllerBase
{
    private readonly IFleetService _fleetService;

    public FleetsController(IFleetService fleetService)
    {
        _fleetService = fleetService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FleetDto>>> GetFleets([FromQuery] Guid? ownerId)
    {
        var fleets = await _fleetService.GetAllFleetsAsync(ownerId);
        return Ok(fleets);
    }

    [HttpGet("{id}")]
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
    public async Task<ActionResult<FleetDto>> CreateFleet(CreateFleetDto createDto)
    {
        var fleet = await _fleetService.CreateFleetAsync(createDto);
        return CreatedAtAction(nameof(GetFleet), new { id = fleet.Id }, fleet);
    }

    [HttpPut("{id}")]
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
    public async Task<IActionResult> DeleteFleet(Guid id)
    {
        var result = await _fleetService.DeleteFleetAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}







