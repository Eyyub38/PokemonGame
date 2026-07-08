using System.Collections.Generic;
using UnityEngine;

public class ContentAuditRunner : MonoBehaviour {
    [Header("Profiles")]
    [Tooltip("Audit profiles that can be run from this component.")]
    [SerializeField] List<ContentAuditProfileDefinition> profiles = new List<ContentAuditProfileDefinition>();

    [Header("Runtime")]
    [Tooltip("If enabled, all assigned audit profiles run when this component starts.")]
    [SerializeField] bool runOnStart = false;
    [Tooltip("If enabled, successful rule results are also logged.")]
    [SerializeField] bool logPassedRules = false;
    [Tooltip("If enabled, failed info-level results are also logged.")]
    [SerializeField] bool logInfoIssues = false;

    readonly List<ContentAuditReport> lastReports = new List<ContentAuditReport>();

    public IReadOnlyList<ContentAuditProfileDefinition> Profiles => profiles;
    public IReadOnlyList<ContentAuditReport> LastReports => lastReports;

    void Start() {
        if(runOnStart) {
            RunAll();
        }
    }

    [ContextMenu("Run Content Audits")]
    public void RunAllFromContextMenu() {
        RunAll();
    }

    public IReadOnlyList<ContentAuditReport> RunAll() {
        lastReports.Clear();

        foreach(var profile in profiles) {
            if(profile == null) {
                GameDebugLogger.Ensure().Record(GameDebugSeverity.Warning, GameDebugCategory.Validation, "Content audit runner has a null profile slot.", this, "ContentAuditRunner");
                continue;
            }

            lastReports.Add(RunProfile(profile));
        }

        return lastReports;
    }

    public ContentAuditReport RunProfile(ContentAuditProfileDefinition profile) {
        if(profile == null) {
            var missingReport = new ContentAuditReport("missing-profile", "Missing Profile", name);
            missingReport.AddIssue(ContentAuditSeverity.Error, "Cannot run a null content audit profile.", name, 0, null);
            LogReport(missingReport);
            return missingReport;
        }

        var report = profile.Run(this);
        LogReport(report);
        return report;
    }

    void LogReport(ContentAuditReport report) {
        if(report == null) {
            return;
        }

        var summarySeverity = report.HasErrors
            ? GameDebugSeverity.Error
            : report.HasWarnings ? GameDebugSeverity.Warning : GameDebugSeverity.Success;
        GameDebugLogger.Ensure().Record(summarySeverity, GameDebugCategory.Validation, report.BuildSummary(), this, "ContentAuditRunner");

        foreach(var result in report.results) {
            if(result == null) {
                continue;
            }

            if(result.Passed && !logPassedRules) {
                continue;
            }

            if(!result.Passed && result.Severity == ContentAuditSeverity.Info && !logInfoIssues) {
                continue;
            }

            var severity = result.Passed
                ? GameDebugSeverity.Success
                : result.Severity == ContentAuditSeverity.Error
                    ? GameDebugSeverity.Error
                    : result.Severity == ContentAuditSeverity.Warning ? GameDebugSeverity.Warning : GameDebugSeverity.Info;
            GameDebugLogger.Ensure().Record(severity, GameDebugCategory.Validation, result.Message, this, "ContentAuditRunner");
        }
    }
}
