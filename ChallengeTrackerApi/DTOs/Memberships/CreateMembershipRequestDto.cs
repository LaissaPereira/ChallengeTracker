using System.ComponentModel.DataAnnotations;

namespace ChallengeTrackerApi.DTOs.Memberships;

public record CreateMembershipRequestDto(
    [property: Required]
    Guid? ChallengeId
);
