using UnityEngine;

public enum CompetitionRankingRequirementMode {
    RankingUnlocked,
    CurrentPointsAtLeast,
    LifetimePointsAtLeast,
    BestPointsAtLeast,
    RankTierReached,
    RankingCanScore,
    ReachedRankingWithTag
}

[CreateAssetMenu(menuName = "Activities/Requirements/Competition Ranking Requirement")]
public class CompetitionRankingRequirement : ActivityRequirement {
    [Tooltip("Which ranking value this requirement checks.")]
    [SerializeField] CompetitionRankingRequirementMode mode = CompetitionRankingRequirementMode.CurrentPointsAtLeast;
    [Tooltip("Ranking track checked by track-specific modes.")]
    [SerializeField] CompetitionRankingDefinition ranking;
    [Tooltip("Tier id checked by Rank Tier Reached mode.")]
    [SerializeField] string tierId;
    [Tooltip("Tag checked by Reached Ranking With Tag mode.")]
    [SerializeField] string tag;
    [Tooltip("Minimum point amount required by point-based modes.")]
    [Min(0)]
    [SerializeField] int requiredPoints = 1;
    [Tooltip("If enabled, the selected ranking condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerCompetitionRankingLog>() : null;
        bool result = mode switch {
            CompetitionRankingRequirementMode.RankingUnlocked => log != null && log.HasUnlocked(ranking),
            CompetitionRankingRequirementMode.LifetimePointsAtLeast => log != null && log.GetLifetimePoints(ranking) >= Mathf.Max(0, requiredPoints),
            CompetitionRankingRequirementMode.BestPointsAtLeast => log != null && log.GetBestPoints(ranking) >= Mathf.Max(0, requiredPoints),
            CompetitionRankingRequirementMode.RankTierReached => log != null && HasReachedTier(log),
            CompetitionRankingRequirementMode.RankingCanScore => ranking != null && ranking.CanScore(player, log, out _),
            CompetitionRankingRequirementMode.ReachedRankingWithTag => HasReachedRankingWithTag(log),
            _ => log != null && log.GetCurrentPoints(ranking) >= Mathf.Max(0, requiredPoints)
        };

        return mustBeMet ? result : !result;
    }

    bool HasReachedTier(PlayerCompetitionRankingLog log) {
        return log.HasReachedTier(ranking, tierId);
    }

    bool HasReachedRankingWithTag(PlayerCompetitionRankingLog log) {
        if(log == null || string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        foreach(var candidate in Resources.LoadAll<CompetitionRankingDefinition>("")) {
            if(candidate == null || !candidate.HasTag(tag)) {
                continue;
            }

            if(log.GetCurrentPoints(candidate) >= Mathf.Max(0, requiredPoints)) {
                return true;
            }
        }

        return false;
    }
}
