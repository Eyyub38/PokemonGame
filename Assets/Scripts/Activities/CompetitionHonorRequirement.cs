using UnityEngine;

public enum CompetitionHonorRequirementMode {
    HonorEarned,
    HonorNotEarned,
    HonorKindCountAtLeast,
    HonorTagCountAtLeast,
    CanAwardHonor
}

[CreateAssetMenu(menuName = "Activities/Requirements/Competition Honor Requirement")]
public class CompetitionHonorRequirement : ActivityRequirement {
    [Tooltip("Which honor value this requirement checks.")]
    [SerializeField] CompetitionHonorRequirementMode mode = CompetitionHonorRequirementMode.HonorEarned;
    [Tooltip("Honor checked by honor-specific modes.")]
    [SerializeField] CompetitionHonorDefinition honor;
    [Tooltip("Honor kind checked by Honor Kind Count At Least mode.")]
    [SerializeField] CompetitionHonorKind kind = CompetitionHonorKind.Medal;
    [Tooltip("Tag checked by Honor Tag Count At Least mode.")]
    [SerializeField] string tag;
    [Tooltip("Minimum count required by count-based modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected honor condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerCompetitionHonorLog>() : null;
        bool result = mode switch {
            CompetitionHonorRequirementMode.HonorNotEarned => log != null && !log.HasHonor(honor),
            CompetitionHonorRequirementMode.HonorKindCountAtLeast => log != null && log.GetHonorCount(kind) >= Mathf.Max(0, requiredCount),
            CompetitionHonorRequirementMode.HonorTagCountAtLeast => log != null && log.GetHonorCountWithTag(tag) >= Mathf.Max(0, requiredCount),
            CompetitionHonorRequirementMode.CanAwardHonor => honor != null && honor.CanAward(player, out _),
            _ => log != null && log.HasHonor(honor)
        };

        return mustBeMet ? result : !result;
    }
}
