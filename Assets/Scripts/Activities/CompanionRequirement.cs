using System.Linq;
using UnityEngine;

public enum CompanionRequirementMode {
    FollowingCompanionCountAtLeast,
    AnyFollowingCompanion,
    RoleFollowing,
    PerkActive,
    BondLevelAtLeast,
    BondPointsAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/Companion Requirement")]
public class CompanionRequirement : ActivityRequirement {
    [Tooltip("Which companion condition this requirement checks.")]
    [SerializeField] CompanionRequirementMode mode = CompanionRequirementMode.AnyFollowingCompanion;
    [Tooltip("Role checked by Role Following mode.")]
    [SerializeField] CompanionRoleDefinition role;
    [Tooltip("Perk checked by Perk Active mode.")]
    [SerializeField] CompanionPerkDefinition perk;
    [Tooltip("Minimum bond level checked by Bond Level At Least mode.")]
    [SerializeField] CompanionBondLevel minimumBondLevel = CompanionBondLevel.Familiar;
    [Tooltip("Required count or bond points depending on mode.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("If enabled, the selected companion condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var companions = CompanionController.GetFollowingCompanions(player).ToList();
        bool result = mode switch {
            CompanionRequirementMode.FollowingCompanionCountAtLeast => companions.Count >= Mathf.Max(0, requiredValue),
            CompanionRequirementMode.RoleFollowing => role != null && companions.Any(companion => companion.RoleDefinition == role),
            CompanionRequirementMode.PerkActive => perk != null && companions.Any(companion => companion.HasActivePerk(perk)),
            CompanionRequirementMode.BondLevelAtLeast => companions.Any(companion => companion.BondLevel >= minimumBondLevel),
            CompanionRequirementMode.BondPointsAtLeast => companions.Any(companion => companion.BondPoints >= Mathf.Max(0, requiredValue)),
            _ => companions.Count > 0
        };

        return mustBeMet ? result : !result;
    }
}
