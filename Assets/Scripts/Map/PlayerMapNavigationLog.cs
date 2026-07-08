using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum MapNavigationRecordStatus {
    TargetSet,
    TargetRefreshed,
    TargetReached,
    TargetCleared
}

public class PlayerMapNavigationLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum operation records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 300;
    [Tooltip("Current active map navigation target. Empty means no active target.")]
    [SerializeField] MapNavigationTargetState activeTarget;
    [Tooltip("Runtime/save history of map navigation operations.")]
    [SerializeField] List<MapNavigationRecord> records = new List<MapNavigationRecord>();

    public MapNavigationTargetState ActiveTarget => activeTarget;
    public IReadOnlyList<MapNavigationRecord> Records => records;
    public bool HasActiveTarget => activeTarget != null && !string.IsNullOrWhiteSpace(activeTarget.markerId);
    public event Action<MapNavigationTargetState> OnTargetSet;
    public event Action<MapNavigationRecord> OnTargetReached;
    public event Action<MapNavigationRecord> OnTargetCleared;
    public event Action OnMapNavigationChanged;

    public bool SetTarget(MapMarkerProvider provider, string sourceId = null, bool discoverMarker = true) {
        if(provider == null) {
            return false;
        }

        var player = GetComponent<PlayerController>();
        var record = provider.BuildRecord(player);
        if(discoverMarker) {
            provider.Discover(player, sourceId);
        }

        return SetTarget(record, sourceId, discoverMarker: false);
    }

    public bool SetTarget(MapMarkerDefinition marker, string sourceId = null, bool discoverMarker = true) {
        if(marker == null) {
            return false;
        }

        var record = BuildRecordFromDefinition(marker, Vector3.zero, hasWorldPosition: false);
        return SetTarget(record, sourceId, discoverMarker);
    }

    public bool SetTarget(MapMarkerDefinition marker, Vector3 worldPosition, string sourceId = null, bool discoverMarker = true) {
        if(marker == null) {
            return false;
        }

        var record = BuildRecordFromDefinition(marker, worldPosition, hasWorldPosition: true);
        return SetTarget(record, sourceId, discoverMarker);
    }

    public bool SetTarget(MapMarkerRecord marker, string sourceId = null, bool discoverMarker = false) {
        if(marker == null || string.IsNullOrWhiteSpace(marker.id)) {
            return false;
        }

        var previous = activeTarget;
        activeTarget = new MapNavigationTargetState(marker, NormalizeSourceId(sourceId));

        if(discoverMarker) {
            GetComponent<PlayerMapLog>()?.DiscoverMarker(marker.id, marker.displayName, sourceId);
        }

        var status = previous != null && previous.markerId == activeTarget.markerId
            ? MapNavigationRecordStatus.TargetRefreshed
            : MapNavigationRecordStatus.TargetSet;
        RecordOperation(status, activeTarget, sourceId);
        OnTargetSet?.Invoke(activeTarget);
        OnMapNavigationChanged?.Invoke();
        PublishNavigationEvent(status, activeTarget, sourceId);
        return true;
    }

    public bool MarkTargetReached(string sourceId = null) {
        if(!HasActiveTarget) {
            return false;
        }

        var reachedTarget = activeTarget;
        activeTarget = null;
        var record = RecordOperation(MapNavigationRecordStatus.TargetReached, reachedTarget, sourceId);
        OnTargetReached?.Invoke(record);
        OnMapNavigationChanged?.Invoke();
        PublishNavigationEvent(MapNavigationRecordStatus.TargetReached, reachedTarget, sourceId);
        return true;
    }

    public bool ClearTarget(string sourceId = null) {
        if(!HasActiveTarget) {
            return false;
        }

        var clearedTarget = activeTarget;
        activeTarget = null;
        var record = RecordOperation(MapNavigationRecordStatus.TargetCleared, clearedTarget, sourceId);
        OnTargetCleared?.Invoke(record);
        OnMapNavigationChanged?.Invoke();
        PublishNavigationEvent(MapNavigationRecordStatus.TargetCleared, clearedTarget, sourceId);
        return true;
    }

    public bool IsTarget(MapMarkerDefinition marker) {
        return marker != null && IsTarget(marker.Id);
    }

    public bool IsTarget(string markerId) {
        return HasActiveTarget
            && !string.IsNullOrWhiteSpace(markerId)
            && string.Equals(activeTarget.markerId, markerId, StringComparison.OrdinalIgnoreCase);
    }

    public bool ActiveTargetHasTag(string tag) {
        return HasActiveTarget && activeTarget.HasTag(tag);
    }

    public float GetDistanceToTarget(Vector3 origin) {
        if(!HasActiveTarget || !activeTarget.hasWorldPosition) {
            return -1f;
        }

        return Vector3.Distance(origin, activeTarget.worldPosition);
    }

    public MapNavigationRecord GetLastRecord(string markerId = null, MapNavigationRecordStatus? status = null, bool includeCleared = true) {
        return records
            .Where(record => MatchesRecord(record, markerId, status, includeCleared))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.frame)
            .FirstOrDefault();
    }

    public int GetRecordCount(string markerId = null, MapNavigationRecordStatus? status = null, bool includeCleared = true) {
        return records.Count(record => MatchesRecord(record, markerId, status, includeCleared));
    }

    MapNavigationRecord RecordOperation(MapNavigationRecordStatus status, MapNavigationTargetState target, string sourceId) {
        if(target == null) {
            return null;
        }

        var record = new MapNavigationRecord {
            recordId = Guid.NewGuid().ToString("N"),
            markerId = target.markerId,
            markerName = target.markerName,
            category = target.category,
            status = status,
            sourceId = NormalizeSourceId(sourceId),
            regionId = target.regionId,
            sceneName = target.sceneName,
            hasWorldPosition = target.hasWorldPosition,
            worldPosition = target.worldPosition,
            distanceFromPlayer = TryGetPlayerDistance(target),
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            frame = Time.frameCount,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            tags = target.tags != null ? target.tags.ToList() : new List<string>()
        };

        records.Add(record);
        TrimHistory();
        return record;
    }

    bool MatchesRecord(MapNavigationRecord record, string markerId, MapNavigationRecordStatus? status, bool includeCleared) {
        return record != null
            && (includeCleared || record.status != MapNavigationRecordStatus.TargetCleared)
            && (string.IsNullOrWhiteSpace(markerId) || string.Equals(record.markerId, markerId, StringComparison.OrdinalIgnoreCase))
            && (!status.HasValue || record.status == status.Value);
    }

    MapMarkerRecord BuildRecordFromDefinition(MapMarkerDefinition marker, Vector3 worldPosition, bool hasWorldPosition) {
        return new MapMarkerRecord {
            id = marker.Id,
            displayName = marker.DisplayName,
            description = marker.Description,
            category = marker.Category,
            worldPosition = hasWorldPosition ? worldPosition : Vector3.zero,
            sceneName = string.Empty,
            icon = marker.Icon,
            color = marker.Color,
            priority = marker.Priority,
            showOnMinimap = marker.ShowOnMinimap,
            showOnWorldMap = marker.ShowOnWorldMap,
            important = marker.Important,
            discovered = GetComponent<PlayerMapLog>()?.HasDiscoveredMarker(marker) ?? false,
            hidden = GetComponent<PlayerMapLog>()?.IsMarkerHidden(marker.Id) ?? false,
            favorite = GetComponent<PlayerMapLog>()?.IsMarkerFavorite(marker.Id) ?? false,
            regionId = marker.Region != null ? marker.Region.Id : string.Empty,
            pokeNavEntryId = marker.PokeNavEntry != null ? marker.PokeNavEntry.Id : string.Empty,
            socialPostId = marker.SocialPost != null ? marker.SocialPost.Id : string.Empty,
            pokemonId = marker.RelatedPokemon != null ? marker.RelatedPokemon.name : string.Empty,
            source = GetType().Name,
            tags = marker.Tags != null ? marker.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() : new List<string>()
        };
    }

    float TryGetPlayerDistance(MapNavigationTargetState target) {
        var player = GetComponent<PlayerController>();
        if(player == null || target == null || !target.hasWorldPosition) {
            return -1f;
        }

        return Vector3.Distance(player.transform.position, target.worldPosition);
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

    void PublishNavigationEvent(MapNavigationRecordStatus status, MapNavigationTargetState target, string sourceId) {
        if(target == null) {
            return;
        }

        string phase = status.ToString();
        GameEventPublishing.PublishOptional(
            null,
            $"map.navigation.{phase}.{target.markerId}",
            $"{target.markerName} navigation {phase}.",
            GameEventCategory.Map,
            status == MapNavigationRecordStatus.TargetReached ? GameEventImportance.Success : GameEventImportance.Info,
            this,
            "PlayerMapNavigationLog",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("markerId", target.markerId),
            GameEventPublishing.Value("markerName", target.markerName),
            GameEventPublishing.Value("source", sourceId));
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "map-navigation" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerMapNavigationLogSaveData {
            activeTarget = activeTarget != null ? activeTarget.ToSaveData() : null,
            records = records.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerMapNavigationLogSaveData;
        activeTarget = saveData?.activeTarget != null ? new MapNavigationTargetState(saveData.activeTarget) : null;
        records = saveData?.records?.Where(record => record != null).Select(record => new MapNavigationRecord(record)).ToList()
            ?? new List<MapNavigationRecord>();
        TrimHistory();
        OnMapNavigationChanged?.Invoke();
    }
}

[Serializable]
public class MapNavigationTargetState {
    [Tooltip("Stable marker id for the active target.")]
    public string markerId;
    [Tooltip("Display name for the active target.")]
    public string markerName;
    [Tooltip("Description copied from the marker record.")]
    [TextArea]
    public string description;
    [Tooltip("Marker category used by filters and UI.")]
    public MapMarkerCategory category;
    [Tooltip("Source id that set this target.")]
    public string sourceId;
    [Tooltip("Related region id.")]
    public string regionId;
    [Tooltip("Scene name where this target exists.")]
    public string sceneName;
    [Tooltip("If enabled, World Position contains a useful target position.")]
    public bool hasWorldPosition;
    [Tooltip("World position of this navigation target.")]
    public Vector3 worldPosition;
    [Tooltip("Icon used by future map/minimap UI.")]
    public Sprite icon;
    [Tooltip("Tint color used by future map/minimap UI.")]
    public Color color = Color.white;
    [Tooltip("Higher priority targets can sort above lower priority targets.")]
    public int priority;
    [Tooltip("If enabled, this target can appear on minimap.")]
    public bool showOnMinimap;
    [Tooltip("If enabled, this target can appear on world map.")]
    public bool showOnWorldMap;
    [Tooltip("Whether this target was discovered when selected.")]
    public bool discovered;
    [Tooltip("Whether this target was favorited when selected.")]
    public bool favorite;
    [Tooltip("In-game day when this target was set.")]
    public int setDay;
    [Tooltip("Absolute in-game hour when this target was set.")]
    public int setAbsoluteHour;
    [Tooltip("Free-form tags copied from the marker record.")]
    public List<string> tags = new List<string>();

    public MapNavigationTargetState() {
    }

    public MapNavigationTargetState(MapMarkerRecord marker, string sourceId) {
        markerId = marker.id;
        markerName = string.IsNullOrWhiteSpace(marker.displayName) ? marker.id : marker.displayName;
        description = marker.description;
        category = marker.category;
        this.sourceId = sourceId;
        regionId = marker.regionId;
        sceneName = marker.sceneName;
        hasWorldPosition = !string.Equals(marker.source, nameof(PlayerMapNavigationLog), StringComparison.Ordinal)
            || marker.worldPosition != Vector3.zero;
        worldPosition = marker.worldPosition;
        icon = marker.icon;
        color = marker.color;
        priority = marker.priority;
        showOnMinimap = marker.showOnMinimap;
        showOnWorldMap = marker.showOnWorldMap;
        discovered = marker.discovered;
        favorite = marker.favorite;
        setDay = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
        setAbsoluteHour = TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
        tags = marker.tags != null ? marker.tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() : new List<string>();
    }

    public MapNavigationTargetState(MapNavigationTargetStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        markerId = saveData.markerId;
        markerName = saveData.markerName;
        description = saveData.description;
        category = saveData.category;
        sourceId = saveData.sourceId;
        regionId = saveData.regionId;
        sceneName = saveData.sceneName;
        hasWorldPosition = saveData.hasWorldPosition;
        worldPosition = saveData.worldPosition;
        color = saveData.color;
        priority = saveData.priority;
        showOnMinimap = saveData.showOnMinimap;
        showOnWorldMap = saveData.showOnWorldMap;
        discovered = saveData.discovered;
        favorite = saveData.favorite;
        setDay = saveData.setDay;
        setAbsoluteHour = saveData.setAbsoluteHour;
        tags = saveData.tags ?? new List<string>();
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public MapNavigationTargetStateSaveData ToSaveData() {
        return new MapNavigationTargetStateSaveData {
            markerId = markerId,
            markerName = markerName,
            description = description,
            category = category,
            sourceId = sourceId,
            regionId = regionId,
            sceneName = sceneName,
            hasWorldPosition = hasWorldPosition,
            worldPosition = worldPosition,
            color = color,
            priority = priority,
            showOnMinimap = showOnMinimap,
            showOnWorldMap = showOnWorldMap,
            discovered = discovered,
            favorite = favorite,
            setDay = setDay,
            setAbsoluteHour = setAbsoluteHour,
            tags = tags != null ? tags.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class MapNavigationRecord {
    [Tooltip("Unique runtime/save id for this operation.")]
    public string recordId;
    [Tooltip("Marker id affected by this operation.")]
    public string markerId;
    [Tooltip("Marker display name copied at the time of the operation.")]
    public string markerName;
    [Tooltip("Marker category copied at the time of the operation.")]
    public MapMarkerCategory category;
    [Tooltip("Operation status.")]
    public MapNavigationRecordStatus status;
    [Tooltip("Source id that requested this operation.")]
    public string sourceId;
    [Tooltip("Related region id.")]
    public string regionId;
    [Tooltip("Scene name copied from the target.")]
    public string sceneName;
    [Tooltip("If enabled, World Position contains a useful target position.")]
    public bool hasWorldPosition;
    [Tooltip("World position copied from the target.")]
    public Vector3 worldPosition;
    [Tooltip("Distance from player when this operation was recorded. -1 means unknown.")]
    public float distanceFromPlayer;
    [Tooltip("In-game day of the operation.")]
    public int day;
    [Tooltip("Absolute in-game hour of the operation.")]
    public int absoluteHour;
    [Tooltip("Unity frame when the operation was recorded.")]
    public int frame;
    [Tooltip("Local timestamp when the operation was recorded.")]
    public string timestamp;
    [Tooltip("Free-form tags copied from the target.")]
    public List<string> tags = new List<string>();

    public MapNavigationRecord() {
    }

    public MapNavigationRecord(MapNavigationRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        markerId = saveData.markerId;
        markerName = saveData.markerName;
        category = saveData.category;
        status = saveData.status;
        sourceId = saveData.sourceId;
        regionId = saveData.regionId;
        sceneName = saveData.sceneName;
        hasWorldPosition = saveData.hasWorldPosition;
        worldPosition = saveData.worldPosition;
        distanceFromPlayer = saveData.distanceFromPlayer;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
        frame = saveData.frame;
        timestamp = saveData.timestamp;
        tags = saveData.tags ?? new List<string>();
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public MapNavigationRecordSaveData ToSaveData() {
        return new MapNavigationRecordSaveData {
            recordId = recordId,
            markerId = markerId,
            markerName = markerName,
            category = category,
            status = status,
            sourceId = sourceId,
            regionId = regionId,
            sceneName = sceneName,
            hasWorldPosition = hasWorldPosition,
            worldPosition = worldPosition,
            distanceFromPlayer = distanceFromPlayer,
            day = day,
            absoluteHour = absoluteHour,
            frame = frame,
            timestamp = timestamp,
            tags = tags != null ? tags.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerMapNavigationLogSaveData {
    public MapNavigationTargetStateSaveData activeTarget;
    public List<MapNavigationRecordSaveData> records;
}

[Serializable]
public class MapNavigationTargetStateSaveData {
    public string markerId;
    public string markerName;
    public string description;
    public MapMarkerCategory category;
    public string sourceId;
    public string regionId;
    public string sceneName;
    public bool hasWorldPosition;
    public Vector3 worldPosition;
    public Color color;
    public int priority;
    public bool showOnMinimap;
    public bool showOnWorldMap;
    public bool discovered;
    public bool favorite;
    public int setDay;
    public int setAbsoluteHour;
    public List<string> tags;
}

[Serializable]
public class MapNavigationRecordSaveData {
    public string recordId;
    public string markerId;
    public string markerName;
    public MapMarkerCategory category;
    public MapNavigationRecordStatus status;
    public string sourceId;
    public string regionId;
    public string sceneName;
    public bool hasWorldPosition;
    public Vector3 worldPosition;
    public float distanceFromPlayer;
    public int day;
    public int absoluteHour;
    public int frame;
    public string timestamp;
    public List<string> tags;
}
