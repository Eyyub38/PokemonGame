using UnityEngine;

public enum CompetitionSeasonRequirementMode {
    SeasonUnlocked,
    SeasonStarted,
    SeasonCompleted,
    SeasonActive,
    SeasonCanStart,
    SeasonCanComplete,
    CompletedSeasonWithTag
}

[CreateAssetMenu(menuName = "Activities/Requirements/Competition Season Requirement")]
public class CompetitionSeasonRequirement : ActivityRequirement {
    [Tooltip("Which season value this requirement checks.")]
    [SerializeField] CompetitionSeasonRequirementMode mode = CompetitionSeasonRequirementMode.SeasonCompleted;
    [Tooltip("Season checked by season-specific modes.")]
    [SerializeField] CompetitionSeasonDefinition season;
    [Tooltip("Tag checked by Completed Season With Tag mode.")]
    [SerializeField] string tag;
    [Tooltip("Minimum count required by started/completed modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected season condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerCompetitionSeasonLog>() : null;
        bool result = mode switch {
            CompetitionSeasonRequirementMode.SeasonUnlocked => log != null && log.HasUnlocked(season),
            CompetitionSeasonRequirementMode.SeasonStarted => log != null && log.GetStartedCount(season) >= Mathf.Max(0, requiredCount),
            CompetitionSeasonRequirementMode.SeasonActive => log != null && log.IsActive(season),
            CompetitionSeasonRequirementMode.SeasonCanStart => season != null && season.CanStart(player, out _),
            CompetitionSeasonRequirementMode.SeasonCanComplete => season != null && season.CanComplete(player, out _),
            CompetitionSeasonRequirementMode.CompletedSeasonWithTag => HasCompletedSeasonWithTag(log),
            _ => log != null && log.GetCompletedCount(season) >= Mathf.Max(0, requiredCount)
        };

        return mustBeMet ? result : !result;
    }

    bool HasCompletedSeasonWithTag(PlayerCompetitionSeasonLog log) {
        if(log == null || string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        foreach(var candidate in Resources.LoadAll<CompetitionSeasonDefinition>("")) {
            if(candidate != null && candidate.HasTag(tag) && log.GetCompletedCount(candidate) >= Mathf.Max(0, requiredCount)) {
                return true;
            }
        }

        return false;
    }
}
