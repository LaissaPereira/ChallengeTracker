using ChallengeTrackerApi.Services.Interfaces;

namespace ChallengeTrackerApi.Endpoints;

public static class LeaderboardEndpoints
{
    public static void MapLeaderboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/leaderboards")
            .WithTags("Leaderboards")
            .RequireAuthorization();

        group.MapGet("/challenges/{id:guid}", async (
            Guid id,
            string? period,
            ILeaderboardService service,
            CancellationToken cancellationToken) =>
        {
            var leaderboard = await service.GetByChallengeAsync(
                id,
                period,
                cancellationToken);

            return Results.Ok(leaderboard);
        })
        .WithSummary("Get a challenge leaderboard");
    }
}
