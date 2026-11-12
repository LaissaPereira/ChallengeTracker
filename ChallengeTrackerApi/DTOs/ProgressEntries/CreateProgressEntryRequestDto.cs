using System.ComponentModel.DataAnnotations;

namespace ChallengeTrackerApi.DTOs.ProgressEntries;

public record CreateProgressEntryRequestDto(
    [property: Required]
    Guid? ChallengeId,

    [property: Range(
        typeof(decimal),
        "0",
        "79228162514264337593543950335",
        MinimumIsExclusive = true,
        ParseLimitsInInvariantCulture = true)]
    decimal Amount,

    string? Note,

    DateTimeOffset? LoggedAt
);
