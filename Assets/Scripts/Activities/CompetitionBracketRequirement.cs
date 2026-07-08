using UnityEngine;

public enum CompetitionBracketRequirementMode {
    RosterGenerated,
    ActiveBracket,
    BracketCompleted,
    BracketWon,
    CurrentRoundAtLeast,
    MatchWinCountAtLeast,
    CanGenerateRoster,
    CompletedRosterWithTag,
    WonRosterWithTag
}

[CreateAssetMenu(menuName = "Activities/Requirements/Competition Bracket Requirement")]
public class CompetitionBracketRequirement : ActivityRequirement {
    [Tooltip("Which bracket or roster value this requirement checks.")]
    [SerializeField] CompetitionBracketRequirementMode mode = CompetitionBracketRequirementMode.BracketWon;
    [Tooltip("Roster checked by roster-specific modes.")]
    [SerializeField] CompetitionRosterDefinition roster;
    [Tooltip("Tag checked by Completed/Won Roster With Tag modes.")]
    [SerializeField] string tag;
    [Tooltip("Minimum count/index required by count and round modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected bracket condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerCompetitionBracketLog>() : null;
        bool result = mode switch {
            CompetitionBracketRequirementMode.RosterGenerated => log != null && log.GetGeneratedCount(roster) >= Mathf.Max(0, requiredCount),
            CompetitionBracketRequirementMode.ActiveBracket => log != null && log.HasActiveBracket(roster),
            CompetitionBracketRequirementMode.BracketCompleted => log != null && log.GetCompletedCount(roster) >= Mathf.Max(0, requiredCount),
            CompetitionBracketRequirementMode.CurrentRoundAtLeast => log != null && log.GetCurrentRoundIndex(roster) >= Mathf.Max(0, requiredCount),
            CompetitionBracketRequirementMode.MatchWinCountAtLeast => log != null && log.GetMatchWinCount(roster) >= Mathf.Max(0, requiredCount),
            CompetitionBracketRequirementMode.CanGenerateRoster => roster != null && roster.CanGenerate(player, out _),
            CompetitionBracketRequirementMode.CompletedRosterWithTag => HasRosterWithTag(log, completed: true, won: false),
            CompetitionBracketRequirementMode.WonRosterWithTag => HasRosterWithTag(log, completed: true, won: true),
            _ => log != null && log.GetWinCount(roster) >= Mathf.Max(0, requiredCount)
        };

        return mustBeMet ? result : !result;
    }

    bool HasRosterWithTag(PlayerCompetitionBracketLog log, bool completed, bool won) {
        if(log == null || string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        int required = Mathf.Max(0, requiredCount);
        foreach(var candidate in Resources.LoadAll<CompetitionRosterDefinition>("")) {
            if(candidate == null || !candidate.HasTag(tag)) {
                continue;
            }

            int count = won ? log.GetWinCount(candidate) : completed ? log.GetCompletedCount(candidate) : log.GetGeneratedCount(candidate);
            if(count >= required) {
                return true;
            }
        }

        return false;
    }
}
