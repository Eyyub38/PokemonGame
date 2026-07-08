using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerWitnessReportLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save history of witness reports made about the player.")]
    [SerializeField] List<WitnessReportRecord> reports = new List<WitnessReportRecord>();

    public IReadOnlyList<WitnessReportRecord> Reports => reports;
    public event Action<WitnessReportRecord> OnReportRecorded;
    public event Action OnReportLogChanged;

    public WitnessReportRecord RecordReport(WitnessReportDefinition report, NPCMemoryProfile reporter, string authorityId, string authorityName, string sourceId = null, UnityEngine.Object context = null) {
        if(report == null) {
            return null;
        }

        var record = new WitnessReportRecord {
            recordId = Guid.NewGuid().ToString("N"),
            reportId = report.Id,
            reportName = report.DisplayName,
            category = report.Category,
            reporterId = reporter != null ? reporter.NpcId : string.Empty,
            reporterName = reporter != null ? reporter.DisplayName : string.Empty,
            authorityId = authorityId,
            authorityName = authorityName,
            sourceId = sourceId,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour()
        };
        reports.Add(record);
        OnReportRecorded?.Invoke(record);
        OnReportLogChanged?.Invoke();
        return record;
    }

    public int GetCount(WitnessReportDefinition report = null, string reporterId = null, string sourceId = null, string authorityId = null) {
        return reports.Count(record => Matches(record, report, reporterId, sourceId, authorityId));
    }

    public int GetCountByCategory(WitnessReportCategory category, string reporterId = null, string sourceId = null, string authorityId = null) {
        return reports.Count(record => record != null
            && record.category == category
            && MatchesFilter(record.reporterId, reporterId)
            && MatchesFilter(record.sourceId, sourceId)
            && MatchesFilter(record.authorityId, authorityId));
    }

    public int GetCountWithTag(string tag, string reporterId = null, string sourceId = null, string authorityId = null) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        int count = 0;
        foreach(var record in reports) {
            if(record == null
                || !MatchesFilter(record.reporterId, reporterId)
                || !MatchesFilter(record.sourceId, sourceId)
                || !MatchesFilter(record.authorityId, authorityId)) {
                continue;
            }

            var report = ResolveReport(record.reportId);
            if(report != null && report.HasTag(tag)) {
                count++;
            }
        }
        return count;
    }

    public int GetHoursSinceLastReport(WitnessReportDefinition report = null, string reporterId = null, string sourceId = null, string authorityId = null) {
        var latest = reports
            .Where(record => Matches(record, report, reporterId, sourceId, authorityId))
            .OrderByDescending(record => record.absoluteHour)
            .FirstOrDefault();

        if(latest == null || latest.absoluteHour < 0) {
            return -1;
        }

        return Mathf.Max(0, GetCurrentAbsoluteHour() - latest.absoluteHour);
    }

    bool Matches(WitnessReportRecord record, WitnessReportDefinition report, string reporterId, string sourceId, string authorityId) {
        return record != null
            && (report == null || record.reportId == report.Id)
            && MatchesFilter(record.reporterId, reporterId)
            && MatchesFilter(record.sourceId, sourceId)
            && MatchesFilter(record.authorityId, authorityId);
    }

    bool MatchesFilter(string value, string filter) {
        return string.IsNullOrWhiteSpace(filter) || value == filter;
    }

    WitnessReportDefinition ResolveReport(string reportId) {
        if(string.IsNullOrWhiteSpace(reportId)) {
            return null;
        }

        return Resources.LoadAll<WitnessReportDefinition>("").FirstOrDefault(report => report != null && report.Id == reportId);
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerWitnessReportLogSaveData {
            reports = reports.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerWitnessReportLogSaveData;
        reports = saveData?.reports?.Where(record => record != null).Select(record => new WitnessReportRecord(record)).ToList() ?? new List<WitnessReportRecord>();
        OnReportLogChanged?.Invoke();
    }
}

[Serializable]
public class WitnessReportRecord {
    [Tooltip("Stable runtime record id.")]
    public string recordId;
    [Tooltip("Witness report definition id that was recorded.")]
    public string reportId;
    [Tooltip("Witness report display name saved for fallback/debug output.")]
    public string reportName;
    [Tooltip("Saved witness report category.")]
    public WitnessReportCategory category;
    [Tooltip("NPC id that made the report. Empty means the report was source-only/global.")]
    public string reporterId;
    [Tooltip("NPC display name saved for fallback/debug output.")]
    public string reporterName;
    [Tooltip("Authority id that received this report.")]
    public string authorityId;
    [Tooltip("Authority display name saved for fallback/debug output.")]
    public string authorityName;
    [Tooltip("Source id that triggered this witness report.")]
    public string sourceId;
    [Tooltip("In-game day this report was recorded.")]
    public int day;
    [Tooltip("Absolute in-game hour this report was recorded.")]
    public int absoluteHour;

    public WitnessReportRecord() {
    }

    public WitnessReportRecord(WitnessReportRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        reportId = saveData.reportId;
        reportName = saveData.reportName;
        category = saveData.category;
        reporterId = saveData.reporterId;
        reporterName = saveData.reporterName;
        authorityId = saveData.authorityId;
        authorityName = saveData.authorityName;
        sourceId = saveData.sourceId;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
    }

    public WitnessReportRecordSaveData ToSaveData() {
        return new WitnessReportRecordSaveData {
            recordId = recordId,
            reportId = reportId,
            reportName = reportName,
            category = category,
            reporterId = reporterId,
            reporterName = reporterName,
            authorityId = authorityId,
            authorityName = authorityName,
            sourceId = sourceId,
            day = day,
            absoluteHour = absoluteHour
        };
    }
}

[Serializable]
public class PlayerWitnessReportLogSaveData {
    public List<WitnessReportRecordSaveData> reports;
}

[Serializable]
public class WitnessReportRecordSaveData {
    public string recordId;
    public string reportId;
    public string reportName;
    public WitnessReportCategory category;
    public string reporterId;
    public string reporterName;
    public string authorityId;
    public string authorityName;
    public string sourceId;
    public int day;
    public int absoluteHour;
}
