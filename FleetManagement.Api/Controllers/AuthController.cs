using FleetManagement.Services.Abstractions;
using FleetManagement.Services.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("register-owner")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> RegisterOwner([FromBody] OwnerRegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterOwnerAsync(request, cancellationToken);
        return CreatedAtAction(nameof(RegisterOwner), response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        await _authService.RevokeRefreshTokenAsync(request, cancellationToken);
        return NoContent();
    }
}

