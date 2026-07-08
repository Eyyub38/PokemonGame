using UnityEngine;

public enum WorldDiscoveryRequirementMode {
    HasDiscovered,
    NeverDiscovered,
    DiscoveryCountAtLeast,
    SourceDiscoveryCountAtLeast,
    KindCountAtLeast,
    PokemonDiscoveryCountAtLeast,
    RegionDiscoveryCountAtLeast,
    MapMarkerDiscoveryCountAtLeast,
    HoursSinceLastDiscoveryAtLeast,
    HoursSinceLastDiscoveryAtMost
}

[CreateAssetMenu(menuName = "Activities/Requirements/World Discovery Requirement")]
public class WorldDiscoveryRequirement : ActivityRequirement {
    [Tooltip("Which world discovery history check this requirement performs.")]
    [SerializeField] WorldDiscoveryRequirementMode mode = WorldDiscoveryRequirementMode.HasDiscovered;
    [Tooltip("Specific discovery definition checked by this requirement. Empty allows broader filters.")]
    [SerializeField] WorldDiscoveryDefinition discovery = null;
    [Tooltip("Optional source id filter for source/count/time modes.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Discovery kind checked by Kind Count At Least.")]
    [SerializeField] WorldDiscoveryKind kind = WorldDiscoveryKind.General;
    [Tooltip("Related Pokemon checked by Pokemon Discovery Count At Least.")]
    [SerializeField] PokemonBase pokemon = null;
    [Tooltip("Related region checked by Region Discovery Count At Least.")]
    [SerializeField] RegionInfoDefinition region = null;
    [Tooltip("Related map marker checked by Map Marker Discovery Count At Least.")]
    [SerializeField] MapMarkerDefinition mapMarker = null;
    [Tooltip("Minimum count required by count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("Minimum/maximum in-game hours used by Hours Since Last Discovery modes.")]
    [Min(0)]
    [SerializeField] int requiredHours = 1;
    [Tooltip("If enabled, blocked attempts are counted too. If disabled, only successful discoveries count.")]
    [SerializeField] bool includeBlockedAttempts;
    [Tooltip("If enabled, the selected condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerWorldDiscoveryLog>() : null;
        bool result = mode switch {
            WorldDiscoveryRequirementMode.NeverDiscovered => log == null || !log.HasDiscovered(discovery, sourceId, includeBlockedAttempts),
            WorldDiscoveryRequirementMode.DiscoveryCountAtLeast => log != null && log.GetDiscoveryCount(discovery, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            WorldDiscoveryRequirementMode.SourceDiscoveryCountAtLeast => log != null && log.GetDiscoveryCount(discovery, sourceId, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            WorldDiscoveryRequirementMode.KindCountAtLeast => log != null && log.GetDiscoveryCount(discovery, sourceId, kind, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            WorldDiscoveryRequirementMode.PokemonDiscoveryCountAtLeast => log != null && log.GetDiscoveryCount(discovery, sourceId, pokemon: pokemon, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            WorldDiscoveryRequirementMode.RegionDiscoveryCountAtLeast => log != null && log.GetDiscoveryCount(discovery, sourceId, region: region, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            WorldDiscoveryRequirementMode.MapMarkerDiscoveryCountAtLeast => log != null && log.GetDiscoveryCount(discovery, sourceId, mapMarker: mapMarker, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            WorldDiscoveryRequirementMode.HoursSinceLastDiscoveryAtLeast => HasHoursSinceLastDiscovery(log, atLeast: true),
            WorldDiscoveryRequirementMode.HoursSinceLastDiscoveryAtMost => HasHoursSinceLastDiscovery(log, atLeast: false),
            _ => log != null && log.HasDiscovered(discovery, sourceId, includeBlockedAttempts)
        };

        return mustBeMet ? result : !result;
    }

    bool HasHoursSinceLastDiscovery(PlayerWorldDiscoveryLog log, bool atLeast) {
        if(log == null) {
            return false;
        }

        int hours = log.GetHoursSinceLastDiscovery(discovery, sourceId, includeBlockedAttempts);
        if(hours < 0) {
            return false;
        }

        int required = Mathf.Max(0, requiredHours);
        return atLeast ? hours >= required : hours <= required;
    }
}
