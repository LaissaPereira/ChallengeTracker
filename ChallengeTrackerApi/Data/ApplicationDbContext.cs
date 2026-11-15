using ChallengeTrackerApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChallengeTrackerApi.Data;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<ProgressEntry> ProgressEntries => Set<ProgressEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>()
            .Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(User.DisplayNameMaxLength);

        builder.Entity<Challenge>()
            .HasOne(challenge => challenge.Owner)
            .WithMany(user => user.OwnedChallenges)
            .HasForeignKey(challenge => challenge.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Membership>()
            .HasOne(membership => membership.User)
            .WithMany(user => user.Memberships)
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Membership>()
            .HasOne(membership => membership.Challenge)
            .WithMany(challenge => challenge.Memberships)
            .HasForeignKey(membership => membership.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Membership>()
            .HasIndex(membership => new { membership.UserId, membership.ChallengeId })
            .IsUnique();

        builder.Entity<ProgressEntry>()
            .Property(progressEntry => progressEntry.Amount)
            .HasPrecision(18, 2);

        builder.Entity<ProgressEntry>()
            .HasOne(progressEntry => progressEntry.User)
            .WithMany(user => user.ProgressEntries)
            .HasForeignKey(progressEntry => progressEntry.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProgressEntry>()
            .HasOne(progressEntry => progressEntry.Challenge)
            .WithMany(challenge => challenge.ProgressEntries)
            .HasForeignKey(progressEntry => progressEntry.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProgressEntry>()
            .HasIndex(progressEntry => new
            {
                progressEntry.UserId,
                progressEntry.ChallengeId,
                progressEntry.LoggedDate
            })
            .IsUnique();
    }
}
