using Microsoft.Extensions.Caching.Memory;

namespace Camstep.Services;

public class LoginThrottle
{
    public const int MaxPerEmail = 5;
    public const int MaxPerIp = 20;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _cache;
    private readonly object _lock = new();

    public LoginThrottle(IMemoryCache cache) => _cache = cache;

    private sealed class Counter
    {
        public int Failures;
        public DateTime FirstFailureUtc;
        public DateTime? LockedUntilUtc;
    }

    public TimeSpan? LockedFor(string email, string ip)
    {
        var now = DateTime.UtcNow;
        var remaining = new[] { Get("e:" + email), Get("i:" + ip) }
            .Where(c => c?.LockedUntilUtc > now)
            .Select(c => c!.LockedUntilUtc!.Value - now)
            .DefaultIfEmpty(TimeSpan.Zero)
            .Max();

        return remaining > TimeSpan.Zero ? remaining : null;
    }

    public int RegisterFailure(string email, string ip)
    {
        lock (_lock)
        {
            var byEmail = Increment("e:" + email, MaxPerEmail);
            Increment("i:" + ip, MaxPerIp);
            return Math.Max(0, MaxPerEmail - byEmail.Failures);
        }
    }

    public void Reset(string email) => _cache.Remove("login:e:" + email);

    private Counter? Get(string key) => _cache.Get<Counter>("login:" + key);

    private Counter Increment(string key, int max)
    {
        var now = DateTime.UtcNow;
        var counter = Get(key);

        var windowExpired = counter != null && counter.LockedUntilUtc is null && now - counter.FirstFailureUtc > Window;
        var lockExpired = counter?.LockedUntilUtc <= now;
        if (counter == null || windowExpired || lockExpired)
            counter = new Counter { FirstFailureUtc = now };

        counter.Failures++;
        if (counter.Failures >= max)
            counter.LockedUntilUtc = now + Window;

        _cache.Set("login:" + key, counter, Window + TimeSpan.FromMinutes(1));
        return counter;
    }
}
