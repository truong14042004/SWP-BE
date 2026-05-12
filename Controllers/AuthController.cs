using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using SWP_BE.Contracts.Auth;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IGoogleAuthService googleAuthService,
    ILogger<AuthController> logger) : ControllerBase
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
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Google login failed. Type={ExceptionType} Source={Source}",
                exception.GetType().FullName,
                exception.Source);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Google login failed on the server.",
                detail = exception.Message,
                type = exception.GetType().Name,
                source = exception.Source
            });
        }
    }
}
