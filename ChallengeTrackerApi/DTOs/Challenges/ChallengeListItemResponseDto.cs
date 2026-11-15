using ChallengeTrackerApi.Models;

namespace ChallengeTrackerApi.DTOs.Challenges;

public record ChallengeListItemResponseDto(
    Guid Id,
    string Title,
    string? Description,
    ChallengeVisibility Visibility,
    ChallengeStatus Status,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid OwnerId,
    string OwnerDisplayName,
    int ActiveMemberCount,
    MembershipStatus? CurrentUserMembershipStatus
);
