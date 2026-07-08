using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PlayerOriginRecordStatus {
    Selected,
    Replaced,
    Blocked
}

public class PlayerOriginLog : MonoBehaviour, ISavable {
    [Tooltip("Saved/current selected origin id.")]
    [SerializeField] string selectedOriginId = string.Empty;
    [Tooltip("Saved/current selected origin display name.")]
    [SerializeField] string selectedOriginName = string.Empty;
    [Tooltip("Saved/current selected origin category.")]
    [SerializeField] PlayerOriginCategory selectedCategory = PlayerOriginCategory.General;
    [Tooltip("Saved/current selected origin tags.")]
    [SerializeField] List<string> selectedTags = new List<string>();
    [Tooltip("Saved/current starting scene name chosen by the origin.")]
    [SerializeField] string startingSceneName = string.Empty;
    [Tooltip("Saved/current spawn point id chosen by the origin.")]
    [SerializeField] string spawnPointId = string.Empty;
    [Tooltip("Total in-game hour when this origin was selected.")]
    [SerializeField] int selectedAtHour = -1;
    [Tooltip("Runtime/save history of origin selection attempts.")]
    [SerializeField] List<PlayerOriginRecord> records = new List<PlayerOriginRecord>();

    public string SelectedOriginId => selectedOriginId;
    public string SelectedOriginName => selectedOriginName;
    public PlayerOriginCategory SelectedCategory => selectedCategory;
    public IReadOnlyList<string> SelectedTags => selectedTags;
    public string StartingSceneName => startingSceneName;
    public string SpawnPointId => spawnPointId;
    public int SelectedAtHour => selectedAtHour;
    public IReadOnlyList<PlayerOriginRecord> Records => records;
    public bool HasSelectedOrigin => !string.IsNullOrWhiteSpace(selectedOriginId);
    public event Action<PlayerOriginRecord> OnOriginSelected;
    public event Action<PlayerOriginRecord> OnOriginBlocked;
    public event Action OnOriginChanged;

    public PlayerOriginApplyResult ApplyOrigin(PlayerOriginDefinition origin, string sourceId = null, string sourceName = null, UnityEngine.Object context = null, bool forceReplace = false) {
        if(origin == null) {
            var result = new PlayerOriginApplyResult(string.Empty, string.Empty, PlayerOriginCategory.General, sourceId, sourceName, string.Empty, string.Empty) {
                blocked = true,
                failureMessage = "No origin selected."
            };
            RecordBlocked(null, result);
            return result;
        }

        return origin.Apply(GetComponent<PlayerController>(), sourceId, sourceName, context != null ? context : this, forceReplace);
    }

    public bool HasOrigin(PlayerOriginDefinition origin) {
        return origin != null && HasOrigin(origin.Id);
    }

    public bool HasOrigin(string originId) {
        return !string.IsNullOrWhiteSpace(originId)
            && string.Equals(selectedOriginId, originId, StringComparison.OrdinalIgnoreCase);
    }

    public bool HasOriginCategory(PlayerOriginCategory category) {
        return HasSelectedOrigin && selectedCategory == category;
    }

    public bool HasOriginTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && selectedTags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public PlayerOriginRecord GetLastRecord(PlayerOriginDefinition origin = null, PlayerOriginRecordStatus? status = null, bool includeBlocked = true) {
        string originId = origin != null ? origin.Id : null;
        return records.LastOrDefault(record => record != null
            && (string.IsNullOrWhiteSpace(originId) || record.originId == originId)
            && (!status.HasValue || record.status == status.Value)
            && (includeBlocked || record.status != PlayerOriginRecordStatus.Blocked));
    }

    public int GetRecordCount(PlayerOriginDefinition origin = null, PlayerOriginRecordStatus? status = null, bool includeBlocked = true) {
        string originId = origin != null ? origin.Id : null;
        return records.Count(record => record != null
            && (string.IsNullOrWhiteSpace(originId) || record.originId == originId)
            && (!status.HasValue || record.status == status.Value)
            && (includeBlocked || record.status != PlayerOriginRecordStatus.Blocked));
    }

    public void ClearOrigin(string source = null) {
        selectedOriginId = string.Empty;
        selectedOriginName = string.Empty;
        selectedCategory = PlayerOriginCategory.General;
        selectedTags.Clear();
        startingSceneName = string.Empty;
        spawnPointId = string.Empty;
        selectedAtHour = -1;
        OnOriginChanged?.Invoke();
        GameDebugLogger.Ensure().Record(GameDebugSeverity.Info, GameDebugCategory.RPG, $"Player origin cleared by {source}.", this, "PlayerOriginLog");
    }

