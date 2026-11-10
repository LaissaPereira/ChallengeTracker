using System.Threading.RateLimiting;

namespace ChallengeTrackerApi.Settings;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public RateLimitPolicyOptions Login { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60 };

    public RateLimitPolicyOptions Register { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60 };

    public RateLimitPolicyOptions PerUser { get; set; } = new() { PermitLimit = 60, WindowSeconds = 60 };
}

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }

    public void Validate(string name)
    {
        if (PermitLimit < 1 || WindowSeconds < 1)
        {
            throw new InvalidOperationException(
                $"RateLimiting:{name} requires PermitLimit and WindowSeconds greater than 0.");
        }
    }

    public FixedWindowRateLimiterOptions ToLimiterOptions() => new()
    {
        PermitLimit = PermitLimit,
        Window = TimeSpan.FromSeconds(WindowSeconds),
        QueueLimit = 0
    };
}
