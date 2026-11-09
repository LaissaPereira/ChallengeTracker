using Microsoft.EntityFrameworkCore;

namespace ChallengeTrackerApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
}
