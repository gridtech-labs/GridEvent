namespace GridTickets.Application.Interfaces;

/// <summary>
/// Notification gateway for OTP delivery.
/// Currently a stub — wire up SMS (MSG91/Twilio), Email (SendGrid), and WhatsApp (Meta API) later.
/// </summary>
public interface INotificationService
{
    Task SendOtpAsync(string mobile, string email, string otp);
}
