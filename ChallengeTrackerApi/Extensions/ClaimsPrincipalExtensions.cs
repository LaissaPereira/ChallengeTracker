using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ChallengeTrackerApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : throw new InvalidOperationException("Invalid user ID in token");
    }
}