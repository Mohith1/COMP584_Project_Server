using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Owners;
using Microsoft.AspNetCore.Mvc;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OwnersController : ControllerBase
{
    private readonly IOwnerService _ownerService;

    public OwnersController(IOwnerService ownerService)
    {
        _ownerService = ownerService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OwnerDto>>> GetOwners()
    {
        var owners = await _ownerService.GetAllOwnersAsync();
        return Ok(owners);
    }

    [HttpGet("{id}")]
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
    public async Task<IActionResult> DeleteOwner(Guid id)
    {
        var result = await _ownerService.DeleteOwnerAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}

