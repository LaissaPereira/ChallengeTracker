namespace ChallengeTrackerApi.Models;

public class ProgressEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ChallengeId { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset LoggedAt { get; set; }
    public DateOnly LoggedDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Challenge Challenge { get; set; } = null!;
}
