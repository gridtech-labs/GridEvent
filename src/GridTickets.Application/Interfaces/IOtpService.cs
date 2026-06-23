namespace GridTickets.Application.Interfaces;

public interface IOtpService
{
    /// <summary>Generate a 6-digit OTP, store it against the key with a 10-minute TTL, and return it.</summary>
    Task<string> GenerateAndStoreAsync(string key);

    /// <summary>Return true if the supplied OTP matches the stored one, then delete it (one-time use).</summary>
    Task<bool> VerifyAndConsumeAsync(string key, string otp);
}
