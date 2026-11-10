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
        var (status, title) = exception switch
        {
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad request"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "Bad request"),
            _ => (StatusCodes.Status500InternalServerError, "Server error")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "An unhandled exception occurred.");
        }

        context.Response.StatusCode = status;

        await Results.Problem(
            statusCode: status,
            title: title,
            detail: status == StatusCodes.Status500InternalServerError ? null : exception.Message
        ).ExecuteAsync(context);

        return true;
    }
}