using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AssetAuditRunner : MonoBehaviour {
    [Header("Profiles")]
    [Tooltip("Asset audit profiles that can be run from this component.")]
    [SerializeField] List<AssetAuditProfileDefinition> profiles = new List<AssetAuditProfileDefinition>();

    [Header("Runtime")]
    [Tooltip("If enabled, all assigned asset audit profiles run when this component starts.")]
    [SerializeField] bool runOnStart;
    [Tooltip("If enabled, successful rule results are also logged.")]
    [SerializeField] bool logPassedRules;
    [Tooltip("If enabled, failed info-level results are also logged.")]
    [SerializeField] bool logInfoIssues = true;
    [Tooltip("If enabled, generated reports are written to Application.persistentDataPath/AssetAuditReports.")]
    [SerializeField] bool writeReportsToFile;
    [Tooltip("File name prefix used when Write Reports To File is enabled.")]
    [SerializeField] string reportFilePrefix = "asset-audit";

    readonly List<AssetAuditReport> lastReports = new List<AssetAuditReport>();

    public IReadOnlyList<AssetAuditProfileDefinition> Profiles => profiles;
    public IReadOnlyList<AssetAuditReport> LastReports => lastReports;

    void Start() {
        if(runOnStart) {
            RunAll();
        }
    }

    [ContextMenu("Run Asset Audits")]
    public void RunAllFromContextMenu() {
        RunAll();
    }

    public IReadOnlyList<AssetAuditReport> RunAll() {
        lastReports.Clear();

        foreach(var profile in profiles) {
            if(profile == null) {
                GameDebugLogger.Ensure().Record(GameDebugSeverity.Warning, GameDebugCategory.Validation, "Asset audit runner has a null profile slot.", this, "AssetAuditRunner");
                continue;
            }

            lastReports.Add(RunProfile(profile));
        }

        return lastReports;
    }

    public AssetAuditReport RunProfile(AssetAuditProfileDefinition profile) {
        if(profile == null) {
            var missingReport = new AssetAuditReport("missing-profile", "Missing Profile", name);
            missingReport.AddIssue(AssetAuditSeverity.Error, "Cannot run a null asset audit profile.", name, null);
            LogReport(missingReport);
            WriteReportIfEnabled(missingReport);
            return missingReport;
        }

        var report = profile.Run(this);
        LogReport(report);
        WriteReportIfEnabled(report);
        return report;
    }

    void LogReport(AssetAuditReport report) {
        if(report == null) {
            return;
        }

        var summarySeverity = report.HasErrors
            ? GameDebugSeverity.Error
            : report.HasWarnings ? GameDebugSeverity.Warning : GameDebugSeverity.Success;
        GameDebugLogger.Ensure().Record(summarySeverity, GameDebugCategory.Validation, report.BuildSummary(), this, "AssetAuditRunner");

        foreach(var result in report.results) {
            if(result == null) {
                continue;
            }

            if(result.Passed && !logPassedRules) {
                continue;
            }

            if(!result.Passed && result.Severity == AssetAuditSeverity.Info && !logInfoIssues) {
                continue;
            }

            var severity = result.Passed
                ? GameDebugSeverity.Success
                : result.Severity == AssetAuditSeverity.Error
                    ? GameDebugSeverity.Error
                    : result.Severity == AssetAuditSeverity.Warning ? GameDebugSeverity.Warning : GameDebugSeverity.Info;
            GameDebugLogger.Ensure().Record(severity, GameDebugCategory.Validation, result.Message, this, "AssetAuditRunner");
        }
    }

    void WriteReportIfEnabled(AssetAuditReport report) {
        if(!writeReportsToFile || report == null) {
            return;
        }

        string folder = Path.Combine(Application.persistentDataPath, "AssetAuditReports");
        Directory.CreateDirectory(folder);
        string safeProfileId = string.IsNullOrWhiteSpace(report.profileId) ? "profile" : report.profileId.Replace('/', '-').Replace('\\', '-');
        string fileName = $"{reportFilePrefix}-{safeProfileId}-{System.DateTime.Now:yyyyMMdd-HHmmss}.json";
        string path = Path.Combine(folder, fileName);
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        GameDebugLogger.Ensure().Record(GameDebugSeverity.Info, GameDebugCategory.Validation, $"Asset audit report written to {path}", this, "AssetAuditRunner");
    }
}
