using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChallengeTrackerApi.Tests.Integration;

public sealed class LeaderboardEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string ValidPassword = "Passw0rd!";
    private readonly AuthApiFactory _factory;

    public LeaderboardEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLeaderboard_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/leaderboards/challenges/{Guid.NewGuid()}?period=total");

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLeaderboard_UnsupportedPeriod_ReturnsBadRequestProblemDetails()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            $"/leaderboards/challenges/{Guid.NewGuid()}?period=weekly");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetLeaderboard_MissingChallenge_ReturnsNotFoundProblemDetails()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            $"/leaderboards/challenges/{Guid.NewGuid()}?period=total");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"leaderboard-{Guid.NewGuid():N}@example.com";

        await client.PostAsJsonAsync(
            "/auth/register",
            new { displayName = "Leaderboard Viewer", email, password = ValidPassword });

        var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new { email, password = ValidPassword });
        var login = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            login.GetProperty("token").GetString());

        return client;
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
    }
}
