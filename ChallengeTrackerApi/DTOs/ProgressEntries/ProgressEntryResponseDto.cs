namespace ChallengeTrackerApi.DTOs.ProgressEntries;

public record ProgressEntryResponseDto(
    Guid Id,
    Guid UserId,
    Guid ChallengeId,
    decimal Amount,
    string? Note,
    DateTimeOffset LoggedAt,
    DateTimeOffset CreatedAt
);
