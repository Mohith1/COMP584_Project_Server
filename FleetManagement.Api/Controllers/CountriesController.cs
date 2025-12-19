using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Cities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CountriesController : ControllerBase
{
    private readonly ICountryService _countryService;

    public CountriesController(ICountryService countryService)
    {
        _countryService = countryService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CountryDto>>> GetCountries()
    {
        try
        {
            var countries = await _countryService.GetAllCountriesAsync();
            return Ok(countries);
        }
        catch (Exception ex)
        {
            // Log error (in production, use proper logging)
            Console.WriteLine($"[CountriesController] Error: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"[CountriesController] Inner: {ex.InnerException.Message}");
            
            // Return 500 with error details (in production, sanitize this)
            return StatusCode(500, new { error = "Failed to retrieve countries", message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CountryDto>> GetCountry(Guid id)
    {
        var country = await _countryService.GetCountryByIdAsync(id);
        if (country == null)
        {
            return NotFound();
        }
        return Ok(country);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CountryDto>> CreateCountry(CreateCountryDto createDto)
    {
        var country = await _countryService.CreateCountryAsync(createDto);
        return CreatedAtAction(nameof(GetCountry), new { id = country.Id }, country);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CountryDto>> UpdateCountry(Guid id, UpdateCountryDto updateDto)
    {
        var country = await _countryService.UpdateCountryAsync(id, updateDto);
        if (country == null)
        {
            return NotFound();
        }
        return Ok(country);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCountry(Guid id)
    {
        var result = await _countryService.DeleteCountryAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}
