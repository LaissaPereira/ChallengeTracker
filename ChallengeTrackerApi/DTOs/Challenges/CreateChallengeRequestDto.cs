using System.ComponentModel.DataAnnotations;
using ChallengeTrackerApi.Models;

namespace ChallengeTrackerApi.DTOs.Challenges;

public record CreateChallengeRequestDto(
    [property: Required]
    string Title,

    string? Description,

    [property: Required]
    [property: EnumDataType(typeof(ChallengeVisibility))]
    ChallengeVisibility? Visibility,

    [property: Required]
    DateOnly? StartDate,

    [property: Required]
    DateOnly? EndDate
);
