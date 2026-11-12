using System.Security.Claims;
using ChallengeTrackerApi.DTOs.ProgressEntries;
using ChallengeTrackerApi.Extensions;
using ChallengeTrackerApi.Services.Interfaces;

namespace ChallengeTrackerApi.Endpoints;

public static class ProgressEntryEndpoints
{
    public static void MapProgressEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/progress-entries")
            .WithTags("Progress Entries")
            .RequireAuthorization();

        group.MapPost("/", async (
            CreateProgressEntryRequestDto dto,
            ClaimsPrincipal user,
            IProgressEntryService service,
            CancellationToken cancellationToken) =>
        {
            var entry = await service.CreateAsync(
                user.GetUserId(),
                dto,
                cancellationToken);

            return Results.Created($"/progress-entries/{entry.Id}", entry);
        })
        .WithSummary("Log progress");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateProgressEntryRequestDto dto,
            ClaimsPrincipal user,
            IProgressEntryService service,
            CancellationToken cancellationToken) =>
        {
            var entry = await service.UpdateAsync(
                id,
                user.GetUserId(),
                dto,
                cancellationToken);

            return Results.Ok(entry);
        })
        .WithSummary("Update a progress entry");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            IProgressEntryService service,
            CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(id, user.GetUserId(), cancellationToken);
            return Results.NoContent();
        })
        .WithSummary("Delete a progress entry");
    }
}
