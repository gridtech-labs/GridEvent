using GridTickets.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace GridTickets.Infrastructure.Services;

/// <summary>
/// Stub notification service — logs OTPs to console.
/// TODO: Wire up real providers:
///   SMS     → MSG91 / Twilio
///   Email   → SendGrid / SMTP
///   WhatsApp → Meta Cloud API / Interakt / WATI
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger) => _logger = logger;

    public Task SendOtpAsync(string mobile, string email, string otp)
    {
        _logger.LogWarning(
            "[OTP] Email={Email} Mobile={Mobile} OTP={Otp}  (valid 10 min) — SMS/Email/WhatsApp not wired yet",
            email, mobile, otp);

        return Task.CompletedTask;
    }
}
