using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Cities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CitiesController : ControllerBase
{
    private readonly ICityService _cityService;

    public CitiesController(ICityService cityService)
    {
        _cityService = cityService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CityDto>>> GetCities([FromQuery] Guid? countryId)
    {
        var cities = await _cityService.GetAllCitiesAsync(countryId);
        return Ok(cities);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CityDto>> GetCity(Guid id)
    {
        var city = await _cityService.GetCityByIdAsync(id);
        if (city == null)
        {
            return NotFound();
        }
        return Ok(city);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CityDto>> CreateCity(CreateCityDto createDto)
    {
        var city = await _cityService.CreateCityAsync(createDto);
        return CreatedAtAction(nameof(GetCity), new { id = city.Id }, city);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CityDto>> UpdateCity(Guid id, UpdateCityDto updateDto)
    {
        var city = await _cityService.UpdateCityAsync(id, updateDto);
        if (city == null)
        {
            return NotFound();
        }
        return Ok(city);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCity(Guid id)
    {
        var result = await _cityService.DeleteCityAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}
