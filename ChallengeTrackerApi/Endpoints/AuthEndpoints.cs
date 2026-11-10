using System.Security.Claims;
using ChallengeTrackerApi.DTOs.Auth;
using ChallengeTrackerApi.Extensions;
using ChallengeTrackerApi.Models;
using ChallengeTrackerApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;


namespace ChallengeTrackerApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints( this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        //Post endpoint for register user
        group.MapPost("/register", async (RegisterRequestDto registerRequestDto, IAuthService authService) =>
        {
            var (result, user) = await authService.RegisterAsync(registerRequestDto);
            return result.Succeeded ? Results.Created("/auth/me", user) : Results.BadRequest(result.Errors);
        });

        //Post endpoint for login user
        group.MapPost("/login", async (LoginRequestDto loginRequestDto, IAuthService authService) =>
        {
            var loginResponse = await authService.LoginAsync(loginRequestDto);
            return loginResponse is null ? Results.Unauthorized() : Results.Ok(loginResponse);
        });

        // Get/me endpoint for current user
        group.MapGet("/me", async (ClaimsPrincipal user, UserManager<User> users) =>
        {
            var currentUser = await users.FindByIdAsync(user.GetUserId().ToString());

            return currentUser is null
                ? Results.NotFound()
                : Results.Ok(new UserResponseDto(currentUser.Id, currentUser.Name, currentUser.Email!));
        })
        .RequireAuthorization();
    }
}
