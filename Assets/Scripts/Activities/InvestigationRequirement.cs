using UnityEngine;

public enum InvestigationRequirementMode {
    CaseUnlocked,
    CaseActive,
    CaseCompleted,
    CaseCompletedCount,
    CaseCanStart,
    CaseCanComplete,
    ClueDiscovered,
    ClueTagDiscoveredCount,
    EvidencePointsAtLeast,
    StageAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/Investigation Requirement")]
public class InvestigationRequirement : ActivityRequirement {
    [Tooltip("Which investigation value this requirement checks.")]
    [SerializeField] InvestigationRequirementMode mode = InvestigationRequirementMode.CaseCompleted;
    [Tooltip("Case checked by case-specific modes.")]
    [SerializeField] InvestigationCaseDefinition investigationCase;
    [Tooltip("Clue checked by Clue Discovered mode.")]
    [SerializeField] InvestigationClueDefinition clue;
    [Tooltip("Tag checked by Clue Tag Discovered Count mode.")]
    [SerializeField] string tag;
    [Tooltip("Minimum value required by count, evidence and stage modes.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("If enabled, the selected investigation condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerInvestigationLog>() : null;
        bool result = mode switch {
            InvestigationRequirementMode.CaseUnlocked => log != null && log.HasUnlockedCase(investigationCase),
            InvestigationRequirementMode.CaseActive => log != null && log.HasActiveCase(investigationCase),
            InvestigationRequirementMode.CaseCompletedCount => log != null && log.GetCompletedCount(investigationCase) >= Mathf.Max(0, requiredValue),
            InvestigationRequirementMode.CaseCanStart => investigationCase != null && investigationCase.CanStart(player, log, out _),
            InvestigationRequirementMode.CaseCanComplete => investigationCase != null && log != null && investigationCase.CanComplete(player, log.GetActiveCase(investigationCase), out _),
            InvestigationRequirementMode.ClueDiscovered => log != null && log.HasDiscoveredClue(investigationCase, clue),
            InvestigationRequirementMode.ClueTagDiscoveredCount => log != null && log.GetDiscoveredClueCountWithTag(tag) >= Mathf.Max(0, requiredValue),
            InvestigationRequirementMode.EvidencePointsAtLeast => log != null && log.GetEvidencePoints(investigationCase) >= Mathf.Max(0, requiredValue),
            InvestigationRequirementMode.StageAtLeast => log != null && log.GetStageIndex(investigationCase) >= Mathf.Max(0, requiredValue),
            _ => log != null && log.HasCompletedCase(investigationCase)
        };

        return mustBeMet ? result : !result;
    }
}
