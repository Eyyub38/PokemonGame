using UnityEngine;

public enum MapNavigationRequirementMode {
    HasActiveTarget,
    HasNoActiveTarget,
    ActiveTargetMatchesMarker,
    ActiveTargetHasTag,
    ActiveTargetInRegion,
    ActiveTargetSceneMatches,
    DistanceToTargetAtMost,
    VisibleMarkersInViewAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/Map Navigation Requirement")]
public class MapNavigationRequirement : ActivityRequirement {
    [Tooltip("Which map navigation value this requirement checks.")]
    [SerializeField] MapNavigationRequirementMode mode = MapNavigationRequirementMode.HasActiveTarget;
    [Tooltip("Marker definition checked by target-specific modes.")]
    [SerializeField] MapMarkerDefinition marker;
    [Tooltip("Optional marker id override. Empty uses Marker Definition id.")]
    [SerializeField] string markerId = string.Empty;
    [Tooltip("Tag checked by Active Target Has Tag mode.")]
    [SerializeField] string requiredTag = string.Empty;
    [Tooltip("Region checked by Active Target In Region mode.")]
    [SerializeField] RegionInfoDefinition region;
    [Tooltip("Scene name checked by Active Target Scene Matches mode.")]
    [SerializeField] string sceneName = string.Empty;
    [Tooltip("Map view profile used by Visible Markers In View At Least mode.")]
    [SerializeField] MapViewProfileDefinition viewProfile;
    [Tooltip("Required marker count used by Visible Markers In View At Least mode.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("Maximum distance from player to active target used by Distance To Target At Most mode.")]
    [Min(0f)]
    [SerializeField] float maxDistance = 8f;
    [Tooltip("If enabled, the selected map navigation condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerMapNavigationLog>() : null;
        bool result = mode switch {
            MapNavigationRequirementMode.HasNoActiveTarget => log == null || !log.HasActiveTarget,
            MapNavigationRequirementMode.ActiveTargetMatchesMarker => log != null && log.IsTarget(ResolveMarkerId()),
            MapNavigationRequirementMode.ActiveTargetHasTag => log != null && log.ActiveTargetHasTag(requiredTag),
            MapNavigationRequirementMode.ActiveTargetInRegion => log != null && log.HasActiveTarget && region != null && log.ActiveTarget.regionId == region.Id,
            MapNavigationRequirementMode.ActiveTargetSceneMatches => log != null && log.HasActiveTarget && !string.IsNullOrWhiteSpace(sceneName) && log.ActiveTarget.sceneName == sceneName,
            MapNavigationRequirementMode.DistanceToTargetAtMost => log != null && log.HasActiveTarget && log.GetDistanceToTarget(player.transform.position) >= 0f && log.GetDistanceToTarget(player.transform.position) <= maxDistance,
            MapNavigationRequirementMode.VisibleMarkersInViewAtLeast => viewProfile != null && viewProfile.GetVisibleMarkers(player).Count >= Mathf.Max(0, requiredCount),
            _ => log != null && log.HasActiveTarget
        };

        return mustBeMet ? result : !result;
    }

    string ResolveMarkerId() {
        return !string.IsNullOrWhiteSpace(markerId) ? markerId : marker != null ? marker.Id : string.Empty;
    }
}
