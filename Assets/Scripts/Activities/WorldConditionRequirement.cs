using UnityEngine;

public enum WorldConditionRequirementMode {
    ConditionActive,
    ConditionInactive,
    ActiveConditionCount,
    ActiveTagCount,
    ActiveCategoryCount,
    StackAtLeast,
    IntensityAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/World Condition Requirement")]
public class WorldConditionRequirement : ActivityRequirement {
    [Tooltip("Which world condition check this requirement performs.")]
    [SerializeField] WorldConditionRequirementMode mode = WorldConditionRequirementMode.ConditionActive;
    [Tooltip("Specific condition checked by condition, stack and intensity modes.")]
    [SerializeField] WorldConditionDefinition condition = null;
    [Tooltip("Source id filter. Empty accepts any source.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Tag checked by Active Tag Count mode.")]
    [SerializeField] string conditionTag = string.Empty;
    [Tooltip("Category checked by Active Category Count mode.")]
    [SerializeField] WorldConditionCategory category = WorldConditionCategory.General;
    [Tooltip("Minimum value required by count and stack modes.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("Minimum intensity required by Intensity At Least mode.")]
    [Min(0f)]
    [SerializeField] float requiredIntensity = 1f;
    [Tooltip("If enabled, the selected condition check must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerWorldConditionLog>() : null;
        bool result = mode switch {
            WorldConditionRequirementMode.ConditionInactive => log == null || !log.IsConditionActive(condition, sourceId),
            WorldConditionRequirementMode.ActiveConditionCount => log != null && log.GetActiveCount(condition) >= Mathf.Max(0, requiredValue),
            WorldConditionRequirementMode.ActiveTagCount => log != null && log.GetActiveCount(tag: conditionTag) >= Mathf.Max(0, requiredValue),
            WorldConditionRequirementMode.ActiveCategoryCount => log != null && log.GetActiveCount(category: category) >= Mathf.Max(0, requiredValue),
            WorldConditionRequirementMode.StackAtLeast => HasState(log, state => state.stacks >= Mathf.Max(0, requiredValue)),
            WorldConditionRequirementMode.IntensityAtLeast => HasState(log, state => state.intensity >= Mathf.Max(0f, requiredIntensity)),
            _ => log != null && log.IsConditionActive(condition, sourceId)
        };

        return mustBeMet ? result : !result;
    }

    bool HasState(PlayerWorldConditionLog log, System.Func<PlayerWorldConditionState, bool> predicate) {
        if(log == null || condition == null) {
            return false;
        }

        log.PruneExpired();
        foreach(var state in log.ActiveConditions) {
            if(state != null
                && state.conditionId == condition.Id
                && (string.IsNullOrWhiteSpace(sourceId) || state.sourceId == sourceId)
                && predicate(state)) {
                return true;
            }
        }

        return false;
    }
}
