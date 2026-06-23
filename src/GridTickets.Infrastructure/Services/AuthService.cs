using GridTickets.Application.DTOs.Auth;
using GridTickets.Application.Interfaces;
using GridTickets.Domain.Common;
using GridTickets.Domain.Entities;
using GridTickets.Domain.Enums;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly GridTicketsDbContext _db;
    private readonly IOtpService _otpService;
    private readonly INotificationService _notifications;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        GridTicketsDbContext db,
        IOtpService otpService,
        INotificationService notifications)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _db = db;
        _otpService = otpService;
        _notifications = notifications;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, string ipAddress)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return Result<AuthResponse>.Failure("An account with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Status = UserStatus.Active,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return Result<AuthResponse>.Failure(result.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, Roles.Customer);
        return await BuildAuthResponseAsync(user, ipAddress);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Result<AuthResponse>.Failure("Invalid email or password.");

        if (user.Status == UserStatus.Suspended)
            return Result<AuthResponse>.Failure("Your account has been suspended. Please contact support.");

        if (user.Status == UserStatus.Inactive)
            return Result<AuthResponse>.Failure("Your account is inactive.");

        return await BuildAuthResponseAsync(user, ipAddress);
    }

    // ── OTP Auth ──────────────────────────────────────────────────────────────

    public async Task<Result> RequestOtpAsync(string email, string mobile)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            // Auto-create account — no password required
            var name = email.Split('@')[0];
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = Capitalize(name),
                LastName = string.Empty,
                PhoneNumber = mobile,
                Status = UserStatus.Active,
                EmailConfirmed = true,
                PhoneNumberConfirmed = false
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return Result.Failure(createResult.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(user, Roles.Customer);
        }
        else
        {
            // Update phone if it has changed
            if (!string.IsNullOrWhiteSpace(mobile) && user.PhoneNumber != mobile)
            {
                user.PhoneNumber = mobile;
                await _userManager.UpdateAsync(user);
            }
        }

        if (user.Status == UserStatus.Suspended)
            return Result.Failure("Your account has been suspended. Please contact support.");

        var otp = await _otpService.GenerateAndStoreAsync(email);
        await _notifications.SendOtpAsync(mobile, email, otp);

        return Result.Success();
    }

    public async Task<Result<AuthResponse>> VerifyOtpAsync(string email, string otp, string ipAddress)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return Result<AuthResponse>.Failure("Account not found. Please request a new OTP.");

        if (user.Status == UserStatus.Suspended)
            return Result<AuthResponse>.Failure("Your account has been suspended.");

        var valid = await _otpService.VerifyAndConsumeAsync(email, otp);
        if (!valid)
            return Result<AuthResponse>.Failure("Invalid or expired OTP. Please request a new one.");

        if (!user.PhoneNumberConfirmed)
        {
            user.PhoneNumberConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        return await BuildAuthResponseAsync(user, ipAddress);
    }

    public async Task<Result<AuthResponse>> SilentLoginAsync(string email, string mobile, string? fullName, string ipAddress)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            // Parse name: split on first space, fall back to email prefix
            var namePart = !string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : email.Split('@')[0];
            var spaceIdx = namePart.IndexOf(' ');
            var firstName = spaceIdx > 0 ? namePart[..spaceIdx] : namePart;
            var lastName  = spaceIdx > 0 ? namePart[(spaceIdx + 1)..] : string.Empty;

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = Capitalize(firstName),
                LastName = lastName,
                PhoneNumber = mobile,
                Status = UserStatus.Active,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return Result<AuthResponse>.Failure(createResult.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(user, Roles.Customer);
        }
        else
        {
            if (user.Status == UserStatus.Suspended)
                return Result<AuthResponse>.Failure("Your account has been suspended. Please contact support.");

            var changed = false;

            if (!string.IsNullOrWhiteSpace(mobile) && user.PhoneNumber != mobile)
            {
                user.PhoneNumber = mobile;
                user.PhoneNumberConfirmed = true;
                changed = true;
            }

            // Update name only if it was auto-generated from email prefix (no space = single word)
            if (!string.IsNullOrWhiteSpace(fullName) && !user.FirstName.Contains(' ') && string.IsNullOrWhiteSpace(user.LastName))
            {
                var spaceIdx = fullName.Trim().IndexOf(' ');
                if (spaceIdx > 0)
                {
                    user.FirstName = Capitalize(fullName[..spaceIdx]);
                    user.LastName  = fullName[(spaceIdx + 1)..];
                    changed = true;
                }
            }

            if (changed)
                await _userManager.UpdateAsync(user);
        }

        return await BuildAuthResponseAsync(user, ipAddress);
    }

    // ── Token helpers ─────────────────────────────────────────────────────────

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        var token = await _db.RefreshTokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token is null || !token.IsActive)
            return Result<AuthResponse>.Failure("Invalid or expired refresh token.");

        var newRefreshToken = _tokenService.GenerateRefreshToken(ipAddress);
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReplacedByToken = newRefreshToken.Token;
        token.User.RefreshTokens.Add(newRefreshToken);

        // Keep last 5 inactive tokens
        var old = token.User.RefreshTokens
            .Where(rt => !rt.IsActive)
            .OrderByDescending(rt => rt.CreatedAt)
            .Skip(5).ToList();
        foreach (var t in old) _db.RefreshTokens.Remove(t);

        await _db.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(token.User);
        var accessToken = _tokenService.GenerateAccessToken(token.User, roles);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = MapToUserDto(token.User, roles)
        });
    }

    public async Task<Result> RevokeTokenAsync(string refreshToken, string ipAddress)
    {
        var token = await _db.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token is null || !token.IsActive)
            return Result.Failure("Invalid or expired refresh token.");

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private async Task<Result<AuthResponse>> BuildAuthResponseAsync(ApplicationUser user, string ipAddress)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(ipAddress);

        // Detach the user entity so EF doesn't try to UPDATE it with a potentially
        // stale ConcurrencyStamp left behind by prior Identity SaveChanges calls.
        var entry = _db.Entry(user);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;

        refreshToken.UserId = user.Id;
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = MapToUserDto(user, roles)
        });
    }

    private static UserDto MapToUserDto(ApplicationUser user, IList<string> roles) => new()
    {
        Id = user.Id,
        Email = user.Email!,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        ProfilePictureUrl = user.ProfilePictureUrl,
        Roles = roles
    };

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}
