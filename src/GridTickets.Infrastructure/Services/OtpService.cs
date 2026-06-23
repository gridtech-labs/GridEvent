using GridTickets.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace GridTickets.Infrastructure.Services;

public class OtpService : IOtpService
{
    private readonly IMemoryCache _cache;
    private readonly IConnectionMultiplexer? _redis;
    private const int TtlMinutes = 10;

    public OtpService(IMemoryCache cache, IConnectionMultiplexer? redis = null)
    {
        _cache = cache;
        _redis = redis;
    }

    public async Task<string> GenerateAndStoreAsync(string key)
    {
        var otp = Random.Shared.Next(100_000, 999_999).ToString();
        var cacheKey = Key(key);
        var ttl = TimeSpan.FromMinutes(TtlMinutes);

        if (_redis is not null)
            await _redis.GetDatabase().StringSetAsync(cacheKey, otp, ttl);
        else
            _cache.Set(cacheKey, otp, ttl);

        return otp;
    }

    public async Task<bool> VerifyAndConsumeAsync(string key, string otp)
    {
        var cacheKey = Key(key);

        if (_redis is not null)
        {
            var db = _redis.GetDatabase();
            var stored = await db.StringGetAsync(cacheKey);
            if (!stored.HasValue || stored.ToString() != otp) return false;
            await db.KeyDeleteAsync(cacheKey);
            return true;
        }

        if (_cache.TryGetValue<string>(cacheKey, out var cached) && cached == otp)
        {
            _cache.Remove(cacheKey);
            return true;
        }

        return false;
    }

    private static string Key(string raw) => $"otp:{raw.Trim().ToLowerInvariant()}";
}
