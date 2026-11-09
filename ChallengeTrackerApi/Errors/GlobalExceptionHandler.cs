using Microsoft.AspNetCore.Diagnostics;

namespace ChallengeTrackerApi.Errors;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception is BadHttpRequestException badRequest ? badRequest.StatusCode : StatusCodes.Status500InternalServerError;

        if(status == 500)
        {
            _logger.LogError(exception, "An unhandled exception occurred.");
        }

        context.Response.StatusCode = status;

        await Results.Problem(
            statusCode: status,
            title: status == 500 ? "Server error" : "Bad request",
            detail: exception.Message
        ).ExecuteAsync(context);

        return true;
    }
}