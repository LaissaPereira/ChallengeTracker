using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChallengeTrackerApi.Tests.Integration;

public sealed class MembershipEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string ValidPassword = "Passw0rd!";
    private readonly AuthApiFactory _factory;

    public MembershipEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Join_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/memberships",
            new { challengeId = Guid.NewGuid() });

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Join_PublicChallenge_ReturnsCreatedActiveMembership()
    {
        var (ownerClient, _) = await CreateAuthenticatedClientAsync("Owner");
        var (memberClient, memberId) = await CreateAuthenticatedClientAsync("Member");
        using (ownerClient)
        using (memberClient)
        {
            var challengeId = await CreateChallengeAsync(ownerClient, "public");

            var response = await JoinAsync(memberClient, challengeId);
            var membership = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(memberId, membership.GetProperty("userId").GetGuid());
            Assert.Equal(challengeId, membership.GetProperty("challengeId").GetGuid());
            Assert.Equal("active", membership.GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task Join_PrivateChallenge_ReturnsCreatedPendingMembership()
    {
        var (ownerClient, _) = await CreateAuthenticatedClientAsync("Owner");
        var (memberClient, _) = await CreateAuthenticatedClientAsync("Member");
        using (ownerClient)
        using (memberClient)
        {
            var challengeId = await CreateChallengeAsync(ownerClient, "private");

            var response = await JoinAsync(memberClient, challengeId);
            var membership = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("pending", membership.GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task Join_Twice_ReturnsConflictProblemDetails()
    {
        var (ownerClient, _) = await CreateAuthenticatedClientAsync("Owner");
        var (memberClient, _) = await CreateAuthenticatedClientAsync("Member");
        using (ownerClient)
        using (memberClient)
        {
            var challengeId = await CreateChallengeAsync(ownerClient, "public");
            await JoinAsync(memberClient, challengeId);

            var response = await JoinAsync(memberClient, challengeId);

            await AssertProblemAsync(response, HttpStatusCode.Conflict);
        }
    }

    [Fact]
    public async Task Join_OwnChallenge_ReturnsConflictProblemDetails()
    {
        var (ownerClient, _) = await CreateAuthenticatedClientAsync("Owner");
        using (ownerClient)
        {
            var challengeId = await CreateChallengeAsync(ownerClient, "public");

            var response = await JoinAsync(ownerClient, challengeId);

            await AssertProblemAsync(response, HttpStatusCode.Conflict);
        }
    }

    [Fact]
    public async Task Leave_OwnMembership_ReturnsNoContent()
    {
        var (ownerClient, _) = await CreateAuthenticatedClientAsync("Owner");
        var (memberClient, _) = await CreateAuthenticatedClientAsync("Member");
        using (ownerClient)
        using (memberClient)
        {
            var challengeId = await CreateChallengeAsync(ownerClient, "public");
            var membership = await ReadJsonAsync(await JoinAsync(memberClient, challengeId));
            var membershipId = membership.GetProperty("id").GetGuid();

            var response = await memberClient.DeleteAsync($"/memberships/{membershipId}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    [Fact]
    public async Task Leave_AnotherUsersMembership_ReturnsForbiddenProblemDetails()
    {
        var (ownerClient, _) = await CreateAuthenticatedClientAsync("Owner");
        var (memberClient, _) = await CreateAuthenticatedClientAsync("Member");
        var (otherClient, _) = await CreateAuthenticatedClientAsync("Other");
        using (ownerClient)
        using (memberClient)
        using (otherClient)
        {
            var challengeId = await CreateChallengeAsync(ownerClient, "public");
            var membership = await ReadJsonAsync(await JoinAsync(memberClient, challengeId));
            var membershipId = membership.GetProperty("id").GetGuid();

            var response = await otherClient.DeleteAsync($"/memberships/{membershipId}");

            await AssertProblemAsync(response, HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task Approve_PendingMembershipByOwner_ReturnsActiveMembership()
    {
        var (ownerClient, _) = await CreateAuthenticatedClientAsync("Owner");
        var (memberClient, _) = await CreateAuthenticatedClientAsync("Member");
        using (ownerClient)
        using (memberClient)
        {
            var challengeId = await CreateChallengeAsync(ownerClient, "private");
            var membership = await ReadJsonAsync(await JoinAsync(memberClient, challengeId));
            var membershipId = membership.GetProperty("id").GetGuid();

            var response = await ownerClient.PostAsync($"/memberships/{membershipId}/approve", null);
            var approved = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("active", approved.GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task Approve_ByNonOwner_ReturnsForbiddenProblemDetails()
    {
        var (ownerClient, _) = await CreateAuthenticatedClientAsync("Owner");
        var (memberClient, _) = await CreateAuthenticatedClientAsync("Member");
        using (ownerClient)
        using (memberClient)
        {
            var challengeId = await CreateChallengeAsync(ownerClient, "private");
            var membership = await ReadJsonAsync(await JoinAsync(memberClient, challengeId));
            var membershipId = membership.GetProperty("id").GetGuid();

            var response = await memberClient.PostAsync($"/memberships/{membershipId}/approve", null);

            await AssertProblemAsync(response, HttpStatusCode.Forbidden);
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

    private static async Task<Guid> CreateChallengeAsync(HttpClient client, string visibility)
    {
        var response = await client.PostAsJsonAsync(
            "/challenges",
            new
            {
                title = $"Membership-{Guid.NewGuid():N}",
                description = "Membership integration test",
                visibility,
                startDate = "2026-01-01",
                endDate = "2026-01-31"
            });
        var challenge = await ReadJsonAsync(response);

        return challenge.GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> JoinAsync(HttpClient client, Guid challengeId) =>
        client.PostAsJsonAsync("/memberships", new { challengeId });

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
