using ChallengeTrackerApi.Data;
using ChallengeTrackerApi.DTOs.Leaderboards;
using ChallengeTrackerApi.Errors;
using ChallengeTrackerApi.Models;
using ChallengeTrackerApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChallengeTrackerApi.Services.EF;

public sealed class LeaderboardService : ILeaderboardService
{
    private const string TotalPeriod = "total";
    private readonly ApplicationDbContext _db;

    public LeaderboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LeaderboardEntryResponseDto>> GetByChallengeAsync(
        Guid challengeId,
        string? period,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(period) &&
            !string.Equals(period, TotalPeriod, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Leaderboard period must be 'total'.");
        }

        var challengeExists = await _db.Challenges.AsNoTracking().AnyAsync(
            challenge => challenge.Id == challengeId,
            cancellationToken);

        if (!challengeExists)
        {
            throw new NotFoundException($"Challenge '{challengeId}' was not found.");
        }

        var participants = await _db.Memberships.AsNoTracking()
            .Where(membership =>
                membership.ChallengeId == challengeId &&
                membership.Status == MembershipStatus.Active)
            .Select(membership => new
            {
                membership.UserId,
                membership.User.DisplayName
            })
            .ToListAsync(cancellationToken);

        var participantIds = participants
            .Select(participant => participant.UserId)
            .ToList();

        var progressEntries = await _db.ProgressEntries.AsNoTracking()
            .Where(entry =>
                entry.ChallengeId == challengeId &&
                participantIds.Contains(entry.UserId))
            .Select(entry => new
            {
                entry.UserId,
                entry.Amount
            })
            .ToListAsync(cancellationToken);

        var totalsByUser = progressEntries
            .GroupBy(entry => entry.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(entry => entry.Amount));

        var leaderboard = participants
            .Select(participant => new
            {
                participant.UserId,
                participant.DisplayName,
                TotalProgress = totalsByUser.GetValueOrDefault(participant.UserId)
            })
            .OrderByDescending(entry => entry.TotalProgress)
            .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.UserId)
            .ToList();

        return leaderboard
            .Select((entry, index) => new LeaderboardEntryResponseDto(
                index + 1,
                entry.UserId,
                entry.DisplayName,
                entry.TotalProgress))
            .ToList();
    }
}