    public PlayerOriginRecord RecordSelected(PlayerOriginDefinition origin, PlayerOriginApplyResult result, bool replacedExisting) {
        if(origin == null || result == null) {
            return null;
        }

        selectedOriginId = origin.Id;
        selectedOriginName = origin.DisplayName;
        selectedCategory = origin.Category;
        selectedTags = origin.Tags?.Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
        startingSceneName = origin.StartingSceneName;
        spawnPointId = origin.SpawnPointId;
        selectedAtHour = GetCurrentTotalHour();

        var record = CreateRecord(origin, result, replacedExisting ? PlayerOriginRecordStatus.Replaced : PlayerOriginRecordStatus.Selected);
        records.Add(record);
        OnOriginSelected?.Invoke(record);
        OnOriginChanged?.Invoke();
        return record;
    }

    public PlayerOriginRecord RecordBlocked(PlayerOriginDefinition origin, PlayerOriginApplyResult result) {
        var record = CreateRecord(origin, result, PlayerOriginRecordStatus.Blocked);
        records.Add(record);
        OnOriginBlocked?.Invoke(record);
        OnOriginChanged?.Invoke();
        return record;
    }

    PlayerOriginRecord CreateRecord(PlayerOriginDefinition origin, PlayerOriginApplyResult result, PlayerOriginRecordStatus status) {
        return new PlayerOriginRecord {
            originId = origin != null ? origin.Id : result?.originId,
            originName = origin != null ? origin.DisplayName : result?.originName,
            category = origin != null ? origin.Category : result != null ? result.category : PlayerOriginCategory.General,
            status = status,
            sourceId = result?.sourceId,
            sourceName = result?.sourceName,
            startingSceneName = origin != null ? origin.StartingSceneName : result?.startingSceneName,
            spawnPointId = origin != null ? origin.SpawnPointId : result?.spawnPointId,
            failureMessage = result?.failureMessage,
            recordedAtHour = GetCurrentTotalHour(),
            frame = Time.frameCount
        };
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerOriginLogSaveData {
            selectedOriginId = selectedOriginId,
            selectedOriginName = selectedOriginName,
            selectedCategory = selectedCategory,
            selectedTags = selectedTags.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            startingSceneName = startingSceneName,
            spawnPointId = spawnPointId,
            selectedAtHour = selectedAtHour,
            records = records.Where(record => record != null).Select(record => new PlayerOriginRecord(record)).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerOriginLogSaveData;
        if(saveData == null) {
            return;
        }

        selectedOriginId = saveData.selectedOriginId;
        selectedOriginName = saveData.selectedOriginName;
        selectedCategory = saveData.selectedCategory;
        selectedTags = saveData.selectedTags?.Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
        startingSceneName = saveData.startingSceneName;
        spawnPointId = saveData.spawnPointId;
        selectedAtHour = saveData.selectedAtHour;
        records = saveData.records?.Where(record => record != null).Select(record => new PlayerOriginRecord(record)).ToList() ?? new List<PlayerOriginRecord>();
        OnOriginChanged?.Invoke();
    }
}

[Serializable]
public class PlayerOriginRecord {
    [Tooltip("Origin id used by this history record.")]
    public string originId;
    [Tooltip("Origin display name used by this history record.")]
    public string originName;
    [Tooltip("Origin category used by this history record.")]
    public PlayerOriginCategory category;
    [Tooltip("Result status of this origin attempt.")]
    public PlayerOriginRecordStatus status;
    [Tooltip("Source id that requested the origin selection.")]
    public string sourceId;
    [Tooltip("Source display name that requested the origin selection.")]
    public string sourceName;
    [Tooltip("Starting scene name stored by the origin.")]
    public string startingSceneName;
    [Tooltip("Spawn point id stored by the origin.")]
    public string spawnPointId;
    [Tooltip("Failure message for blocked attempts.")]
    public string failureMessage;
    [Tooltip("Total in-game hour when this record was created.")]
    public int recordedAtHour;
    [Tooltip("Unity frame when this record was created.")]
    public int frame;

    public PlayerOriginRecord() {
    }

    public PlayerOriginRecord(PlayerOriginRecord other) {
        originId = other.originId;
        originName = other.originName;
        category = other.category;
        status = other.status;
        sourceId = other.sourceId;
        sourceName = other.sourceName;
        startingSceneName = other.startingSceneName;
        spawnPointId = other.spawnPointId;
        failureMessage = other.failureMessage;
        recordedAtHour = other.recordedAtHour;
        frame = other.frame;
    }
}

[Serializable]
public class PlayerOriginLogSaveData {
    public string selectedOriginId;
    public string selectedOriginName;
    public PlayerOriginCategory selectedCategory;
    public List<string> selectedTags;
    public string startingSceneName;
    public string spawnPointId;
    public int selectedAtHour;
    public List<PlayerOriginRecord> records;
}
