using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using SWP_BE.Contracts.Auth;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IGoogleAuthService googleAuthService) : ControllerBase
{
    [HttpPost("google")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> LoginWithGoogle(
        GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await googleAuthService.LoginAsync(request.IdToken, cancellationToken));
        }
        catch (InvalidJwtException)
        {
            return Unauthorized(new { message = "Invalid Google token." });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
    }
}
