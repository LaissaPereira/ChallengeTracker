
namespace ChallengeTrackerApi.DTOs.Auth;

public record LoginResponseDto
(
    string Token,
    DateTimeOffset ExpiresAt
);