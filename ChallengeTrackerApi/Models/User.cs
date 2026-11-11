using Microsoft.AspNetCore.Identity;

namespace ChallengeTrackerApi.Models;

public class User : IdentityUser<Guid>
{
    public const int DisplayNameMaxLength = 50;

    public string DisplayName { get; set; } = string.Empty;
    public ICollection<Challenge> OwnedChallenges { get; set; } = new List<Challenge>();
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<ProgressEntry> ProgressEntries { get; set; } = new List<ProgressEntry>();
}
