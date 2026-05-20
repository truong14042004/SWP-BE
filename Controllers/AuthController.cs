using System.Security.Claims;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Contracts.Auth;
using SWP_BE.Data;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IGoogleAuthService googleAuthService,
    IPasswordAuthService passwordAuthService,
    IRefreshTokenService refreshTokenService,
    AppDbContext dbContext,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<AuthMessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthMessageResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await passwordAuthService.RegisterAsync(request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("resend-otp")]
    [ProducesResponseType<AuthMessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthMessageResponse>> ResendOtp(
        ResendOtpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await passwordAuthService.ResendOtpAsync(request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("verify-email")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> VerifyEmailOtp(
        VerifyEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await passwordAuthService.VerifyEmailOtpAsync(request, cancellationToken));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        PasswordLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await passwordAuthService.LoginAsync(request, cancellationToken));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
    }

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

    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await refreshTokenService.RefreshAsync(request.RefreshToken, cancellationToken));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
    }

    [HttpPost("revoke")]
    [ProducesResponseType<AuthMessageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthMessageResponse>> Revoke(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        await refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);
        return Ok(new AuthMessageResponse("Refresh token revoked.", string.Empty));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeResponse>> GetMe(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier); //lay chuoi id tu token
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) //kiem tra id co hop le khong
        {
            return Unauthorized(new { message = "Invalid token." });
        }

        var user = await dbContext.Users //lay user tu database
            .AsNoTracking()
            .SingleOrDefaultAsync(existingUser => existingUser.Id == userId, cancellationToken);

        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "Account is inactive." });
        }

        return Ok(new MeResponse(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.AvatarUrl,
            user.Role,
            user.IsEmailVerified,
            user.IsActive));
    }

    /// <summary>
    /// Acknowledges logout. JWT khong duoc luu tren server, nen client phai xoa token local.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType<AuthMessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<AuthMessageResponse> Logout()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty; //lay email tu token

        return Ok(new AuthMessageResponse(
            "Logged out successfully. Remove the stored JWT access token on the client; the server does not revoke stateless tokens.",
            email));
    }
}
