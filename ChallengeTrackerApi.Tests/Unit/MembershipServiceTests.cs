using ChallengeTrackerApi.Data;
using ChallengeTrackerApi.DTOs.Memberships;
using ChallengeTrackerApi.Errors;
using ChallengeTrackerApi.Models;
using ChallengeTrackerApi.Services.EF;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChallengeTrackerApi.Tests.Unit;

public sealed class MembershipServiceTests
{
    [Fact]
    public async Task JoinAsync_PublicChallenge_CreatesActiveMembership()
    {
        await using var database = await MembershipServiceTestDatabase.CreateAsync(ChallengeVisibility.Public);

        var result = await database.Service.JoinAsync(
            database.MemberId,
            new CreateMembershipRequestDto(database.ChallengeId));

        Assert.Equal(MembershipStatus.Active, result.Status);
    }

    [Fact]
    public async Task JoinAsync_PrivateChallenge_CreatesPendingMembership()
    {
        await using var database = await MembershipServiceTestDatabase.CreateAsync(ChallengeVisibility.Private);

        var result = await database.Service.JoinAsync(
            database.MemberId,
            new CreateMembershipRequestDto(database.ChallengeId));

        Assert.Equal(MembershipStatus.Pending, result.Status);
    }

    [Fact]
    public async Task JoinAsync_ExistingMembership_ThrowsConflictException()
    {
        await using var database = await MembershipServiceTestDatabase.CreateAsync(ChallengeVisibility.Public);
        await database.AddMembershipAsync(MembershipStatus.Active);

        await Assert.ThrowsAsync<ConflictException>(() =>
            database.Service.JoinAsync(
                database.MemberId,
                new CreateMembershipRequestDto(database.ChallengeId)));
    }

    [Fact]
    public async Task JoinAsync_OwnerJoiningOwnChallenge_ThrowsConflictException()
    {
        await using var database = await MembershipServiceTestDatabase.CreateAsync(ChallengeVisibility.Public);

        await Assert.ThrowsAsync<ConflictException>(() =>
            database.Service.JoinAsync(
                database.OwnerId,
                new CreateMembershipRequestDto(database.ChallengeId)));
    }

    [Fact]
    public async Task JoinAsync_MissingChallenge_ThrowsNotFoundException()
    {
        await using var database = await MembershipServiceTestDatabase.CreateAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            database.Service.JoinAsync(
                database.MemberId,
                new CreateMembershipRequestDto(Guid.NewGuid())));
    }

    [Fact]
    public async Task LeaveAsync_OwnMembership_RemovesMembership()
    {
        await using var database = await MembershipServiceTestDatabase.CreateAsync(ChallengeVisibility.Public);
        var membership = await database.AddMembershipAsync(MembershipStatus.Active);

        await database.Service.LeaveAsync(membership.Id, database.MemberId);

        Assert.False(await database.Db.Memberships.AnyAsync());
    }

    [Fact]
    public async Task LeaveAsync_AnotherUsersMembership_ThrowsForbiddenException()
    {
        await using var database = await MembershipServiceTestDatabase.CreateAsync(ChallengeVisibility.Public);
        var membership = await database.AddMembershipAsync(MembershipStatus.Active);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            database.Service.LeaveAsync(membership.Id, database.OtherUserId));
    }

    [Fact]
    public async Task ApproveAsync_PendingMembershipByOwner_ChangesStatusToActive()
    {
        await using var database = await MembershipServiceTestDatabase.CreateAsync(ChallengeVisibility.Private);
        var membership = await database.AddMembershipAsync(MembershipStatus.Pending);

        var result = await database.Service.ApproveAsync(membership.Id, database.OwnerId);

        Assert.Equal(MembershipStatus.Active, result.Status);
    }

    [Fact]
    public async Task ApproveAsync_ByNonOwner_ThrowsForbiddenException()
    {
        await using var database = await MembershipServiceTestDatabase.CreateAsync(ChallengeVisibility.Private);
        var membership = await database.AddMembershipAsync(MembershipStatus.Pending);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            database.Service.ApproveAsync(membership.Id, database.OtherUserId));
    }

    [Fact]
    public async Task ApproveAsync_ActiveMembership_ThrowsConflictException()
    {
        await using var database = await MembershipServiceTestDatabase.CreateAsync(ChallengeVisibility.Public);
        var membership = await database.AddMembershipAsync(MembershipStatus.Active);

        await Assert.ThrowsAsync<ConflictException>(() =>
            database.Service.ApproveAsync(membership.Id, database.OwnerId));
    }

    [Fact]
    public async Task ApproveAsync_MissingMembership_ThrowsNotFoundException()
    {
        await using var database = await MembershipServiceTestDatabase.CreateAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            database.Service.ApproveAsync(Guid.NewGuid(), database.OwnerId));
    }

    private sealed class MembershipServiceTestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private MembershipServiceTestDatabase(
            SqliteConnection connection,
            ApplicationDbContext db,
            Guid ownerId,
            Guid memberId,
            Guid otherUserId,
            Guid challengeId)
        {
            _connection = connection;
            Db = db;
            OwnerId = ownerId;
            MemberId = memberId;
            OtherUserId = otherUserId;
            ChallengeId = challengeId;
            Service = new MembershipService(db);
        }

        public ApplicationDbContext Db { get; }
        public MembershipService Service { get; }
        public Guid OwnerId { get; }
        public Guid MemberId { get; }
        public Guid OtherUserId { get; }
        public Guid ChallengeId { get; }

        public static async Task<MembershipServiceTestDatabase> CreateAsync(
            ChallengeVisibility? visibility = null)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var db = new ApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var owner = NewUser("Owner");
            var member = NewUser("Member");
            var otherUser = NewUser("Other");
            db.Users.AddRange(owner, member, otherUser);

            var challengeId = Guid.Empty;
            if (visibility.HasValue)
            {
                var challenge = new Challenge
                {
                    Id = Guid.NewGuid(),
                    OwnerId = owner.Id,
                    Title = "Membership test challenge",
                    Visibility = visibility.Value,
                    Status = ChallengeStatus.Open,
                    StartDate = new DateOnly(2026, 1, 1),
                    EndDate = new DateOnly(2026, 1, 31),
                    CreatedAt = DateTimeOffset.UtcNow
                };

                challengeId = challenge.Id;
                db.Challenges.Add(challenge);
            }

            await db.SaveChangesAsync();

            return new MembershipServiceTestDatabase(
                connection,
                db,
                owner.Id,
                member.Id,
                otherUser.Id,
                challengeId);
        }

        public async Task<Membership> AddMembershipAsync(MembershipStatus status)
        {
            var membership = new Membership
            {
                Id = Guid.NewGuid(),
                UserId = MemberId,
                ChallengeId = ChallengeId,
                Status = status,
                JoinedAt = DateTimeOffset.UtcNow
            };

            Db.Memberships.Add(membership);
            await Db.SaveChangesAsync();

            return membership;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private static User NewUser(string displayName)
        {
            var email = $"{displayName.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
            return new User
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                DisplayName = displayName
            };
        }
    }
}
