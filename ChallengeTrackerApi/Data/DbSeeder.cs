using ChallengeTrackerApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChallengeTrackerApi.Data;

public sealed class DbSeeder
{
    private const string SeedPasswordConfigurationKey = "SeedData:Password";
    private const string OwnerEmail = "owner@challenge.local";
    private const string MemberEmail = "member@challenge.local";
    private const string ChallengeTitle = "30-Day Challenge";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;

    public DbSeeder(
        ApplicationDbContext db,
        UserManager<User> userManager,
        IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var password = _configuration[SeedPasswordConfigurationKey]
            ?? throw new InvalidOperationException(
                $"{SeedPasswordConfigurationKey} is missing. Configure it with user secrets or an environment variable.");

        var owner = await FindOrCreateUserAsync(
            OwnerEmail,
            "Demo Owner",
            password,
            cancellationToken);

        var member = await FindOrCreateUserAsync(
            MemberEmail,
            "Demo Member",
            password,
            cancellationToken);

        var challenge = await _db.Challenges.FirstOrDefaultAsync(
            existingChallenge =>
                existingChallenge.OwnerId == owner.Id &&
                existingChallenge.Title == ChallengeTitle,
            cancellationToken);

        if (challenge is null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            challenge = new Challenge
            {
                Id = Guid.NewGuid(),
                OwnerId = owner.Id,
                Title = ChallengeTitle,
                Description = "A sample public challenge created for local development.",
                Visibility = ChallengeVisibility.Public,
                Status = ChallengeStatus.Open,
                StartDate = today,
                EndDate = today.AddDays(29),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Challenges.Add(challenge);
        }

        var membershipExists = await _db.Memberships.AnyAsync(
            membership =>
                membership.UserId == member.Id &&
                membership.ChallengeId == challenge.Id,
            cancellationToken);

        if (!membershipExists)
        {
            _db.Memberships.Add(new Membership
            {
                Id = Guid.NewGuid(),
                UserId = member.Id,
                ChallengeId = challenge.Id,
                Status = MembershipStatus.Active,
                JoinedAt = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> FindOrCreateUserAsync(
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(email);

        if (user is not null)
        {
            return user;
        }

        user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(error => $"{error.Code}: {error.Description}"));

            throw new InvalidOperationException($"Could not create development seed user '{email}': {errors}");
        }

        return user;
    }
}
