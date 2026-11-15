using ChallengeTrackerApi.DTOs.Memberships;

namespace ChallengeTrackerApi.Services.Interfaces;

public interface IMembershipService
{
    Task<MembershipResponseDto> JoinAsync(
        Guid userId,
        CreateMembershipRequestDto dto,
        CancellationToken cancellationToken = default);

    Task LeaveAsync(
        Guid membershipId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<MembershipResponseDto> ApproveAsync(
        Guid membershipId,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}
