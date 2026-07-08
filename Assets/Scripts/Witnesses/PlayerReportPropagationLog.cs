using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerReportPropagationLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save history of witness reports propagated to groups, authorities or systems.")]
    [SerializeField] List<ReportPropagationRecord> propagations = new List<ReportPropagationRecord>();

    public IReadOnlyList<ReportPropagationRecord> Propagations => propagations;
    public event Action<ReportPropagationRecord> OnPropagationRecorded;
    public event Action OnPropagationLogChanged;

    public ReportPropagationRecord RecordPropagation(
        ReportPropagationDefinition propagation,
        WitnessReportDefinition report,
        NPCMemoryProfile reporter,
        ReportPropagationResolvedTarget target,
        string authorityId,
        string authorityName,
        string sourceId,
        WitnessReportRecord sourceRecord,
        UnityEngine.Object context = null
    ) {
        if(propagation == null || report == null || target == null) {
            return null;
        }

        var record = new ReportPropagationRecord {
            recordId = Guid.NewGuid().ToString("N"),
            propagationId = propagation.Id,
            propagationName = propagation.DisplayName,
            propagationCategory = propagation.Category,
            reportId = report.Id,
            reportName = report.DisplayName,
            reportCategory = report.Category,
            reporterId = reporter != null ? reporter.NpcId : string.Empty,
            reporterName = reporter != null ? reporter.DisplayName : string.Empty,
            authorityId = authorityId,
            authorityName = authorityName,
            sourceId = sourceId,
            sourceReportRecordId = sourceRecord != null ? sourceRecord.recordId : string.Empty,
            targetType = target.targetType,
            targetId = target.targetId,
            targetName = target.targetName,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour()
        };
        propagations.Add(record);
        OnPropagationRecorded?.Invoke(record);
        OnPropagationLogChanged?.Invoke();
        return record;
    }

    public int GetCount(ReportPropagationDefinition propagation = null, WitnessReportDefinition report = null, string targetId = null, string sourceId = null) {
        return propagations.Count(record => Matches(record, propagation, report, targetId, sourceId));
    }

    public int GetCountByCategory(ReportPropagationCategory category, string targetId = null, string sourceId = null) {
        return propagations.Count(record => record != null
            && record.propagationCategory == category
            && MatchesFilter(record.targetId, targetId)
            && MatchesFilter(record.sourceId, sourceId));
    }

    public int GetCountWithTag(string tag, string targetId = null, string sourceId = null) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        int count = 0;
        foreach(var record in propagations) {
            if(record == null || !MatchesFilter(record.targetId, targetId) || !MatchesFilter(record.sourceId, sourceId)) {
                continue;
            }

            var propagation = ResolvePropagation(record.propagationId);
            if(propagation != null && propagation.HasTag(tag)) {
                count++;
            }
        }
        return count;
    }

    public int GetHoursSinceLastPropagation(ReportPropagationDefinition propagation = null, WitnessReportDefinition report = null, string targetId = null, string sourceId = null) {
        var latest = propagations
            .Where(record => Matches(record, propagation, report, targetId, sourceId))
            .OrderByDescending(record => record.absoluteHour)
            .FirstOrDefault();

        if(latest == null || latest.absoluteHour < 0) {
            return -1;
        }

        return Mathf.Max(0, GetCurrentAbsoluteHour() - latest.absoluteHour);
    }

    bool Matches(ReportPropagationRecord record, ReportPropagationDefinition propagation, WitnessReportDefinition report, string targetId, string sourceId) {
        return record != null
            && (propagation == null || record.propagationId == propagation.Id)
            && (report == null || record.reportId == report.Id)
            && MatchesFilter(record.targetId, targetId)
            && MatchesFilter(record.sourceId, sourceId);
    }

    bool MatchesFilter(string value, string filter) {
        return string.IsNullOrWhiteSpace(filter) || value == filter;
    }

    ReportPropagationDefinition ResolvePropagation(string propagationId) {
        if(string.IsNullOrWhiteSpace(propagationId)) {
            return null;
        }

        return Resources.LoadAll<ReportPropagationDefinition>("").FirstOrDefault(propagation => propagation != null && propagation.Id == propagationId);
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerReportPropagationLogSaveData {
            propagations = propagations.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerReportPropagationLogSaveData;
        propagations = saveData?.propagations?.Where(record => record != null).Select(record => new ReportPropagationRecord(record)).ToList() ?? new List<ReportPropagationRecord>();
        OnPropagationLogChanged?.Invoke();
    }
}

