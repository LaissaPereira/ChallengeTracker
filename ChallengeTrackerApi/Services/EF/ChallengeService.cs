using ChallengeTrackerApi.Data;
using ChallengeTrackerApi.DTOs.Challenges;
using ChallengeTrackerApi.Errors;
using ChallengeTrackerApi.Models;
using ChallengeTrackerApi.Services.Interfaces;
using ChallengeEntity = ChallengeTrackerApi.Models.Challenge;
using Microsoft.EntityFrameworkCore;

namespace ChallengeTrackerApi.Services.EF;

public sealed class ChallengeService : IChallengeService
{
    private readonly ApplicationDbContext _db;

    public ChallengeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ChallengeListItemResponseDto>> ListAsync(
        Guid? currentUserId,
        ChallengeVisibility? visibility,
        ChallengeStatus? status,
        bool joinedOnly,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Challenges.AsNoTracking();

        if (joinedOnly)
        {
            if (!currentUserId.HasValue)
            {
                throw new ArgumentException(
                    "A current user is required when listing joined challenges.",
                    nameof(currentUserId));
            }

            var userId = currentUserId.Value;
            query = query.Where(challenge =>
                challenge.OwnerId == userId ||
                challenge.Memberships.Any(membership =>
                    membership.UserId == userId &&
                    (membership.Status == MembershipStatus.Active ||
                     membership.Status == MembershipStatus.Pending)));
        }

        if (visibility.HasValue)
        {
            query = query.Where(challenge => challenge.Visibility == visibility.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(challenge => challenge.Status == status.Value);
        }

        return await query
            .OrderBy(challenge => challenge.StartDate)
            .ThenBy(challenge => challenge.Id)
            .Select(challenge => new ChallengeListItemResponseDto(
                challenge.Id,
                challenge.Title,
                challenge.Description,
                challenge.Visibility,
                challenge.Status,
                challenge.StartDate,
                challenge.EndDate,
                challenge.OwnerId,
                challenge.Owner.DisplayName,
                challenge.Memberships.Count(membership => membership.Status == MembershipStatus.Active),
                challenge.Memberships
                    .Where(membership => currentUserId.HasValue && membership.UserId == currentUserId.Value)
                    .Select(membership => (MembershipStatus?)membership.Status)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task<ChallengeDetailResponseDto> GetAsync(
        Guid id,
        Guid? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var challenge = await LoadDetailAsync(id, currentUserId, cancellationToken)
            ?? throw new NotFoundException($"Challenge '{id}' was not found.");

        return ToDetailResponse(challenge, currentUserId);
    }

    public async Task<ChallengeDetailResponseDto> CreateAsync(
        Guid ownerId,
        CreateChallengeRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var title = dto.Title.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new BadRequestException("Challenge title is required.");
        }

        if (dto.StartDate > dto.EndDate)
        {
            throw new BadRequestException("Start date must be on or before end date.");
        }

        var challenge = new ChallengeEntity
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = title,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            Visibility = dto.Visibility!.Value,
            Status = ChallengeStatus.Open,
            StartDate = dto.StartDate!.Value,
            EndDate = dto.EndDate!.Value,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Challenges.Add(challenge);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(challenge.Id, ownerId, cancellationToken);
    }

    public Task<ChallengeDetailResponseDto> StartAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            id,
            ownerId,
            ChallengeStatus.Open,
            ChallengeStatus.Running,
            "Only an open challenge can be started.",
            cancellationToken);

    public Task<ChallengeDetailResponseDto> CompleteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            id,
            ownerId,
            ChallengeStatus.Running,
            ChallengeStatus.Completed,
            "Only a running challenge can be completed.",
            cancellationToken);

    private async Task<ChallengeDetailResponseDto> TransitionAsync(
        Guid id,
        Guid ownerId,
        ChallengeStatus requiredStatus,
        ChallengeStatus nextStatus,
        string conflictMessage,
        CancellationToken cancellationToken)
    {
        var challenge = await _db.Challenges.FirstOrDefaultAsync(
            challenge => challenge.Id == id,
            cancellationToken)
            ?? throw new NotFoundException($"Challenge '{id}' was not found.");

        if (challenge.OwnerId != ownerId)
        {
            throw new ForbiddenException("Only the challenge owner can change its status.");
        }

        if (challenge.Status != requiredStatus)
        {
            throw new ConflictException(conflictMessage);
        }

        challenge.Status = nextStatus;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(challenge.Id, ownerId, cancellationToken);
    }

    private Task<ChallengeEntity?> LoadDetailAsync(
        Guid id,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var query = _db.Challenges
            .Include(challenge => challenge.Owner)
            .Include(challenge => challenge.Memberships)
                .ThenInclude(membership => membership.User)
            .Include(challenge => challenge.ProgressEntries.Where(progressEntry =>
                currentUserId.HasValue &&
                progressEntry.UserId == currentUserId.Value &&
                progressEntry.LoggedDate == today))
            .AsSplitQuery()
            .AsNoTracking();

        return query.FirstOrDefaultAsync(challenge => challenge.Id == id, cancellationToken);
    }

    private static ChallengeDetailResponseDto ToDetailResponse(
        ChallengeEntity challenge,
        Guid? currentUserId)
    {
        var membership = challenge.Memberships.FirstOrDefault(
            existingMembership => existingMembership.UserId == currentUserId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayProgress = challenge.ProgressEntries.FirstOrDefault(
            progressEntry =>
                progressEntry.UserId == currentUserId &&
                progressEntry.LoggedDate == today);

        return new ChallengeDetailResponseDto(
            challenge.Id,
            challenge.Title,
            challenge.Description,
            challenge.Visibility,
            challenge.Status,
            challenge.StartDate,
            challenge.EndDate,
            challenge.CreatedAt,
            challenge.OwnerId,
            challenge.Owner.DisplayName,
            challenge.Memberships
                .OrderBy(existingMembership => existingMembership.JoinedAt)
                .ThenBy(existingMembership => existingMembership.Id)
                .Select(existingMembership => new ChallengeMemberResponseDto(
                    existingMembership.Id,
                    existingMembership.UserId,
                    existingMembership.User.DisplayName,
                    existingMembership.Status,
                    existingMembership.JoinedAt))
                .ToList(),
            new CurrentUserChallengeStateDto(
                membership?.Id,
                membership?.Status,
                todayProgress is null
                    ? null
                    : new ProgressEntrySummaryDto(
                        todayProgress.Id,
                        todayProgress.Amount,
                        todayProgress.Note,
                        todayProgress.LoggedAt)));
    }
}
