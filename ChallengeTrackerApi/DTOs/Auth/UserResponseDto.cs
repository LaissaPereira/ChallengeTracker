
namespace ChallengeTrackerApi.DTOs.Auth;

public record UserResponseDto
(
    Guid Id,
    string Name,
    string Email
);