namespace ChallengeTrackerApi.DTOs.Leaderboards;

public record LeaderboardEntryResponseDto(
    int Rank,
    Guid UserId,
    string DisplayName,
    decimal TotalProgress);
