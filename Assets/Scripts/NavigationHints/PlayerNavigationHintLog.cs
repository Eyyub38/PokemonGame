using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum NavigationHintRecordStatus {
    Activated,
    Refreshed,
    Completed,
    Cleared,
    Blocked
}

public class PlayerNavigationHintLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum operation records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 600;
    [Tooltip("Runtime/save list of currently active navigation hints.")]
    [SerializeField] List<PlayerNavigationHintState> activeHints = new List<PlayerNavigationHintState>();
    [Tooltip("Runtime/save history of navigation hint operations.")]
    [SerializeField] List<PlayerNavigationHintRecord> records = new List<PlayerNavigationHintRecord>();

    public IReadOnlyList<PlayerNavigationHintState> ActiveHints => activeHints;
    public IReadOnlyList<PlayerNavigationHintRecord> Records => records;
    public event Action<PlayerNavigationHintState> OnHintActivated;
    public event Action<PlayerNavigationHintRecord> OnHintCompleted;
    public event Action<PlayerNavigationHintRecord> OnHintCleared;
    public event Action OnNavigationHintsChanged;

    public bool CanActivate(NavigationHintDefinition hint, string sourceId, ConsequenceChainRepeatMode repeatMode, int cooldownHours, int maxActivationCount, out string failureMessage) {
        if(hint == null) {
            failureMessage = "No navigation hint selected.";
            return false;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId);
        int totalActivations = GetRecordCount(hint, status: NavigationHintRecordStatus.Activated, includeBlocked: false)
            + GetRecordCount(hint, status: NavigationHintRecordStatus.Refreshed, includeBlocked: false);
        if(maxActivationCount > 0 && totalActivations >= maxActivationCount) {
            failureMessage = $"{hint.DisplayName} has reached its maximum activation count.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && totalActivations > 0) {
            failureMessage = $"{hint.DisplayName} has already been activated.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetActivationCount(hint, normalizedSourceId) > 0) {
            failureMessage = $"{hint.DisplayName} has already been activated from this source.";
            return false;
        }

        var lastSourceRecord = GetLastRecord(hint, sourceId: normalizedSourceId, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastSourceRecord != null && lastSourceRecord.day == GetCurrentDay()) {
            failureMessage = $"{hint.DisplayName} can only be activated once per day from this source.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastSourceRecord != null) {
            int elapsed = GetCurrentAbsoluteHour() - lastSourceRecord.absoluteHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{hint.DisplayName} will be available again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public PlayerNavigationHintState ActivateHint(NavigationHintDefinition hint, NavigationHintResult result) {
        if(hint == null || result == null) {
            return null;
        }

        var existing = GetActiveHint(hint);
        if(existing != null && hint.RefreshExistingHint) {
            RefreshState(existing, hint, result);
            RecordOperation(hint, result, NavigationHintRecordStatus.Refreshed, existing);
            OnHintActivated?.Invoke(existing);
            OnNavigationHintsChanged?.Invoke();
            return existing;
        }

        var state = CreateState(hint, result);
        activeHints.Add(state);
        RecordOperation(hint, result, NavigationHintRecordStatus.Activated, state);
        OnHintActivated?.Invoke(state);
        OnNavigationHintsChanged?.Invoke();
        return state;
    }

    public bool CompleteHint(NavigationHintDefinition hint, NavigationHintResult result) {
        var state = GetActiveHint(hint);
        if(state == null || hint == null || result == null) {
            return false;
        }

        activeHints.Remove(state);
        var record = RecordOperation(hint, result, NavigationHintRecordStatus.Completed, state);
        OnHintCompleted?.Invoke(record);
        OnNavigationHintsChanged?.Invoke();
        return true;
    }

    public bool ClearHint(NavigationHintDefinition hint, NavigationHintResult result) {
        var state = GetActiveHint(hint);
        if(state == null || hint == null || result == null) {
            return false;
        }

        activeHints.Remove(state);
        var record = RecordOperation(hint, result, NavigationHintRecordStatus.Cleared, state);
        OnHintCleared?.Invoke(record);
        OnNavigationHintsChanged?.Invoke();
        return true;
    }

    public PlayerNavigationHintRecord RecordBlocked(NavigationHintDefinition hint, NavigationHintResult result) {
        return RecordOperation(hint, result, NavigationHintRecordStatus.Blocked, null);
    }

    public bool HasActiveHint(NavigationHintDefinition hint = null, string sourceId = null, NavigationHintCategory? category = null, string tag = null) {
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return activeHints.Any(state => MatchesActive(state, hint, normalizedSourceId, category, tag));
    }

    public PlayerNavigationHintState GetActiveHint(NavigationHintDefinition hint, string sourceId = null) {
        if(hint == null) {
            return null;
        }

        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return activeHints.FirstOrDefault(state => state != null
            && state.hintId == hint.Id
            && (string.IsNullOrWhiteSpace(normalizedSourceId) || state.sourceId == normalizedSourceId));
    }

    public int GetRecordCount(
        NavigationHintDefinition hint = null,
        string sourceId = null,
        NavigationHintRecordStatus? status = null,
        NavigationHintCategory? category = null,
        string tag = null,
        RegionInfoDefinition region = null,
        string sceneName = null,
        string locationKey = null,
        bool includeBlocked = false
    ) {
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        string regionId = region != null ? region.Id : null;
        return records.Count(record => MatchesRecord(record, hint, normalizedSourceId, status, category, tag, regionId, sceneName, locationKey, includeBlocked));
    }

    public PlayerNavigationHintRecord GetLastRecord(
        NavigationHintDefinition hint = null,
        string sourceId = null,
        NavigationHintRecordStatus? status = null,
        NavigationHintCategory? category = null,
        string tag = null,
        RegionInfoDefinition region = null,
        string sceneName = null,
        string locationKey = null,
        bool includeBlocked = false
    ) {
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        string regionId = region != null ? region.Id : null;
        return records
            .Where(record => MatchesRecord(record, hint, normalizedSourceId, status, category, tag, regionId, sceneName, locationKey, includeBlocked))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.frame)
            .FirstOrDefault();
    }

    public int GetHoursSinceLastRecord(NavigationHintDefinition hint = null, string sourceId = null, NavigationHintRecordStatus? status = null, bool includeBlocked = false) {
        var lastRecord = GetLastRecord(hint, sourceId, status, includeBlocked: includeBlocked);
        return lastRecord != null ? Mathf.Max(0, GetCurrentAbsoluteHour() - lastRecord.absoluteHour) : -1;
    }

    public bool SetHintPinned(string hintId, bool pinned) {
        var state = activeHints.FirstOrDefault(entry => entry != null && entry.hintId == hintId);
        if(state == null) {
            return false;
        }

        state.pinned = pinned;
        OnNavigationHintsChanged?.Invoke();
        return true;
    }

    public bool SetHintRead(string hintId, bool read) {
        var state = activeHints.FirstOrDefault(entry => entry != null && entry.hintId == hintId);
        if(state == null) {
            return false;
        }

        state.read = read;
        OnNavigationHintsChanged?.Invoke();
        return true;
    }

    int GetActivationCount(NavigationHintDefinition hint, string sourceId) {
        return GetRecordCount(hint, sourceId, NavigationHintRecordStatus.Activated, includeBlocked: false)
            + GetRecordCount(hint, sourceId, NavigationHintRecordStatus.Refreshed, includeBlocked: false);
    }

    PlayerNavigationHintState CreateState(NavigationHintDefinition hint, NavigationHintResult result) {
        var state = new PlayerNavigationHintState {
            stateId = Guid.NewGuid().ToString("N"),
            hintId = hint.Id,
            hintName = hint.DisplayName,
            category = hint.Category,
            sourceId = NormalizeSourceId(result.sourceId),
            sourceName = result.sourceName ?? string.Empty,
            targetLabel = result.targetLabel,
            regionId = hint.Region != null ? hint.Region.Id : string.Empty,
            regionName = hint.Region != null ? hint.Region.DisplayName : string.Empty,
            mapMarkerId = hint.MapMarker != null ? hint.MapMarker.Id : string.Empty,
            mapMarkerName = hint.MapMarker != null ? hint.MapMarker.DisplayName : string.Empty,
            activityZoneId = hint.ActivityZone != null ? hint.ActivityZone.Id : string.Empty,
            sceneName = result.sceneName,
            locationKey = result.locationKey,
            hasWorldPosition = result.targetWorldPosition.HasValue,
            worldPosition = result.targetWorldPosition ?? Vector3.zero,
            icon = hint.Icon,
            color = hint.Color,
            priority = hint.Priority,
            showOnMinimap = hint.ShowOnMinimap,
            showOnWorldMap = hint.ShowOnWorldMap,
            showInPokeNav = hint.ShowInPokeNav,
            pinned = hint.PinnedByDefault,
            activatedDay = GetCurrentDay(),
            activatedAbsoluteHour = GetCurrentAbsoluteHour(),
            lastUpdatedDay = GetCurrentDay(),
            lastUpdatedAbsoluteHour = GetCurrentAbsoluteHour(),
            activationCount = 1,
            tags = hint.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };

        return state;
    }

    void RefreshState(PlayerNavigationHintState state, NavigationHintDefinition hint, NavigationHintResult result) {
        state.sourceId = NormalizeSourceId(result.sourceId);
        state.sourceName = result.sourceName ?? string.Empty;
        state.targetLabel = result.targetLabel;
        state.sceneName = result.sceneName;
        state.locationKey = result.locationKey;
        state.hasWorldPosition = result.targetWorldPosition.HasValue;
        state.worldPosition = result.targetWorldPosition ?? state.worldPosition;
        state.priority = hint.Priority;
        state.showOnMinimap = hint.ShowOnMinimap;
        state.showOnWorldMap = hint.ShowOnWorldMap;
        state.showInPokeNav = hint.ShowInPokeNav;
        state.lastUpdatedDay = GetCurrentDay();
        state.lastUpdatedAbsoluteHour = GetCurrentAbsoluteHour();
        state.activationCount = Mathf.Max(1, state.activationCount + 1);
    }

    PlayerNavigationHintRecord RecordOperation(NavigationHintDefinition hint, NavigationHintResult result, NavigationHintRecordStatus status, PlayerNavigationHintState state) {
        if(hint == null || result == null) {
            return null;
        }

        var record = new PlayerNavigationHintRecord {
            recordId = Guid.NewGuid().ToString("N"),
            stateId = state != null ? state.stateId : string.Empty,
            hintId = hint.Id,
            hintName = hint.DisplayName,
            category = hint.Category,
            status = status,
            sourceId = NormalizeSourceId(result.sourceId),
            sourceName = result.sourceName ?? string.Empty,
            targetLabel = result.targetLabel,
            regionId = hint.Region != null ? hint.Region.Id : string.Empty,
            regionName = hint.Region != null ? hint.Region.DisplayName : string.Empty,
            mapMarkerId = hint.MapMarker != null ? hint.MapMarker.Id : string.Empty,
            mapMarkerName = hint.MapMarker != null ? hint.MapMarker.DisplayName : string.Empty,
            activityZoneId = hint.ActivityZone != null ? hint.ActivityZone.Id : string.Empty,
            sceneName = result.sceneName,
            locationKey = result.locationKey,
            hasWorldPosition = result.targetWorldPosition.HasValue,
            worldPosition = result.targetWorldPosition ?? state?.worldPosition ?? Vector3.zero,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            frame = Time.frameCount,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            blocked = result.blocked || status == NavigationHintRecordStatus.Blocked,
            failureMessage = result.failureMessage,
            regionDiscovered = result.regionDiscovered,
            mapMarkerDiscovered = result.mapMarkerDiscovered,
            pokeNavEntryDiscovered = result.pokeNavEntryDiscovered,
            socialPostUnlocked = result.socialPostUnlocked,
            locationVisitRecorded = result.locationVisitRecorded,
            appliedDiscoveries = Mathf.Max(0, result.appliedDiscoveries),
            blockedDiscoveries = Mathf.Max(0, result.blockedDiscoveries),
            skippedDiscoveries = Mathf.Max(0, result.skippedDiscoveries),
            appliedChains = Mathf.Max(0, result.appliedChains),
            blockedChains = Mathf.Max(0, result.blockedChains),
            skippedChains = Mathf.Max(0, result.skippedChains),
            tags = hint.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            messages = result.messages != null ? result.messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToList() : new List<string>()
        };

        records.Add(record);
        TrimHistory();
        return record;
    }

    bool MatchesActive(PlayerNavigationHintState state, NavigationHintDefinition hint, string sourceId, NavigationHintCategory? category, string tag) {
        return state != null
            && (hint == null || state.hintId == hint.Id)
            && (string.IsNullOrWhiteSpace(sourceId) || state.sourceId == sourceId)
            && (!category.HasValue || state.category == category.Value)
            && (string.IsNullOrWhiteSpace(tag) || state.HasTag(tag));
    }

    bool MatchesRecord(PlayerNavigationHintRecord record, NavigationHintDefinition hint, string sourceId, NavigationHintRecordStatus? status, NavigationHintCategory? category, string tag, string regionId, string sceneName, string locationKey, bool includeBlocked) {
        return record != null
            && (includeBlocked || !record.blocked)
            && (hint == null || record.hintId == hint.Id)
            && (string.IsNullOrWhiteSpace(sourceId) || record.sourceId == sourceId)
            && (!status.HasValue || record.status == status.Value)
            && (!category.HasValue || record.category == category.Value)
            && (string.IsNullOrWhiteSpace(tag) || record.HasTag(tag))
            && (string.IsNullOrWhiteSpace(regionId) || record.regionId == regionId)
            && (string.IsNullOrWhiteSpace(sceneName) || record.sceneName == sceneName)
            && (string.IsNullOrWhiteSpace(locationKey) || record.locationKey == locationKey);
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
        return string.IsNullOrWhiteSpace(sourceId) ? "navigation-hint" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerNavigationHintLogSaveData {
            activeHints = activeHints.Where(state => state != null).Select(state => state.ToSaveData()).ToList(),
            records = records.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerNavigationHintLogSaveData;
        activeHints = saveData?.activeHints?.Where(entry => entry != null).Select(entry => new PlayerNavigationHintState(entry)).ToList()
            ?? new List<PlayerNavigationHintState>();
        records = saveData?.records?.Where(record => record != null).Select(record => new PlayerNavigationHintRecord(record)).ToList()
            ?? new List<PlayerNavigationHintRecord>();
        TrimHistory();
        OnNavigationHintsChanged?.Invoke();
    }
}

[Serializable]
public class PlayerNavigationHintState {
    [Tooltip("Unique runtime/save id for this active hint.")]
    public string stateId;
    [Tooltip("Saved navigation hint definition id.")]
    public string hintId;
    [Tooltip("Saved hint display name for fallback/debug output.")]
    public string hintName;
    [Tooltip("Hint category used by filters and future UI.")]
    public NavigationHintCategory category;
    [Tooltip("Source id that activated this hint.")]
    public string sourceId;
    [Tooltip("Source display name for debug/fallback output.")]
    public string sourceName;
    [Tooltip("Short target label shown by future UI.")]
    public string targetLabel;
    [Tooltip("Related region id.")]
    public string regionId;
    [Tooltip("Related region display name.")]
    public string regionName;
    [Tooltip("Related map marker id.")]
    public string mapMarkerId;
    [Tooltip("Related map marker display name.")]
    public string mapMarkerName;
    [Tooltip("Related activity zone id.")]
    public string activityZoneId;
    [Tooltip("Target scene name.")]
    public string sceneName;
    [Tooltip("Custom location key for route, room, portal or area matching.")]
    public string locationKey;
    [Tooltip("If enabled, World Position contains a useful target position.")]
    public bool hasWorldPosition;
    [Tooltip("World position for future minimap/world map UI.")]
    public Vector3 worldPosition;
    [Tooltip("Icon used by future minimap/world map UI.")]
    public Sprite icon;
    [Tooltip("Tint color used by future minimap/world map UI.")]
    public Color color = Color.white;
    [Tooltip("Higher priority hints can sort above lower priority hints.")]
    public int priority;
    [Tooltip("If enabled, this active hint can appear on minimap.")]
    public bool showOnMinimap;
    [Tooltip("If enabled, this active hint can appear on world map.")]
    public bool showOnWorldMap;
    [Tooltip("If enabled, this active hint can appear in PokeNav guidance.")]
    public bool showInPokeNav;
    [Tooltip("Whether future UI has marked this hint as read.")]
    public bool read;
    [Tooltip("Whether future UI should keep this hint pinned.")]
    public bool pinned;
    [Tooltip("In-game day when this hint first activated.")]
    public int activatedDay;
    [Tooltip("Absolute in-game hour when this hint first activated.")]
    public int activatedAbsoluteHour;
    [Tooltip("In-game day when this hint last refreshed.")]
    public int lastUpdatedDay;
    [Tooltip("Absolute in-game hour when this hint last refreshed.")]
    public int lastUpdatedAbsoluteHour;
    [Tooltip("How many times this active hint has been activated/refreshed.")]
    [Min(0)]
    public int activationCount;
    [Tooltip("Free-form tags used by filters and requirements.")]
    public List<string> tags = new List<string>();

    public PlayerNavigationHintState() {
    }

    public PlayerNavigationHintState(PlayerNavigationHintStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        stateId = saveData.stateId;
        hintId = saveData.hintId;
        hintName = saveData.hintName;
        category = saveData.category;
        sourceId = saveData.sourceId;
        sourceName = saveData.sourceName;
        targetLabel = saveData.targetLabel;
        regionId = saveData.regionId;
        regionName = saveData.regionName;
        mapMarkerId = saveData.mapMarkerId;
        mapMarkerName = saveData.mapMarkerName;
        activityZoneId = saveData.activityZoneId;
        sceneName = saveData.sceneName;
        locationKey = saveData.locationKey;
        hasWorldPosition = saveData.hasWorldPosition;
        worldPosition = saveData.worldPosition;
        color = saveData.color;
        priority = saveData.priority;
        showOnMinimap = saveData.showOnMinimap;
        showOnWorldMap = saveData.showOnWorldMap;
        showInPokeNav = saveData.showInPokeNav;
        read = saveData.read;
        pinned = saveData.pinned;
        activatedDay = saveData.activatedDay;
        activatedAbsoluteHour = saveData.activatedAbsoluteHour;
        lastUpdatedDay = saveData.lastUpdatedDay;
        lastUpdatedAbsoluteHour = saveData.lastUpdatedAbsoluteHour;
        activationCount = Mathf.Max(0, saveData.activationCount);
        tags = saveData.tags ?? new List<string>();
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public NavigationHintDefinition ResolveDefinition() {
        if(string.IsNullOrWhiteSpace(hintId)) {
            return null;
        }

        return Resources.LoadAll<NavigationHintDefinition>("").FirstOrDefault(hint => hint != null && hint.Id == hintId);
    }

    public PlayerNavigationHintStateSaveData ToSaveData() {
        return new PlayerNavigationHintStateSaveData {
            stateId = stateId,
            hintId = hintId,
            hintName = hintName,
            category = category,
            sourceId = sourceId,
            sourceName = sourceName,
            targetLabel = targetLabel,
            regionId = regionId,
            regionName = regionName,
            mapMarkerId = mapMarkerId,
            mapMarkerName = mapMarkerName,
            activityZoneId = activityZoneId,
            sceneName = sceneName,
            locationKey = locationKey,
            hasWorldPosition = hasWorldPosition,
            worldPosition = worldPosition,
            color = color,
            priority = priority,
            showOnMinimap = showOnMinimap,
            showOnWorldMap = showOnWorldMap,
            showInPokeNav = showInPokeNav,
            read = read,
            pinned = pinned,
            activatedDay = activatedDay,
            activatedAbsoluteHour = activatedAbsoluteHour,
            lastUpdatedDay = lastUpdatedDay,
            lastUpdatedAbsoluteHour = lastUpdatedAbsoluteHour,
            activationCount = activationCount,
            tags = tags != null ? tags.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerNavigationHintRecord {
    [Tooltip("Unique runtime/save id for this operation record.")]
    public string recordId;
    [Tooltip("Active state id connected to this operation when available.")]
    public string stateId;
    [Tooltip("Saved navigation hint definition id.")]
    public string hintId;
    [Tooltip("Saved hint display name for fallback/debug output.")]
    public string hintName;
    [Tooltip("Hint category used by filters and future UI.")]
    public NavigationHintCategory category;
    [Tooltip("Operation status recorded for this hint.")]
    public NavigationHintRecordStatus status;
    [Tooltip("Source id used by repeat rules and filters.")]
    public string sourceId;
    [Tooltip("Source display name for debug/fallback output.")]
    public string sourceName;
    [Tooltip("Short target label shown by future UI.")]
    public string targetLabel;
    [Tooltip("Related region id.")]
    public string regionId;
    [Tooltip("Related region display name.")]
    public string regionName;
    [Tooltip("Related map marker id.")]
    public string mapMarkerId;
    [Tooltip("Related map marker display name.")]
    public string mapMarkerName;
    [Tooltip("Related activity zone id.")]
    public string activityZoneId;
    [Tooltip("Target scene name.")]
    public string sceneName;
    [Tooltip("Custom location key for route, room, portal or area matching.")]
    public string locationKey;
    [Tooltip("If enabled, World Position contains a useful target position.")]
    public bool hasWorldPosition;
    [Tooltip("World position saved with this operation.")]
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
    [Tooltip("If enabled, this operation discovered a region.")]
    public bool regionDiscovered;
    [Tooltip("If enabled, this operation discovered a map marker.")]
    public bool mapMarkerDiscovered;
    [Tooltip("If enabled, this operation discovered a PokeNav entry.")]
    public bool pokeNavEntryDiscovered;
    [Tooltip("If enabled, this operation unlocked a social post.")]
    public bool socialPostUnlocked;
    [Tooltip("If enabled, this operation recorded a linked location visit.")]
    public bool locationVisitRecorded;
    [Tooltip("Number of linked world discoveries applied successfully.")]
    [Min(0)]
    public int appliedDiscoveries;
    [Tooltip("Number of linked world discoveries blocked by their own rules.")]
    [Min(0)]
    public int blockedDiscoveries;
    [Tooltip("Number of null discovery slots skipped.")]
    [Min(0)]
    public int skippedDiscoveries;
    [Tooltip("Number of consequence chains applied successfully.")]
    [Min(0)]
    public int appliedChains;
    [Tooltip("Number of consequence chains blocked by their own rules.")]
    [Min(0)]
    public int blockedChains;
    [Tooltip("Number of null chain slots skipped.")]
    [Min(0)]
    public int skippedChains;
    [Tooltip("Free-form tags used by filters and requirements.")]
    public List<string> tags = new List<string>();
    [Tooltip("Extra debug messages saved with this record.")]
    public List<string> messages = new List<string>();

    public PlayerNavigationHintRecord() {
    }

    public PlayerNavigationHintRecord(PlayerNavigationHintRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        stateId = saveData.stateId;
        hintId = saveData.hintId;
        hintName = saveData.hintName;
        category = saveData.category;
        status = saveData.status;
        sourceId = saveData.sourceId;
        sourceName = saveData.sourceName;
        targetLabel = saveData.targetLabel;
        regionId = saveData.regionId;
        regionName = saveData.regionName;
        mapMarkerId = saveData.mapMarkerId;
        mapMarkerName = saveData.mapMarkerName;
        activityZoneId = saveData.activityZoneId;
        sceneName = saveData.sceneName;
        locationKey = saveData.locationKey;
        hasWorldPosition = saveData.hasWorldPosition;
        worldPosition = saveData.worldPosition;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
        frame = saveData.frame;
        timestamp = saveData.timestamp;
        blocked = saveData.blocked;
        failureMessage = saveData.failureMessage;
        regionDiscovered = saveData.regionDiscovered;
        mapMarkerDiscovered = saveData.mapMarkerDiscovered;
        pokeNavEntryDiscovered = saveData.pokeNavEntryDiscovered;
        socialPostUnlocked = saveData.socialPostUnlocked;
        locationVisitRecorded = saveData.locationVisitRecorded;
        appliedDiscoveries = Mathf.Max(0, saveData.appliedDiscoveries);
        blockedDiscoveries = Mathf.Max(0, saveData.blockedDiscoveries);
        skippedDiscoveries = Mathf.Max(0, saveData.skippedDiscoveries);
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

    public PlayerNavigationHintRecordSaveData ToSaveData() {
        return new PlayerNavigationHintRecordSaveData {
            recordId = recordId,
            stateId = stateId,
            hintId = hintId,
            hintName = hintName,
            category = category,
            status = status,
            sourceId = sourceId,
            sourceName = sourceName,
            targetLabel = targetLabel,
            regionId = regionId,
            regionName = regionName,
            mapMarkerId = mapMarkerId,
            mapMarkerName = mapMarkerName,
            activityZoneId = activityZoneId,
            sceneName = sceneName,
            locationKey = locationKey,
            hasWorldPosition = hasWorldPosition,
            worldPosition = worldPosition,
            day = day,
            absoluteHour = absoluteHour,
            frame = frame,
            timestamp = timestamp,
            blocked = blocked,
            failureMessage = failureMessage,
            regionDiscovered = regionDiscovered,
            mapMarkerDiscovered = mapMarkerDiscovered,
            pokeNavEntryDiscovered = pokeNavEntryDiscovered,
            socialPostUnlocked = socialPostUnlocked,
            locationVisitRecorded = locationVisitRecorded,
            appliedDiscoveries = appliedDiscoveries,
            blockedDiscoveries = blockedDiscoveries,
            skippedDiscoveries = skippedDiscoveries,
            appliedChains = appliedChains,
            blockedChains = blockedChains,
            skippedChains = skippedChains,
            tags = tags != null ? tags.ToList() : new List<string>(),
            messages = messages != null ? messages.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerNavigationHintLogSaveData {
    public List<PlayerNavigationHintStateSaveData> activeHints = new List<PlayerNavigationHintStateSaveData>();
    public List<PlayerNavigationHintRecordSaveData> records = new List<PlayerNavigationHintRecordSaveData>();
}

[Serializable]
public class PlayerNavigationHintStateSaveData {
    public string stateId;
    public string hintId;
    public string hintName;
    public NavigationHintCategory category;
    public string sourceId;
    public string sourceName;
    public string targetLabel;
    public string regionId;
    public string regionName;
    public string mapMarkerId;
    public string mapMarkerName;
    public string activityZoneId;
    public string sceneName;
    public string locationKey;
    public bool hasWorldPosition;
    public Vector3 worldPosition;
    public Color color;
    public int priority;
    public bool showOnMinimap;
    public bool showOnWorldMap;
    public bool showInPokeNav;
    public bool read;
    public bool pinned;
    public int activatedDay;
    public int activatedAbsoluteHour;
    public int lastUpdatedDay;
    public int lastUpdatedAbsoluteHour;
    public int activationCount;
    public List<string> tags;
}

[Serializable]
public class PlayerNavigationHintRecordSaveData {
    public string recordId;
    public string stateId;
    public string hintId;
    public string hintName;
    public NavigationHintCategory category;
    public NavigationHintRecordStatus status;
    public string sourceId;
    public string sourceName;
    public string targetLabel;
    public string regionId;
    public string regionName;
    public string mapMarkerId;
    public string mapMarkerName;
    public string activityZoneId;
    public string sceneName;
    public string locationKey;
    public bool hasWorldPosition;
    public Vector3 worldPosition;
    public int day;
    public int absoluteHour;
    public int frame;
    public string timestamp;
    public bool blocked;
    public string failureMessage;
    public bool regionDiscovered;
    public bool mapMarkerDiscovered;
    public bool pokeNavEntryDiscovered;
    public bool socialPostUnlocked;
    public bool locationVisitRecorded;
    public int appliedDiscoveries;
    public int blockedDiscoveries;
    public int skippedDiscoveries;
    public int appliedChains;
    public int blockedChains;
    public int skippedChains;
    public List<string> tags;
    public List<string> messages;
}
