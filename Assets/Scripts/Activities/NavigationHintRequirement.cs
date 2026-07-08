using UnityEngine;

public enum NavigationHintRequirementMode {
    HasActiveHint,
    DoesNotHaveActiveHint,
    HasCompletedHint,
    ActivationCountAtLeast,
    SourceActivationCountAtLeast,
    ActiveCategoryCountAtLeast,
    CompletedCategoryCountAtLeast,
    ActiveTaggedCountAtLeast,
    CompletedTaggedCountAtLeast,
    RegionHintCountAtLeast,
    SceneHintCountAtLeast,
    LocationKeyHintCountAtLeast,
    HoursSinceLastActivationAtLeast,
    HoursSinceLastActivationAtMost
}

[CreateAssetMenu(menuName = "Activities/Requirements/Navigation Hint Requirement")]
public class NavigationHintRequirement : ActivityRequirement {
    [Header("Mode")]
    [Tooltip("How this requirement checks PlayerNavigationHintLog.")]
    [SerializeField] NavigationHintRequirementMode mode = NavigationHintRequirementMode.HasActiveHint;

    [Header("Filters")]
    [Tooltip("Optional navigation hint definition filter.")]
    [SerializeField] NavigationHintDefinition hint = null;
    [Tooltip("Optional source id filter. Empty ignores source id.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Optional category filter used by category-based modes.")]
    [SerializeField] NavigationHintCategory category = NavigationHintCategory.General;
    [Tooltip("Optional free-form tag filter used by tagged modes.")]
    [SerializeField] string tag = string.Empty;
    [Tooltip("Optional region filter used by region-based modes.")]
    [SerializeField] RegionInfoDefinition region = null;
    [Tooltip("Optional scene name filter used by scene-based modes.")]
    [SerializeField] string sceneName = string.Empty;
    [Tooltip("Optional location key filter used by location key modes.")]
    [SerializeField] string locationKey = string.Empty;
    [Tooltip("If enabled, blocked hint records can satisfy history-based checks.")]
    [SerializeField] bool includeBlocked;

    [Header("Thresholds")]
    [Tooltip("Minimum count or hour threshold depending on the selected mode.")]
    [Min(0)]
    [SerializeField] int requiredAmount = 1;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerNavigationHintLog>() : null;
        if(log == null) {
            return false;
        }

        switch(mode) {
            case NavigationHintRequirementMode.HasActiveHint:
                return log.HasActiveHint(hint, sourceId);
            case NavigationHintRequirementMode.DoesNotHaveActiveHint:
                return !log.HasActiveHint(hint, sourceId);
            case NavigationHintRequirementMode.HasCompletedHint:
                return log.GetRecordCount(hint, status: NavigationHintRecordStatus.Completed, includeBlocked: includeBlocked) > 0;
            case NavigationHintRequirementMode.ActivationCountAtLeast:
                return CountActivations(log, hint, null) >= requiredAmount;
            case NavigationHintRequirementMode.SourceActivationCountAtLeast:
                return CountActivations(log, hint, sourceId) >= requiredAmount;
            case NavigationHintRequirementMode.ActiveCategoryCountAtLeast:
                return log.ActiveHints != null && CountActiveByCategory(log) >= requiredAmount;
            case NavigationHintRequirementMode.CompletedCategoryCountAtLeast:
                return log.GetRecordCount(status: NavigationHintRecordStatus.Completed, category: category, includeBlocked: includeBlocked) >= requiredAmount;
            case NavigationHintRequirementMode.ActiveTaggedCountAtLeast:
                return CountActiveByTag(log) >= requiredAmount;
            case NavigationHintRequirementMode.CompletedTaggedCountAtLeast:
                return log.GetRecordCount(status: NavigationHintRecordStatus.Completed, tag: tag, includeBlocked: includeBlocked) >= requiredAmount;
            case NavigationHintRequirementMode.RegionHintCountAtLeast:
                return log.GetRecordCount(region: region, includeBlocked: includeBlocked) >= requiredAmount;
            case NavigationHintRequirementMode.SceneHintCountAtLeast:
                return log.GetRecordCount(sceneName: sceneName, includeBlocked: includeBlocked) >= requiredAmount;
            case NavigationHintRequirementMode.LocationKeyHintCountAtLeast:
                return log.GetRecordCount(locationKey: locationKey, includeBlocked: includeBlocked) >= requiredAmount;
            case NavigationHintRequirementMode.HoursSinceLastActivationAtLeast:
                return log.GetHoursSinceLastRecord(hint, sourceId, NavigationHintRecordStatus.Activated, includeBlocked) >= requiredAmount;
            case NavigationHintRequirementMode.HoursSinceLastActivationAtMost: {
                int hours = log.GetHoursSinceLastRecord(hint, sourceId, NavigationHintRecordStatus.Activated, includeBlocked);
                return hours >= 0 && hours <= requiredAmount;
            }
            default:
                return false;
        }
    }

    int CountActivations(PlayerNavigationHintLog log, NavigationHintDefinition hintFilter, string sourceFilter) {
        return log.GetRecordCount(hintFilter, sourceFilter, NavigationHintRecordStatus.Activated, includeBlocked: includeBlocked)
            + log.GetRecordCount(hintFilter, sourceFilter, NavigationHintRecordStatus.Refreshed, includeBlocked: includeBlocked);
    }

    int CountActiveByCategory(PlayerNavigationHintLog log) {
        int count = 0;
        foreach(var activeHint in log.ActiveHints) {
            if(activeHint != null && activeHint.category == category) {
                count++;
            }
        }

        return count;
    }

    int CountActiveByTag(PlayerNavigationHintLog log) {
        int count = 0;
        foreach(var activeHint in log.ActiveHints) {
            if(activeHint != null && activeHint.HasTag(tag)) {
                count++;
            }
        }

        return count;
    }
}
