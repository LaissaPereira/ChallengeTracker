using System.ComponentModel.DataAnnotations;

namespace ChallengeTrackerApi.DTOs.Auth;

public record LoginRequestDto
(
    [Required][EmailAddress] string Email,
    [Required] string Password
);