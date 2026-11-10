using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ChallengeTrackerApi.Tests.Integration;

public class ErrorHandlingTests
{
    [Fact]
    public async Task UnknownRoute_ReturnsProblemDetailsWithoutStackTrace()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("Jwt:Key", new string('x', 64));
            });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/does-not-exist");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.DoesNotContain("stackTrace", responseBody, StringComparison.OrdinalIgnoreCase);
    }
}
