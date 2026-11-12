using ChallengeTrackerApi.Data;
using ChallengeTrackerApi.Errors;
using ChallengeTrackerApi.Models;
using ChallengeTrackerApi.Services.EF;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChallengeTrackerApi.Tests.Unit;

public sealed class LeaderboardServiceTests
{
    [Fact]
    public async Task GetByChallengeAsync_Total_RanksActiveParticipantsByProgress()
    {
        await using var database = await LeaderboardServiceTestDatabase.CreateAsync();
        var first = await database.AddParticipantAsync("First", MembershipStatus.Active);
        var second = await database.AddParticipantAsync("Second", MembershipStatus.Active);
        var noProgress = await database.AddParticipantAsync("No progress", MembershipStatus.Active);
        var pending = await database.AddParticipantAsync("Pending", MembershipStatus.Pending);
        await database.AddProgressAsync(first.Id, 10m, 15m);
        await database.AddProgressAsync(second.Id, 20m);
        await database.AddProgressAsync(pending.Id, 100m);

        var result = await database.Service.GetByChallengeAsync(
            database.ChallengeId,
            "total");

        Assert.Collection(
            result,
            entry =>
            {
                Assert.Equal(1, entry.Rank);
                Assert.Equal(first.Id, entry.UserId);
                Assert.Equal(25m, entry.TotalProgress);
            },
            entry =>
            {
                Assert.Equal(2, entry.Rank);
                Assert.Equal(second.Id, entry.UserId);
                Assert.Equal(20m, entry.TotalProgress);
            },
            entry =>
            {
                Assert.Equal(3, entry.Rank);
                Assert.Equal(noProgress.Id, entry.UserId);
                Assert.Equal(0m, entry.TotalProgress);
            });
    }

    [Fact]
    public async Task GetByChallengeAsync_TiedTotals_UsesDisplayNameAsTieBreaker()
    {
        await using var database = await LeaderboardServiceTestDatabase.CreateAsync();
        var zoe = await database.AddParticipantAsync("Zoe", MembershipStatus.Active);
        var amy = await database.AddParticipantAsync("Amy", MembershipStatus.Active);
        await database.AddProgressAsync(zoe.Id, 10m);
        await database.AddProgressAsync(amy.Id, 10m);

        var result = await database.Service.GetByChallengeAsync(
            database.ChallengeId,
            null);

        Assert.Equal(amy.Id, result[0].UserId);
        Assert.Equal(zoe.Id, result[1].UserId);
    }

    [Fact]
    public async Task GetByChallengeAsync_UnsupportedPeriod_ThrowsBadRequestException()
    {
        await using var database = await LeaderboardServiceTestDatabase.CreateAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            database.Service.GetByChallengeAsync(database.ChallengeId, "weekly"));
    }

    [Fact]
    public async Task GetByChallengeAsync_MissingChallenge_ThrowsNotFoundException()
    {
        await using var database = await LeaderboardServiceTestDatabase.CreateAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            database.Service.GetByChallengeAsync(Guid.NewGuid(), "total"));
    }

    private sealed class LeaderboardServiceTestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private int _loggedDay = 1;

        private LeaderboardServiceTestDatabase(
            SqliteConnection connection,
            ApplicationDbContext db,
            Guid challengeId)
        {
            _connection = connection;
            Db = db;
            ChallengeId = challengeId;
            Service = new LeaderboardService(db);
        }

        public ApplicationDbContext Db { get; }
        public LeaderboardService Service { get; }
        public Guid ChallengeId { get; }

        public static async Task<LeaderboardServiceTestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var db = new ApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var owner = NewUser("Owner");
            var challenge = new Challenge
            {
                Id = Guid.NewGuid(),
                OwnerId = owner.Id,
                Title = "Leaderboard test challenge",
                Visibility = ChallengeVisibility.Public,
                Status = ChallengeStatus.Running,
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 1, 31),
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.Users.Add(owner);
            db.Challenges.Add(challenge);
            await db.SaveChangesAsync();

            return new LeaderboardServiceTestDatabase(connection, db, challenge.Id);
        }

        public async Task<User> AddParticipantAsync(
            string displayName,
            MembershipStatus status)
        {
            var user = NewUser(displayName);
            Db.Users.Add(user);
            Db.Memberships.Add(new Membership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ChallengeId = ChallengeId,
                Status = status,
                JoinedAt = DateTimeOffset.UtcNow
            });
            await Db.SaveChangesAsync();

            return user;
        }

        public async Task AddProgressAsync(Guid userId, params decimal[] amounts)
        {
            foreach (var amount in amounts)
            {
                var loggedDate = new DateOnly(2026, 1, _loggedDay++);
                Db.ProgressEntries.Add(new ProgressEntry
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ChallengeId = ChallengeId,
                    Amount = amount,
                    LoggedAt = new DateTimeOffset(
                        loggedDate.ToDateTime(TimeOnly.MinValue),
                        TimeSpan.Zero),
                    LoggedDate = loggedDate,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            await Db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private static User NewUser(string displayName)
        {
            var email = $"{displayName.Replace(" ", "-").ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
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
