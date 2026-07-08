using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CampStationLogOperation {
    Viewed,
    Ran,
    Blocked
}

public class PlayerCampStationLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save history of camp station views, successful actions and blocked attempts.")]
    [SerializeField] List<PlayerCampStationHistory> history = new List<PlayerCampStationHistory>();
    [Tooltip("Maximum history records kept in memory and save data. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryEntries = 200;

    public IReadOnlyList<PlayerCampStationHistory> History => history;
    public event Action<PlayerCampStationHistory> OnCampStationRecorded;

    public void RecordView(CampStationDefinition station, string sourceId, string sourceName, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        AddRecord(new PlayerCampStationHistory {
            operation = CampStationLogOperation.Viewed,
            stationId = station != null ? station.Id : string.Empty,
            stationName = station != null ? station.DisplayName : string.Empty,
            sourceId = sourceId,
            sourceName = sourceName,
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            success = true,
            message = "Camp station viewed."
        });
    }

    public void RecordRun(CampStationRunResult result, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        if(result == null) {
            return;
        }

        AddRecord(new PlayerCampStationHistory {
            operation = result.success ? CampStationLogOperation.Ran : CampStationLogOperation.Blocked,
            stationId = result.stationId,
            stationName = result.stationName,
            actionId = result.actionId,
            actionName = result.actionName,
            actionType = result.actionType,
            sourceId = result.sourceId,
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            success = result.success,
            message = result.message
        });
    }

    public int GetRunCount(string stationId = null, string actionId = null, bool successfulOnly = true) {
        return history.Count(record => record != null
            && record.operation == CampStationLogOperation.Ran
            && (!successfulOnly || record.success)
            && (string.IsNullOrWhiteSpace(stationId) || record.stationId == stationId)
            && (string.IsNullOrWhiteSpace(actionId) || record.actionId == actionId));
    }

    void AddRecord(PlayerCampStationHistory record) {
        if(record == null) {
            return;
        }

        record.day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
        record.hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0;
        record.absoluteHour = TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
        history.Add(record);
        TrimHistory();
        OnCampStationRecorded?.Invoke(record);
    }

    void TrimHistory() {
        if(maxHistoryEntries <= 0 || history.Count <= maxHistoryEntries) {
            return;
        }

        int removeCount = history.Count - maxHistoryEntries;
        history.RemoveRange(0, removeCount);
    }

    public object CaptureState() {
        return new PlayerCampStationLogSaveData {
            history = history
                .Where(record => record != null)
                .Select(record => record.ToSaveData())
                .ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCampStationLogSaveData;
        history = saveData?.history?
            .Where(record => record != null)
            .Select(record => new PlayerCampStationHistory(record))
            .ToList() ?? new List<PlayerCampStationHistory>();
        TrimHistory();
    }
}

[Serializable]
public class PlayerCampStationHistory {
    [Tooltip("Whether this record is a view, successful action or blocked attempt.")]
    public CampStationLogOperation operation;
    [Tooltip("Camp station definition id.")]
    public string stationId;
    [Tooltip("Camp station display name saved for fallback/debug UI.")]
    public string stationName;
    [Tooltip("Action id selected by the player or source.")]
    public string actionId;
    [Tooltip("Action display name saved for fallback/debug UI.")]
    public string actionName;
    [Tooltip("Action type saved for filters and future UI.")]
    public CampStationActionType actionType;
    [Tooltip("Scene/source id used by the station action.")]
    public string sourceId;
    [Tooltip("Scene/source display name used by the station action.")]
    public string sourceName;
    [Tooltip("Region id active when this record was written.")]
    public string regionId;
    [Tooltip("Region display name active when this record was written.")]
    public string regionName;
    [Tooltip("Activity zone id active when this record was written.")]
    public string zoneId;
    [Tooltip("Activity zone display name active when this record was written.")]
    public string zoneName;
    [Tooltip("If enabled, the operation succeeded.")]
    public bool success;
    [Tooltip("Result or failure message.")]
    public string message;
    [Tooltip("In-game day when this record was written.")]
    public int day;
    [Tooltip("In-game hour when this record was written.")]
    public int hour;
    [Tooltip("Absolute in-game hour when this record was written.")]
    public int absoluteHour;

    public PlayerCampStationHistory() {
    }

    public PlayerCampStationHistory(PlayerCampStationHistorySaveData saveData) {
        if(saveData == null) {
            return;
        }

        operation = saveData.operation;
        stationId = saveData.stationId;
        stationName = saveData.stationName;
        actionId = saveData.actionId;
        actionName = saveData.actionName;
        actionType = saveData.actionType;
        sourceId = saveData.sourceId;
        sourceName = saveData.sourceName;
        regionId = saveData.regionId;
        regionName = saveData.regionName;
        zoneId = saveData.zoneId;
        zoneName = saveData.zoneName;
        success = saveData.success;
        message = saveData.message;
        day = saveData.day;
        hour = saveData.hour;
        absoluteHour = saveData.absoluteHour;
    }

    public PlayerCampStationHistorySaveData ToSaveData() {
        return new PlayerCampStationHistorySaveData {
            operation = operation,
            stationId = stationId,
            stationName = stationName,
            actionId = actionId,
            actionName = actionName,
            actionType = actionType,
            sourceId = sourceId,
            sourceName = sourceName,
            regionId = regionId,
            regionName = regionName,
            zoneId = zoneId,
            zoneName = zoneName,
            success = success,
            message = message,
            day = day,
            hour = hour,
            absoluteHour = absoluteHour
        };
    }
}

[Serializable]
public class PlayerCampStationLogSaveData {
    public List<PlayerCampStationHistorySaveData> history;
}

[Serializable]
public class PlayerCampStationHistorySaveData {
    public CampStationLogOperation operation;
    public string stationId;
    public string stationName;
    public string actionId;
    public string actionName;
    public CampStationActionType actionType;
    public string sourceId;
    public string sourceName;
    public string regionId;
    public string regionName;
    public string zoneId;
    public string zoneName;
    public bool success;
    public string message;
    public int day;
    public int hour;
    public int absoluteHour;
}
