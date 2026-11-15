using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChallengeTrackerApi.Tests.Integration;

public class RateLimitingTests
{
    private const string Password = "Passw0rd!";

    [Fact]
    public async Task Login_ExceedingLimit_Returns429ProblemDetailsWithRetryAfter()
    {
        await using var factory = CreateFactory(("Login", 2));
        using var client = factory.CreateClient();

        for (var i = 0; i < 2; i++)
        {
            var allowed = await LoginAsync(client);
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var response = await LoginAsync(client);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(429, problem.GetProperty("status").GetInt32());
        Assert.Equal("Too many requests", problem.GetProperty("title").GetString());
        Assert.True(response.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task Register_ExceedingLimit_Returns429()
    {
        await using var factory = CreateFactory(("Register", 1));
        using var client = factory.CreateClient();

        var first = await RegisterAsync(client);
        var second = await RegisterAsync(client);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [Fact]
    public async Task Login_LimitIsIndependentFromRegisterLimit()
    {
        await using var factory = CreateFactory(("Login", 1));
        using var client = factory.CreateClient();

        await LoginAsync(client);
        var loginBlocked = await LoginAsync(client);
        var register = await RegisterAsync(client);

        Assert.Equal(HttpStatusCode.TooManyRequests, loginBlocked.StatusCode);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
    }

    [Fact]
    public async Task Me_LimitIsAppliedPerUser()
    {
        await using var factory = CreateFactory(("PerUser", 2));
        using var client = factory.CreateClient();
        var tokenA = await RegisterAndLoginAsync(client);
        var tokenB = await RegisterAndLoginAsync(client);

        Assert.Equal(HttpStatusCode.OK, (await MeAsync(client, tokenA)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await MeAsync(client, tokenA)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await MeAsync(client, tokenA)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await MeAsync(client, tokenB)).StatusCode);
    }

    [Theory]
    [InlineData("Login")]
    [InlineData("Register")]
    [InlineData("PerUser")]
    public void InvalidConfiguration_FailsAtStartup(string policy)
    {
        using var factory = CreateFactory((policy, 0));

        Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
    }

    private static AuthApiFactory CreateFactory(params (string Policy, int PermitLimit)[] limits) =>
        new(
            AuthApiFactory.ValidJwtKey,
            limits.ToDictionary(
                limit => $"RateLimiting:{limit.Policy}:PermitLimit",
                limit => limit.PermitLimit.ToString()));

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string? email = null) =>
        client.PostAsJsonAsync("/auth/login", new { email = email ?? NewEmail(), password = "WrongPassw0rd!" });

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string? email = null) =>
        client.PostAsJsonAsync("/auth/register", new { displayName = "Laissa", email = email ?? NewEmail(), password = Password });

    private static async Task<HttpResponseMessage> MeAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await client.SendAsync(request);
    }

    // Registration and login use the default (high) limits unless the test overrides them.
    private static async Task<string> RegisterAndLoginAsync(HttpClient client)
    {
        var email = NewEmail();
        var register = await RegisterAsync(client, email);
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<JsonElement>();

        return body.GetProperty("token").GetString()!;
    }
}
