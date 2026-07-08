using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AreaProfileRecordStatus {
    Entered,
    Refreshed,
    Exited,
    Blocked
}

public class PlayerAreaProfileLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum area operation records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 600;
    [Tooltip("Runtime/save list of currently active area profiles.")]
    [SerializeField] List<PlayerAreaProfileState> activeAreas = new List<PlayerAreaProfileState>();
    [Tooltip("Runtime/save history of area profile operations.")]
    [SerializeField] List<PlayerAreaProfileRecord> records = new List<PlayerAreaProfileRecord>();

    public IReadOnlyList<PlayerAreaProfileState> ActiveAreas => activeAreas;
    public IReadOnlyList<PlayerAreaProfileRecord> Records => records;
    public event Action<PlayerAreaProfileState> OnAreaEntered;
    public event Action<PlayerAreaProfileRecord> OnAreaExited;
    public event Action OnAreaProfilesChanged;

    public bool CanEnter(AreaProfileDefinition profile, string sourceId, ConsequenceChainRepeatMode repeatMode, int cooldownHours, int maxEnterCount, out string failureMessage) {
        if(profile == null) {
            failureMessage = "No area profile selected.";
            return false;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId);
        int totalEnters = GetRecordCount(profile, status: AreaProfileRecordStatus.Entered, includeBlocked: false)
            + GetRecordCount(profile, status: AreaProfileRecordStatus.Refreshed, includeBlocked: false);
        if(maxEnterCount > 0 && totalEnters >= maxEnterCount) {
            failureMessage = $"{profile.DisplayName} has reached its maximum enter count.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && totalEnters > 0) {
            failureMessage = $"{profile.DisplayName} has already been entered.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetEnterCount(profile, normalizedSourceId) > 0) {
            failureMessage = $"{profile.DisplayName} has already been entered from this source.";
            return false;
        }

        var lastSourceRecord = GetLastRecord(profile, sourceId: normalizedSourceId, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastSourceRecord != null && lastSourceRecord.day == GetCurrentDay()) {
            failureMessage = $"{profile.DisplayName} can only be entered once per day from this source.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastSourceRecord != null) {
            int elapsed = GetCurrentAbsoluteHour() - lastSourceRecord.absoluteHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{profile.DisplayName} will be available again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public PlayerAreaProfileState EnterArea(AreaProfileDefinition profile, AreaProfileResult result) {
        if(profile == null || result == null) {
            return null;
        }

        var existing = GetActiveArea(profile, result.sourceId);
        if(existing != null) {
            RefreshState(existing, profile, result);
            result.state = existing;
            result.record = RecordOperation(profile, result, AreaProfileRecordStatus.Refreshed, existing);
            OnAreaEntered?.Invoke(existing);
            OnAreaProfilesChanged?.Invoke();
            return existing;
        }

        var state = CreateState(profile, result);
        activeAreas.Add(state);
        result.state = state;
        result.record = RecordOperation(profile, result, AreaProfileRecordStatus.Entered, state);
        OnAreaEntered?.Invoke(state);
        OnAreaProfilesChanged?.Invoke();
        return state;
    }

    public bool ExitArea(AreaProfileDefinition profile, AreaProfileResult result) {
        if(profile == null || result == null) {
            return false;
        }

        var state = GetActiveArea(profile, result.sourceId) ?? GetActiveArea(profile);
        if(state != null) {
            activeAreas.Remove(state);
        }

        result.state = state;
        result.record = RecordOperation(profile, result, AreaProfileRecordStatus.Exited, state);
        OnAreaExited?.Invoke(result.record);
        OnAreaProfilesChanged?.Invoke();
        return state != null;
    }

    public PlayerAreaProfileRecord RecordBlocked(AreaProfileDefinition profile, AreaProfileResult result) {
        result.record = RecordOperation(profile, result, AreaProfileRecordStatus.Blocked, null);
        OnAreaProfilesChanged?.Invoke();
        return result.record;
    }

    public void SyncRecordWithResult(AreaProfileResult result) {
        if(result?.record == null) {
            return;
        }

        result.record.locationVisitsApplied = Mathf.Max(0, result.locationVisitsApplied);
        result.record.worldDiscoveriesApplied = Mathf.Max(0, result.worldDiscoveriesApplied);
        result.record.navigationHintsApplied = Mathf.Max(0, result.navigationHintsApplied);
        result.record.chronicleEntriesApplied = Mathf.Max(0, result.chronicleEntriesApplied);
        result.record.worldConditionsApplied = Mathf.Max(0, result.worldConditionsApplied);
        result.record.skippedActions = Mathf.Max(0, result.skippedActions);
        result.record.blockedActions = Mathf.Max(0, result.blockedActions);
        result.record.appliedChains = Mathf.Max(0, result.appliedChains);
        result.record.blockedChains = Mathf.Max(0, result.blockedChains);
        result.record.skippedChains = Mathf.Max(0, result.skippedChains);
        result.record.messages = result.messages != null ? result.messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToList() : new List<string>();
    }

    public bool HasActiveArea(AreaProfileDefinition profile = null, string sourceId = null, AreaProfileCategory? category = null, string tag = null) {
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return activeAreas.Any(state => MatchesActive(state, profile, normalizedSourceId, category, tag));
    }

    public PlayerAreaProfileState GetActiveArea(AreaProfileDefinition profile, string sourceId = null) {
        if(profile == null) {
            return null;
        }

        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return activeAreas.FirstOrDefault(state => state != null
            && state.areaProfileId == profile.Id
            && (string.IsNullOrWhiteSpace(normalizedSourceId) || state.sourceId == normalizedSourceId));
    }

    public int GetRecordCount(
        AreaProfileDefinition profile = null,
        string sourceId = null,
        AreaProfileRecordStatus? status = null,
        AreaProfileCategory? category = null,
        string tag = null,
        RegionInfoDefinition region = null,
        ActivityZoneDefinition zone = null,
        string sceneName = null,
        string areaKey = null,
        bool includeBlocked = false
    ) {
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        string regionId = region != null ? region.Id : null;
        string zoneId = zone != null ? zone.Id : null;
        return records.Count(record => MatchesRecord(record, profile, normalizedSourceId, status, category, tag, regionId, zoneId, sceneName, areaKey, includeBlocked));
    }

    public PlayerAreaProfileRecord GetLastRecord(
        AreaProfileDefinition profile = null,
        string sourceId = null,
        AreaProfileRecordStatus? status = null,
        AreaProfileCategory? category = null,
        string tag = null,
        RegionInfoDefinition region = null,
        ActivityZoneDefinition zone = null,
        string sceneName = null,
        string areaKey = null,
        bool includeBlocked = false
    ) {
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        string regionId = region != null ? region.Id : null;
        string zoneId = zone != null ? zone.Id : null;
        return records
            .Where(record => MatchesRecord(record, profile, normalizedSourceId, status, category, tag, regionId, zoneId, sceneName, areaKey, includeBlocked))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.frame)
            .FirstOrDefault();
    }

    public int GetHoursSinceLastEnter(AreaProfileDefinition profile = null, string sourceId = null, bool includeBlocked = false) {
        var lastEntered = GetLastRecord(profile, sourceId, AreaProfileRecordStatus.Entered, includeBlocked: includeBlocked);
        var lastRefreshed = GetLastRecord(profile, sourceId, AreaProfileRecordStatus.Refreshed, includeBlocked: includeBlocked);
        var last = lastEntered;
        if(lastRefreshed != null && (last == null || lastRefreshed.absoluteHour > last.absoluteHour)) {
            last = lastRefreshed;
        }

        return last != null ? Mathf.Max(0, GetCurrentAbsoluteHour() - last.absoluteHour) : -1;
    }

    int GetEnterCount(AreaProfileDefinition profile, string sourceId) {
        return GetRecordCount(profile, sourceId, AreaProfileRecordStatus.Entered, includeBlocked: false)
            + GetRecordCount(profile, sourceId, AreaProfileRecordStatus.Refreshed, includeBlocked: false);
    }

    PlayerAreaProfileState CreateState(AreaProfileDefinition profile, AreaProfileResult result) {
        return new PlayerAreaProfileState {
            stateId = Guid.NewGuid().ToString("N"),
            areaProfileId = profile.Id,
            areaProfileName = profile.DisplayName,
            category = profile.Category,
            sourceId = NormalizeSourceId(result.sourceId),
            sourceName = result.sourceName ?? string.Empty,
            regionId = profile.Region != null ? profile.Region.Id : string.Empty,
            regionName = profile.Region != null ? profile.Region.DisplayName : string.Empty,
            activityZoneId = profile.ActivityZone != null ? profile.ActivityZone.Id : string.Empty,
            activityZoneName = profile.ActivityZone != null ? profile.ActivityZone.DisplayName : string.Empty,
            mapMarkerId = profile.MapMarker != null ? profile.MapMarker.Id : string.Empty,
            mapMarkerName = profile.MapMarker != null ? profile.MapMarker.DisplayName : string.Empty,
            sceneName = result.sceneName,
            areaKey = result.areaKey,
            hasWorldPosition = result.worldPosition.HasValue,
            worldPosition = result.worldPosition ?? Vector3.zero,
            enteredDay = GetCurrentDay(),
            enteredAbsoluteHour = GetCurrentAbsoluteHour(),
            lastUpdatedDay = GetCurrentDay(),
            lastUpdatedAbsoluteHour = GetCurrentAbsoluteHour(),
            enterCount = 1,
            tags = profile.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    void RefreshState(PlayerAreaProfileState state, AreaProfileDefinition profile, AreaProfileResult result) {
        state.sourceId = NormalizeSourceId(result.sourceId);
        state.sourceName = result.sourceName ?? string.Empty;
        state.sceneName = result.sceneName;
        state.areaKey = result.areaKey;
        state.hasWorldPosition = result.worldPosition.HasValue;
        if(result.worldPosition.HasValue) {
            state.worldPosition = result.worldPosition.Value;
        }
        state.lastUpdatedDay = GetCurrentDay();
        state.lastUpdatedAbsoluteHour = GetCurrentAbsoluteHour();
        state.enterCount = Mathf.Max(1, state.enterCount + 1);
        state.tags = profile.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    PlayerAreaProfileRecord RecordOperation(AreaProfileDefinition profile, AreaProfileResult result, AreaProfileRecordStatus status, PlayerAreaProfileState state) {
        if(profile == null || result == null) {
            return null;
        }

        var record = new PlayerAreaProfileRecord {
            recordId = Guid.NewGuid().ToString("N"),
            stateId = state != null ? state.stateId : string.Empty,
            areaProfileId = profile.Id,
            areaProfileName = profile.DisplayName,
            category = profile.Category,
            status = status,
            sourceId = NormalizeSourceId(result.sourceId),
            sourceName = result.sourceName ?? string.Empty,
            regionId = profile.Region != null ? profile.Region.Id : string.Empty,
            regionName = profile.Region != null ? profile.Region.DisplayName : string.Empty,
            activityZoneId = profile.ActivityZone != null ? profile.ActivityZone.Id : string.Empty,
            activityZoneName = profile.ActivityZone != null ? profile.ActivityZone.DisplayName : string.Empty,
            mapMarkerId = profile.MapMarker != null ? profile.MapMarker.Id : string.Empty,
            mapMarkerName = profile.MapMarker != null ? profile.MapMarker.DisplayName : string.Empty,
            sceneName = result.sceneName,
            areaKey = result.areaKey,
            hasWorldPosition = result.worldPosition.HasValue,
            worldPosition = result.worldPosition ?? state?.worldPosition ?? Vector3.zero,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            frame = Time.frameCount,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            blocked = result.blocked || status == AreaProfileRecordStatus.Blocked,
            failureMessage = result.failureMessage,
            locationVisitsApplied = Mathf.Max(0, result.locationVisitsApplied),
            worldDiscoveriesApplied = Mathf.Max(0, result.worldDiscoveriesApplied),
            navigationHintsApplied = Mathf.Max(0, result.navigationHintsApplied),
            chronicleEntriesApplied = Mathf.Max(0, result.chronicleEntriesApplied),
            worldConditionsApplied = Mathf.Max(0, result.worldConditionsApplied),
            skippedActions = Mathf.Max(0, result.skippedActions),
            blockedActions = Mathf.Max(0, result.blockedActions),
            appliedChains = Mathf.Max(0, result.appliedChains),
            blockedChains = Mathf.Max(0, result.blockedChains),
            skippedChains = Mathf.Max(0, result.skippedChains),
            tags = profile.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            messages = result.messages != null ? result.messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToList() : new List<string>()
        };

        records.Add(record);
        TrimHistory();
        return record;
    }

    bool MatchesActive(PlayerAreaProfileState state, AreaProfileDefinition profile, string sourceId, AreaProfileCategory? category, string tag) {
        return state != null
            && (profile == null || state.areaProfileId == profile.Id)
            && (string.IsNullOrWhiteSpace(sourceId) || state.sourceId == sourceId)
            && (!category.HasValue || state.category == category.Value)
            && (string.IsNullOrWhiteSpace(tag) || state.HasTag(tag));
    }

    bool MatchesRecord(PlayerAreaProfileRecord record, AreaProfileDefinition profile, string sourceId, AreaProfileRecordStatus? status, AreaProfileCategory? category, string tag, string regionId, string zoneId, string sceneName, string areaKey, bool includeBlocked) {
        return record != null
            && (includeBlocked || !record.blocked)
            && (profile == null || record.areaProfileId == profile.Id)
            && (string.IsNullOrWhiteSpace(sourceId) || record.sourceId == sourceId)
            && (!status.HasValue || record.status == status.Value)
            && (!category.HasValue || record.category == category.Value)
            && (string.IsNullOrWhiteSpace(tag) || record.HasTag(tag))
            && (string.IsNullOrWhiteSpace(regionId) || record.regionId == regionId)
            && (string.IsNullOrWhiteSpace(zoneId) || record.activityZoneId == zoneId)
            && (string.IsNullOrWhiteSpace(sceneName) || record.sceneName == sceneName)
            && (string.IsNullOrWhiteSpace(areaKey) || record.areaKey == areaKey);
    }

    void TrimHistory() {
        if(maxHistoryRecords <= 0 || records.Count <= maxHistoryRecords) {
            return;
        }

        records = records
            .Where(record => record != null)
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.frame)
            .Take(maxHistoryRecords)
            .OrderBy(record => record.absoluteHour)
            .ThenBy(record => record.frame)
            .ToList();
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "area-profile" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerAreaProfileLogSaveData {
            activeAreas = activeAreas.Where(state => state != null).Select(state => state.ToSaveData()).ToList(),
            records = records.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerAreaProfileLogSaveData;
        activeAreas = saveData?.activeAreas?.Where(entry => entry != null).Select(entry => new PlayerAreaProfileState(entry)).ToList()
            ?? new List<PlayerAreaProfileState>();
        records = saveData?.records?.Where(record => record != null).Select(record => new PlayerAreaProfileRecord(record)).ToList()
            ?? new List<PlayerAreaProfileRecord>();
        TrimHistory();
        OnAreaProfilesChanged?.Invoke();
    }
}

[Serializable]
public class PlayerAreaProfileState {
    [Tooltip("Unique runtime/save id for this active area state.")]
    public string stateId;
    [Tooltip("Saved area profile definition id.")]
    public string areaProfileId;
    [Tooltip("Saved area profile display name.")]
    public string areaProfileName;
    [Tooltip("Area profile category.")]
    public AreaProfileCategory category;
    [Tooltip("Source id that activated this area.")]
    public string sourceId;
    [Tooltip("Source display name for fallback/debug output.")]
    public string sourceName;
    [Tooltip("Related region id.")]
    public string regionId;
    [Tooltip("Related region display name.")]
    public string regionName;
    [Tooltip("Related activity zone id.")]
    public string activityZoneId;
    [Tooltip("Related activity zone display name.")]
    public string activityZoneName;
    [Tooltip("Related map marker id.")]
    public string mapMarkerId;
    [Tooltip("Related map marker display name.")]
    public string mapMarkerName;
    [Tooltip("Scene name connected to this area.")]
    public string sceneName;
    [Tooltip("Custom area key connected to this state.")]
    public string areaKey;
    [Tooltip("If enabled, World Position contains a useful area position.")]
    public bool hasWorldPosition;
    [Tooltip("World position saved for debug/future UI.")]
    public Vector3 worldPosition;
    [Tooltip("In-game day when this area was entered.")]
    public int enteredDay;
    [Tooltip("Absolute in-game hour when this area was entered.")]
    public int enteredAbsoluteHour;
    [Tooltip("In-game day when this state was last refreshed.")]
    public int lastUpdatedDay;
    [Tooltip("Absolute in-game hour when this state was last refreshed.")]
    public int lastUpdatedAbsoluteHour;
    [Tooltip("How many times this active area has been entered/refreshed.")]
    [Min(0)]
    public int enterCount;
    [Tooltip("Free-form tags copied from the area profile.")]
    public List<string> tags = new List<string>();

    public PlayerAreaProfileState() {
    }

    public PlayerAreaProfileState(PlayerAreaProfileStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        stateId = saveData.stateId;
        areaProfileId = saveData.areaProfileId;
        areaProfileName = saveData.areaProfileName;
        category = saveData.category;
        sourceId = saveData.sourceId;
        sourceName = saveData.sourceName;
        regionId = saveData.regionId;
        regionName = saveData.regionName;
        activityZoneId = saveData.activityZoneId;
        activityZoneName = saveData.activityZoneName;
        mapMarkerId = saveData.mapMarkerId;
        mapMarkerName = saveData.mapMarkerName;
        sceneName = saveData.sceneName;
        areaKey = saveData.areaKey;
        hasWorldPosition = saveData.hasWorldPosition;
        worldPosition = saveData.worldPosition;
        enteredDay = saveData.enteredDay;
        enteredAbsoluteHour = saveData.enteredAbsoluteHour;
        lastUpdatedDay = saveData.lastUpdatedDay;
        lastUpdatedAbsoluteHour = saveData.lastUpdatedAbsoluteHour;
        enterCount = Mathf.Max(0, saveData.enterCount);
        tags = saveData.tags ?? new List<string>();
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public AreaProfileDefinition ResolveDefinition() {
        if(string.IsNullOrWhiteSpace(areaProfileId)) {
            return null;
        }

        return Resources.LoadAll<AreaProfileDefinition>("").FirstOrDefault(profile => profile != null && profile.Id == areaProfileId);
    }

    public PlayerAreaProfileStateSaveData ToSaveData() {
        return new PlayerAreaProfileStateSaveData {
            stateId = stateId,
            areaProfileId = areaProfileId,
            areaProfileName = areaProfileName,
            category = category,
            sourceId = sourceId,
            sourceName = sourceName,
            regionId = regionId,
            regionName = regionName,
            activityZoneId = activityZoneId,
            activityZoneName = activityZoneName,
            mapMarkerId = mapMarkerId,
            mapMarkerName = mapMarkerName,
            sceneName = sceneName,
            areaKey = areaKey,
            hasWorldPosition = hasWorldPosition,
            worldPosition = worldPosition,
            enteredDay = enteredDay,
            enteredAbsoluteHour = enteredAbsoluteHour,
            lastUpdatedDay = lastUpdatedDay,
            lastUpdatedAbsoluteHour = lastUpdatedAbsoluteHour,
            enterCount = enterCount,
            tags = tags != null ? tags.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerAreaProfileRecord {
    [Tooltip("Unique runtime/save id for this area operation record.")]
    public string recordId;
    [Tooltip("Active state id connected to this operation when available.")]
    public string stateId;
    [Tooltip("Saved area profile definition id.")]
    public string areaProfileId;
    [Tooltip("Saved area profile display name.")]
    public string areaProfileName;
    [Tooltip("Area profile category.")]
    public AreaProfileCategory category;
    [Tooltip("Operation status recorded for this area.")]
    public AreaProfileRecordStatus status;
    [Tooltip("Source id used by repeat rules and filters.")]
    public string sourceId;
    [Tooltip("Source display name for fallback/debug output.")]
    public string sourceName;
    [Tooltip("Related region id.")]
    public string regionId;
    [Tooltip("Related region display name.")]
    public string regionName;
    [Tooltip("Related activity zone id.")]
    public string activityZoneId;
    [Tooltip("Related activity zone display name.")]
    public string activityZoneName;
    [Tooltip("Related map marker id.")]
    public string mapMarkerId;
    [Tooltip("Related map marker display name.")]
    public string mapMarkerName;
    [Tooltip("Scene name connected to this record.")]
    public string sceneName;
    [Tooltip("Custom area key connected to this record.")]
    public string areaKey;
    [Tooltip("If enabled, World Position contains a useful area position.")]
    public bool hasWorldPosition;
    [Tooltip("World position saved for debug/future UI.")]
    public Vector3 worldPosition;
    [Tooltip("In-game day when this operation was recorded.")]
    public int day;
    [Tooltip("Absolute in-game hour when this operation was recorded.")]
    public int absoluteHour;
    [Tooltip("Unity frame when this operation was recorded.")]
    public int frame;
    [Tooltip("Local timestamp when this operation was recorded.")]
    public string timestamp;
    [Tooltip("If enabled, this record represents a blocked operation.")]
    public bool blocked;
    [Tooltip("Failure reason saved for blocked operations.")]
    public string failureMessage;
    [Tooltip("Number of location visits applied.")]
    [Min(0)]
    public int locationVisitsApplied;
    [Tooltip("Number of world discoveries applied.")]
    [Min(0)]
    public int worldDiscoveriesApplied;
    [Tooltip("Number of navigation hint actions applied.")]
    [Min(0)]
    public int navigationHintsApplied;
    [Tooltip("Number of chronicle entries applied.")]
    [Min(0)]
    public int chronicleEntriesApplied;
    [Tooltip("Number of world condition changes applied.")]
    [Min(0)]
    public int worldConditionsApplied;
    [Tooltip("Number of null or skipped area actions.")]
    [Min(0)]
    public int skippedActions;
    [Tooltip("Number of area actions blocked by their own rules.")]
    [Min(0)]
    public int blockedActions;
    [Tooltip("Number of consequence chains applied successfully.")]
    [Min(0)]
    public int appliedChains;
    [Tooltip("Number of consequence chains blocked by their own rules.")]
    [Min(0)]
    public int blockedChains;
    [Tooltip("Number of null chain slots skipped.")]
    [Min(0)]
    public int skippedChains;
    [Tooltip("Free-form tags copied from the area profile.")]
    public List<string> tags = new List<string>();
    [Tooltip("Extra debug messages saved with this record.")]
    public List<string> messages = new List<string>();

    public PlayerAreaProfileRecord() {
    }

    public PlayerAreaProfileRecord(PlayerAreaProfileRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        stateId = saveData.stateId;
        areaProfileId = saveData.areaProfileId;
        areaProfileName = saveData.areaProfileName;
        category = saveData.category;
        status = saveData.status;
        sourceId = saveData.sourceId;
        sourceName = saveData.sourceName;
        regionId = saveData.regionId;
        regionName = saveData.regionName;
        activityZoneId = saveData.activityZoneId;
        activityZoneName = saveData.activityZoneName;
        mapMarkerId = saveData.mapMarkerId;
        mapMarkerName = saveData.mapMarkerName;
        sceneName = saveData.sceneName;
        areaKey = saveData.areaKey;
        hasWorldPosition = saveData.hasWorldPosition;
        worldPosition = saveData.worldPosition;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
        frame = saveData.frame;
        timestamp = saveData.timestamp;
        blocked = saveData.blocked;
        failureMessage = saveData.failureMessage;
        locationVisitsApplied = Mathf.Max(0, saveData.locationVisitsApplied);
        worldDiscoveriesApplied = Mathf.Max(0, saveData.worldDiscoveriesApplied);
        navigationHintsApplied = Mathf.Max(0, saveData.navigationHintsApplied);
        chronicleEntriesApplied = Mathf.Max(0, saveData.chronicleEntriesApplied);
        worldConditionsApplied = Mathf.Max(0, saveData.worldConditionsApplied);
        skippedActions = Mathf.Max(0, saveData.skippedActions);
        blockedActions = Mathf.Max(0, saveData.blockedActions);
        appliedChains = Mathf.Max(0, saveData.appliedChains);
        blockedChains = Mathf.Max(0, saveData.blockedChains);
        skippedChains = Mathf.Max(0, saveData.skippedChains);
        tags = saveData.tags ?? new List<string>();
        messages = saveData.messages ?? new List<string>();
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public PlayerAreaProfileRecordSaveData ToSaveData() {
        return new PlayerAreaProfileRecordSaveData {
            recordId = recordId,
            stateId = stateId,
            areaProfileId = areaProfileId,
            areaProfileName = areaProfileName,
            category = category,
            status = status,
            sourceId = sourceId,
            sourceName = sourceName,
            regionId = regionId,
            regionName = regionName,
            activityZoneId = activityZoneId,
            activityZoneName = activityZoneName,
            mapMarkerId = mapMarkerId,
            mapMarkerName = mapMarkerName,
            sceneName = sceneName,
            areaKey = areaKey,
            hasWorldPosition = hasWorldPosition,
            worldPosition = worldPosition,
            day = day,
            absoluteHour = absoluteHour,
            frame = frame,
            timestamp = timestamp,
            blocked = blocked,
            failureMessage = failureMessage,
            locationVisitsApplied = locationVisitsApplied,
            worldDiscoveriesApplied = worldDiscoveriesApplied,
            navigationHintsApplied = navigationHintsApplied,
            chronicleEntriesApplied = chronicleEntriesApplied,
            worldConditionsApplied = worldConditionsApplied,
            skippedActions = skippedActions,
            blockedActions = blockedActions,
            appliedChains = appliedChains,
            blockedChains = blockedChains,
            skippedChains = skippedChains,
            tags = tags != null ? tags.ToList() : new List<string>(),
            messages = messages != null ? messages.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerAreaProfileLogSaveData {
    public List<PlayerAreaProfileStateSaveData> activeAreas = new List<PlayerAreaProfileStateSaveData>();
    public List<PlayerAreaProfileRecordSaveData> records = new List<PlayerAreaProfileRecordSaveData>();
}

[Serializable]
public class PlayerAreaProfileStateSaveData {
    public string stateId;
    public string areaProfileId;
    public string areaProfileName;
    public AreaProfileCategory category;
    public string sourceId;
    public string sourceName;
    public string regionId;
    public string regionName;
    public string activityZoneId;
    public string activityZoneName;
    public string mapMarkerId;
    public string mapMarkerName;
    public string sceneName;
    public string areaKey;
    public bool hasWorldPosition;
    public Vector3 worldPosition;
    public int enteredDay;
    public int enteredAbsoluteHour;
    public int lastUpdatedDay;
    public int lastUpdatedAbsoluteHour;
    public int enterCount;
    public List<string> tags;
}

[Serializable]
public class PlayerAreaProfileRecordSaveData {
    public string recordId;
    public string stateId;
    public string areaProfileId;
    public string areaProfileName;
    public AreaProfileCategory category;
    public AreaProfileRecordStatus status;
    public string sourceId;
    public string sourceName;
    public string regionId;
    public string regionName;
    public string activityZoneId;
    public string activityZoneName;
    public string mapMarkerId;
    public string mapMarkerName;
    public string sceneName;
    public string areaKey;
    public bool hasWorldPosition;
    public Vector3 worldPosition;
    public int day;
    public int absoluteHour;
    public int frame;
    public string timestamp;
    public bool blocked;
    public string failureMessage;
    public int locationVisitsApplied;
    public int worldDiscoveriesApplied;
    public int navigationHintsApplied;
    public int chronicleEntriesApplied;
    public int worldConditionsApplied;
    public int skippedActions;
    public int blockedActions;
    public int appliedChains;
    public int blockedChains;
    public int skippedChains;
    public List<string> tags;
    public List<string> messages;
}
