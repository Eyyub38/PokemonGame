using UnityEngine;

public enum AreaProfileRequirementMode {
    HasActiveArea,
    DoesNotHaveActiveArea,
    HasEnteredArea,
    NeverEnteredArea,
    EnterCountAtLeast,
    SourceEnterCountAtLeast,
    ActiveCategoryCountAtLeast,
    EnteredCategoryCountAtLeast,
    ActiveTaggedCountAtLeast,
    EnteredTaggedCountAtLeast,
    RegionEnterCountAtLeast,
    ActivityZoneEnterCountAtLeast,
    SceneEnterCountAtLeast,
    AreaKeyEnterCountAtLeast,
    HoursSinceLastEnterAtLeast,
    HoursSinceLastEnterAtMost
}

[CreateAssetMenu(menuName = "Activities/Requirements/Area Profile Requirement")]
public class AreaProfileRequirement : ActivityRequirement {
    [Header("Mode")]
    [Tooltip("How this requirement checks PlayerAreaProfileLog.")]
    [SerializeField] AreaProfileRequirementMode mode = AreaProfileRequirementMode.HasActiveArea;

    [Header("Filters")]
    [Tooltip("Optional area profile definition filter.")]
    [SerializeField] AreaProfileDefinition areaProfile = null;
    [Tooltip("Optional source id filter. Empty ignores source id.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Optional category filter used by category-based modes.")]
    [SerializeField] AreaProfileCategory category = AreaProfileCategory.General;
    [Tooltip("Optional free-form tag filter used by tagged modes.")]
    [SerializeField] string tag = string.Empty;
    [Tooltip("Optional region filter used by region-based modes.")]
    [SerializeField] RegionInfoDefinition region = null;
    [Tooltip("Optional activity zone filter used by zone-based modes.")]
    [SerializeField] ActivityZoneDefinition activityZone = null;
    [Tooltip("Optional scene name filter used by scene-based modes.")]
    [SerializeField] string sceneName = string.Empty;
    [Tooltip("Optional area key filter used by area key modes.")]
    [SerializeField] string areaKey = string.Empty;
    [Tooltip("If enabled, blocked area records can satisfy history-based checks.")]
    [SerializeField] bool includeBlocked;

    [Header("Thresholds")]
    [Tooltip("Minimum count or hour threshold depending on the selected mode.")]
    [Min(0)]
    [SerializeField] int requiredAmount = 1;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerAreaProfileLog>() : null;
        if(log == null) {
            return false;
        }

