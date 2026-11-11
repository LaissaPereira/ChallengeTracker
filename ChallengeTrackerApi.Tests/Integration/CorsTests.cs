using System.Net;

namespace ChallengeTrackerApi.Tests.Integration;

public class CorsTests
{
    private static async Task<HttpResponseMessage> PreflightAsync(string origin)
    {
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Options, "/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Preflight_AllowedOrigin_ReturnsCorsHeaders()
    {
        var response = await PreflightAsync("http://localhost:5173");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Preflight_UnknownOrigin_ReturnsNoCorsHeaders()
    {
        var response = await PreflightAsync("http://evil.example");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
