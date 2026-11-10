
namespace ChallengeTrackerApi.DTOs.Auth;

public record UserResponseDto
(
    Guid Id,
    string DisplayName,
    string Email
);