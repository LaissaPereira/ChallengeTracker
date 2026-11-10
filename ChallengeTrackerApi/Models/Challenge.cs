namespace ChallengeTrackerApi.Models;

public class Challenge
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChallengeVisibility Visibility { get; set; }
    public ChallengeStatus Status { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User Owner { get; set; } = null!;
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<ProgressEntry> ProgressEntries { get; set; } = new List<ProgressEntry>();

    public bool IsWithinWindow(DateOnly date) =>
        date >= StartDate && date <= EndDate;
}
