using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSceneSpawnLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum spawn records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 400;
    [Tooltip("Runtime/save history of scene spawns and optional blocked attempts.")]
    [SerializeField] List<PlayerSceneSpawnRecord> records = new List<PlayerSceneSpawnRecord>();

    public IReadOnlyList<PlayerSceneSpawnRecord> Records => records;
    public event Action<SceneSpawnProfileDefinition, PlayerSceneSpawnRecord> OnSceneSpawnRecorded;

    public bool CanSpawn(SceneSpawnProfileDefinition profile, string spawnerId, ConsequenceChainRepeatMode repeatMode, int cooldownHours, int maxSuccessfulSpawns, out string failureMessage) {
        if(profile == null) {
            failureMessage = "No scene spawn profile selected.";
            return false;
        }

        string normalizedSpawnerId = NormalizeSpawnerId(spawnerId);
        int totalSuccessfulSpawns = GetSpawnCount(profile, includeBlocked: false);
        if(maxSuccessfulSpawns > 0 && totalSuccessfulSpawns >= maxSuccessfulSpawns) {
            failureMessage = $"{profile.DisplayName} has reached its maximum spawn count.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && totalSuccessfulSpawns > 0) {
            failureMessage = $"{profile.DisplayName} has already spawned.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetSpawnCount(profile, normalizedSpawnerId, includeBlocked: false) > 0) {
            failureMessage = $"{profile.DisplayName} has already spawned from this spawner.";
            return false;
        }

        var lastSpawnerRun = GetLastSpawn(profile, normalizedSpawnerId, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastSpawnerRun != null && lastSpawnerRun.day == GetCurrentDay()) {
            failureMessage = $"{profile.DisplayName} can only spawn once per day from this spawner.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastSpawnerRun != null) {
            int elapsed = GetCurrentAbsoluteHour() - lastSpawnerRun.absoluteHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{profile.DisplayName} will be available again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public PlayerSceneSpawnRecord RecordSpawn(
        SceneSpawnProfileDefinition profile,
        string spawnerId,
        string spawnerName,
        SceneSpawnEntry entry,
        string prefabName,
        int batchIndex,
        int batchSize,
        bool blocked = false,
        string failureMessage = null,
        IEnumerable<string> messages = null
    ) {
        if(profile == null) {
            return null;
        }

        var record = new PlayerSceneSpawnRecord {
            recordId = Guid.NewGuid().ToString("N"),
            profileId = profile.Id,
            profileName = profile.DisplayName,
            spawnerId = NormalizeSpawnerId(spawnerId),
            spawnerName = spawnerName ?? string.Empty,
            entryId = entry != null ? entry.EntryId : string.Empty,
            entryName = entry != null ? entry.DisplayName : string.Empty,
            prefabName = prefabName ?? string.Empty,
            sceneName = SceneManager.GetActiveScene().name,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            batchIndex = Mathf.Max(0, batchIndex),
            batchSize = Mathf.Max(0, batchSize),
            blocked = blocked,
            failureMessage = failureMessage,
            messages = messages != null ? messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToList() : new List<string>()
        };

        records.Add(record);
        TrimHistory();
        OnSceneSpawnRecorded?.Invoke(profile, record);
        return record;
    }

    public int GetSpawnCount(SceneSpawnProfileDefinition profile, string spawnerId = null, string entryId = null, bool includeBlocked = false) {
        if(profile == null) {
            return 0;
        }

        string normalizedSpawnerId = string.IsNullOrWhiteSpace(spawnerId) ? null : NormalizeSpawnerId(spawnerId);
        return records.Count(record => record != null
            && record.profileId == profile.Id
            && (includeBlocked || !record.blocked)
            && (string.IsNullOrWhiteSpace(normalizedSpawnerId) || record.spawnerId == normalizedSpawnerId)
            && (string.IsNullOrWhiteSpace(entryId) || record.entryId == entryId));
    }

    public bool HasSpawned(SceneSpawnProfileDefinition profile, string spawnerId = null, string entryId = null, bool includeBlocked = false) {
        return GetSpawnCount(profile, spawnerId, entryId, includeBlocked) > 0;
    }

    public PlayerSceneSpawnRecord GetLastSpawn(SceneSpawnProfileDefinition profile, string spawnerId = null, string entryId = null, bool includeBlocked = false) {
        if(profile == null) {
            return null;
        }

        string normalizedSpawnerId = string.IsNullOrWhiteSpace(spawnerId) ? null : NormalizeSpawnerId(spawnerId);
        return records
            .Where(record => record != null
                && record.profileId == profile.Id
                && (includeBlocked || !record.blocked)
                && (string.IsNullOrWhiteSpace(normalizedSpawnerId) || record.spawnerId == normalizedSpawnerId)
                && (string.IsNullOrWhiteSpace(entryId) || record.entryId == entryId))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .ThenByDescending(record => record.batchIndex)
            .FirstOrDefault();
    }

    public int GetHoursSinceLastSpawn(SceneSpawnProfileDefinition profile, string spawnerId = null, string entryId = null, bool includeBlocked = false) {
        var lastSpawn = GetLastSpawn(profile, spawnerId, entryId, includeBlocked);
        return lastSpawn != null ? Mathf.Max(0, GetCurrentAbsoluteHour() - lastSpawn.absoluteHour) : -1;
    }

    void TrimHistory() {
        if(maxHistoryRecords <= 0 || records.Count <= maxHistoryRecords) {
            return;
        }

        records = records
            .Where(record => record != null)
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .ThenByDescending(record => record.batchIndex)
            .Take(maxHistoryRecords)
            .OrderBy(record => record.absoluteHour)
            .ThenBy(record => record.day)
            .ThenBy(record => record.batchIndex)
            .ToList();
    }

    static string NormalizeSpawnerId(string spawnerId) {
        return string.IsNullOrWhiteSpace(spawnerId) ? "scene-spawn" : spawnerId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerSceneSpawnLogSaveData {
            records = records.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerSceneSpawnLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => new PlayerSceneSpawnRecord(record)).ToList()
            ?? new List<PlayerSceneSpawnRecord>();
        TrimHistory();
    }
}

[Serializable]
public class PlayerSceneSpawnRecord {
    [Tooltip("Unique runtime/save id for this scene spawn record.")]
    public string recordId;
    [Tooltip("Saved scene spawn profile id.")]
    public string profileId;
    [Tooltip("Saved spawn profile display name for fallback/debug output.")]
    public string profileName;
    [Tooltip("Spawner/source id used by repeat rules.")]
    public string spawnerId;
    [Tooltip("Spawner/source display name for debug output.")]
    public string spawnerName;
    [Tooltip("Entry id selected from the profile.")]
    public string entryId;
    [Tooltip("Entry display name selected from the profile.")]
    public string entryName;
    [Tooltip("Spawned prefab name, if a prefab was instantiated.")]
    public string prefabName;
    [Tooltip("Scene name where this spawn attempt was recorded.")]
    public string sceneName;
    [Tooltip("In-game day when this spawn attempt was recorded.")]
    public int day;
    [Tooltip("Absolute in-game hour when this spawn attempt was recorded.")]
    public int absoluteHour;
    [Tooltip("Index of this entry inside its spawn batch.")]
    [Min(0)]
    public int batchIndex;
    [Tooltip("Total selected entries in this spawn batch.")]
    [Min(0)]
    public int batchSize;
    [Tooltip("If enabled, this spawn attempt was blocked before instantiating a prefab.")]
    public bool blocked;
    [Tooltip("Failure reason saved for blocked attempts.")]
    public string failureMessage;
    [Tooltip("Extra debug messages saved with this record.")]
    public List<string> messages = new List<string>();

    public PlayerSceneSpawnRecord() {
    }

    public PlayerSceneSpawnRecord(PlayerSceneSpawnRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        profileId = saveData.profileId;
        profileName = saveData.profileName;
        spawnerId = saveData.spawnerId;
        spawnerName = saveData.spawnerName;
        entryId = saveData.entryId;
        entryName = saveData.entryName;
        prefabName = saveData.prefabName;
        sceneName = saveData.sceneName;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
        batchIndex = Mathf.Max(0, saveData.batchIndex);
        batchSize = Mathf.Max(0, saveData.batchSize);
        blocked = saveData.blocked;
        failureMessage = saveData.failureMessage;
        messages = saveData.messages ?? new List<string>();
    }

    public SceneSpawnProfileDefinition ResolveDefinition() {
        if(string.IsNullOrWhiteSpace(profileId)) {
            return null;
        }

        return Resources.LoadAll<SceneSpawnProfileDefinition>("").FirstOrDefault(profile => profile != null && profile.Id == profileId);
    }

    public PlayerSceneSpawnRecordSaveData ToSaveData() {
        return new PlayerSceneSpawnRecordSaveData {
            recordId = recordId,
            profileId = profileId,
            profileName = profileName,
            spawnerId = spawnerId,
            spawnerName = spawnerName,
            entryId = entryId,
            entryName = entryName,
            prefabName = prefabName,
            sceneName = sceneName,
            day = day,
            absoluteHour = absoluteHour,
            batchIndex = batchIndex,
            batchSize = batchSize,
            blocked = blocked,
            failureMessage = failureMessage,
            messages = messages != null ? messages.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerSceneSpawnLogSaveData {
    public List<PlayerSceneSpawnRecordSaveData> records;
}

[Serializable]
public class PlayerSceneSpawnRecordSaveData {
    public string recordId;
    public string profileId;
    public string profileName;
    public string spawnerId;
    public string spawnerName;
    public string entryId;
    public string entryName;
    public string prefabName;
    public string sceneName;
    public int day;
    public int absoluteHour;
    public int batchIndex;
    public int batchSize;
    public bool blocked;
    public string failureMessage;
    public List<string> messages;
}
