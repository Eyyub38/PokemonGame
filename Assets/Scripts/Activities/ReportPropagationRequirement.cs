using UnityEngine;

public enum ReportPropagationRequirementMode {
    PropagationCount,
    PropagationTagCount,
    PropagationCategoryCount,
    HoursSinceLastPropagationAtMost
}

[CreateAssetMenu(menuName = "Activities/Requirements/Report Propagation Requirement")]
public class ReportPropagationRequirement : ActivityRequirement {
    [Tooltip("Which report propagation value this requirement checks.")]
    [SerializeField] ReportPropagationRequirementMode mode = ReportPropagationRequirementMode.PropagationCount;
    [Tooltip("Propagation definition checked by propagation-specific modes. Empty accepts any propagation.")]
    [SerializeField] ReportPropagationDefinition propagation;
    [Tooltip("Witness report definition filter. Empty accepts any source witness report.")]
    [SerializeField] WitnessReportDefinition report;
    [Tooltip("Target id filter. Empty accepts any target.")]
    [SerializeField] string targetId;
    [Tooltip("Source id filter. Empty accepts any source.")]
    [SerializeField] string sourceId;
    [Tooltip("Tag checked by Propagation Tag Count mode.")]
    [SerializeField] string propagationTag;
    [Tooltip("Category checked by Propagation Category Count mode.")]
    [SerializeField] ReportPropagationCategory category = ReportPropagationCategory.General;
    [Tooltip("Minimum value required by count modes, or max hours for Hours Since Last Propagation At Most.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("If enabled, the selected propagation condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerReportPropagationLog>() : null;
        bool result = mode switch {
            ReportPropagationRequirementMode.PropagationTagCount => log != null && log.GetCountWithTag(propagationTag, targetId, sourceId) >= Mathf.Max(0, requiredValue),
            ReportPropagationRequirementMode.PropagationCategoryCount => log != null && log.GetCountByCategory(category, targetId, sourceId) >= Mathf.Max(0, requiredValue),
            ReportPropagationRequirementMode.HoursSinceLastPropagationAtMost => log != null && log.GetHoursSinceLastPropagation(propagation, report, targetId, sourceId) >= 0 && log.GetHoursSinceLastPropagation(propagation, report, targetId, sourceId) <= Mathf.Max(0, requiredValue),
            _ => log != null && log.GetCount(propagation, report, targetId, sourceId) >= Mathf.Max(0, requiredValue)
        };

        return mustBeMet ? result : !result;
    }
}
