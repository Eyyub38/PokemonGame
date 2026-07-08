using UnityEngine;

public enum AccessProfileRequirementMode {
    CanPass,
    HasPassed,
    PassedCount,
    DeniedCount
}

[CreateAssetMenu(menuName = "Activities/Requirements/Access Profile Requirement")]
public class AccessProfileRequirement : ActivityRequirement {
    [Tooltip("Which access value this requirement checks.")]
    [SerializeField] AccessProfileRequirementMode mode = AccessProfileRequirementMode.CanPass;
    [Tooltip("Access profile checked by this requirement.")]
    [SerializeField] AccessProfileDefinition accessProfile;
    [Tooltip("Optional gate/context id filter. Empty accepts any context for history checks.")]
    [SerializeField] string contextId;
    [Tooltip("Minimum count required by count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected access condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerAccessLog>() : null;
        bool result = mode switch {
            AccessProfileRequirementMode.HasPassed => log != null && log.HasPassed(accessProfile, contextId),
            AccessProfileRequirementMode.PassedCount => log != null && log.GetPassedCount(accessProfile, contextId) >= Mathf.Max(0, requiredCount),
            AccessProfileRequirementMode.DeniedCount => log != null && log.GetDeniedCount(accessProfile, contextId) >= Mathf.Max(0, requiredCount),
            _ => accessProfile != null && accessProfile.CanAccess(player, out _)
        };

        return mustBeMet ? result : !result;
    }
}
