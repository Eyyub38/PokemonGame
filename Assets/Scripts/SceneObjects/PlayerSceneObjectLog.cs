using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerSceneObjectLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save state records for logical scene objects.")]
    [SerializeField] List<PlayerSceneObjectRecord> records = new List<PlayerSceneObjectRecord>();

    public IReadOnlyList<PlayerSceneObjectRecord> Records => records;
    public event Action<SceneObjectDefinition, PlayerSceneObjectRecord> OnSceneObjectStateChanged;
    public event Action<SceneObjectDefinition, PlayerSceneObjectRecord> OnSceneObjectInteractionRecorded;

    public SceneObjectState GetState(SceneObjectDefinition sceneObject) {
        if(sceneObject == null) {
            return SceneObjectState.Available;
        }

        var record = GetRecord(sceneObject.Id);
        return record != null && record.hasStateOverride ? record.state : sceneObject.DefaultState;
    }

    public bool IsAvailable(SceneObjectDefinition sceneObject) {
        return sceneObject == null || sceneObject.IsAvailableState(GetState(sceneObject));
    }

    public PlayerSceneObjectRecord SetState(SceneObjectDefinition sceneObject, SceneObjectState state, string sourceId = null, UnityEngine.Object context = null) {
        if(sceneObject == null) {
            return null;
        }

        var record = GetOrCreateRecord(sceneObject);
        record.state = state;
        record.hasStateOverride = true;
        record.lastSourceId = sourceId ?? string.Empty;
        record.lastStateChangeDay = GetCurrentDay();
        record.lastStateChangeAbsoluteHour = GetCurrentAbsoluteHour();
        OnSceneObjectStateChanged?.Invoke(sceneObject, record);
        PublishSceneObjectEvent(sceneObject, record, "state-changed", context, GameEventImportance.Info);
        return record;
    }

    public bool ClearState(SceneObjectDefinition sceneObject, string sourceId = null, UnityEngine.Object context = null) {
        if(sceneObject == null) {
            return false;
        }

        var record = GetRecord(sceneObject.Id);
        if(record == null || !record.hasStateOverride) {
            return false;
        }

        record.hasStateOverride = false;
        record.state = sceneObject.DefaultState;
        record.lastSourceId = sourceId ?? string.Empty;
        record.lastStateChangeDay = GetCurrentDay();
        record.lastStateChangeAbsoluteHour = GetCurrentAbsoluteHour();
        OnSceneObjectStateChanged?.Invoke(sceneObject, record);
        PublishSceneObjectEvent(sceneObject, record, "state-cleared", context, GameEventImportance.Info);
        return true;
    }

    public PlayerSceneObjectRecord RecordInteraction(SceneObjectDefinition sceneObject, string sourceId = null, UnityEngine.Object context = null) {
        if(sceneObject == null) {
            return null;
        }

        var record = GetOrCreateRecord(sceneObject);
        record.interactionCount++;
        record.lastInteractionDay = GetCurrentDay();
        record.lastInteractionAbsoluteHour = GetCurrentAbsoluteHour();
        record.lastSourceId = sourceId ?? string.Empty;
        OnSceneObjectInteractionRecorded?.Invoke(sceneObject, record);
        PublishSceneObjectEvent(sceneObject, record, "interacted", context, GameEventImportance.Trace);
        return record;
    }

    public bool HasInteracted(SceneObjectDefinition sceneObject, string sourceId = null) {
        return GetInteractionCount(sceneObject, sourceId) > 0;
    }

    public int GetInteractionCount(SceneObjectDefinition sceneObject, string sourceId = null) {
        if(sceneObject == null) {
            return 0;
        }

        var record = GetRecord(sceneObject.Id);
        if(record == null) {
            return 0;
        }

        if(!string.IsNullOrWhiteSpace(sourceId) && record.lastSourceId != sourceId) {
            return 0;
        }

        return Mathf.Max(0, record.interactionCount);
    }

    public int GetStateCount(SceneObjectState state, string tag = null, SceneObjectCategory? category = null) {
        int count = 0;
        foreach(var record in records) {
            if(record == null) {
                continue;
            }

            var definition = record.ResolveDefinition();
            if(definition == null) {
                continue;
            }

            if(record.state != state) {
                continue;
            }

            if(category.HasValue && definition.Category != category.Value) {
                continue;
            }

            if(!string.IsNullOrWhiteSpace(tag) && !definition.HasTag(tag)) {
                continue;
            }

            count++;
        }

        return count;
    }

    PlayerSceneObjectRecord GetOrCreateRecord(SceneObjectDefinition sceneObject) {
        var record = GetRecord(sceneObject.Id);
        if(record != null) {
            return record;
        }

        record = new PlayerSceneObjectRecord {
            sceneObjectId = sceneObject.Id,
            sceneObjectName = sceneObject.DisplayName,
            state = sceneObject.DefaultState
        };
        records.Add(record);
        return record;
    }

    PlayerSceneObjectRecord GetRecord(string sceneObjectId) {
        if(string.IsNullOrWhiteSpace(sceneObjectId)) {
            return null;
        }

        return records.FirstOrDefault(record => record != null && record.sceneObjectId == sceneObjectId);
    }

    void PublishSceneObjectEvent(SceneObjectDefinition sceneObject, PlayerSceneObjectRecord record, string phase, UnityEngine.Object context, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            phase == "interacted" ? sceneObject.InteractionEvent : sceneObject.StateChangedEvent,
            $"scene-object.{phase}.{sceneObject.Id}",
            $"{sceneObject.DisplayName} {phase}.",
            GameEventCategory.SceneObject,
            importance,
            context != null ? context : this,
            "PlayerSceneObjectLog",
            GameEventScope.Player,
            showInFeed: sceneObject.ShowEventsInFeed,
            writeToDebugLog: sceneObject.WriteEventsToDebugLog,
            GameEventPublishing.Value("sceneObjectId", sceneObject.Id),
            GameEventPublishing.Value("sceneObjectName", sceneObject.DisplayName),
            GameEventPublishing.Value("category", sceneObject.Category),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("state", record != null ? record.state : sceneObject.DefaultState),
            GameEventPublishing.Value("interactionCount", record != null ? record.interactionCount : 0),
            GameEventPublishing.Value("sourceId", record != null ? record.lastSourceId : string.Empty));
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerSceneObjectLogSaveData {
            records = records.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerSceneObjectLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => new PlayerSceneObjectRecord(record)).ToList()
            ?? new List<PlayerSceneObjectRecord>();
    }
}

