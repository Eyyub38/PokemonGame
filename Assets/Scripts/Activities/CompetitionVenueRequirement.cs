using UnityEngine;

public enum CompetitionVenueRequirementMode {
    VenueCanEnter,
    VenueCanHostRegistration,
    VenueCanHostRoster,
    HasUsedVenue,
    VenueUseCountAtLeast,
    LastUsedVenue
}

[CreateAssetMenu(menuName = "Activities/Requirements/Competition Venue Requirement")]
public class CompetitionVenueRequirement : ActivityRequirement {
    [Tooltip("Which venue condition this requirement checks.")]
    [SerializeField] CompetitionVenueRequirementMode mode = CompetitionVenueRequirementMode.VenueCanEnter;
    [Tooltip("Venue checked by this requirement.")]
    [SerializeField] CompetitionVenueDefinition venue;
    [Tooltip("Registration checked by Venue Can Host Registration mode.")]
    [SerializeField] CompetitionRegistrationDefinition registration;
    [Tooltip("Roster checked by Venue Can Host Roster mode.")]
    [SerializeField] CompetitionRosterDefinition roster;
    [Tooltip("Minimum count required by Venue Use Count At Least mode.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, blocked venue attempts also count for history checks.")]
    [SerializeField] bool includeBlockedAttempts;
    [Tooltip("If enabled, the selected venue condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerCompetitionVenueLog>() : null;
        bool result = mode switch {
            CompetitionVenueRequirementMode.VenueCanHostRegistration => venue != null && venue.CanHost(player, registration, out _),
            CompetitionVenueRequirementMode.VenueCanHostRoster => venue != null && venue.CanHost(player, roster, out _),
            CompetitionVenueRequirementMode.HasUsedVenue => log != null && log.HasUsedVenue(venue, includeBlockedAttempts),
            CompetitionVenueRequirementMode.VenueUseCountAtLeast => log != null && log.GetUseCount(venue, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            CompetitionVenueRequirementMode.LastUsedVenue => log != null && log.GetLastUse(null, null, includeBlockedAttempts)?.venueId == (venue != null ? venue.Id : string.Empty),
            _ => venue != null && venue.CanEnter(player, out _)
        };

        return mustBeMet ? result : !result;
    }
}
