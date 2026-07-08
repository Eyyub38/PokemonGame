using UnityEngine;

public enum RumorLifecycleRequirementMode {
    IsSpreading,
    StageAtLeast,
    StageEquals,
    ReachedSource,
    OriginRegion
}

[CreateAssetMenu(menuName = "Activities/Requirements/Rumor Lifecycle Requirement")]
public class RumorLifecycleRequirement : ActivityRequirement {
    [Tooltip("Rumor checked by this lifecycle requirement.")]
    [SerializeField] RumorDefinition rumor;
    [Tooltip("Which lifecycle condition this requirement checks.")]
    [SerializeField] RumorLifecycleRequirementMode mode = RumorLifecycleRequirementMode.IsSpreading;
    [Tooltip("Lifecycle stage checked by stage modes.")]
    [SerializeField] RumorLifecycleStage stage = RumorLifecycleStage.Known;
    [Tooltip("Source checked by Reached Source mode.")]
    [SerializeField] RumorSource source;
    [Tooltip("Region checked by Origin Region mode.")]
    [SerializeField] RegionInfoDefinition region;
    [Tooltip("If enabled, the selected lifecycle condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerRumorLifecycleLog>() : null;
        var state = log != null ? log.GetState(rumor) : null;
        bool result = mode switch {
            RumorLifecycleRequirementMode.StageAtLeast => state != null && log.GetStage(rumor) >= stage,
            RumorLifecycleRequirementMode.StageEquals => state != null && log.GetStage(rumor) == stage,
            RumorLifecycleRequirementMode.ReachedSource => state != null && source != null && log.CanHear(rumor, source, out _),
            RumorLifecycleRequirementMode.OriginRegion => state != null && region != null && state.originRegionId == region.Id,
            _ => state != null
        };

        return mustBeMet ? result : !result;
    }
}
