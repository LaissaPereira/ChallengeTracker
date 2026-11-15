using ChallengeTrackerApi.DTOs.ProgressEntries;

namespace ChallengeTrackerApi.Services.Interfaces;

public interface IProgressEntryService
{
    Task<ProgressEntryResponseDto> CreateAsync(
        Guid userId,
        CreateProgressEntryRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<ProgressEntryResponseDto> UpdateAsync(
        Guid entryId,
        Guid userId,
        UpdateProgressEntryRequestDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid entryId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
