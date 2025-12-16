using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Cities;
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
        var countries = await _countryService.GetAllCountriesAsync();
        return Ok(countries);
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
    public async Task<ActionResult<CountryDto>> CreateCountry(CreateCountryDto createDto)
    {
        var country = await _countryService.CreateCountryAsync(createDto);
        return CreatedAtAction(nameof(GetCountry), new { id = country.Id }, country);
    }

    [HttpPut("{id}")]
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







