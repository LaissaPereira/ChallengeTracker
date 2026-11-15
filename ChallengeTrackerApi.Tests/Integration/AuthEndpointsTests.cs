using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChallengeTrackerApi.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string ValidPassword = "Passw0rd!";

    private readonly HttpClient _client;

    public AuthEndpointsTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsCreatedWithTrimmedDisplayName()
    {
        var email = NewEmail();

        var response = await RegisterAsync("  Laissa  ", email);
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/auth/me", response.Headers.Location?.OriginalString);
        Assert.Equal("Laissa", body.GetProperty("displayName").GetString());
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.False(body.TryGetProperty("passwordHash", out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public async Task Register_InvalidDisplayName_ReturnsValidationProblem(string displayName)
    {
        var response = await RegisterAsync(displayName, NewEmail());

        await AssertValidationProblemAsync(response, "DisplayName");
    }

    [Fact]
    public async Task Register_DisplayNameTooLong_ReturnsValidationProblem()
    {
        var response = await RegisterAsync(new string('a', 51), NewEmail());

        await AssertValidationProblemAsync(response, "DisplayName");
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsValidationProblem()
    {
        var response = await RegisterAsync("Laissa", NewEmail(), "12345678");

        await AssertValidationProblemAsync(response, "PasswordRequiresUpper");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsValidationProblem()
    {
        var email = NewEmail();
        await RegisterAsync("Laissa", email);

        var response = await RegisterAsync("Other", email);

        await AssertValidationProblemAsync(response, "DuplicateEmail");
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndExpiry()
    {
        var email = NewEmail();
        await RegisterAsync("Laissa", email);

        var response = await LoginAsync(email, ValidPassword);
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("token").GetString()));
        Assert.True(body.GetProperty("expiresAt").GetDateTimeOffset() > DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Login_InvalidCredentials_ReturnsUnauthorizedProblemDetails(bool knownEmail)
    {
        var email = NewEmail();
        if (knownEmail)
        {
            await RegisterAsync("Laissa", email);
        }

        var response = await LoginAsync(email, "WrongPassw0rd!");

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        var response = await _client.GetAsync("/auth/me");

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithInvalidToken_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsCurrentUser()
    {
        var email = NewEmail();
        await RegisterAsync("Laissa", email);
        var login = await ReadJsonAsync(await LoginAsync(email, ValidPassword));

        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("token").GetString());
        var response = await _client.SendAsync(request);
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Laissa", body.GetProperty("displayName").GetString());
        Assert.Equal(email, body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Authentication_JwtKeyShorterThan32Bytes_FailsWithoutLeakingDetails()
    {
        await using var factory = new AuthApiFactory(jwtKey: new string('x', 31));
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "any-token");

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("Jwt:Key", responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authentication_JwtKeyOfExactly32Bytes_IsAccepted()
    {
        await using var factory = new AuthApiFactory(jwtKey: new string('x', 32));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private Task<HttpResponseMessage> RegisterAsync(string displayName, string email, string password = ValidPassword) =>
        _client.PostAsJsonAsync("/auth/register", new { displayName, email, password });

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _client.PostAsJsonAsync("/auth/login", new { email, password });

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>());

    private static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await ReadJsonAsync(response);
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());

        return problem;
    }

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, string expectedErrorKey)
    {
        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);

        Assert.True(
            problem.GetProperty("errors").TryGetProperty(expectedErrorKey, out _),
            $"Expected validation error '{expectedErrorKey}'.");
    }
}
