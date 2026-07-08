using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RoleActivityBoardLogOperation {
    Viewed,
    Ran,
    Blocked
}

public class PlayerRoleActivityBoardLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save history of role board views, runs and blocked attempts.")]
    [SerializeField] List<PlayerRoleActivityBoardHistory> history = new List<PlayerRoleActivityBoardHistory>();
    [Tooltip("Maximum history records kept in memory and save data. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryEntries = 200;

    public IReadOnlyList<PlayerRoleActivityBoardHistory> History => history;
    public event Action<PlayerRoleActivityBoardHistory> OnRoleActivityBoardRecorded;

    public void RecordView(RoleActivityBoardDefinition board, string sourceId, string sourceName, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        AddRecord(new PlayerRoleActivityBoardHistory {
            operation = RoleActivityBoardLogOperation.Viewed,
            boardId = board != null ? board.Id : string.Empty,
            boardName = board != null ? board.DisplayName : string.Empty,
            sourceId = sourceId,
            sourceName = sourceName,
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            success = true,
            message = "Board viewed."
        });
    }

    public void RecordRun(RoleActivityBoardRunResult result, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        if(result == null) {
            return;
        }

        AddRecord(new PlayerRoleActivityBoardHistory {
            operation = result.success ? RoleActivityBoardLogOperation.Ran : RoleActivityBoardLogOperation.Blocked,
            boardId = result.boardId,
            boardName = result.boardName,
            entryId = result.entryId,
            entryName = result.entryName,
            entryType = result.entryType,
            sourceId = result.sourceId,
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            success = result.success,
            message = result.message
        });
    }

    public int GetRunCount(string boardId = null, string entryId = null, bool successfulOnly = true) {
        return history.Count(record => record != null
            && record.operation == RoleActivityBoardLogOperation.Ran
            && (!successfulOnly || record.success)
            && (string.IsNullOrWhiteSpace(boardId) || record.boardId == boardId)
            && (string.IsNullOrWhiteSpace(entryId) || record.entryId == entryId));
    }

    void AddRecord(PlayerRoleActivityBoardHistory record) {
        if(record == null) {
            return;
        }

        record.day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
        record.hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0;
        record.absoluteHour = TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
        history.Add(record);
        TrimHistory();
        OnRoleActivityBoardRecorded?.Invoke(record);
    }

    void TrimHistory() {
        if(maxHistoryEntries <= 0 || history.Count <= maxHistoryEntries) {
            return;
        }

        int removeCount = history.Count - maxHistoryEntries;
        history.RemoveRange(0, removeCount);
    }

    public object CaptureState() {
        return new PlayerRoleActivityBoardLogSaveData {
            history = history
                .Where(record => record != null)
                .Select(record => record.ToSaveData())
                .ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerRoleActivityBoardLogSaveData;
        history = saveData?.history?
            .Where(record => record != null)
            .Select(record => new PlayerRoleActivityBoardHistory(record))
            .ToList() ?? new List<PlayerRoleActivityBoardHistory>();
        TrimHistory();
    }
}

[Serializable]
public class PlayerRoleActivityBoardHistory {
    [Tooltip("Whether this record is a view, successful run or blocked attempt.")]
    public RoleActivityBoardLogOperation operation;
    [Tooltip("Board definition id.")]
    public string boardId;
    [Tooltip("Board display name saved for fallback/debug UI.")]
    public string boardName;
    [Tooltip("Entry id selected by the player or source.")]
    public string entryId;
    [Tooltip("Entry display name saved for fallback/debug UI.")]
    public string entryName;
    [Tooltip("Entry content type.")]
    public RoleActivityBoardEntryType entryType;
    [Tooltip("Scene/source id used by the board action.")]
    public string sourceId;
    [Tooltip("Scene/source display name used by the board action.")]
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

    public PlayerRoleActivityBoardHistory() {
    }

    public PlayerRoleActivityBoardHistory(PlayerRoleActivityBoardHistorySaveData saveData) {
        if(saveData == null) {
            return;
        }

        operation = saveData.operation;
        boardId = saveData.boardId;
        boardName = saveData.boardName;
        entryId = saveData.entryId;
        entryName = saveData.entryName;
        entryType = saveData.entryType;
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

    public PlayerRoleActivityBoardHistorySaveData ToSaveData() {
        return new PlayerRoleActivityBoardHistorySaveData {
            operation = operation,
            boardId = boardId,
            boardName = boardName,
            entryId = entryId,
            entryName = entryName,
            entryType = entryType,
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
public class PlayerRoleActivityBoardLogSaveData {
    public List<PlayerRoleActivityBoardHistorySaveData> history;
}

[Serializable]
public class PlayerRoleActivityBoardHistorySaveData {
    public RoleActivityBoardLogOperation operation;
    public string boardId;
    public string boardName;
    public string entryId;
    public string entryName;
    public RoleActivityBoardEntryType entryType;
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
