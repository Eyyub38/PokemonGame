using UnityEngine;

public enum RumorRequirementMode {
    RumorUnlocked,
    RumorHeard,
    RumorHeardCount,
    RumorTagHeard,
    RumorRead,
    RumorDismissed
}

[CreateAssetMenu(menuName = "Activities/Requirements/Rumor Requirement")]
public class RumorRequirement : ActivityRequirement {
    [Tooltip("Which rumor value this requirement checks.")]
    [SerializeField] RumorRequirementMode mode = RumorRequirementMode.RumorHeard;
    [Tooltip("Rumor checked by this requirement.")]
    [SerializeField] RumorDefinition rumor;
    [Tooltip("Optional source id filter used by heard count modes.")]
    [SerializeField] string sourceId;
    [Tooltip("Tag checked by Rumor Tag Heard mode.")]
    [SerializeField] string tag;
    [Tooltip("Minimum count required by count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected rumor condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerRumorLog>() : null;
        bool result = mode switch {
            RumorRequirementMode.RumorUnlocked => log != null && log.HasUnlockedRumor(rumor),
            RumorRequirementMode.RumorHeardCount => log != null && log.GetHeardCount(rumor, sourceId) >= Mathf.Max(0, requiredCount),
            RumorRequirementMode.RumorTagHeard => log != null && log.GetHeardCountWithTag(tag) >= Mathf.Max(0, requiredCount),
            RumorRequirementMode.RumorRead => log != null && log.IsRumorRead(rumor),
            RumorRequirementMode.RumorDismissed => log != null && log.IsRumorDismissed(rumor),
            _ => log != null && log.HasHeardRumor(rumor, sourceId)
        };

        return mustBeMet ? result : !result;
    }
}
