using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChallengeTrackerApi.Tests.Integration;

public sealed class ChallengeEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string ValidPassword = "Passw0rd!";
    private readonly AuthApiFactory _factory;

    public ChallengeEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/challenges", ValidChallengeRequest());

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedChallengeOwnedByCurrentUser()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync("Owner");
        using (client)
        {
            var response = await client.PostAsJsonAsync("/challenges", ValidChallengeRequest("  Read daily  "));
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("Read daily", body.GetProperty("title").GetString());
            Assert.Equal(userId, body.GetProperty("ownerId").GetGuid());
            Assert.Equal("open", body.GetProperty("status").GetString());
            Assert.Equal("public", body.GetProperty("visibility").GetString());
            Assert.Equal($"/challenges/{body.GetProperty("id").GetGuid()}", response.Headers.Location?.OriginalString);
        }
    }

    [Fact]
    public async Task Create_StartDateAfterEndDate_ReturnsBadRequestProblemDetails()
    {
        var (client, _) = await CreateAuthenticatedClientAsync("Owner");
        using (client)
        {
            var request = new
            {
                title = "Invalid dates",
                description = (string?)null,
                visibility = "public",
                startDate = "2026-02-01",
                endDate = "2026-01-01"
            };

            var response = await client.PostAsJsonAsync("/challenges", request);

            await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task Create_WhitespaceTitle_ReturnsBadRequestProblemDetails()
    {
        var (client, _) = await CreateAuthenticatedClientAsync("Owner");
        using (client)
        {
            var response = await client.PostAsJsonAsync("/challenges", ValidChallengeRequest("   "));

            await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task List_WithVisibilityAndStatusFilters_ReturnsOnlyMatchingChallenges()
    {
        var (client, _) = await CreateAuthenticatedClientAsync("Owner");
        using (client)
        {
            var expectedTitle = $"Public-{Guid.NewGuid():N}";
            await client.PostAsJsonAsync("/challenges", ValidChallengeRequest(expectedTitle, "public"));
            await client.PostAsJsonAsync(
                "/challenges",
                ValidChallengeRequest($"Private-{Guid.NewGuid():N}", "private"));

            var response = await client.GetAsync("/challenges?visibility=public&status=open");
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(body.EnumerateArray(), item => item.GetProperty("title").GetString() == expectedTitle);
            Assert.All(body.EnumerateArray(), item =>
            {
                Assert.Equal("public", item.GetProperty("visibility").GetString());
                Assert.Equal("open", item.GetProperty("status").GetString());
            });
        }
    }

    [Fact]
    public async Task Get_MissingChallenge_ReturnsNotFoundProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/challenges/{Guid.NewGuid()}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_InvalidVisibility_ReturnsBadRequestProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/challenges?visibility=unknown");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_JoinedScopeWithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/challenges?scope=joined");

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_InvalidScope_ReturnsBadRequestProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/challenges?scope=unknown");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_JoinedScope_ReturnsOwnedActiveAndPendingChallengesOnly()
    {
        var (viewerClient, _) = await CreateAuthenticatedClientAsync("Dashboard viewer");
        var (ownerClient, _) = await CreateAuthenticatedClientAsync("Challenge owner");
        using (viewerClient)
        using (ownerClient)
        {
            var ownedChallengeId = await CreateChallengeAsync(viewerClient);
            var activeChallengeId = await CreateChallengeAsync(ownerClient, "public");
            var pendingChallengeId = await CreateChallengeAsync(ownerClient, "private");
            var unrelatedChallengeId = await CreateChallengeAsync(ownerClient, "public");

            await viewerClient.PostAsJsonAsync(
                "/memberships",
                new { challengeId = activeChallengeId });
            await viewerClient.PostAsJsonAsync(
                "/memberships",
                new { challengeId = pendingChallengeId });

            var response = await viewerClient.GetAsync("/challenges?scope=joined");
            var body = await ReadJsonAsync(response);
            var challengeIds = body.EnumerateArray()
                .Select(challenge => challenge.GetProperty("id").GetGuid())
                .ToList();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(ownedChallengeId, challengeIds);
            Assert.Contains(activeChallengeId, challengeIds);
            Assert.Contains(pendingChallengeId, challengeIds);
            Assert.DoesNotContain(unrelatedChallengeId, challengeIds);
        }
    }

    [Fact]
    public async Task StartAndComplete_AsOwner_ReturnUpdatedChallenge()
    {
        var (client, _) = await CreateAuthenticatedClientAsync("Owner");
        using (client)
        {
            var challengeId = await CreateChallengeAsync(client);

            var startResponse = await client.PostAsync($"/challenges/{challengeId}/start", null);
            var started = await ReadJsonAsync(startResponse);
            var completeResponse = await client.PostAsync($"/challenges/{challengeId}/complete", null);
            var completed = await ReadJsonAsync(completeResponse);

            Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
            Assert.Equal("running", started.GetProperty("status").GetString());
            Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
            Assert.Equal("completed", completed.GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task Start_AsNonOwner_ReturnsForbiddenProblemDetails()
    {
        var (ownerClient, _) = await CreateAuthenticatedClientAsync("Owner");
        var (otherClient, _) = await CreateAuthenticatedClientAsync("Other user");
        using (ownerClient)
        using (otherClient)
        {
            var challengeId = await CreateChallengeAsync(ownerClient);

            var response = await otherClient.PostAsync($"/challenges/{challengeId}/start", null);

            await AssertProblemAsync(response, HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task Complete_OpenChallenge_ReturnsConflictProblemDetails()
    {
        var (client, _) = await CreateAuthenticatedClientAsync("Owner");
        using (client)
        {
            var challengeId = await CreateChallengeAsync(client);

            var response = await client.PostAsync($"/challenges/{challengeId}/complete", null);
            var problem = await AssertProblemAsync(response, HttpStatusCode.Conflict);

            Assert.Equal("Only a running challenge can be completed.", problem.GetProperty("detail").GetString());
        }
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedClientAsync(string displayName)
    {
        var client = _factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/auth/register",
            new { displayName, email, password = ValidPassword });
        var registeredUser = await ReadJsonAsync(registerResponse);

        var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new { email, password = ValidPassword });
        var login = await ReadJsonAsync(loginResponse);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            login.GetProperty("token").GetString());

        return (client, registeredUser.GetProperty("id").GetGuid());
    }

    private static async Task<Guid> CreateChallengeAsync(
        HttpClient client,
        string visibility = "public")
    {
        var response = await client.PostAsJsonAsync(
            "/challenges",
            ValidChallengeRequest($"Challenge-{Guid.NewGuid():N}", visibility));
        var body = await ReadJsonAsync(response);

        return body.GetProperty("id").GetGuid();
    }

    private static object ValidChallengeRequest(
        string title = "Read daily",
        string visibility = "public") => new
        {
            title,
            description = "Read every day",
            visibility,
            startDate = "2026-01-01",
            endDate = "2026-01-31"
        };

    private static async Task<JsonElement> AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await ReadJsonAsync(response);
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());

        return problem;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
