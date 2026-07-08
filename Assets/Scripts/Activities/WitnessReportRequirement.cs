using UnityEngine;

public enum WitnessReportRequirementMode {
    ReportCount,
    ReportTagCount,
    ReportCategoryCount,
    HoursSinceLastReportAtMost
}

[CreateAssetMenu(menuName = "Activities/Requirements/Witness Report Requirement")]
public class WitnessReportRequirement : ActivityRequirement {
    [Tooltip("Which witness report value this requirement checks.")]
    [SerializeField] WitnessReportRequirementMode mode = WitnessReportRequirementMode.ReportCount;
    [Tooltip("Witness report definition checked by report-specific modes. Empty accepts any report.")]
    [SerializeField] WitnessReportDefinition report;
    [Tooltip("Reporter NPC id filter. Empty accepts any reporter.")]
    [SerializeField] string reporterId;
    [Tooltip("Source id filter. Empty accepts any source.")]
    [SerializeField] string sourceId;
    [Tooltip("Authority id filter. Empty accepts any authority.")]
    [SerializeField] string authorityId;
    [Tooltip("Tag checked by Report Tag Count mode.")]
    [SerializeField] string reportTag;
    [Tooltip("Category checked by Report Category Count mode.")]
    [SerializeField] WitnessReportCategory category = WitnessReportCategory.General;
    [Tooltip("Minimum value required by count modes, or max hours for Hours Since Last Report At Most.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("If enabled, the selected witness report condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerWitnessReportLog>() : null;
        bool result = mode switch {
            WitnessReportRequirementMode.ReportTagCount => log != null && log.GetCountWithTag(reportTag, reporterId, sourceId, authorityId) >= Mathf.Max(0, requiredValue),
            WitnessReportRequirementMode.ReportCategoryCount => log != null && log.GetCountByCategory(category, reporterId, sourceId, authorityId) >= Mathf.Max(0, requiredValue),
            WitnessReportRequirementMode.HoursSinceLastReportAtMost => log != null && log.GetHoursSinceLastReport(report, reporterId, sourceId, authorityId) >= 0 && log.GetHoursSinceLastReport(report, reporterId, sourceId, authorityId) <= Mathf.Max(0, requiredValue),
            _ => log != null && log.GetCount(report, reporterId, sourceId, authorityId) >= Mathf.Max(0, requiredValue)
        };

        return mustBeMet ? result : !result;
    }
}
