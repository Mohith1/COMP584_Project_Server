using FleetManagement.Api.Authorization;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/seed")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = SystemRoles.Administrator, Policy = AuthorizationPolicies.AdminOnly)]
public class SeedController(ISeedService seedService) : ControllerBase
{
    private readonly ISeedService _seedService = seedService;

    [HttpPost("cities")]
    public async Task<IActionResult> SeedCities([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        await using var stream = file.OpenReadStream();
        var inserted = await _seedService.SeedCitiesAsync(stream, cancellationToken);
        return Ok(new { inserted });
    }
}

