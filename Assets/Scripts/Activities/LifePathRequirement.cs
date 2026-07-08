using UnityEngine;

public enum LifePathRequirementMode {
    PathExperienceAtLeast,
    AvailablePerkPointsAtLeast,
    SpentPerkPointsAtLeast,
    BranchProgressAtLeast,
    TagProgressAtLeast,
    HasUnlockedPerk,
    AnyPathWithTagExperienceAtLeast,
    DominantPath,
    DominantPathHasTag
}

[CreateAssetMenu(menuName = "Activities/Requirements/Life Path Requirement")]
public class LifePathRequirement : ActivityRequirement {
    [Tooltip("How this requirement checks player life path state.")]
    [SerializeField] LifePathRequirementMode mode = LifePathRequirementMode.PathExperienceAtLeast;
    [Tooltip("Life path checked by path-specific modes.")]
    [SerializeField] LifePathDefinition lifePath = null;
    [Tooltip("Perk checked by Has Unlocked Perk mode.")]
    [SerializeField] LifePathPerkDefinition perk = null;
    [Tooltip("Branch id checked by Branch Progress At Least mode.")]
    [SerializeField] string branchId = string.Empty;
    [Tooltip("Tag checked by tag-based modes.")]
    [SerializeField] string tag = string.Empty;
    [Tooltip("Minimum value required by XP, perk point, branch and tag modes.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("If enabled, the selected condition must be true. If disabled, the selected condition must be false.")]
    [SerializeField] bool expected = true;

    public LifePathRequirementMode Mode => mode;
    public LifePathDefinition LifePath => lifePath;
    public LifePathPerkDefinition Perk => perk;
    public string BranchId => branchId;
    public string Tag => tag;
    public int RequiredValue => Mathf.Max(0, requiredValue);
    public bool Expected => expected;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerLifePathLog>() : null;
        bool value = mode switch {
            LifePathRequirementMode.PathExperienceAtLeast => log != null && log.GetTotalExperience(lifePath) >= Mathf.Max(0, requiredValue),
            LifePathRequirementMode.AvailablePerkPointsAtLeast => log != null && log.GetAvailablePerkPoints(lifePath) >= Mathf.Max(0, requiredValue),
            LifePathRequirementMode.SpentPerkPointsAtLeast => log != null && log.GetSpentPerkPoints(lifePath) >= Mathf.Max(0, requiredValue),
            LifePathRequirementMode.BranchProgressAtLeast => log != null && log.GetBranchProgress(lifePath, branchId) >= Mathf.Max(0, requiredValue),
            LifePathRequirementMode.TagProgressAtLeast => log != null && log.GetTagProgress(lifePath, tag) >= Mathf.Max(0, requiredValue),
            LifePathRequirementMode.HasUnlockedPerk => log != null && log.HasPerk(perk),
            LifePathRequirementMode.AnyPathWithTagExperienceAtLeast => log != null && log.HasAnyPathWithTag(tag, requiredValue),
            LifePathRequirementMode.DominantPath => log != null && log.DominantPathIs(lifePath),
            LifePathRequirementMode.DominantPathHasTag => log != null && log.DominantPathHasTag(tag),
            _ => false
        };

        return expected ? value : !value;
    }
}
