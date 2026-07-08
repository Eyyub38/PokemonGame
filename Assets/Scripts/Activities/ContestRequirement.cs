using UnityEngine;

public enum ContestRequirementMode {
    ContestUnlocked,
    ContestAttemptCount,
    ContestWinCount,
    ContestBestScore,
    ContestBestRankAtLeast,
    ContestTagWinCount,
    ContestAvailable
}

[CreateAssetMenu(menuName = "Activities/Requirements/Contest Requirement")]
public class ContestRequirement : ActivityRequirement {
    [Tooltip("Which contest value this requirement checks.")]
    [SerializeField] ContestRequirementMode mode = ContestRequirementMode.ContestWinCount;
    [Tooltip("Contest checked by contest-specific modes.")]
    [SerializeField] ContestDefinition contest;
    [Tooltip("Tag checked by Contest Tag Win Count mode.")]
    [SerializeField] string tag;
    [Tooltip("Minimum count, score or rank index required by the selected mode.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("If enabled, the selected contest condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerContestLog>() : null;
        bool result = mode switch {
            ContestRequirementMode.ContestUnlocked => log != null && log.HasUnlockedContest(contest),
            ContestRequirementMode.ContestAttemptCount => log != null && log.GetAttemptCount(contest) >= Mathf.Max(0, requiredValue),
            ContestRequirementMode.ContestBestScore => log != null && log.GetBestScore(contest) >= Mathf.Max(0, requiredValue),
            ContestRequirementMode.ContestBestRankAtLeast => log != null && log.GetBestRankIndex(contest) >= Mathf.Max(0, requiredValue),
            ContestRequirementMode.ContestTagWinCount => log != null && log.GetWinCountWithTag(tag) >= Mathf.Max(0, requiredValue),
            ContestRequirementMode.ContestAvailable => contest != null && contest.CanEnter(player, null, out _),
            _ => log != null && log.GetWinCount(contest) >= Mathf.Max(0, requiredValue)
        };

        return mustBeMet ? result : !result;
    }
}
