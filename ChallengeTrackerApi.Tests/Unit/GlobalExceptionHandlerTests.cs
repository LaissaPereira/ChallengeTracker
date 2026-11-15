using System.Text.Json;
using ChallengeTrackerApi.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChallengeTrackerApi.Tests.Unit;

public class GlobalExceptionHandlerTests
{
    public static TheoryData<Exception, int, string> ExpectedExceptions => new()
    {
        { new BadRequestException("Invalid request"), StatusCodes.Status400BadRequest, "Bad request" },
        { new NotFoundException("Challenge not found"), StatusCodes.Status404NotFound, "Not found" },
        { new ConflictException("Invalid transition"), StatusCodes.Status409Conflict, "Conflict" }
    };

    [Theory]
    [MemberData(nameof(ExpectedExceptions))]
    public async Task TryHandleAsync_KnownException_ReturnsExpectedProblemDetails(
        Exception exception,
        int expectedStatus,
        string expectedTitle)
    {
        var (context, problem) = await HandleAsync(exception);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal(expectedStatus, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(expectedTitle, problem.RootElement.GetProperty("title").GetString());
        Assert.Equal(exception.Message, problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_UnexpectedException_DoesNotExposeExceptionDetails()
    {
        const string sensitiveMessage = "database-password-is-secret";

        var (context, problem) = await HandleAsync(new InvalidOperationException(sensitiveMessage));
        var responseBody = problem.RootElement.GetRawText();

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(
            "Server error",
            problem.RootElement.GetProperty("title").GetString());
        Assert.DoesNotContain(sensitiveMessage, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain("stackTrace", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<(DefaultHttpContext Context, JsonDocument Problem)> HandleAsync(
        Exception exception)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Response.Body = new MemoryStream();

        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        context.Response.Body.Position = 0;

        return (context, await JsonDocument.ParseAsync(context.Response.Body));
    }
}