[Serializable]
public class ReportPropagationRecord {
    [Tooltip("Stable runtime record id.")]
    public string recordId;
    [Tooltip("Report propagation definition id that was recorded.")]
    public string propagationId;
    [Tooltip("Report propagation display name saved for fallback/debug output.")]
    public string propagationName;
    [Tooltip("Saved propagation category.")]
    public ReportPropagationCategory propagationCategory;
    [Tooltip("Source witness report id.")]
    public string reportId;
    [Tooltip("Source witness report display name saved for fallback/debug output.")]
    public string reportName;
    [Tooltip("Source witness report category.")]
    public WitnessReportCategory reportCategory;
    [Tooltip("NPC id that originally reported the event.")]
    public string reporterId;
    [Tooltip("NPC display name saved for fallback/debug output.")]
    public string reporterName;
    [Tooltip("Authority id from the original witness report.")]
    public string authorityId;
    [Tooltip("Authority display name saved for fallback/debug output.")]
    public string authorityName;
    [Tooltip("Source id that triggered the original witness report.")]
    public string sourceId;
    [Tooltip("Record id of the source witness report.")]
    public string sourceReportRecordId;
    [Tooltip("Propagation target type.")]
    public ReportPropagationTargetType targetType;
    [Tooltip("Target id that received this propagated report.")]
    public string targetId;
    [Tooltip("Target display name saved for fallback/debug output.")]
    public string targetName;
    [Tooltip("In-game day this propagation was recorded.")]
    public int day;
    [Tooltip("Absolute in-game hour this propagation was recorded.")]
    public int absoluteHour;

    public ReportPropagationRecord() {
    }

    public ReportPropagationRecord(ReportPropagationRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        propagationId = saveData.propagationId;
        propagationName = saveData.propagationName;
        propagationCategory = saveData.propagationCategory;
        reportId = saveData.reportId;
        reportName = saveData.reportName;
        reportCategory = saveData.reportCategory;
        reporterId = saveData.reporterId;
        reporterName = saveData.reporterName;
        authorityId = saveData.authorityId;
        authorityName = saveData.authorityName;
        sourceId = saveData.sourceId;
        sourceReportRecordId = saveData.sourceReportRecordId;
        targetType = saveData.targetType;
        targetId = saveData.targetId;
        targetName = saveData.targetName;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
    }

    public ReportPropagationRecordSaveData ToSaveData() {
        return new ReportPropagationRecordSaveData {
            recordId = recordId,
            propagationId = propagationId,
            propagationName = propagationName,
            propagationCategory = propagationCategory,
            reportId = reportId,
            reportName = reportName,
            reportCategory = reportCategory,
            reporterId = reporterId,
            reporterName = reporterName,
            authorityId = authorityId,
            authorityName = authorityName,
            sourceId = sourceId,
            sourceReportRecordId = sourceReportRecordId,
            targetType = targetType,
            targetId = targetId,
            targetName = targetName,
            day = day,
            absoluteHour = absoluteHour
        };
    }
}

[Serializable]
public class PlayerReportPropagationLogSaveData {
    public List<ReportPropagationRecordSaveData> propagations;
}

[Serializable]
public class ReportPropagationRecordSaveData {
    public string recordId;
    public string propagationId;
    public string propagationName;
    public ReportPropagationCategory propagationCategory;
    public string reportId;
    public string reportName;
    public WitnessReportCategory reportCategory;
    public string reporterId;
    public string reporterName;
    public string authorityId;
    public string authorityName;
    public string sourceId;
    public string sourceReportRecordId;
    public ReportPropagationTargetType targetType;
    public string targetId;
    public string targetName;
    public int day;
    public int absoluteHour;
}
