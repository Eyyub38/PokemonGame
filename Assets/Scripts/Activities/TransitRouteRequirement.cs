using UnityEngine;

public enum TransitRequirementMode {
    RouteUnlocked,
    StopUnlocked,
    RouteTravelCount,
    RouteTagTravelCount,
    AnyTravelCount
}

[CreateAssetMenu(menuName = "Activities/Requirements/Transit Route Requirement")]
public class TransitRouteRequirement : ActivityRequirement {
    [Tooltip("Which transit value this requirement checks.")]
    [SerializeField] TransitRequirementMode mode = TransitRequirementMode.RouteUnlocked;
    [Tooltip("Specific route checked by Route Unlocked and Route Travel Count modes.")]
    [SerializeField] TransitRouteDefinition route;
    [Tooltip("Specific stop checked by Stop Unlocked mode.")]
    [SerializeField] TransitStopDefinition stop;
    [Tooltip("Optional route tag checked by Route Tag Travel Count mode.")]
    [SerializeField] string routeTag;
    [Tooltip("Optional origin stop filter used by Route Travel Count mode.")]
    [SerializeField] string originStopId;
    [Tooltip("Optional destination stop filter used by Route Travel Count mode.")]
    [SerializeField] string destinationStopId;
    [Tooltip("Minimum count required by travel count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected transit condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerTransitLog>() : null;
        bool result = mode switch {
            TransitRequirementMode.StopUnlocked => log != null && log.HasUnlockedStop(stop),
            TransitRequirementMode.RouteTravelCount => log != null && log.GetTravelCount(route, originStopId, destinationStopId) >= Mathf.Max(0, requiredCount),
            TransitRequirementMode.RouteTagTravelCount => log != null && log.GetTravelCountWithTag(routeTag) >= Mathf.Max(0, requiredCount),
            TransitRequirementMode.AnyTravelCount => log != null && log.GetTotalTravelCount() >= Mathf.Max(0, requiredCount),
            _ => log != null && log.HasUnlockedRoute(route)
        };

        return mustBeMet ? result : !result;
    }
}
