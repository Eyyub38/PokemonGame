using UnityEngine;

public enum CompetitionPrizeRequirementMode {
    PrizeAwarded,
    PrizeAwardCountAtLeast,
    PrizeNotAwarded,
    AwardedPrizeWithTag
}

[CreateAssetMenu(menuName = "Activities/Requirements/Competition Prize Requirement")]
public class CompetitionPrizeRequirement : ActivityRequirement {
    [Tooltip("Which prize log value this requirement checks.")]
    [SerializeField] CompetitionPrizeRequirementMode mode = CompetitionPrizeRequirementMode.PrizeAwarded;
    [Tooltip("Prize table checked by prize-specific modes.")]
    [SerializeField] CompetitionPrizeTableDefinition prizeTable;
    [Tooltip("Tag checked by Awarded Prize With Tag mode.")]
    [SerializeField] string tag;
    [Tooltip("Minimum award count required by count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected prize condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerCompetitionPrizeLog>() : null;
        bool result = mode switch {
            CompetitionPrizeRequirementMode.PrizeAwardCountAtLeast => log != null && log.GetAwardCount(prizeTable) >= Mathf.Max(0, requiredCount),
            CompetitionPrizeRequirementMode.PrizeNotAwarded => log != null && !log.HasAwarded(prizeTable),
            CompetitionPrizeRequirementMode.AwardedPrizeWithTag => HasAwardedPrizeWithTag(log),
            _ => log != null && log.HasAwarded(prizeTable)
        };

        return mustBeMet ? result : !result;
    }

    bool HasAwardedPrizeWithTag(PlayerCompetitionPrizeLog log) {
        if(log == null || string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        int required = Mathf.Max(0, requiredCount);
        foreach(var candidate in Resources.LoadAll<CompetitionPrizeTableDefinition>("")) {
            if(candidate != null && candidate.HasTag(tag) && log.GetAwardCount(candidate) >= required) {
                return true;
            }
        }

        return false;
    }
}
