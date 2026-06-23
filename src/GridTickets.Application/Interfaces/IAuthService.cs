using GridTickets.Application.DTOs.Auth;
using GridTickets.Domain.Common;

namespace GridTickets.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, string ipAddress);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress);
    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task<Result> RevokeTokenAsync(string refreshToken, string ipAddress);

    /// <summary>Find or create account by email + mobile, generate and dispatch OTP.</summary>
    Task<Result> RequestOtpAsync(string email, string mobile);

    /// <summary>Verify the OTP and issue JWT + refresh token if valid.</summary>
    Task<Result<AuthResponse>> VerifyOtpAsync(string email, string otp, string ipAddress);

    /// <summary>Booking flow: find-or-create user and issue tokens immediately (no OTP required until SMS/email is wired).</summary>
    Task<Result<AuthResponse>> SilentLoginAsync(string email, string mobile, string? fullName, string ipAddress);
}