        switch(mode) {
            case AreaProfileRequirementMode.HasActiveArea:
                return log.HasActiveArea(areaProfile, sourceId);
            case AreaProfileRequirementMode.DoesNotHaveActiveArea:
                return !log.HasActiveArea(areaProfile, sourceId);
            case AreaProfileRequirementMode.HasEnteredArea:
                return CountEnters(log, areaProfile, sourceId) > 0;
            case AreaProfileRequirementMode.NeverEnteredArea:
                return CountEnters(log, areaProfile, sourceId) == 0;
            case AreaProfileRequirementMode.EnterCountAtLeast:
                return CountEnters(log, areaProfile, null) >= requiredAmount;
            case AreaProfileRequirementMode.SourceEnterCountAtLeast:
                return CountEnters(log, areaProfile, sourceId) >= requiredAmount;
            case AreaProfileRequirementMode.ActiveCategoryCountAtLeast:
                return CountActiveByCategory(log) >= requiredAmount;
            case AreaProfileRequirementMode.EnteredCategoryCountAtLeast:
                return CountEnteredByCategory(log) >= requiredAmount;
            case AreaProfileRequirementMode.ActiveTaggedCountAtLeast:
                return CountActiveByTag(log) >= requiredAmount;
            case AreaProfileRequirementMode.EnteredTaggedCountAtLeast:
                return CountEnteredByTag(log) >= requiredAmount;
            case AreaProfileRequirementMode.RegionEnterCountAtLeast:
                return CountRegionEnters(log) >= requiredAmount;
            case AreaProfileRequirementMode.ActivityZoneEnterCountAtLeast:
                return CountZoneEnters(log) >= requiredAmount;
            case AreaProfileRequirementMode.SceneEnterCountAtLeast:
                return CountSceneEnters(log) >= requiredAmount;
            case AreaProfileRequirementMode.AreaKeyEnterCountAtLeast:
                return CountAreaKeyEnters(log) >= requiredAmount;
            case AreaProfileRequirementMode.HoursSinceLastEnterAtLeast:
                return log.GetHoursSinceLastEnter(areaProfile, sourceId, includeBlocked) >= requiredAmount;
            case AreaProfileRequirementMode.HoursSinceLastEnterAtMost: {
                int hours = log.GetHoursSinceLastEnter(areaProfile, sourceId, includeBlocked);
                return hours >= 0 && hours <= requiredAmount;
            }
            default:
                return false;
        }
    }

    int CountEnters(PlayerAreaProfileLog log, AreaProfileDefinition profile, string sourceFilter) {
        return log.GetRecordCount(profile, sourceFilter, AreaProfileRecordStatus.Entered, includeBlocked: includeBlocked)
            + log.GetRecordCount(profile, sourceFilter, AreaProfileRecordStatus.Refreshed, includeBlocked: includeBlocked);
    }

    int CountActiveByCategory(PlayerAreaProfileLog log) {
        int count = 0;
        foreach(var activeArea in log.ActiveAreas) {
            if(activeArea != null && activeArea.category == category) {
                count++;
            }
        }

        return count;
    }

    int CountEnteredByCategory(PlayerAreaProfileLog log) {
        return log.GetRecordCount(status: AreaProfileRecordStatus.Entered, category: category, includeBlocked: includeBlocked)
            + log.GetRecordCount(status: AreaProfileRecordStatus.Refreshed, category: category, includeBlocked: includeBlocked);
    }

    int CountActiveByTag(PlayerAreaProfileLog log) {
        int count = 0;
        foreach(var activeArea in log.ActiveAreas) {
            if(activeArea != null && activeArea.HasTag(tag)) {
                count++;
            }
        }

        return count;
    }

    int CountEnteredByTag(PlayerAreaProfileLog log) {
        return log.GetRecordCount(status: AreaProfileRecordStatus.Entered, tag: tag, includeBlocked: includeBlocked)
            + log.GetRecordCount(status: AreaProfileRecordStatus.Refreshed, tag: tag, includeBlocked: includeBlocked);
    }

    int CountRegionEnters(PlayerAreaProfileLog log) {
        return log.GetRecordCount(status: AreaProfileRecordStatus.Entered, region: region, includeBlocked: includeBlocked)
            + log.GetRecordCount(status: AreaProfileRecordStatus.Refreshed, region: region, includeBlocked: includeBlocked);
    }

    int CountZoneEnters(PlayerAreaProfileLog log) {
        return log.GetRecordCount(status: AreaProfileRecordStatus.Entered, zone: activityZone, includeBlocked: includeBlocked)
            + log.GetRecordCount(status: AreaProfileRecordStatus.Refreshed, zone: activityZone, includeBlocked: includeBlocked);
    }

    int CountSceneEnters(PlayerAreaProfileLog log) {
        return log.GetRecordCount(status: AreaProfileRecordStatus.Entered, sceneName: sceneName, includeBlocked: includeBlocked)
            + log.GetRecordCount(status: AreaProfileRecordStatus.Refreshed, sceneName: sceneName, includeBlocked: includeBlocked);
    }

    int CountAreaKeyEnters(PlayerAreaProfileLog log) {
        return log.GetRecordCount(status: AreaProfileRecordStatus.Entered, areaKey: areaKey, includeBlocked: includeBlocked)
            + log.GetRecordCount(status: AreaProfileRecordStatus.Refreshed, areaKey: areaKey, includeBlocked: includeBlocked);
    }
}
