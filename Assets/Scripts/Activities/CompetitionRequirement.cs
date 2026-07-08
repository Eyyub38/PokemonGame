using UnityEngine;

public enum CompetitionRequirementMode {
    CompetitionUnlocked,
    CompetitionStarted,
    CompetitionCompleted,
    CompetitionNotCompleted,
    CurrentStageAtLeast,
    StageCompleted,
    WinCountAtLeast,
    LossCountAtLeast,
    BestStreakAtLeast,
    ChallengeWonCountAtLeast,
    CanEnterCompetition,
    CompletedCompetitionWithTag
}

[CreateAssetMenu(menuName = "Activities/Requirements/Competition Requirement")]
public class CompetitionRequirement : ActivityRequirement {
    [Tooltip("Which competition value this requirement checks.")]
    [SerializeField] CompetitionRequirementMode mode = CompetitionRequirementMode.CompetitionCompleted;
    [Tooltip("Competition checked by competition-specific modes.")]
    [SerializeField] CompetitionDefinition competition;
    [Tooltip("Battle challenge checked by Challenge Won Count At Least mode.")]
    [SerializeField] BattleChallengeDefinition challenge;
    [Tooltip("Stage id checked by Stage Completed mode.")]
    [SerializeField] string stageId;
    [Tooltip("Tag checked by Completed Competition With Tag mode.")]
    [SerializeField] string tag;
    [Tooltip("Minimum count/index required by count and stage modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected competition condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerCompetitionLog>() : null;
        bool result = mode switch {
            CompetitionRequirementMode.CompetitionUnlocked => log != null && log.HasUnlocked(competition),
            CompetitionRequirementMode.CompetitionStarted => log != null && log.GetState(competition)?.enteredCount >= Mathf.Max(0, requiredCount),
            CompetitionRequirementMode.CompetitionNotCompleted => log != null && !log.HasCompleted(competition),
            CompetitionRequirementMode.CurrentStageAtLeast => log != null && log.GetCurrentStageIndex(competition) >= Mathf.Max(0, requiredCount),
            CompetitionRequirementMode.StageCompleted => log != null && log.HasCompletedStage(competition, stageId),
            CompetitionRequirementMode.WinCountAtLeast => log != null && log.GetWinCount(competition) >= Mathf.Max(0, requiredCount),
            CompetitionRequirementMode.LossCountAtLeast => log != null && log.GetLossCount(competition) >= Mathf.Max(0, requiredCount),
            CompetitionRequirementMode.BestStreakAtLeast => log != null && log.GetBestStreak(competition) >= Mathf.Max(0, requiredCount),
            CompetitionRequirementMode.ChallengeWonCountAtLeast => log != null && log.GetChallengeWinCount(competition, challenge) >= Mathf.Max(0, requiredCount),
            CompetitionRequirementMode.CanEnterCompetition => competition != null && competition.CanEnter(player, out _),
            CompetitionRequirementMode.CompletedCompetitionWithTag => HasCompletedCompetitionWithTag(log),
            _ => log != null && log.HasCompleted(competition)
        };

        return mustBeMet ? result : !result;
    }

    bool HasCompletedCompetitionWithTag(PlayerCompetitionLog log) {
        if(log == null || string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        foreach(var candidate in Resources.LoadAll<CompetitionDefinition>("")) {
            if(candidate != null && candidate.HasTag(tag) && log.HasCompleted(candidate)) {
                return true;
            }
        }

        return false;
    }
}
