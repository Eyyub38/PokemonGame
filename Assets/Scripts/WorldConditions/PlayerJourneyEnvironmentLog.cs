using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerJourneyEnvironmentLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save history for journey environment rule evaluations.")]
    [SerializeField] List<JourneyEnvironmentRecord> records = new List<JourneyEnvironmentRecord>();

    public IReadOnlyList<JourneyEnvironmentRecord> Records => records;
    public event Action<JourneyEnvironmentRecord> OnEnvironmentRecorded;

    public bool CanEvaluate(JourneyEnvironmentProfileDefinition profile, JourneyEnvironmentRule rule, int intervalHours, out string failureMessage) {
        if(profile == null || rule == null) {
            failureMessage = "Journey environment profile or rule is missing.";
            return false;
        }

        var latest = GetLatest(profile.Id, rule.RuleId, includeBlocked: false);
        if(latest != null && intervalHours > 0) {
            int elapsed = GetCurrentAbsoluteHour() - latest.absoluteHour;
            if(elapsed < intervalHours) {
                failureMessage = $"{rule.DisplayName} can apply again in {intervalHours - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public JourneyEnvironmentRecord RecordEvaluation(
        JourneyEnvironmentProfileDefinition profile,
        JourneyEnvironmentRule rule,
        JourneyEnvironmentEvaluationTrigger trigger,
        string sourceId,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        int survivalChanges,
        int pokemonCareChanges,
        int rolledPools,
        int startedEvents,
        int lifePathRewardsApplied,
        bool blocked,
        string message) {
        var record = new JourneyEnvironmentRecord {
            profileId = profile != null ? profile.Id : string.Empty,
            profileName = profile != null ? profile.DisplayName : string.Empty,
            ruleId = rule != null ? rule.RuleId : string.Empty,
            ruleName = rule != null ? rule.DisplayName : string.Empty,
            trigger = trigger,
            sourceId = sourceId,
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            survivalChanges = Mathf.Max(0, survivalChanges),
            pokemonCareChanges = Mathf.Max(0, pokemonCareChanges),
            rolledPools = Mathf.Max(0, rolledPools),
            startedEvents = Mathf.Max(0, startedEvents),
            lifePathRewardsApplied = Mathf.Max(0, lifePathRewardsApplied),
            blocked = blocked,
            message = message,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour()
        };

        records.Add(record);
        OnEnvironmentRecorded?.Invoke(record);
        return record;
    }

    public JourneyEnvironmentRecord GetLatest(string profileId, string ruleId, bool includeBlocked = true) {
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
        return new PlayerJourneyEnvironmentLogSaveData {
            records = records != null ? records.Where(record => record != null).Select(record => record.ToSaveData()).ToList() : new List<JourneyEnvironmentRecordSaveData>()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerJourneyEnvironmentLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => new JourneyEnvironmentRecord(record)).ToList()
            ?? new List<JourneyEnvironmentRecord>();
    }
}

[Serializable]
public class JourneyEnvironmentRecord {
    [Tooltip("Journey environment profile id.")]
    public string profileId;
    [Tooltip("Journey environment profile display name.")]
    public string profileName;
    [Tooltip("Journey environment rule id.")]
    public string ruleId;
    [Tooltip("Journey environment rule display name.")]
    public string ruleName;
    [Tooltip("Trigger that evaluated this rule.")]
    public JourneyEnvironmentEvaluationTrigger trigger;
    [Tooltip("Source id used for need changes, pool rolls and reward history.")]
    public string sourceId;
    [Tooltip("Region id used by this evaluation.")]
    public string regionId;
    [Tooltip("Region display name used by this evaluation.")]
    public string regionName;
    [Tooltip("Activity zone id used by this evaluation.")]
    public string zoneId;
    [Tooltip("Activity zone display name used by this evaluation.")]
    public string zoneName;
    [Tooltip("Number of survival need changes applied.")]
    public int survivalChanges;
    [Tooltip("Number of Pokemon care need changes applied.")]
    public int pokemonCareChanges;
    [Tooltip("Number of situation event pools rolled.")]
    public int rolledPools;
    [Tooltip("Number of situation events started.")]
    public int startedEvents;
    [Tooltip("Number of Life Path reward entries with payload applied.")]
    public int lifePathRewardsApplied;
    [Tooltip("If enabled, this evaluation was blocked.")]
    public bool blocked;
    [Tooltip("Readable result/failure message.")]
    public string message;
    [Tooltip("In-game day when this rule was evaluated.")]
    public int day;
    [Tooltip("Absolute in-game hour when this rule was evaluated.")]
    public int absoluteHour;

    public JourneyEnvironmentRecord() {
    }

    public JourneyEnvironmentRecord(JourneyEnvironmentRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        profileId = saveData.profileId;
        profileName = saveData.profileName;
        ruleId = saveData.ruleId;
        ruleName = saveData.ruleName;
        trigger = saveData.trigger;
        sourceId = saveData.sourceId;
        regionId = saveData.regionId;
        regionName = saveData.regionName;
        zoneId = saveData.zoneId;
        zoneName = saveData.zoneName;
        survivalChanges = saveData.survivalChanges;
        pokemonCareChanges = saveData.pokemonCareChanges;
        rolledPools = saveData.rolledPools;
        startedEvents = saveData.startedEvents;
        lifePathRewardsApplied = saveData.lifePathRewardsApplied;
        blocked = saveData.blocked;
        message = saveData.message;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
    }

    public JourneyEnvironmentRecordSaveData ToSaveData() {
        return new JourneyEnvironmentRecordSaveData {
            profileId = profileId,
            profileName = profileName,
            ruleId = ruleId,
            ruleName = ruleName,
            trigger = trigger,
            sourceId = sourceId,
            regionId = regionId,
            regionName = regionName,
            zoneId = zoneId,
            zoneName = zoneName,
            survivalChanges = survivalChanges,
            pokemonCareChanges = pokemonCareChanges,
            rolledPools = rolledPools,
            startedEvents = startedEvents,
            lifePathRewardsApplied = lifePathRewardsApplied,
            blocked = blocked,
            message = message,
            day = day,
            absoluteHour = absoluteHour
        };
    }
}

[Serializable]
public class PlayerJourneyEnvironmentLogSaveData {
    public List<JourneyEnvironmentRecordSaveData> records;
}

[Serializable]
public class JourneyEnvironmentRecordSaveData {
    public string profileId;
    public string profileName;
    public string ruleId;
    public string ruleName;
    public JourneyEnvironmentEvaluationTrigger trigger;
    public string sourceId;
    public string regionId;
    public string regionName;
    public string zoneId;
    public string zoneName;
    public int survivalChanges;
    public int pokemonCareChanges;
    public int rolledPools;
    public int startedEvents;
    public int lifePathRewardsApplied;
    public bool blocked;
    public string message;
    public int day;
    public int absoluteHour;
}
