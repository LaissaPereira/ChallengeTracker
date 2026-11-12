using System.Security.Claims;
using ChallengeTrackerApi.DTOs.Memberships;
using ChallengeTrackerApi.Extensions;
using ChallengeTrackerApi.Services.Interfaces;

namespace ChallengeTrackerApi.Endpoints;

public static class MembershipEndpoints
{
    public static void MapMembershipEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/memberships")
            .WithTags("Memberships")
            .RequireAuthorization();

        group.MapPost("/", async (
            CreateMembershipRequestDto dto,
            ClaimsPrincipal user,
            IMembershipService service,
            CancellationToken cancellationToken) =>
        {
            var membership = await service.JoinAsync(
                user.GetUserId(),
                dto,
                cancellationToken);

            return Results.Created($"/memberships/{membership.Id}", membership);
        })
        .WithSummary("Join a challenge");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            IMembershipService service,
            CancellationToken cancellationToken) =>
        {
            await service.LeaveAsync(id, user.GetUserId(), cancellationToken);
            return Results.NoContent();
        })
        .WithSummary("Leave a challenge");

        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            ClaimsPrincipal user,
            IMembershipService service,
            CancellationToken cancellationToken) =>
        {
            var membership = await service.ApproveAsync(
                id,
                user.GetUserId(),
                cancellationToken);

            return Results.Ok(membership);
        })
        .WithSummary("Approve a pending membership");
    }
}
