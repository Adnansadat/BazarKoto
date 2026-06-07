using System.Security.Claims;
using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Auth;
using BazarKoto.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BazarKoto.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _authService.LoginAsync(request, cancellationToken));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _authService.RefreshAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));
        }

        return Ok(await _authService.LogoutAsync(userId, cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("admin/email")]
    public async Task<IActionResult> UpdateAdminEmail(UpdateAdminEmailRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));
        }

        return ToCredentialUpdateResult(await _authService.UpdateAdminEmailAsync(userId, request, cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("admin/password")]
    public async Task<IActionResult> UpdateAdminPassword(UpdateAdminPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));
        }

        return ToCredentialUpdateResult(await _authService.UpdateAdminPasswordAsync(userId, request, cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("admin/credentials")]
    public async Task<IActionResult> UpdateAdminCredentials(UpdateAdminCredentialsRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));
        }

        return ToCredentialUpdateResult(await _authService.UpdateAdminCredentialsAsync(userId, request, cancellationToken));
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out userId);
    }

    private IActionResult ToCredentialUpdateResult(ApiResponse<object> response)
    {
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
