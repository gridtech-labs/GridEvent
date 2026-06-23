using FluentValidation;
using GridTickets.Application.DTOs.Auth;
using GridTickets.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridTickets.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    // ── OTP Login (primary flow) ──────────────────────────────────────────────

    /// <summary>Find or create account by email + mobile, then send OTP.</summary>
    [HttpPost("request-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Mobile))
            return BadRequest(new { errors = new[] { "Email and mobile are required." } });

        var result = await _authService.RequestOtpAsync(request.Email.Trim(), request.Mobile.Trim());
        if (!result.IsSuccess)
            return BadRequest(new { errors = result.Errors });

        return Ok(new { message = "OTP sent to your email and mobile." });
    }

    /// <summary>Booking flow: find-or-create user and issue tokens immediately (no OTP).</summary>
    [HttpPost("silent-login")]
    [AllowAnonymous]
    public async Task<IActionResult> SilentLogin([FromBody] SilentLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Mobile))
            return BadRequest(new { errors = new[] { "Email and mobile are required." } });

        var result = await _authService.SilentLoginAsync(
            request.Email.Trim(), request.Mobile.Trim(), request.FullName?.Trim(), GetIpAddress());

        if (!result.IsSuccess)
            return BadRequest(new { errors = result.Errors });

        SetRefreshTokenCookie(result.Value!.RefreshToken);
        return Ok(new { result.Value.AccessToken, result.Value.AccessTokenExpiresAt, result.Value.User });
    }

    /// <summary>Verify OTP and issue tokens.</summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
            return BadRequest(new { errors = new[] { "Email and OTP are required." } });

        var result = await _authService.VerifyOtpAsync(
            request.Email.Trim(), request.Otp.Trim(), GetIpAddress());

        if (!result.IsSuccess)
            return Unauthorized(new { errors = result.Errors });

        SetRefreshTokenCookie(result.Value!.RefreshToken);
        return Ok(new { result.Value.AccessToken, result.Value.AccessTokenExpiresAt, result.Value.User });
    }

    // ── Classic email/password (kept for admin / migration) ───────────────────

    /// <summary>Register a new customer account.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var result = await _authService.RegisterAsync(request, GetIpAddress());
        if (!result.IsSuccess)
            return BadRequest(new { errors = result.Errors });

        SetRefreshTokenCookie(result.Value!.RefreshToken);
        return Ok(new { result.Value.AccessToken, result.Value.AccessTokenExpiresAt, result.Value.User });
    }

    /// <summary>Login with email and password.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var result = await _authService.LoginAsync(request, GetIpAddress());
        if (!result.IsSuccess)
            return Unauthorized(new { errors = result.Errors });

        SetRefreshTokenCookie(result.Value!.RefreshToken);
        return Ok(new { result.Value.AccessToken, result.Value.AccessTokenExpiresAt, result.Value.User });
    }

    // ── Token management ──────────────────────────────────────────────────────

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest(new { errors = new[] { "Refresh token not found." } });

        var result = await _authService.RefreshTokenAsync(refreshToken, GetIpAddress());
        if (!result.IsSuccess)
            return Unauthorized(new { errors = result.Errors });

        SetRefreshTokenCookie(result.Value!.RefreshToken);
        return Ok(new { result.Value.AccessToken, result.Value.AccessTokenExpiresAt, result.Value.User });
    }

    /// <summary>Logout — revokes the refresh token cookie.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(token))
        {
            await _authService.RevokeTokenAsync(token, GetIpAddress());
            Response.Cookies.Delete("refreshToken");
        }
        return Ok(new { message = "Logged out." });
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest? body)
    {
        var token = body?.RefreshToken ?? Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(token))
            return BadRequest(new { errors = new[] { "Token is required." } });

        var result = await _authService.RevokeTokenAsync(token, GetIpAddress());
        if (!result.IsSuccess)
            return BadRequest(new { errors = result.Errors });

        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "Token revoked." });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string GetIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
            return forwarded.ToString().Split(',')[0].Trim();
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private void SetRefreshTokenCookie(string token) =>
        Response.Cookies.Append("refreshToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
}
