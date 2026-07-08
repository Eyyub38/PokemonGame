using UnityEngine;

public enum WorldTriggerRequirementMode {
    HasTriggered,
    NeverTriggered,
    TriggerCountAtLeast,
    SourceTriggerCountAtLeast,
    HoursSinceLastTriggerAtLeast,
    HoursSinceLastTriggerAtMost
}

[CreateAssetMenu(menuName = "Activities/Requirements/World Trigger Requirement")]
public class WorldTriggerRequirement : ActivityRequirement {
    [Tooltip("Which world trigger history check this requirement performs.")]
    [SerializeField] WorldTriggerRequirementMode mode = WorldTriggerRequirementMode.HasTriggered;
    [Tooltip("World trigger checked by this requirement.")]
    [SerializeField] WorldTriggerDefinition trigger = null;
    [Tooltip("Optional source id filter for source/count/time modes. Empty checks all sources except Source Trigger Count At Least.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Minimum count required by count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("Minimum/maximum in-game hours used by Hours Since Last Trigger modes.")]
    [Min(0)]
    [SerializeField] int requiredHours = 1;
    [Tooltip("If enabled, blocked attempts are counted too. If disabled, only successful trigger runs count.")]
    [SerializeField] bool includeBlockedAttempts;
    [Tooltip("If enabled, the selected condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerWorldTriggerLog>() : null;
        bool result = mode switch {
            WorldTriggerRequirementMode.NeverTriggered => log == null || !log.HasTriggered(trigger, sourceId, includeBlockedAttempts),
            WorldTriggerRequirementMode.TriggerCountAtLeast => log != null && log.GetTriggerCount(trigger, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            WorldTriggerRequirementMode.SourceTriggerCountAtLeast => log != null && log.GetTriggerCount(trigger, sourceId, includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            WorldTriggerRequirementMode.HoursSinceLastTriggerAtLeast => HasHoursSinceLastTrigger(log, atLeast: true),
            WorldTriggerRequirementMode.HoursSinceLastTriggerAtMost => HasHoursSinceLastTrigger(log, atLeast: false),
            _ => log != null && log.HasTriggered(trigger, sourceId, includeBlockedAttempts)
        };

        return mustBeMet ? result : !result;
    }

    bool HasHoursSinceLastTrigger(PlayerWorldTriggerLog log, bool atLeast) {
        if(log == null || trigger == null) {
            return false;
        }

        int hours = log.GetHoursSinceLastTrigger(trigger, sourceId, includeBlockedAttempts);
        if(hours < 0) {
            return false;
        }

        int required = Mathf.Max(0, requiredHours);
        return atLeast ? hours >= required : hours <= required;
    }
}
