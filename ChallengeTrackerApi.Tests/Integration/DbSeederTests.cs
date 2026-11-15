using ChallengeTrackerApi.Data;
using ChallengeTrackerApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChallengeTrackerApi.Tests.Integration;

public sealed class DbSeederTests : IClassFixture<AuthApiFactory>
{
    private const string SeedPassword = "Development-seed-password-123!";
    private readonly AuthApiFactory _factory;

    public DbSeederTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeedAsync_CreatesExpectedDataOnce_WhenCalledTwice()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = await db.Users.OrderBy(user => user.Email).ToListAsync();
        var challenge = await db.Challenges.SingleAsync();
        var membership = await db.Memberships.SingleAsync();

        Assert.Equal(2, users.Count);
        Assert.Equal(ChallengeVisibility.Public, challenge.Visibility);
        Assert.Equal(ChallengeStatus.Open, challenge.Status);
        Assert.Equal(challenge.Id, membership.ChallengeId);
        Assert.Equal(MembershipStatus.Active, membership.Status);

        var member = Assert.Single(users, user => user.Id == membership.UserId);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        Assert.NotEqual(SeedPassword, member.PasswordHash);
        Assert.True(await userManager.CheckPasswordAsync(member, SeedPassword));
    }
}
