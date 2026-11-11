using ChallengeTrackerApi.DTOs.Challenges;
using ChallengeTrackerApi.Models;

namespace ChallengeTrackerApi.Services.Interfaces;

public interface IChallengeService
{
    Task<IReadOnlyList<ChallengeListItemResponseDto>> ListAsync(
        Guid? currentUserId,
        ChallengeVisibility? visibility,
        ChallengeStatus? status,
        CancellationToken cancellationToken = default);

    Task<ChallengeDetailResponseDto> GetAsync(
        Guid id,
        Guid? currentUserId,
        CancellationToken cancellationToken = default);

    Task<ChallengeDetailResponseDto> CreateAsync(
        Guid ownerId,
        CreateChallengeRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<ChallengeDetailResponseDto> StartAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<ChallengeDetailResponseDto> CompleteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}
