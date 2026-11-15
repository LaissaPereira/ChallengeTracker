using System.ComponentModel.DataAnnotations;

namespace ChallengeTrackerApi.DTOs.ProgressEntries;

public record UpdateProgressEntryRequestDto(
    [property: Range(
        typeof(decimal),
        "0",
        "79228162514264337593543950335",
        MinimumIsExclusive = true,
        ParseLimitsInInvariantCulture = true)]
    decimal Amount,

    string? Note,

    [property: Required]
    DateTimeOffset? LoggedAt
);
