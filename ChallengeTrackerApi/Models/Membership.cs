namespace ChallengeTrackerApi.Models;

public class Membership
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ChallengeId { get; set; }
    public MembershipStatus Status { get; set; }
    public DateTimeOffset JoinedAt { get; set; }

    public User User { get; set; } = null!;
    public Challenge Challenge { get; set; } = null!;
}