[Serializable]
public class PlayerSceneObjectRecord {
    [Tooltip("Saved scene object definition id.")]
    public string sceneObjectId;
    [Tooltip("Saved scene object display name for fallback/debug output.")]
    public string sceneObjectName;
    [Tooltip("Saved scene object state.")]
    public SceneObjectState state = SceneObjectState.Available;
    [Tooltip("If enabled, this state overrides the definition default state.")]
    public bool hasStateOverride;
    [Tooltip("Number of interactions recorded for this object.")]
    [Min(0)]
    public int interactionCount;
    [Tooltip("Last source id that changed or interacted with this object.")]
    public string lastSourceId;
    [Tooltip("In-game day when state last changed.")]
    public int lastStateChangeDay = -1;
    [Tooltip("Absolute in-game hour when state last changed.")]
    public int lastStateChangeAbsoluteHour = -1;
    [Tooltip("In-game day when this object was last interacted with.")]
    public int lastInteractionDay = -1;
    [Tooltip("Absolute in-game hour when this object was last interacted with.")]
    public int lastInteractionAbsoluteHour = -1;

    public PlayerSceneObjectRecord() {
    }

    public PlayerSceneObjectRecord(PlayerSceneObjectRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        sceneObjectId = saveData.sceneObjectId;
        sceneObjectName = saveData.sceneObjectName;
        state = saveData.state;
        hasStateOverride = saveData.hasStateOverride;
        interactionCount = Mathf.Max(0, saveData.interactionCount);
        lastSourceId = saveData.lastSourceId;
        lastStateChangeDay = saveData.lastStateChangeDay;
        lastStateChangeAbsoluteHour = saveData.lastStateChangeAbsoluteHour;
        lastInteractionDay = saveData.lastInteractionDay;
        lastInteractionAbsoluteHour = saveData.lastInteractionAbsoluteHour;
    }

    public SceneObjectDefinition ResolveDefinition() {
        if(string.IsNullOrWhiteSpace(sceneObjectId)) {
            return null;
        }

        return Resources.LoadAll<SceneObjectDefinition>("").FirstOrDefault(sceneObject => sceneObject != null && sceneObject.Id == sceneObjectId);
    }

    public PlayerSceneObjectRecordSaveData ToSaveData() {
        return new PlayerSceneObjectRecordSaveData {
            sceneObjectId = sceneObjectId,
            sceneObjectName = sceneObjectName,
            state = state,
            hasStateOverride = hasStateOverride,
            interactionCount = interactionCount,
            lastSourceId = lastSourceId,
            lastStateChangeDay = lastStateChangeDay,
            lastStateChangeAbsoluteHour = lastStateChangeAbsoluteHour,
            lastInteractionDay = lastInteractionDay,
            lastInteractionAbsoluteHour = lastInteractionAbsoluteHour
        };
    }
}

[Serializable]
public class PlayerSceneObjectLogSaveData {
    public List<PlayerSceneObjectRecordSaveData> records;
}

[Serializable]
public class PlayerSceneObjectRecordSaveData {
    public string sceneObjectId;
    public string sceneObjectName;
    public SceneObjectState state;
    public bool hasStateOverride;
    public int interactionCount;
    public string lastSourceId;
    public int lastStateChangeDay;
    public int lastStateChangeAbsoluteHour;
    public int lastInteractionDay;
    public int lastInteractionAbsoluteHour;
}
