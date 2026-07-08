using UnityEngine;

public enum LocationVisitRequirementMode {
    HasVisited,
    NeverVisited,
    VisitCountAtLeast,
    SourceVisitCountAtLeast,
    KindVisitCountAtLeast,
    RegionVisitCountAtLeast,
    SceneVisitCountAtLeast,
    LocationKeyVisitCountAtLeast,
    HoursSinceLastVisitAtLeast,
    HoursSinceLastVisitAtMost
}

[CreateAssetMenu(menuName = "Activities/Requirements/Location Visit Requirement")]
public class LocationVisitRequirement : ActivityRequirement {
    [Tooltip("Which location visit history check this requirement performs.")]
    [SerializeField] LocationVisitRequirementMode mode = LocationVisitRequirementMode.HasVisited;
    [Tooltip("Specific visit definition checked by this requirement. Empty allows broader filters.")]
    [SerializeField] LocationVisitDefinition visit = null;
    [Tooltip("Optional source id filter for source/count/time modes.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Visit kind checked by Kind Visit Count At Least.")]
    [SerializeField] LocationVisitKind kind = LocationVisitKind.General;
    [Tooltip("Region checked by Region Visit Count At Least.")]
    [SerializeField] RegionInfoDefinition region = null;
    [Tooltip("Scene name checked by Scene Visit Count At Least.")]
    [SerializeField] string sceneName = string.Empty;
    [Tooltip("Location key checked by Location Key Visit Count At Least.")]
    [SerializeField] string locationKey = string.Empty;
    [Tooltip("Minimum count required by count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("Minimum/maximum in-game hours used by Hours Since Last Visit modes.")]
    [Min(0)]
    [SerializeField] int requiredHours = 1;
    [Tooltip("If enabled, blocked attempts are counted too. If disabled, only successful visits count.")]
    [SerializeField] bool includeBlockedAttempts;
    [Tooltip("If enabled, the selected condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerLocationVisitLog>() : null;
        bool result = mode switch {
            LocationVisitRequirementMode.NeverVisited => log == null || !log.HasVisited(visit, sourceId, includeBlockedAttempts),
            LocationVisitRequirementMode.VisitCountAtLeast => log != null && log.GetVisitCount(visit, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            LocationVisitRequirementMode.SourceVisitCountAtLeast => log != null && log.GetVisitCount(visit, sourceId: sourceId, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            LocationVisitRequirementMode.KindVisitCountAtLeast => log != null && log.GetVisitCount(visit, sourceId: sourceId, kind: kind, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            LocationVisitRequirementMode.RegionVisitCountAtLeast => log != null && log.GetVisitCount(visit, sourceId: sourceId, region: region, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            LocationVisitRequirementMode.SceneVisitCountAtLeast => log != null && log.GetVisitCount(visit, sourceId: sourceId, sceneName: sceneName, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            LocationVisitRequirementMode.LocationKeyVisitCountAtLeast => log != null && log.GetVisitCount(visit, sourceId: sourceId, locationKey: locationKey, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            LocationVisitRequirementMode.HoursSinceLastVisitAtLeast => HasHoursSinceLastVisit(log, atLeast: true),
            LocationVisitRequirementMode.HoursSinceLastVisitAtMost => HasHoursSinceLastVisit(log, atLeast: false),
            _ => log != null && log.HasVisited(visit, sourceId, includeBlockedAttempts)
        };

        return mustBeMet ? result : !result;
    }

    bool HasHoursSinceLastVisit(PlayerLocationVisitLog log, bool atLeast) {
        if(log == null) {
            return false;
        }

        int hours = log.GetHoursSinceLastVisit(visit, sourceId, includeBlockedAttempts);
        if(hours < 0) {
            return false;
        }

        int required = Mathf.Max(0, requiredHours);
        return atLeast ? hours >= required : hours <= required;
    }
}
