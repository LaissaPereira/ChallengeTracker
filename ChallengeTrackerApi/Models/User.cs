using Microsoft.AspNetCore.Identity;

namespace ChallengeTrackerApi.Models;

public class User : IdentityUser<Guid>
{
    public const int DisplayNameMaxLength = 50;

    public string DisplayName { get; set; } = string.Empty;

    // TODO: add navigation properties once these models exist.
    // User 1-* Membership *-1 Challenge; User 1-* ProgressEntry; Challenge 1-* ProgressEntry
    // public ICollection<Membership> Memberships { get; set; } = [];
    // public ICollection<ProgressEntry> ProgressEntries { get; set; } = [];

    // TODO: models to create in Models/ (all Ids are Guid):
    // Challenge:     Id, Title, StartDate (DateOnly), EndDate (DateOnly), CreatedAt (DateTimeOffset),
    //                ICollection<Membership> Memberships, ICollection<ProgressEntry> ProgressEntries
    // Membership:    Id, UserId, ChallengeId, JoinedAt (DateTimeOffset), User, Challenge
    // ProgressEntry: Id, UserId, ChallengeId, LoggedAt (DateTimeOffset), User, Challenge
}