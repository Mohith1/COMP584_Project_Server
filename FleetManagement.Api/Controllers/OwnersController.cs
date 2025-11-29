using FleetManagement.Api.Authorization;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.Constants;
using FleetManagement.Services.DTOs.Owners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Okta.AspNetCore;
using System.Security.Claims;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/owners")]
[Authorize(
    AuthenticationSchemes = $"{Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme},{OktaDefaults.ApiAuthenticationScheme}",
    Policy = AuthorizationPolicies.OwnerOrAdmin)]
public class OwnersController(IOwnerService ownerService) : ControllerBase
{
    private readonly IOwnerService _ownerService = ownerService;

    [HttpGet("me")]
    public async Task<ActionResult<OwnerDetailResponse>> GetMyProfile(CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        var response = await _ownerService.GetAsync(ownerId, cancellationToken);
        return Ok(response);
    }

    [HttpPut("me")]
    public async Task<ActionResult<OwnerDetailResponse>> UpdateMyProfile([FromBody] UpdateOwnerRequest request, CancellationToken cancellationToken)
    {
        var ownerId = GetOwnerId();
        var response = await _ownerService.UpdateAsync(request with { OwnerId = ownerId }, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{ownerId:guid}")]
    [Authorize(Roles = SystemRoles.Administrator)]
    public async Task<ActionResult<OwnerDetailResponse>> GetById(Guid ownerId, CancellationToken cancellationToken)
    {
        var response = await _ownerService.GetAsync(ownerId, cancellationToken);
        return Ok(response);
    }

    private Guid GetOwnerId()
    {
        var ownerClaim = User.FindFirst("ownerId")?.Value;
        if (Guid.TryParse(ownerClaim, out var ownerId))
        {
            return ownerId;
        }

        throw new UnauthorizedAccessException("Owner context missing.");
    }
}

