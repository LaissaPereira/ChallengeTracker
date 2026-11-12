using ChallengeTrackerApi.DTOs.Leaderboards;

namespace ChallengeTrackerApi.Services.Interfaces;

public interface ILeaderboardService
{
    Task<IReadOnlyList<LeaderboardEntryResponseDto>> GetByChallengeAsync(
        Guid challengeId,
        string? period,
        CancellationToken cancellationToken = default);
}
