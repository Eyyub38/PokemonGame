using UnityEngine;

public enum WorldRegionRequirementMode {
    CurrentRegionIs,
    RegionDiscovered,
    RegionNotDiscovered,
    RouteUnlocked,
    RouteTravelCountAtLeast,
    AnyRegionTravelCountAtLeast,
    ChallengeActive,
    ChallengeCompleted,
    StorageLockedByChallenge,
    PokemonAllowedByActiveChallenge
}

[CreateAssetMenu(menuName = "Activities/Requirements/World Region Requirement")]
public class WorldRegionRequirement : ActivityRequirement {
    [Header("Target")]
    [Tooltip("How the player's world region state should be checked.")]
    [SerializeField] WorldRegionRequirementMode mode = WorldRegionRequirementMode.CurrentRegionIs;
    [Tooltip("World region used by region-based modes.")]
    [SerializeField] WorldRegionDefinition region;
    [Tooltip("Regional route used by route-based modes.")]
    [SerializeField] RegionTravelRouteDefinition route;
    [Tooltip("Region challenge used by challenge-based modes.")]
    [SerializeField] RegionChallengeProfileDefinition challenge;

    [Header("Threshold")]
    [Tooltip("Required count for travel-count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, blocked travel attempts also count.")]
    [SerializeField] bool includeBlockedTravel;
    [Tooltip("If enabled, the final result is inverted.")]
    [SerializeField] bool invertResult;

    public WorldRegionRequirementMode Mode => mode;
    public WorldRegionDefinition Region => region;
    public RegionTravelRouteDefinition Route => route;
    public RegionChallengeProfileDefinition Challenge => challenge;
    public int RequiredCount => Mathf.Max(0, requiredCount);
    public bool IncludeBlockedTravel => includeBlockedTravel;
    public bool InvertResult => invertResult;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerWorldRegionLog>() : null;
        bool met = log != null && Evaluate(log, player);
        return invertResult ? !met : met;
    }

    bool Evaluate(PlayerWorldRegionLog log, PlayerController player) {
        switch(mode) {
            case WorldRegionRequirementMode.CurrentRegionIs:
                return region != null && log.IsCurrentRegion(region);
            case WorldRegionRequirementMode.RegionDiscovered:
                return region != null && log.HasDiscoveredRegion(region);
            case WorldRegionRequirementMode.RegionNotDiscovered:
                return region != null && !log.HasDiscoveredRegion(region);
            case WorldRegionRequirementMode.RouteUnlocked:
                return route != null && log.HasUnlockedRoute(route);
            case WorldRegionRequirementMode.RouteTravelCountAtLeast:
                return route != null && log.GetTravelCount(route, includeBlocked: includeBlockedTravel) >= RequiredCount;
            case WorldRegionRequirementMode.AnyRegionTravelCountAtLeast:
                return log.GetTravelCount(null, includeBlocked: includeBlockedTravel) >= RequiredCount;
            case WorldRegionRequirementMode.ChallengeActive:
                return challenge == null ? log.HasActiveChallenge : log.HasActiveChallenge && log.ActiveChallenge.challengeId == challenge.Id;
            case WorldRegionRequirementMode.ChallengeCompleted:
                return challenge != null && log.HasCompletedChallenge(challenge);
            case WorldRegionRequirementMode.StorageLockedByChallenge:
                return log.HasActiveChallenge && log.ActiveChallenge.storageLocked;
            case WorldRegionRequirementMode.PokemonAllowedByActiveChallenge:
                var party = player != null ? player.GetComponent<PokemonParty>() : null;
                var pokemon = party != null ? party.GetHealthyPokemon() : null;
                return pokemon == null || log.IsPokemonAllowedByActiveChallenge(pokemon);
            default:
                return false;
        }
    }
}
