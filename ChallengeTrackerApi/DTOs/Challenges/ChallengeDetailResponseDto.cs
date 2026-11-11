using ChallengeTrackerApi.Models;

namespace ChallengeTrackerApi.DTOs.Challenges;

public record ChallengeDetailResponseDto(
    Guid Id,
    string Title,
    string? Description,
    ChallengeVisibility Visibility,
    ChallengeStatus Status,
    DateOnly StartDate,
    DateOnly EndDate,
    DateTimeOffset CreatedAt,
    Guid OwnerId,
    string OwnerDisplayName,
    IReadOnlyList<ChallengeMemberResponseDto> Members,
    CurrentUserChallengeStateDto CurrentUser
);

public record ChallengeMemberResponseDto(
    Guid MembershipId,
    Guid UserId,
    string DisplayName,
    MembershipStatus Status,
    DateTimeOffset JoinedAt
);

public record CurrentUserChallengeStateDto(
    Guid? MembershipId,
    MembershipStatus? MembershipStatus,
    ProgressEntrySummaryDto? TodayProgress
);

public record ProgressEntrySummaryDto(
    Guid Id,
    decimal Amount,
    string? Note,
    DateTimeOffset LoggedAt
);
