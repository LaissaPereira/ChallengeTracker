using System.Security.Claims;
using ChallengeTrackerApi.DTOs.Auth;
using ChallengeTrackerApi.Extensions;
using ChallengeTrackerApi.Models;
using ChallengeTrackerApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;


namespace ChallengeTrackerApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        //Post endpoint for register user
        group.MapPost("/register", async (RegisterRequestDto registerRequestDto, IAuthService authService) =>
        {
            var (result, user) = await authService.RegisterAsync(registerRequestDto);
            if (result.Succeeded)
            {
                return Results.Created("/auth/me", user);
            }

            var errors = result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.Description).ToArray());

            return Results.ValidationProblem(errors);
        }).WithSummary("Register a new user")
        .RequireRateLimiting(RateLimitPolicies.Register)
        .WithDescription("Register a new user with display name, email, and password");

        //Post endpoint for login user
        group.MapPost("/login", async (LoginRequestDto loginRequestDto, IAuthService authService) =>
        {
            var loginResponse = await authService.LoginAsync(loginRequestDto);
            return loginResponse is null ? Results.Unauthorized() : Results.Ok(loginResponse);
        }).WithSummary("Login a user")
        .RequireRateLimiting(RateLimitPolicies.Login)
        .WithDescription("Login a user with email and password");

        // Get/me endpoint for current user
        group.MapGet("/me", async (ClaimsPrincipal user, UserManager<User> users) =>
        {
            var currentUser = await users.FindByIdAsync(user.GetUserId().ToString());

            return currentUser is null
                ? Results.NotFound()
                : Results.Ok(new UserResponseDto(currentUser.Id, currentUser.DisplayName, currentUser.Email!));
        }).RequireAuthorization()
        .RequireRateLimiting(RateLimitPolicies.PerUser)
        .WithSummary("Get current user")
        .WithDescription("Get the currently authenticated user's information");

    }
}
