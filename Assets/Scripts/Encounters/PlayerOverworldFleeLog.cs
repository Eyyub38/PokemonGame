using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum OverworldVirtualFleeState {
    Active,
    Recovered,
    Expired,
    Cleared
}

public class PlayerOverworldFleeLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum flee records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 300;
    [Tooltip("If enabled, expired active flee records are marked expired whenever records are queried or changed.")]
    [SerializeField] bool pruneExpiredOnAccess = true;
    [Tooltip("Runtime/save records for overworld Pokemon/NPCs that fled into virtual or unloaded-map state.")]
    [SerializeField] List<OverworldFleeRecord> records = new List<OverworldFleeRecord>();

    public IReadOnlyList<OverworldFleeRecord> Records {
        get {
            if(pruneExpiredOnAccess) {
                MarkExpiredRecords();
            }

            return records;
        }
    }

    public event Action<OverworldFleeRecord> OnFleeRecorded;
    public event Action<OverworldFleeRecord> OnFleeStateChanged;

    public OverworldFleeRecord RecordFlee(
        string entityId,
        string entityName,
        string speciesId,
        string speciesName,
        OverworldEncounterNode fromNode,
        OverworldEncounterNode escapeNode,
        Vector3 lastPosition,
        Vector3 threatPosition,
        int expireAfterHours,
        string sourceId = null,
        string sourceName = null
    ) {
        string normalizedEntityId = NormalizeEntityId(entityId, entityName);
        var record = new OverworldFleeRecord {
            recordId = Guid.NewGuid().ToString("N"),
            entityId = normalizedEntityId,
            entityName = entityName ?? string.Empty,
            speciesId = speciesId ?? string.Empty,
            speciesName = speciesName ?? string.Empty,
            sourceId = NormalizeSourceId(sourceId),
            sourceName = sourceName ?? string.Empty,
            sceneName = SceneManager.GetActiveScene().name,
            fromNodeId = fromNode != null ? fromNode.NodeId : string.Empty,
            fromNodeName = fromNode != null ? fromNode.DisplayName : string.Empty,
            escapeNodeId = escapeNode != null ? escapeNode.NodeId : string.Empty,
            escapeNodeName = escapeNode != null ? escapeNode.DisplayName : string.Empty,
            lastPosition = SerializableVector3.FromVector3(lastPosition),
            threatPosition = SerializableVector3.FromVector3(threatPosition),
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            expiresAtAbsoluteHour = expireAfterHours > 0 ? GetCurrentAbsoluteHour() + expireAfterHours : 0,
            state = OverworldVirtualFleeState.Active
        };

        records.RemoveAll(existing => existing != null
            && existing.state == OverworldVirtualFleeState.Active
            && existing.entityId == normalizedEntityId);
        records.Add(record);
        TrimHistory();
        OnFleeRecorded?.Invoke(record);
        return record;
    }

    public bool TryGetActiveRecord(string entityId, out OverworldFleeRecord record) {
        if(pruneExpiredOnAccess) {
            MarkExpiredRecords();
        }

        string normalizedEntityId = NormalizeEntityId(entityId, null);
        record = records
            .Where(entry => entry != null && entry.state == OverworldVirtualFleeState.Active)
            .Where(entry => string.IsNullOrWhiteSpace(normalizedEntityId) || entry.entityId == normalizedEntityId)
            .OrderByDescending(entry => entry.absoluteHour)
            .FirstOrDefault();
        return record != null;
    }

    public IReadOnlyList<OverworldFleeRecord> GetActiveRecords(string sceneName = null, string escapeNodeId = null) {
        if(pruneExpiredOnAccess) {
            MarkExpiredRecords();
        }

        return records
            .Where(entry => entry != null && entry.state == OverworldVirtualFleeState.Active)
            .Where(entry => string.IsNullOrWhiteSpace(sceneName) || entry.sceneName == sceneName)
            .Where(entry => string.IsNullOrWhiteSpace(escapeNodeId) || entry.escapeNodeId == escapeNodeId)
            .ToList();
    }

    public bool MarkRecovered(string recordId, string message = null) {
        return SetRecordState(recordId, OverworldVirtualFleeState.Recovered, message);
    }

    public bool ClearRecord(string recordId, string message = null) {
        return SetRecordState(recordId, OverworldVirtualFleeState.Cleared, message);
    }

    public int MarkExpiredRecords() {
        int currentHour = GetCurrentAbsoluteHour();
        int changed = 0;
        foreach(var record in records.Where(record => record != null && record.state == OverworldVirtualFleeState.Active)) {
            if(record.expiresAtAbsoluteHour > 0 && currentHour >= record.expiresAtAbsoluteHour) {
                record.state = OverworldVirtualFleeState.Expired;
                record.stateMessage = "Virtual flee timer expired.";
                changed++;
                OnFleeStateChanged?.Invoke(record);
            }
        }

        if(changed > 0) {
            TrimHistory();
        }

        return changed;
    }

    bool SetRecordState(string recordId, OverworldVirtualFleeState state, string message) {
        if(string.IsNullOrWhiteSpace(recordId)) {
            return false;
        }

        var record = records.FirstOrDefault(entry => entry != null && entry.recordId == recordId);
        if(record == null) {
            return false;
        }

        record.state = state;
        record.stateMessage = message ?? string.Empty;
        OnFleeStateChanged?.Invoke(record);
        TrimHistory();
        return true;
    }

    void TrimHistory() {
        records = records?.Where(record => record != null).ToList() ?? new List<OverworldFleeRecord>();
        if(maxHistoryRecords <= 0 || records.Count <= maxHistoryRecords) {
            return;
        }

        records = records
            .OrderByDescending(record => record.state == OverworldVirtualFleeState.Active)
            .ThenByDescending(record => record.absoluteHour)
            .Take(maxHistoryRecords)
            .OrderBy(record => record.absoluteHour)
            .ToList();
    }

    static string NormalizeEntityId(string entityId, string entityName) {
        if(!string.IsNullOrWhiteSpace(entityId)) {
            return entityId;
        }

        return string.IsNullOrWhiteSpace(entityName) ? "overworld-flee-entity" : entityName;
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "overworld-flee" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        MarkExpiredRecords();
        TrimHistory();
        return new PlayerOverworldFleeLogSaveData {
            records = records.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerOverworldFleeLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => new OverworldFleeRecord(record)).ToList()
            ?? new List<OverworldFleeRecord>();
        MarkExpiredRecords();
        TrimHistory();
    }
}

