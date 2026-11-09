using ChallengeTrackerApi.DTOs.Auth;
using Microsoft.AspNetCore.Identity;

namespace ChallengeTrackerApi.Services.Interfaces;

public interface IAuthService
{
    Task<(IdentityResult Result, UserResponseDto? User )> RegisterAsync(RegisterRequestDto registerRequestDto);
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequestDto);
}