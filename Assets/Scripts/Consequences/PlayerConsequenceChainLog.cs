using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerConsequenceChainLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum chain history records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 300;
    [Tooltip("Runtime/save history of consequence chain runs and blocked attempts.")]
    [SerializeField] List<PlayerConsequenceChainRecord> records = new List<PlayerConsequenceChainRecord>();

    public IReadOnlyList<PlayerConsequenceChainRecord> Records => records;
    public event Action<ConsequenceChainDefinition, PlayerConsequenceChainRecord> OnChainRunRecorded;

    public bool CanRun(
        ConsequenceChainDefinition chain,
        string sourceId,
        ConsequenceChainRepeatMode repeatMode,
        int cooldownHours,
        int maxRunCount,
        out string failureMessage
    ) {
        if(chain == null) {
            failureMessage = "No consequence chain selected.";
            return false;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId);
        int totalSuccessfulRuns = GetRunCount(chain, includeBlocked: false);
        if(maxRunCount > 0 && totalSuccessfulRuns >= maxRunCount) {
            failureMessage = $"{chain.DisplayName} has reached its maximum run count.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && totalSuccessfulRuns > 0) {
            failureMessage = $"{chain.DisplayName} has already run.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetRunCount(chain, normalizedSourceId, includeBlocked: false) > 0) {
            failureMessage = $"{chain.DisplayName} has already run from this source.";
            return false;
        }

        var lastSourceRun = GetLastRun(chain, normalizedSourceId, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastSourceRun != null && lastSourceRun.day == GetCurrentDay()) {
            failureMessage = $"{chain.DisplayName} can only run once per day from this source.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastSourceRun != null) {
            int elapsed = GetCurrentAbsoluteHour() - lastSourceRun.absoluteHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{chain.DisplayName} will be available again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public PlayerConsequenceChainRecord RecordRun(ConsequenceChainDefinition chain, string sourceId, ConsequenceChainRunResult result) {
        if(chain == null) {
            return null;
        }

        var record = new PlayerConsequenceChainRecord {
            recordId = Guid.NewGuid().ToString("N"),
            chainId = chain.Id,
            chainName = chain.DisplayName,
            sourceId = NormalizeSourceId(sourceId),
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            appliedSteps = result != null ? Mathf.Max(0, result.appliedSteps) : 0,
            failedSteps = result != null ? Mathf.Max(0, result.failedSteps) : 0,
            skippedSteps = result != null ? Mathf.Max(0, result.skippedSteps) : 0,
            blocked = result != null && result.blocked,
            stoppedEarly = result != null && result.stoppedEarly,
            failureMessage = result != null ? result.failureMessage : null,
            messages = result != null && result.messages != null ? result.messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToList() : new List<string>()
        };

        records.Add(record);
        TrimHistory();
        OnChainRunRecorded?.Invoke(chain, record);
        return record;
    }

    public int GetRunCount(ConsequenceChainDefinition chain, string sourceId = null, bool includeBlocked = false) {
        if(chain == null) {
            return 0;
        }

        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return records.Count(record => record != null
            && record.chainId == chain.Id
            && (includeBlocked || !record.blocked)
            && (string.IsNullOrWhiteSpace(normalizedSourceId) || record.sourceId == normalizedSourceId));
    }

    public bool HasRun(ConsequenceChainDefinition chain, string sourceId = null, bool includeBlocked = false) {
        return GetRunCount(chain, sourceId, includeBlocked) > 0;
    }

    public PlayerConsequenceChainRecord GetLastRun(ConsequenceChainDefinition chain, string sourceId = null, bool includeBlocked = false) {
        if(chain == null) {
            return null;
        }

        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return records
            .Where(record => record != null
                && record.chainId == chain.Id
                && (includeBlocked || !record.blocked)
                && (string.IsNullOrWhiteSpace(normalizedSourceId) || record.sourceId == normalizedSourceId))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .FirstOrDefault();
    }

    public int GetHoursSinceLastRun(ConsequenceChainDefinition chain, string sourceId = null, bool includeBlocked = false) {
        var lastRun = GetLastRun(chain, sourceId, includeBlocked);
        return lastRun != null ? Mathf.Max(0, GetCurrentAbsoluteHour() - lastRun.absoluteHour) : -1;
    }

    void TrimHistory() {
        if(maxHistoryRecords <= 0 || records.Count <= maxHistoryRecords) {
            return;
        }

        records = records
            .Where(record => record != null)
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .Take(maxHistoryRecords)
            .OrderBy(record => record.absoluteHour)
            .ThenBy(record => record.day)
            .ToList();
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "consequence-chain" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerConsequenceChainLogSaveData {
            records = records.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerConsequenceChainLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => new PlayerConsequenceChainRecord(record)).ToList()
            ?? new List<PlayerConsequenceChainRecord>();
        TrimHistory();
    }
}

[Serializable]
public class PlayerConsequenceChainRecord {
    [Tooltip("Unique runtime/save id for this chain run record.")]
    public string recordId;
    [Tooltip("Saved consequence chain id.")]
    public string chainId;
    [Tooltip("Saved chain display name for fallback/debug output.")]
    public string chainName;
    [Tooltip("Source id used by repeat rules.")]
    public string sourceId;
    [Tooltip("In-game day when this chain attempt was recorded.")]
    public int day;
    [Tooltip("Absolute in-game hour when this chain attempt was recorded.")]
    public int absoluteHour;
    [Tooltip("Number of steps that applied successfully.")]
    [Min(0)]
    public int appliedSteps;
    [Tooltip("Number of steps that failed.")]
    [Min(0)]
    public int failedSteps;
    [Tooltip("Number of disabled/null steps skipped.")]
    [Min(0)]
    public int skippedSteps;
    [Tooltip("If enabled, this record was blocked before execution by repeat rules or requirements.")]
    public bool blocked;
    [Tooltip("If enabled, execution stopped before all steps were visited.")]
    public bool stoppedEarly;
    [Tooltip("Failure reason saved for blocked attempts.")]
    public string failureMessage;
    [Tooltip("Extra step failure/debug messages saved with this record.")]
    public List<string> messages = new List<string>();

    public PlayerConsequenceChainRecord() {
    }

    public PlayerConsequenceChainRecord(PlayerConsequenceChainRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        chainId = saveData.chainId;
        chainName = saveData.chainName;
        sourceId = saveData.sourceId;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
        appliedSteps = Mathf.Max(0, saveData.appliedSteps);
        failedSteps = Mathf.Max(0, saveData.failedSteps);
        skippedSteps = Mathf.Max(0, saveData.skippedSteps);
        blocked = saveData.blocked;
        stoppedEarly = saveData.stoppedEarly;
        failureMessage = saveData.failureMessage;
        messages = saveData.messages ?? new List<string>();
    }

    public ConsequenceChainDefinition ResolveDefinition() {
        if(string.IsNullOrWhiteSpace(chainId)) {
            return null;
        }

        return Resources.LoadAll<ConsequenceChainDefinition>("").FirstOrDefault(chain => chain != null && chain.Id == chainId);
    }

    public PlayerConsequenceChainRecordSaveData ToSaveData() {
        return new PlayerConsequenceChainRecordSaveData {
            recordId = recordId,
            chainId = chainId,
            chainName = chainName,
            sourceId = sourceId,
            day = day,
            absoluteHour = absoluteHour,
            appliedSteps = appliedSteps,
            failedSteps = failedSteps,
            skippedSteps = skippedSteps,
            blocked = blocked,
            stoppedEarly = stoppedEarly,
            failureMessage = failureMessage,
            messages = messages != null ? messages.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerConsequenceChainLogSaveData {
    public List<PlayerConsequenceChainRecordSaveData> records;
}

[Serializable]
public class PlayerConsequenceChainRecordSaveData {
    public string recordId;
    public string chainId;
    public string chainName;
    public string sourceId;
    public int day;
    public int absoluteHour;
    public int appliedSteps;
    public int failedSteps;
    public int skippedSteps;
    public bool blocked;
    public bool stoppedEarly;
    public string failureMessage;
    public List<string> messages;
}
