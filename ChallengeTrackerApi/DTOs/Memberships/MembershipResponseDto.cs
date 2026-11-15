using ChallengeTrackerApi.Models;

namespace ChallengeTrackerApi.DTOs.Memberships;

public record MembershipResponseDto(
    Guid Id,
    Guid UserId,
    Guid ChallengeId,
    MembershipStatus Status,
    DateTimeOffset JoinedAt
);
