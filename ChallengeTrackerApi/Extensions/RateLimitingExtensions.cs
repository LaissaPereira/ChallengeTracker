using System.Globalization;
using System.Threading.RateLimiting;
using ChallengeTrackerApi.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ChallengeTrackerApi.Extensions;

public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string Register = "register";
    public const string PerUser = "per-user";
}

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new();

        // Fail at startup on a bad configuration instead of on the first request.
        settings.Login.Validate(nameof(settings.Login));
        settings.Register.Validate(nameof(settings.Register));
        settings.PerUser.Validate(nameof(settings.PerUser));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Anonymous endpoints are limited per client IP, so one client cannot lock out everyone else.
            options.AddPolicy(RateLimitPolicies.Login, context => PartitionByIp(context, settings.Login));
            options.AddPolicy(RateLimitPolicies.Register, context => PartitionByIp(context, settings.Register));

            // Authenticated endpoints are limited per user, falling back to the IP when there is no token.
            options.AddPolicy(RateLimitPolicies.PerUser, context =>
            {
                var userId = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                return userId is null
                    ? PartitionByIp(context, settings.PerUser)
                    : RateLimitPartition.GetFixedWindowLimiter($"user:{userId}", _ => settings.PerUser.ToLimiterOptions());
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    httpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("RateLimiting")
                    .LogWarning(
                        "Rate limit exceeded for {Method} {Path} from {IP}",
                        httpContext.Request.Method,
                        httpContext.Request.Path,
                        httpContext.Connection.RemoteIpAddress);

                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                await httpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Detail = "Rate limit exceeded. Please try again later."
                    }
                });
            };
        });

        return services;
    }

    private static RateLimitPartition<string> PartitionByIp(HttpContext context, RateLimitPolicyOptions policy) =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
            _ => policy.ToLimiterOptions());
}
