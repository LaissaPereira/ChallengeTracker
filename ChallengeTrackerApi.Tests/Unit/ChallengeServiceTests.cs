using ChallengeTrackerApi.Data;
using ChallengeTrackerApi.Errors;
using ChallengeTrackerApi.Models;
using ChallengeTrackerApi.Services.EF;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChallengeTrackerApi.Tests.Unit;

public sealed class ChallengeServiceTests
{
    [Fact]
    public async Task StartAsync_OpenChallengeOwnedByUser_ChangesStatusToRunning()
    {
        await using var database = await ChallengeServiceTestDatabase.CreateAsync(ChallengeStatus.Open);

        var result = await database.Service.StartAsync(database.ChallengeId, database.OwnerId);

        Assert.Equal(ChallengeStatus.Running, result.Status);
    }

    [Fact]
    public async Task CompleteAsync_RunningChallengeOwnedByUser_ChangesStatusToCompleted()
    {
        await using var database = await ChallengeServiceTestDatabase.CreateAsync(ChallengeStatus.Running);

        var result = await database.Service.CompleteAsync(database.ChallengeId, database.OwnerId);

        Assert.Equal(ChallengeStatus.Completed, result.Status);
    }

    [Theory]
    [InlineData(ChallengeStatus.Running)]
    [InlineData(ChallengeStatus.Completed)]
    public async Task StartAsync_ChallengeNotOpen_ThrowsConflictException(ChallengeStatus status)
    {
        await using var database = await ChallengeServiceTestDatabase.CreateAsync(status);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            database.Service.StartAsync(database.ChallengeId, database.OwnerId));

        Assert.Equal("Only an open challenge can be started.", exception.Message);
    }

    [Theory]
    [InlineData(ChallengeStatus.Open)]
    [InlineData(ChallengeStatus.Completed)]
    public async Task CompleteAsync_ChallengeNotRunning_ThrowsConflictException(ChallengeStatus status)
    {
        await using var database = await ChallengeServiceTestDatabase.CreateAsync(status);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            database.Service.CompleteAsync(database.ChallengeId, database.OwnerId));

        Assert.Equal("Only a running challenge can be completed.", exception.Message);
    }

    [Fact]
    public async Task StartAsync_ChallengeOwnedByAnotherUser_ThrowsForbiddenException()
    {
        await using var database = await ChallengeServiceTestDatabase.CreateAsync(ChallengeStatus.Open);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            database.Service.StartAsync(database.ChallengeId, Guid.NewGuid()));
    }

    [Fact]
    public async Task CompleteAsync_ChallengeOwnedByAnotherUser_ThrowsForbiddenException()
    {
        await using var database = await ChallengeServiceTestDatabase.CreateAsync(ChallengeStatus.Running);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            database.Service.CompleteAsync(database.ChallengeId, Guid.NewGuid()));
    }

    [Fact]
    public async Task StartAsync_MissingChallenge_ThrowsNotFoundException()
    {
        await using var database = await ChallengeServiceTestDatabase.CreateAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            database.Service.StartAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CompleteAsync_MissingChallenge_ThrowsNotFoundException()
    {
        await using var database = await ChallengeServiceTestDatabase.CreateAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            database.Service.CompleteAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    private sealed class ChallengeServiceTestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _db;

        private ChallengeServiceTestDatabase(
            SqliteConnection connection,
            ApplicationDbContext db,
            Guid ownerId,
            Guid challengeId)
        {
            _connection = connection;
            _db = db;
            OwnerId = ownerId;
            ChallengeId = challengeId;
            Service = new ChallengeService(db);
        }

        public ChallengeService Service { get; }
        public Guid OwnerId { get; }
        public Guid ChallengeId { get; }

        public static async Task<ChallengeServiceTestDatabase> CreateAsync(
            ChallengeStatus? status = null)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var db = new ApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync();

            if (!status.HasValue)
            {
                return new ChallengeServiceTestDatabase(connection, db, Guid.Empty, Guid.Empty);
            }

            var owner = new User
            {
                Id = Guid.NewGuid(),
                UserName = $"owner-{Guid.NewGuid():N}@example.com",
                Email = $"owner-{Guid.NewGuid():N}@example.com",
                DisplayName = "Owner"
            };

            var challenge = new Challenge
            {
                Id = Guid.NewGuid(),
                OwnerId = owner.Id,
                Title = "Test challenge",
                Visibility = ChallengeVisibility.Public,
                Status = status.Value,
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 1, 31),
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.Users.Add(owner);
            db.Challenges.Add(challenge);
            await db.SaveChangesAsync();

            return new ChallengeServiceTestDatabase(connection, db, owner.Id, challenge.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await _db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
