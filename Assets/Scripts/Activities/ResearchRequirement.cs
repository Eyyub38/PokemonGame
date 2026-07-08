using UnityEngine;

public enum ResearchRequirementMode {
    ResearchStarted,
    ResearchCompleted,
    ResearchPointsAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/Research Requirement")]
public class ResearchRequirement : ActivityRequirement {
    [Tooltip("Which research value this requirement checks.")]
    [SerializeField] ResearchRequirementMode mode = ResearchRequirementMode.ResearchCompleted;
    [Tooltip("Research subject checked by this requirement.")]
    [SerializeField] ResearchSubjectDefinition subject;
    [Tooltip("Minimum research points required by Research Points At Least mode.")]
    [Min(0)]
    [SerializeField] int requiredPoints = 1;
    [Tooltip("If enabled, the selected research condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerResearchLog>() : null;
        var entry = log != null ? log.GetEntry(subject) : null;
        bool result = mode switch {
            ResearchRequirementMode.ResearchStarted => entry != null && entry.points > 0,
            ResearchRequirementMode.ResearchPointsAtLeast => entry != null && entry.points >= Mathf.Max(0, requiredPoints),
            _ => log != null && log.IsCompleted(subject)
        };

        return mustBeMet ? result : !result;
    }
}
