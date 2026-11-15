using System.ComponentModel.DataAnnotations;
using ChallengeTrackerApi.Models;

namespace ChallengeTrackerApi.DTOs.Auth;

public record RegisterRequestDto
(
    [Required][StringLength(User.DisplayNameMaxLength, MinimumLength = 2)] string DisplayName,
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password
);
