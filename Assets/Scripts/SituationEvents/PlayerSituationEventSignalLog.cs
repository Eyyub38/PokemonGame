using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerSituationEventSignalLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save history for situation event signal rule evaluations.")]
    [SerializeField] List<SituationEventSignalRecord> records = new List<SituationEventSignalRecord>();

    public IReadOnlyList<SituationEventSignalRecord> Records => records;
    public event Action<SituationEventSignalRecord> OnSignalRecorded;

    public bool CanEvaluate(SituationEventSignalProfileDefinition profile, SituationEventSignalRule rule, int cooldownHours, out string failureMessage) {
        if(profile == null || rule == null) {
            failureMessage = "Signal profile or rule is missing.";
            return false;
        }

        var latest = GetLatest(profile.Id, rule.RuleId, includeBlocked: false);
        if(latest != null && cooldownHours > 0) {
            int elapsed = GetCurrentAbsoluteHour() - latest.absoluteHour;
            if(elapsed < cooldownHours) {
                failureMessage = $"{rule.DisplayName} can evaluate again in {cooldownHours - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public SituationEventSignalRecord RecordEvaluation(
        SituationEventSignalProfileDefinition profile,
        SituationEventSignalRule rule,
        SituationEventSignalTrigger trigger,
        string sourceId,
        int rolledPools,
        int startedEvents,
        bool blocked,
        string message) {
        var record = new SituationEventSignalRecord {
            profileId = profile != null ? profile.Id : string.Empty,
            profileName = profile != null ? profile.DisplayName : string.Empty,
            ruleId = rule != null ? rule.RuleId : string.Empty,
            ruleName = rule != null ? rule.DisplayName : string.Empty,
            trigger = trigger,
            sourceId = sourceId,
            rolledPools = Mathf.Max(0, rolledPools),
            startedEvents = Mathf.Max(0, startedEvents),
            blocked = blocked,
            message = message,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour()
        };

        records.Add(record);
        OnSignalRecorded?.Invoke(record);
        return record;
    }

    public SituationEventSignalRecord GetLatest(string profileId, string ruleId, bool includeBlocked = true) {
        return records
            .Where(record => record != null
                && record.profileId == profileId
                && record.ruleId == ruleId
                && (includeBlocked || !record.blocked))
            .OrderByDescending(record => record.absoluteHour)
            .FirstOrDefault();
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerSituationEventSignalLogSaveData {
            records = records != null ? records.Where(record => record != null).Select(record => record.ToSaveData()).ToList() : new List<SituationEventSignalRecordSaveData>()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerSituationEventSignalLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => new SituationEventSignalRecord(record)).ToList()
            ?? new List<SituationEventSignalRecord>();
    }
}

[Serializable]
public class SituationEventSignalRecord {
    [Tooltip("Signal profile id.")]
    public string profileId;
    [Tooltip("Signal profile display name.")]
    public string profileName;
    [Tooltip("Signal rule id.")]
    public string ruleId;
    [Tooltip("Signal rule display name.")]
    public string ruleName;
    [Tooltip("Trigger that evaluated this signal.")]
    public SituationEventSignalTrigger trigger;
    [Tooltip("Source id used for pool rolls.")]
    public string sourceId;
    [Tooltip("Number of pools rolled by this evaluation.")]
    public int rolledPools;
    [Tooltip("Number of events started by this evaluation.")]
    public int startedEvents;
    [Tooltip("If enabled, this evaluation was blocked.")]
    public bool blocked;
    [Tooltip("Readable result/failure message.")]
    public string message;
    [Tooltip("In-game day when this signal was evaluated.")]
    public int day;
    [Tooltip("Absolute in-game hour when this signal was evaluated.")]
    public int absoluteHour;

    public SituationEventSignalRecord() {
    }

    public SituationEventSignalRecord(SituationEventSignalRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        profileId = saveData.profileId;
        profileName = saveData.profileName;
        ruleId = saveData.ruleId;
        ruleName = saveData.ruleName;
        trigger = saveData.trigger;
        sourceId = saveData.sourceId;
        rolledPools = saveData.rolledPools;
        startedEvents = saveData.startedEvents;
        blocked = saveData.blocked;
        message = saveData.message;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
    }

    public SituationEventSignalRecordSaveData ToSaveData() {
        return new SituationEventSignalRecordSaveData {
            profileId = profileId,
            profileName = profileName,
            ruleId = ruleId,
            ruleName = ruleName,
            trigger = trigger,
            sourceId = sourceId,
            rolledPools = rolledPools,
            startedEvents = startedEvents,
            blocked = blocked,
            message = message,
            day = day,
            absoluteHour = absoluteHour
        };
    }
}

[Serializable]
public class PlayerSituationEventSignalLogSaveData {
    public List<SituationEventSignalRecordSaveData> records;
}

[Serializable]
public class SituationEventSignalRecordSaveData {
    public string profileId;
    public string profileName;
    public string ruleId;
    public string ruleName;
    public SituationEventSignalTrigger trigger;
    public string sourceId;
    public int rolledPools;
    public int startedEvents;
    public bool blocked;
    public string message;
    public int day;
    public int absoluteHour;
}
