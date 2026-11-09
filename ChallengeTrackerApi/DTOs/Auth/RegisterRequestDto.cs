using System.ComponentModel.DataAnnotations;

namespace ChallengeTrackerApi.DTOs.Auth;

public record RegisterRequestDto
(
    [Required] string Name,
    [Required] [EmailAddress] string Email,
    [Required] [MinLength(8)] string Password
);
    