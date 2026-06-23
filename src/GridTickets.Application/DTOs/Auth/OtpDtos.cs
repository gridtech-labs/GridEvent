using System.ComponentModel.DataAnnotations;

namespace GridTickets.Application.DTOs.Auth;

public record RequestOtpRequest(
    [Required, EmailAddress]
    string Email,

    [Required, Phone, MaxLength(15)]
    string Mobile
);

public record VerifyOtpRequest(
    [Required, EmailAddress]
    string Email,

    [Required, StringLength(6, MinimumLength = 6)]
    string Otp
);

/// <summary>Booking-flow: find-or-create user and issue tokens immediately (no OTP). OTP will be added later.</summary>
public record SilentLoginRequest(
    [Required, EmailAddress]
    string Email,

    [Required, Phone, MaxLength(15)]
    string Mobile,

    string? FullName
);