[Serializable]
public class OverworldFleeRecord {
    [Tooltip("Unique runtime/save id for this virtual flee record.")]
    public string recordId;
    [Tooltip("Stable id of the fleeing actor or spawn instance.")]
    public string entityId;
    [Tooltip("Display/debug name of the fleeing actor.")]
    public string entityName;
    [Tooltip("Optional Pokemon species/base id.")]
    public string speciesId;
    [Tooltip("Optional Pokemon species/base display name.")]
    public string speciesName;
    [Tooltip("Source id that created this flee record.")]
    public string sourceId;
    [Tooltip("Source display name that created this flee record.")]
    public string sourceName;
    [Tooltip("Scene where the actor fled.")]
    public string sceneName;
    [Tooltip("Node id where the actor started fleeing.")]
    public string fromNodeId;
    [Tooltip("Node display name where the actor started fleeing.")]
    public string fromNodeName;
    [Tooltip("Escape node id where the actor ended fleeing.")]
    public string escapeNodeId;
    [Tooltip("Escape node display name where the actor ended fleeing.")]
    public string escapeNodeName;
    [Tooltip("Last known actor position.")]
    public SerializableVector3 lastPosition;
    [Tooltip("Threat position that caused the flee.")]
    public SerializableVector3 threatPosition;
    [Tooltip("In-game day when this flee was recorded.")]
    public int day;
    [Tooltip("Absolute in-game hour when this flee was recorded.")]
    public int absoluteHour;
    [Tooltip("Absolute in-game hour when this virtual flee record expires. 0 means no timer.")]
    public int expiresAtAbsoluteHour;
    [Tooltip("Current virtual flee state.")]
    public OverworldVirtualFleeState state;
    [Tooltip("Optional state change/debug message.")]
    public string stateMessage;

    public OverworldFleeRecord() {
    }

    public OverworldFleeRecord(OverworldFleeRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        entityId = saveData.entityId;
        entityName = saveData.entityName;
        speciesId = saveData.speciesId;
        speciesName = saveData.speciesName;
        sourceId = saveData.sourceId;
        sourceName = saveData.sourceName;
        sceneName = saveData.sceneName;
        fromNodeId = saveData.fromNodeId;
        fromNodeName = saveData.fromNodeName;
        escapeNodeId = saveData.escapeNodeId;
        escapeNodeName = saveData.escapeNodeName;
        lastPosition = saveData.lastPosition;
        threatPosition = saveData.threatPosition;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
        expiresAtAbsoluteHour = saveData.expiresAtAbsoluteHour;
        state = saveData.state;
        stateMessage = saveData.stateMessage;
    }

    public OverworldFleeRecordSaveData ToSaveData() {
        return new OverworldFleeRecordSaveData {
            recordId = recordId,
            entityId = entityId,
            entityName = entityName,
            speciesId = speciesId,
            speciesName = speciesName,
            sourceId = sourceId,
            sourceName = sourceName,
            sceneName = sceneName,
            fromNodeId = fromNodeId,
            fromNodeName = fromNodeName,
            escapeNodeId = escapeNodeId,
            escapeNodeName = escapeNodeName,
            lastPosition = lastPosition,
            threatPosition = threatPosition,
            day = day,
            absoluteHour = absoluteHour,
            expiresAtAbsoluteHour = expiresAtAbsoluteHour,
            state = state,
            stateMessage = stateMessage
        };
    }
}

[Serializable]
public struct SerializableVector3 {
    [Tooltip("Saved x coordinate.")]
    public float x;
    [Tooltip("Saved y coordinate.")]
    public float y;
    [Tooltip("Saved z coordinate.")]
    public float z;

    public Vector3 ToVector3() {
        return new Vector3(x, y, z);
    }

    public static SerializableVector3 FromVector3(Vector3 value) {
        return new SerializableVector3 {
            x = value.x,
            y = value.y,
            z = value.z
        };
    }
}

[Serializable]
public class PlayerOverworldFleeLogSaveData {
    public List<OverworldFleeRecordSaveData> records;
}

[Serializable]
public class OverworldFleeRecordSaveData {
    public string recordId;
    public string entityId;
    public string entityName;
    public string speciesId;
    public string speciesName;
    public string sourceId;
    public string sourceName;
    public string sceneName;
    public string fromNodeId;
    public string fromNodeName;
    public string escapeNodeId;
    public string escapeNodeName;
    public SerializableVector3 lastPosition;
    public SerializableVector3 threatPosition;
    public int day;
    public int absoluteHour;
    public int expiresAtAbsoluteHour;
    public OverworldVirtualFleeState state;
    public string stateMessage;
}
