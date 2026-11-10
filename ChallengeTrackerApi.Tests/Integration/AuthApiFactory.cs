using ChallengeTrackerApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChallengeTrackerApi.Tests.Integration;

// Boots the API against an isolated SQLite file with the real migrations applied.
public class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string ValidJwtKey = "integration-tests-signing-key-with-more-than-32-bytes";

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"challenge-tests-{Guid.NewGuid():N}.db");
    private readonly string _jwtKey;
    private readonly Dictionary<string, string> _settings = new()
    {
        // High defaults so tests that call login/register repeatedly are never throttled.
        ["RateLimiting:Login:PermitLimit"] = "10000",
        ["RateLimiting:Register:PermitLimit"] = "10000",
        ["RateLimiting:PerUser:PermitLimit"] = "10000"
    };

    public AuthApiFactory() : this(ValidJwtKey)
    {
    }

    internal AuthApiFactory(string jwtKey, IReadOnlyDictionary<string, string>? settings = null)
    {
        _jwtKey = jwtKey;

        foreach (var (key, value) in settings ?? new Dictionary<string, string>())
        {
            _settings[key] = value;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");
        builder.UseSetting("Jwt:Key", _jwtKey);

        foreach (var (key, value) in _settings)
        {
            builder.UseSetting(key, value);
        }
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}
