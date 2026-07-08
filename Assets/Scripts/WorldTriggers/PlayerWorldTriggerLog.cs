using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerWorldTriggerLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum world trigger records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 300;
    [Tooltip("Runtime/save history of world trigger runs and optional blocked attempts.")]
    [SerializeField] List<PlayerWorldTriggerRecord> records = new List<PlayerWorldTriggerRecord>();

    public IReadOnlyList<PlayerWorldTriggerRecord> Records => records;
    public event Action<WorldTriggerDefinition, PlayerWorldTriggerRecord> OnWorldTriggerRecorded;

    public bool CanRun(WorldTriggerDefinition trigger, string sourceId, ConsequenceChainRepeatMode repeatMode, int cooldownHours, int maxTriggerCount, out string failureMessage) {
        if(trigger == null) {
            failureMessage = "No world trigger selected.";
            return false;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId);
        int totalSuccessfulRuns = GetTriggerCount(trigger, includeBlocked: false);
        if(maxTriggerCount > 0 && totalSuccessfulRuns >= maxTriggerCount) {
            failureMessage = $"{trigger.DisplayName} has reached its maximum trigger count.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && totalSuccessfulRuns > 0) {
            failureMessage = $"{trigger.DisplayName} has already triggered.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetTriggerCount(trigger, normalizedSourceId, includeBlocked: false) > 0) {
            failureMessage = $"{trigger.DisplayName} has already triggered from this source.";
            return false;
        }

        var lastSourceRun = GetLastTrigger(trigger, normalizedSourceId, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastSourceRun != null && lastSourceRun.day == GetCurrentDay()) {
            failureMessage = $"{trigger.DisplayName} can only trigger once per day from this source.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastSourceRun != null) {
            int elapsed = GetCurrentAbsoluteHour() - lastSourceRun.absoluteHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{trigger.DisplayName} will be available again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public PlayerWorldTriggerRecord RecordRun(WorldTriggerDefinition trigger, string sourceId, WorldTriggerRunResult result) {
        if(trigger == null) {
            return null;
        }

        var record = new PlayerWorldTriggerRecord {
            recordId = Guid.NewGuid().ToString("N"),
            triggerId = trigger.Id,
            triggerName = trigger.DisplayName,
            triggerKind = result != null ? result.triggerKind : trigger.TriggerKind,
            sourceId = NormalizeSourceId(sourceId),
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            appliedChains = result != null ? Mathf.Max(0, result.appliedChains) : 0,
            blockedChains = result != null ? Mathf.Max(0, result.blockedChains) : 0,
            skippedChains = result != null ? Mathf.Max(0, result.skippedChains) : 0,
            blocked = result != null && result.blocked,
            failureMessage = result != null ? result.failureMessage : null,
            messages = result != null && result.messages != null ? result.messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToList() : new List<string>()
        };

        records.Add(record);
        TrimHistory();
        OnWorldTriggerRecorded?.Invoke(trigger, record);
        return record;
    }

    public int GetTriggerCount(WorldTriggerDefinition trigger, string sourceId = null, bool includeBlocked = false) {
        if(trigger == null) {
            return 0;
        }

        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return records.Count(record => record != null
            && record.triggerId == trigger.Id
            && (includeBlocked || !record.blocked)
            && (string.IsNullOrWhiteSpace(normalizedSourceId) || record.sourceId == normalizedSourceId));
    }

    public bool HasTriggered(WorldTriggerDefinition trigger, string sourceId = null, bool includeBlocked = false) {
        return GetTriggerCount(trigger, sourceId, includeBlocked) > 0;
    }

    public PlayerWorldTriggerRecord GetLastTrigger(WorldTriggerDefinition trigger, string sourceId = null, bool includeBlocked = false) {
        if(trigger == null) {
            return null;
        }

        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return records
            .Where(record => record != null
                && record.triggerId == trigger.Id
                && (includeBlocked || !record.blocked)
                && (string.IsNullOrWhiteSpace(normalizedSourceId) || record.sourceId == normalizedSourceId))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .FirstOrDefault();
    }

    public int GetHoursSinceLastTrigger(WorldTriggerDefinition trigger, string sourceId = null, bool includeBlocked = false) {
        var lastRun = GetLastTrigger(trigger, sourceId, includeBlocked);
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
        return string.IsNullOrWhiteSpace(sourceId) ? "world-trigger" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerWorldTriggerLogSaveData {
            records = records.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerWorldTriggerLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => new PlayerWorldTriggerRecord(record)).ToList()
            ?? new List<PlayerWorldTriggerRecord>();
        TrimHistory();
    }
}

[Serializable]
public class PlayerWorldTriggerRecord {
    [Tooltip("Unique runtime/save id for this world trigger record.")]
    public string recordId;
    [Tooltip("Saved world trigger id.")]
    public string triggerId;
    [Tooltip("Saved trigger display name for fallback/debug output.")]
    public string triggerName;
    [Tooltip("Kind of signal that started this trigger.")]
    public WorldTriggerKind triggerKind;
    [Tooltip("Source id used by repeat rules.")]
    public string sourceId;
    [Tooltip("In-game day when this trigger attempt was recorded.")]
    public int day;
    [Tooltip("Absolute in-game hour when this trigger attempt was recorded.")]
    public int absoluteHour;
    [Tooltip("Number of consequence chains applied successfully.")]
    [Min(0)]
    public int appliedChains;
    [Tooltip("Number of consequence chains blocked by their own rules.")]
    [Min(0)]
    public int blockedChains;
    [Tooltip("Number of null/disabled chain slots skipped.")]
    [Min(0)]
    public int skippedChains;
    [Tooltip("If enabled, this trigger attempt was blocked before applying chains.")]
    public bool blocked;
    [Tooltip("Failure reason saved for blocked attempts.")]
    public string failureMessage;
    [Tooltip("Extra chain failure/debug messages saved with this record.")]
    public List<string> messages = new List<string>();

    public PlayerWorldTriggerRecord() {
    }

    public PlayerWorldTriggerRecord(PlayerWorldTriggerRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        triggerId = saveData.triggerId;
        triggerName = saveData.triggerName;
        triggerKind = saveData.triggerKind;
        sourceId = saveData.sourceId;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
        appliedChains = Mathf.Max(0, saveData.appliedChains);
        blockedChains = Mathf.Max(0, saveData.blockedChains);
        skippedChains = Mathf.Max(0, saveData.skippedChains);
        blocked = saveData.blocked;
        failureMessage = saveData.failureMessage;
        messages = saveData.messages ?? new List<string>();
    }

    public WorldTriggerDefinition ResolveDefinition() {
        if(string.IsNullOrWhiteSpace(triggerId)) {
            return null;
        }

        return Resources.LoadAll<WorldTriggerDefinition>("").FirstOrDefault(trigger => trigger != null && trigger.Id == triggerId);
    }

    public PlayerWorldTriggerRecordSaveData ToSaveData() {
        return new PlayerWorldTriggerRecordSaveData {
            recordId = recordId,
            triggerId = triggerId,
            triggerName = triggerName,
            triggerKind = triggerKind,
            sourceId = sourceId,
            day = day,
            absoluteHour = absoluteHour,
            appliedChains = appliedChains,
            blockedChains = blockedChains,
            skippedChains = skippedChains,
            blocked = blocked,
            failureMessage = failureMessage,
            messages = messages != null ? messages.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerWorldTriggerLogSaveData {
    public List<PlayerWorldTriggerRecordSaveData> records;
}

[Serializable]
public class PlayerWorldTriggerRecordSaveData {
    public string recordId;
    public string triggerId;
    public string triggerName;
    public WorldTriggerKind triggerKind;
    public string sourceId;
    public int day;
    public int absoluteHour;
    public int appliedChains;
    public int blockedChains;
    public int skippedChains;
    public bool blocked;
    public string failureMessage;
    public List<string> messages;
}
