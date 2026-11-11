using System.Security.Claims;
using ChallengeTrackerApi.DTOs.Challenges;
using ChallengeTrackerApi.Errors;
using ChallengeTrackerApi.Extensions;
using ChallengeTrackerApi.Models;
using ChallengeTrackerApi.Services.Interfaces;

namespace ChallengeTrackerApi.Endpoints;

public static class ChallengeEndpoints
{
    public static void MapChallengeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/challenges").WithTags("Challenges");

        group.MapGet("/", async (
            string? visibility,
            string? status,
            ClaimsPrincipal user,
            IChallengeService service,
            CancellationToken cancellationToken) =>
        {
            var currentUserId = user.Identity?.IsAuthenticated == true
                ? user.GetUserId()
                : (Guid?)null;

            var challenges = await service.ListAsync(
                currentUserId,
                ParseOptionalEnum<ChallengeVisibility>(visibility, nameof(visibility)),
                ParseOptionalEnum<ChallengeStatus>(status, nameof(status)),
                cancellationToken);

            return Results.Ok(challenges);
        })
        .WithSummary("List challenges");

        group.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            IChallengeService service,
            CancellationToken cancellationToken) =>
        {
            var currentUserId = user.Identity?.IsAuthenticated == true
                ? user.GetUserId()
                : (Guid?)null;

            var challenge = await service.GetAsync(id, currentUserId, cancellationToken);
            return Results.Ok(challenge);
        })
        .WithSummary("Get a challenge");

        group.MapPost("/", async (
            CreateChallengeRequestDto dto,
            ClaimsPrincipal user,
            IChallengeService service,
            CancellationToken cancellationToken) =>
        {
            var challenge = await service.CreateAsync(
                user.GetUserId(),
                dto,
                cancellationToken);

            return Results.Created($"/challenges/{challenge.Id}", challenge);
        })
        .RequireAuthorization()
        .WithSummary("Create a challenge");

        group.MapPost("/{id:guid}/start", async (
            Guid id,
            ClaimsPrincipal user,
            IChallengeService service,
            CancellationToken cancellationToken) =>
        {
            var challenge = await service.StartAsync(
                id,
                user.GetUserId(),
                cancellationToken);

            return Results.Ok(challenge);
        })
        .RequireAuthorization()
        .WithSummary("Start a challenge");

        group.MapPost("/{id:guid}/complete", async (
            Guid id,
            ClaimsPrincipal user,
            IChallengeService service,
            CancellationToken cancellationToken) =>
        {
            var challenge = await service.CompleteAsync(
                id,
                user.GetUserId(),
                cancellationToken);

            return Results.Ok(challenge);
        })
        .RequireAuthorization()
        .WithSummary("Complete a challenge");
    }

    private static TEnum? ParseOptionalEnum<TEnum>(string? value, string parameterName)
        where TEnum : struct, Enum
    {
        if (value is null)
        {
            return null;
        }

        if (Enum.TryParse<TEnum>(value, true, out var parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new BadRequestException($"Invalid {parameterName} value '{value}'.");
    }
}
