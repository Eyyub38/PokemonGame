using UnityEngine;

public enum ChronicleRequirementMode {
    HasEntry,
    NeverRecorded,
    EntryCountAtLeast,
    SourceEntryCountAtLeast,
    CategoryCountAtLeast,
    TaggedEntryCountAtLeast,
    RegionEntryCountAtLeast,
    SceneEntryCountAtLeast,
    LocationKeyEntryCountAtLeast,
    HoursSinceLastEntryAtLeast,
    HoursSinceLastEntryAtMost
}

[CreateAssetMenu(menuName = "Activities/Requirements/Chronicle Requirement")]
public class ChronicleRequirement : ActivityRequirement {
    [Header("Mode")]
    [Tooltip("How this requirement checks PlayerChronicleLog.")]
    [SerializeField] ChronicleRequirementMode mode = ChronicleRequirementMode.HasEntry;

    [Header("Filters")]
    [Tooltip("Optional chronicle entry definition filter.")]
    [SerializeField] ChronicleEntryDefinition entry = null;
    [Tooltip("Optional source id filter. Empty ignores source id.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Optional category filter used by category-based modes.")]
    [SerializeField] ChronicleCategory category = ChronicleCategory.General;
    [Tooltip("Optional free-form tag filter used by tagged modes.")]
    [SerializeField] string tag = string.Empty;
    [Tooltip("Optional region filter used by region-based modes.")]
    [SerializeField] RegionInfoDefinition region = null;
    [Tooltip("Optional scene name filter used by scene-based modes.")]
    [SerializeField] string sceneName = string.Empty;
    [Tooltip("Optional location key filter used by location key modes.")]
    [SerializeField] string locationKey = string.Empty;
    [Tooltip("If enabled, blocked chronicle attempts can satisfy this requirement.")]
    [SerializeField] bool includeBlocked;

    [Header("Thresholds")]
    [Tooltip("Minimum count or hour threshold depending on the selected mode.")]
    [Min(0)]
    [SerializeField] int requiredAmount = 1;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerChronicleLog>() : null;
        if(log == null) {
            return false;
        }

        switch(mode) {
            case ChronicleRequirementMode.HasEntry:
                return log.HasEntry(entry, sourceId, includeBlocked);
            case ChronicleRequirementMode.NeverRecorded:
                return !log.HasEntry(entry, sourceId, includeBlocked);
            case ChronicleRequirementMode.EntryCountAtLeast:
                return log.GetEntryCount(entry, includeBlocked: includeBlocked) >= requiredAmount;
            case ChronicleRequirementMode.SourceEntryCountAtLeast:
                return log.GetEntryCount(entry, sourceId: sourceId, includeBlocked: includeBlocked) >= requiredAmount;
            case ChronicleRequirementMode.CategoryCountAtLeast:
                return log.GetEntryCount(category: category, includeBlocked: includeBlocked) >= requiredAmount;
            case ChronicleRequirementMode.TaggedEntryCountAtLeast:
                return log.GetEntryCount(tag: tag, includeBlocked: includeBlocked) >= requiredAmount;
            case ChronicleRequirementMode.RegionEntryCountAtLeast:
                return log.GetEntryCount(region: region, includeBlocked: includeBlocked) >= requiredAmount;
            case ChronicleRequirementMode.SceneEntryCountAtLeast:
                return log.GetEntryCount(sceneName: sceneName, includeBlocked: includeBlocked) >= requiredAmount;
            case ChronicleRequirementMode.LocationKeyEntryCountAtLeast:
                return log.GetEntryCount(locationKey: locationKey, includeBlocked: includeBlocked) >= requiredAmount;
            case ChronicleRequirementMode.HoursSinceLastEntryAtLeast:
                return log.GetHoursSinceLastEntry(entry, sourceId, includeBlocked) >= requiredAmount;
            case ChronicleRequirementMode.HoursSinceLastEntryAtMost: {
                int hours = log.GetHoursSinceLastEntry(entry, sourceId, includeBlocked);
                return hours >= 0 && hours <= requiredAmount;
            }
            default:
                return false;
        }
    }
}
