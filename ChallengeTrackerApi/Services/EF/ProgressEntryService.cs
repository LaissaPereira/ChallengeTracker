using ChallengeTrackerApi.Data;
using ChallengeTrackerApi.DTOs.ProgressEntries;
using ChallengeTrackerApi.Errors;
using ChallengeTrackerApi.Models;
using ChallengeTrackerApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChallengeTrackerApi.Services.EF;

public sealed class ProgressEntryService : IProgressEntryService
{
    private static readonly TimeSpan EditWindow = TimeSpan.FromHours(24);
    private readonly ApplicationDbContext _db;

    public ProgressEntryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ProgressEntryResponseDto> CreateAsync(
        Guid userId,
        CreateProgressEntryRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var challengeId = dto.ChallengeId!.Value;
        var challenge = await _db.Challenges.AsNoTracking().FirstOrDefaultAsync(
            challenge => challenge.Id == challengeId,
            cancellationToken)
            ?? throw new NotFoundException($"Challenge '{challengeId}' was not found.");

        ValidateAmount(dto.Amount);
        await EnsureActiveMembershipAsync(userId, challengeId, cancellationToken);

        var loggedAt = (dto.LoggedAt ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var loggedDate = DateOnly.FromDateTime(loggedAt.UtcDateTime);
        ValidateChallengeWindow(challenge, loggedDate);

        await EnsureEntryDoesNotExistAsync(
            userId,
            challengeId,
            loggedDate,
            null,
            cancellationToken);

        var entry = new ProgressEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ChallengeId = challengeId,
            Amount = dto.Amount,
            Note = NormalizeNote(dto.Note),
            LoggedAt = loggedAt,
            LoggedDate = loggedDate,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.ProgressEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(entry);
    }

    public async Task<ProgressEntryResponseDto> UpdateAsync(
        Guid entryId,
        Guid userId,
        UpdateProgressEntryRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var entry = await _db.ProgressEntries
            .Include(progressEntry => progressEntry.Challenge)
            .FirstOrDefaultAsync(
                progressEntry => progressEntry.Id == entryId,
                cancellationToken)
            ?? throw new NotFoundException($"Progress entry '{entryId}' was not found.");

        if (entry.UserId != userId)
        {
            throw new ForbiddenException("Users can only update their own progress entries.");
        }

        if (DateTimeOffset.UtcNow - entry.CreatedAt > EditWindow)
        {
            throw new ConflictException("Progress entries can only be edited within 24 hours of creation.");
        }

        ValidateAmount(dto.Amount);
        await EnsureActiveMembershipAsync(userId, entry.ChallengeId, cancellationToken);

        var loggedAt = dto.LoggedAt!.Value.ToUniversalTime();
        var loggedDate = DateOnly.FromDateTime(loggedAt.UtcDateTime);
        ValidateChallengeWindow(entry.Challenge, loggedDate);

        await EnsureEntryDoesNotExistAsync(
            userId,
            entry.ChallengeId,
            loggedDate,
            entry.Id,
            cancellationToken);

        entry.Amount = dto.Amount;
        entry.Note = NormalizeNote(dto.Note);
        entry.LoggedAt = loggedAt;
        entry.LoggedDate = loggedDate;

        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(entry);
    }

    public async Task DeleteAsync(
        Guid entryId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _db.ProgressEntries.FirstOrDefaultAsync(
            progressEntry => progressEntry.Id == entryId,
            cancellationToken)
            ?? throw new NotFoundException($"Progress entry '{entryId}' was not found.");

        if (entry.UserId != userId)
        {
            throw new ForbiddenException("Users can only delete their own progress entries.");
        }

        _db.ProgressEntries.Remove(entry);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureActiveMembershipAsync(
        Guid userId,
        Guid challengeId,
        CancellationToken cancellationToken)
    {
        var isActiveMember = await _db.Memberships.AnyAsync(
            membership =>
                membership.UserId == userId &&
                membership.ChallengeId == challengeId &&
                membership.Status == MembershipStatus.Active,
            cancellationToken);

        if (!isActiveMember)
        {
            throw new ForbiddenException("Only active challenge members can log progress.");
        }
    }

    private async Task EnsureEntryDoesNotExistAsync(
        Guid userId,
        Guid challengeId,
        DateOnly loggedDate,
        Guid? excludedEntryId,
        CancellationToken cancellationToken)
    {
        var entryExists = await _db.ProgressEntries.AnyAsync(
            progressEntry =>
                progressEntry.UserId == userId &&
                progressEntry.ChallengeId == challengeId &&
                progressEntry.LoggedDate == loggedDate &&
                (!excludedEntryId.HasValue || progressEntry.Id != excludedEntryId.Value),
            cancellationToken);

        if (entryExists)
        {
            throw new ConflictException("Only one progress entry is allowed per challenge per day.");
        }
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new BadRequestException("Progress amount must be greater than zero.");
        }
    }

    private static void ValidateChallengeWindow(Challenge challenge, DateOnly loggedDate)
    {
        if (!challenge.IsWithinWindow(loggedDate))
        {
            throw new BadRequestException("Progress date must be within the challenge date range.");
        }
    }

    private static string? NormalizeNote(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim();

    private static ProgressEntryResponseDto ToResponse(ProgressEntry entry) =>
        new(
            entry.Id,
            entry.UserId,
            entry.ChallengeId,
            entry.Amount,
            entry.Note,
            entry.LoggedAt,
            entry.CreatedAt);
}
