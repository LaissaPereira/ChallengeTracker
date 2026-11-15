using ChallengeTrackerApi.Data;
using ChallengeTrackerApi.DTOs.Memberships;
using ChallengeTrackerApi.Errors;
using ChallengeTrackerApi.Models;
using ChallengeTrackerApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChallengeTrackerApi.Services.EF;

public sealed class MembershipService : IMembershipService
{
    private readonly ApplicationDbContext _db;

    public MembershipService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<MembershipResponseDto> JoinAsync(
        Guid userId,
        CreateMembershipRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var challengeId = dto.ChallengeId!.Value;
        var challenge = await _db.Challenges.AsNoTracking().FirstOrDefaultAsync(
            challenge => challenge.Id == challengeId,
            cancellationToken)
            ?? throw new NotFoundException($"Challenge '{challengeId}' was not found.");

        if (challenge.OwnerId == userId)
        {
            throw new ConflictException("A challenge owner cannot join their own challenge.");
        }

        var membershipExists = await _db.Memberships.AnyAsync(
            membership =>
                membership.UserId == userId &&
                membership.ChallengeId == challengeId,
            cancellationToken);

        if (membershipExists)
        {
            throw new ConflictException("The user has already joined this challenge.");
        }

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ChallengeId = challengeId,
            Status = challenge.Visibility == ChallengeVisibility.Public
                ? MembershipStatus.Active
                : MembershipStatus.Pending,
            JoinedAt = DateTimeOffset.UtcNow
        };

        _db.Memberships.Add(membership);
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(membership);
    }

    public async Task LeaveAsync(
        Guid membershipId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var membership = await _db.Memberships.FirstOrDefaultAsync(
            membership => membership.Id == membershipId,
            cancellationToken)
            ?? throw new NotFoundException($"Membership '{membershipId}' was not found.");

        if (membership.UserId != userId)
        {
            throw new ForbiddenException("Users can only leave their own memberships.");
        }

        _db.Memberships.Remove(membership);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MembershipResponseDto> ApproveAsync(
        Guid membershipId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var membership = await _db.Memberships
            .Include(existingMembership => existingMembership.Challenge)
            .FirstOrDefaultAsync(
                existingMembership => existingMembership.Id == membershipId,
                cancellationToken)
            ?? throw new NotFoundException($"Membership '{membershipId}' was not found.");

        if (membership.Challenge.OwnerId != ownerId)
        {
            throw new ForbiddenException("Only the challenge owner can approve memberships.");
        }

        if (membership.Status != MembershipStatus.Pending)
        {
            throw new ConflictException("Only a pending membership can be approved.");
        }

        membership.Status = MembershipStatus.Active;
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(membership);
    }

    private static MembershipResponseDto ToResponse(Membership membership) =>
        new(
            membership.Id,
            membership.UserId,
            membership.ChallengeId,
            membership.Status,
            membership.JoinedAt);
}
