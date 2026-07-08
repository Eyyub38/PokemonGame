using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerChronicleLog : MonoBehaviour, ISavable {
    [Header("Game Event Capture")]
    [Tooltip("If enabled, GameEventBus records can be copied into this player's chronicle.")]
    [SerializeField] bool captureGameEvents = true;
    [Tooltip("If enabled, existing GameEventBus history is replayed into this log when the component enables.")]
    [SerializeField] bool replayEventHistoryOnEnable = true;
    [Tooltip("If enabled, events hidden from notification/feed systems can be captured by the fallback capture rules.")]
    [SerializeField] bool includeHiddenGameEvents;
    [Tooltip("Only fallback-captured events at or above this importance become chronicle records.")]
    [SerializeField] GameEventImportance minimumEventImportance = GameEventImportance.Info;
    [Tooltip("Optional fallback category allow-list. Empty means all categories can be captured when no custom capture rule matches.")]
    [SerializeField] List<GameEventCategory> capturedCategories = new List<GameEventCategory>();
    [Tooltip("Scriptable capture rules checked before fallback event capture. The first matching rule decides output category/tags.")]
    [SerializeField] List<ChronicleCaptureRuleDefinition> captureRules = new List<ChronicleCaptureRuleDefinition>();
    [Tooltip("If disabled, generated Chronicle category events are ignored to avoid duplicate entries from manual ChronicleEntryDefinition.Apply calls.")]
    [SerializeField] bool captureGeneratedChronicleEvents;

    [Header("History")]
    [Tooltip("Maximum chronicle records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 1000;
    [Tooltip("If enabled, CaptureState includes chronicle history when this component is saved by SavableEntity.")]
    [SerializeField] bool saveHistory = true;
    [Tooltip("Runtime/save history of chronicle records.")]
    [SerializeField] List<PlayerChronicleRecord> records = new List<PlayerChronicleRecord>();

    [Header("Debug")]
    [Tooltip("If enabled, captured chronicle entries are written to GameDebugLogger.")]
    [SerializeField] bool writeCapturedEntriesToDebug;

    readonly HashSet<string> capturedEventRecordKeys = new HashSet<string>();

    public IReadOnlyList<PlayerChronicleRecord> Records => records;
    public event Action<PlayerChronicleRecord> OnChronicleRecordAdded;
    public event Action OnChronicleChanged;

    void OnEnable() {
        if(captureGameEvents) {
            GameEventBus.Subscribe(HandleGameEvent, replayEventHistoryOnEnable);
        }
    }

    void OnDisable() {
        GameEventBus.Unsubscribe(HandleGameEvent);
    }

    public bool CanRecord(ChronicleEntryDefinition entry, string sourceId, ConsequenceChainRepeatMode repeatMode, int cooldownHours, int maxRecordCount, out string failureMessage) {
        if(entry == null) {
            failureMessage = "No chronicle entry selected.";
            return false;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId);
        int totalSuccessfulRecords = GetEntryCount(entry, includeBlocked: false);
        if(maxRecordCount > 0 && totalSuccessfulRecords >= maxRecordCount) {
            failureMessage = $"{entry.DisplayName} has reached its maximum chronicle count.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && totalSuccessfulRecords > 0) {
            failureMessage = $"{entry.DisplayName} has already been recorded.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetEntryCount(entry, sourceId: normalizedSourceId, includeBlocked: false) > 0) {
            failureMessage = $"{entry.DisplayName} has already been recorded from this source.";
            return false;
        }

        var lastSourceRecord = GetLastEntry(entry, sourceId: normalizedSourceId, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastSourceRecord != null && lastSourceRecord.day == GetCurrentDay()) {
            failureMessage = $"{entry.DisplayName} can only be recorded once per day from this source.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastSourceRecord != null) {
            int elapsed = GetCurrentAbsoluteHour() - lastSourceRecord.absoluteHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{entry.DisplayName} will be recordable again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public PlayerChronicleRecord RecordEntry(ChronicleEntryDefinition entry, string sourceId, string sourceName, ChronicleEntryResult result) {
        if(entry == null) {
            return null;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId);
        var record = new PlayerChronicleRecord {
            recordId = Guid.NewGuid().ToString("N"),
            entryId = entry.Id,
            entryName = entry.DisplayName,
            title = result != null ? result.title : entry.Title,
            message = result != null ? result.message : entry.Message,
            category = entry.Category,
            importance = result != null ? result.importance : entry.Importance,
            sourceId = normalizedSourceId,
            sourceName = sourceName ?? string.Empty,
            regionId = entry.Region != null ? entry.Region.Id : string.Empty,
            regionName = entry.Region != null ? entry.Region.DisplayName : string.Empty,
            mapMarkerId = entry.MapMarker != null ? entry.MapMarker.Id : string.Empty,
            mapMarkerName = entry.MapMarker != null ? entry.MapMarker.DisplayName : string.Empty,
            relatedPokemonName = entry.RelatedPokemon != null ? entry.RelatedPokemon.Name : string.Empty,
            sceneName = result != null ? result.sceneName : entry.SceneName,
            locationKey = result != null ? result.locationKey : entry.LocationKey,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            frame = Time.frameCount,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            entryNumber = GetEntryCount(entry, sourceId: normalizedSourceId, includeBlocked: false) + 1,
            pinned = entry.PinnedByDefault,
            blocked = result != null && result.blocked,
            failureMessage = result != null ? result.failureMessage : null,
            tags = entry.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            messages = result != null && result.messages != null ? result.messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToList() : new List<string>(),
            appliedChains = result != null ? Mathf.Max(0, result.appliedChains) : 0,
            blockedChains = result != null ? Mathf.Max(0, result.blockedChains) : 0,
            skippedChains = result != null ? Mathf.Max(0, result.skippedChains) : 0
        };

        AddRecord(record);
        return record;
    }

    public PlayerChronicleRecord RecordFromEvent(GameEventRecord gameEvent, ChronicleCaptureRuleDefinition rule = null) {
        if(gameEvent == null) {
            return null;
        }

        string eventKey = GetEventRecordKey(gameEvent);
        if(capturedEventRecordKeys.Contains(eventKey)) {
            return null;
        }

        var values = gameEvent.values != null
            ? gameEvent.values.Where(value => value != null).Select(value => new GameEventValue(value.key, value.value)).ToList()
            : new List<GameEventValue>();
        var record = new PlayerChronicleRecord {
            recordId = Guid.NewGuid().ToString("N"),
            entryId = string.Empty,
            entryName = rule != null ? rule.DisplayName : gameEvent.displayName,
            title = rule != null ? rule.BuildTitle(gameEvent) : string.IsNullOrWhiteSpace(gameEvent.displayName) ? gameEvent.id : gameEvent.displayName,
            message = rule != null ? rule.BuildMessage(gameEvent) : string.IsNullOrWhiteSpace(gameEvent.message) ? gameEvent.displayName : gameEvent.message,
            category = rule != null ? rule.OutputCategory : ToChronicleCategory(gameEvent.category),
            importance = rule != null ? rule.OutputImportance(gameEvent) : gameEvent.importance,
            sourceId = NormalizeSourceId(gameEvent.source),
            sourceName = gameEvent.source ?? string.Empty,
            sourceEventId = gameEvent.id,
            sourceEventRecordKey = eventKey,
            sourceEventCategory = gameEvent.category,
            sceneName = gameEvent.sceneName,
            locationKey = gameEvent.GetValue("locationKey") ?? string.Empty,
            regionId = gameEvent.GetValue("region") ?? gameEvent.GetValue("regionId") ?? string.Empty,
            mapMarkerId = gameEvent.GetValue("mapMarker") ?? gameEvent.GetValue("mapMarkerId") ?? string.Empty,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            frame = gameEvent.frame,
            timestamp = gameEvent.timestamp,
            pinned = rule != null && rule.Pinned,
            tags = rule != null ? rule.OutputTags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() : new List<string>(),
            values = values
        };

        capturedEventRecordKeys.Add(eventKey);
        AddRecord(record);
        return record;
    }

    public int GetEntryCount(
        ChronicleEntryDefinition entry = null,
        string sourceId = null,
        ChronicleCategory? category = null,
        string tag = null,
        RegionInfoDefinition region = null,
        string sceneName = null,
        string locationKey = null,
        bool includeBlocked = false
    ) {
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        string regionId = region != null ? region.Id : null;
        return records.Count(record => Matches(record, entry, normalizedSourceId, category, tag, regionId, sceneName, locationKey, includeBlocked));
    }

    public bool HasEntry(ChronicleEntryDefinition entry = null, string sourceId = null, bool includeBlocked = false) {
        return GetEntryCount(entry, sourceId: sourceId, includeBlocked: includeBlocked) > 0;
    }

    public PlayerChronicleRecord GetLastEntry(
        ChronicleEntryDefinition entry = null,
        string sourceId = null,
        ChronicleCategory? category = null,
        string tag = null,
        RegionInfoDefinition region = null,
        string sceneName = null,
        string locationKey = null,
        bool includeBlocked = false
    ) {
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        string regionId = region != null ? region.Id : null;
        return records
            .Where(record => Matches(record, entry, normalizedSourceId, category, tag, regionId, sceneName, locationKey, includeBlocked))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.frame)
            .FirstOrDefault();
    }

    public int GetHoursSinceLastEntry(ChronicleEntryDefinition entry = null, string sourceId = null, bool includeBlocked = false) {
        var lastRecord = GetLastEntry(entry, sourceId: sourceId, includeBlocked: includeBlocked);
        return lastRecord != null ? Mathf.Max(0, GetCurrentAbsoluteHour() - lastRecord.absoluteHour) : -1;
    }

    public bool MarkRead(string recordId, bool read = true) {
        var record = records.FirstOrDefault(entry => entry != null && entry.recordId == recordId);
        if(record == null) {
            return false;
        }

        record.read = read;
        OnChronicleChanged?.Invoke();
        return true;
    }

    public bool SetPinned(string recordId, bool pinned = true) {
        var record = records.FirstOrDefault(entry => entry != null && entry.recordId == recordId);
        if(record == null) {
            return false;
        }

        record.pinned = pinned;
        OnChronicleChanged?.Invoke();
        return true;
    }

    public void Clear(bool includePinned = false) {
        records.RemoveAll(record => record == null || includePinned || !record.pinned);
        RebuildCapturedEventIndex();
        OnChronicleChanged?.Invoke();
    }

    bool Matches(PlayerChronicleRecord record, ChronicleEntryDefinition entry, string sourceId, ChronicleCategory? category, string tag, string regionId, string sceneName, string locationKey, bool includeBlocked) {
        return record != null
            && (includeBlocked || !record.blocked)
            && (entry == null || record.entryId == entry.Id)
            && (string.IsNullOrWhiteSpace(sourceId) || record.sourceId == sourceId)
            && (!category.HasValue || record.category == category.Value)
            && (string.IsNullOrWhiteSpace(tag) || record.HasTag(tag))
            && (string.IsNullOrWhiteSpace(regionId) || record.regionId == regionId)
            && (string.IsNullOrWhiteSpace(sceneName) || record.sceneName == sceneName)
            && (string.IsNullOrWhiteSpace(locationKey) || record.locationKey == locationKey);
    }

    void HandleGameEvent(GameEventRecord record) {
        if(!ShouldCapture(record, out var rule)) {
            return;
        }

        RecordFromEvent(record, rule);
    }

    bool ShouldCapture(GameEventRecord record, out ChronicleCaptureRuleDefinition matchedRule) {
        matchedRule = null;
        if(record == null) {
            return false;
        }

        if(record.category == GameEventCategory.Chronicle && !captureGeneratedChronicleEvents) {
            return false;
        }

        string key = GetEventRecordKey(record);
        if(capturedEventRecordKeys.Contains(key)) {
            return false;
        }

        matchedRule = captureRules != null ? captureRules.FirstOrDefault(rule => rule != null && rule.Matches(record)) : null;
        if(matchedRule != null) {
            return true;
        }

        if(!includeHiddenGameEvents && !record.showInFeed) {
            return false;
        }

        if(record.importance < minimumEventImportance) {
            return false;
        }

        return capturedCategories == null || capturedCategories.Count == 0 || capturedCategories.Contains(record.category);
    }

    void AddRecord(PlayerChronicleRecord record) {
        if(record == null) {
            return;
        }

        records.Add(record);
        TrimHistory();
        WriteDebug(record);
        OnChronicleRecordAdded?.Invoke(record);
        OnChronicleChanged?.Invoke();
    }

    void TrimHistory() {
        if(maxHistoryRecords <= 0 || records.Count <= maxHistoryRecords) {
            return;
        }

        records = records
            .Where(record => record != null)
            .OrderByDescending(record => record.pinned)
            .ThenByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.frame)
            .Take(maxHistoryRecords)
            .OrderBy(record => record.absoluteHour)
            .ThenBy(record => record.frame)
            .ToList();

        RebuildCapturedEventIndex();
    }

    void RebuildCapturedEventIndex() {
        capturedEventRecordKeys.Clear();
        foreach(var record in records) {
            if(record != null && !string.IsNullOrWhiteSpace(record.sourceEventRecordKey)) {
                capturedEventRecordKeys.Add(record.sourceEventRecordKey);
            }
        }
    }

    void WriteDebug(PlayerChronicleRecord record) {
        if(!writeCapturedEntriesToDebug || record == null) {
            return;
        }

        GameDebugLogger.Ensure().Record(
            ToDebugSeverity(record.importance),
            GameDebugCategory.Chronicle,
            $"{record.title}: {record.message}",
            this,
            "PlayerChronicleLog");
    }

    static GameDebugSeverity ToDebugSeverity(GameEventImportance importance) {
        return importance switch {
            GameEventImportance.Trace => GameDebugSeverity.Trace,
            GameEventImportance.Success => GameDebugSeverity.Success,
            GameEventImportance.Warning => GameDebugSeverity.Warning,
            GameEventImportance.Error => GameDebugSeverity.Error,
            _ => GameDebugSeverity.Info
        };
    }

    static ChronicleCategory ToChronicleCategory(GameEventCategory category) {
        return category switch {
            GameEventCategory.Battle => ChronicleCategory.Battle,
            GameEventCategory.Encounter => ChronicleCategory.Activity,
            GameEventCategory.Activity => ChronicleCategory.Activity,
            GameEventCategory.Farming => ChronicleCategory.Activity,
            GameEventCategory.Resource => ChronicleCategory.Activity,
            GameEventCategory.Crafting => ChronicleCategory.Activity,
            GameEventCategory.PokemonCare => ChronicleCategory.PokemonCare,
            GameEventCategory.Research => ChronicleCategory.Research,
            GameEventCategory.Quest => ChronicleCategory.Story,
            GameEventCategory.NPC => ChronicleCategory.Social,
            GameEventCategory.Companion => ChronicleCategory.Social,
            GameEventCategory.Reputation => ChronicleCategory.Social,
            GameEventCategory.Relationship => ChronicleCategory.Social,
            GameEventCategory.WorldEvent => ChronicleCategory.WorldEvent,
            GameEventCategory.Milestone => ChronicleCategory.Story,
            GameEventCategory.PokeNav => ChronicleCategory.Discovery,
            GameEventCategory.Map => ChronicleCategory.Discovery,
            GameEventCategory.Rumor => ChronicleCategory.Rumor,
            GameEventCategory.Calendar => ChronicleCategory.WorldEvent,
            GameEventCategory.BattleRule => ChronicleCategory.Battle,
            GameEventCategory.Contest => ChronicleCategory.Activity,
            GameEventCategory.Career => ChronicleCategory.Activity,
            GameEventCategory.Organization => ChronicleCategory.Social,
            GameEventCategory.Assignment => ChronicleCategory.Story,
            GameEventCategory.Law => ChronicleCategory.Law,
            GameEventCategory.Investigation => ChronicleCategory.Investigation,
            GameEventCategory.Risk => ChronicleCategory.Law,
            GameEventCategory.WorldDiscovery => ChronicleCategory.Discovery,
            GameEventCategory.LocationVisit => ChronicleCategory.Travel,
            GameEventCategory.Chronicle => ChronicleCategory.General,
            GameEventCategory.Navigation => ChronicleCategory.Travel,
            GameEventCategory.AreaProfile => ChronicleCategory.Travel,
            GameEventCategory.Debug => ChronicleCategory.Debug,
            _ => ChronicleCategory.General
        };
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "chronicle" : sourceId;
    }

    static string GetEventRecordKey(GameEventRecord gameEvent) {
        if(gameEvent == null) {
            return string.Empty;
        }

        return $"{gameEvent.id}|{gameEvent.frame}|{gameEvent.timestamp}";
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        if(!saveHistory) {
            return new PlayerChronicleLogSaveData();
        }

        TrimHistory();
        return new PlayerChronicleLogSaveData {
            records = records.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerChronicleLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => new PlayerChronicleRecord(record)).ToList()
            ?? new List<PlayerChronicleRecord>();
        TrimHistory();
        RebuildCapturedEventIndex();
        OnChronicleChanged?.Invoke();
    }
}

[Serializable]
public class PlayerChronicleRecord {
    [Tooltip("Unique runtime/save id for this chronicle record.")]
    public string recordId;
    [Tooltip("Saved chronicle entry definition id. Empty means this record came from a captured GameEventBus event.")]
    public string entryId;
    [Tooltip("Saved entry display name or capture rule name for fallback/debug output.")]
    public string entryName;
    [Tooltip("Short title for future journal/log UI.")]
    public string title;
    [Tooltip("Main chronicle text.")]
    public string message;
    [Tooltip("Chronicle category used by filters and requirements.")]
    public ChronicleCategory category;
    [Tooltip("Importance used by future UI styling and filters.")]
    public GameEventImportance importance;
    [Tooltip("Source id used by repeat rules and filters.")]
    public string sourceId;
    [Tooltip("Source display name for debug/fallback output.")]
    public string sourceName;
    [Tooltip("Source GameEvent id when this was captured from GameEventBus.")]
    public string sourceEventId;
    [Tooltip("Unique source event record key used to prevent duplicate capture during history replay.")]
    public string sourceEventRecordKey;
    [Tooltip("Source GameEvent category when this was captured from GameEventBus.")]
    public GameEventCategory sourceEventCategory;
    [Tooltip("Related region id.")]
    public string regionId;
    [Tooltip("Related region display name.")]
    public string regionName;
    [Tooltip("Related map marker id.")]
    public string mapMarkerId;
    [Tooltip("Related map marker display name.")]
    public string mapMarkerName;
    [Tooltip("Related Pokemon name for debug/future UI.")]
    public string relatedPokemonName;
    [Tooltip("Scene name connected to this record.")]
    public string sceneName;
    [Tooltip("Custom location key connected to this record.")]
    public string locationKey;
    [Tooltip("In-game day when this record was created.")]
    public int day;
    [Tooltip("Absolute in-game hour when this record was created.")]
    public int absoluteHour;
    [Tooltip("Unity frame when this record was created.")]
    public int frame;
    [Tooltip("Local timestamp when this record was created.")]
    public string timestamp;
    [Tooltip("Successful record number for this entry/source pair.")]
    [Min(0)]
    public int entryNumber;
    [Tooltip("Whether future UI has marked this record as read.")]
    public bool read;
    [Tooltip("Whether future UI should keep this record when clearing normal entries.")]
    public bool pinned;
    [Tooltip("If enabled, this record represents a blocked attempt.")]
    public bool blocked;
    [Tooltip("Failure reason saved for blocked attempts.")]
    public string failureMessage;
    [Tooltip("Free-form tags used by filters and requirements.")]
    public List<string> tags = new List<string>();
    [Tooltip("Extra debug messages saved with this record.")]
    public List<string> messages = new List<string>();
    [Tooltip("Extra key/value details copied from captured events or systems.")]
    public List<GameEventValue> values = new List<GameEventValue>();
    [Tooltip("Number of consequence chains applied successfully.")]
    [Min(0)]
    public int appliedChains;
    [Tooltip("Number of consequence chains blocked by their own rules.")]
    [Min(0)]
    public int blockedChains;
    [Tooltip("Number of null chain slots skipped.")]
    [Min(0)]
    public int skippedChains;

    public PlayerChronicleRecord() {
    }

    public PlayerChronicleRecord(PlayerChronicleRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        entryId = saveData.entryId;
        entryName = saveData.entryName;
        title = saveData.title;
        message = saveData.message;
        category = saveData.category;
        importance = saveData.importance;
        sourceId = saveData.sourceId;
        sourceName = saveData.sourceName;
        sourceEventId = saveData.sourceEventId;
        sourceEventRecordKey = saveData.sourceEventRecordKey;
        sourceEventCategory = saveData.sourceEventCategory;
        regionId = saveData.regionId;
        regionName = saveData.regionName;
        mapMarkerId = saveData.mapMarkerId;
        mapMarkerName = saveData.mapMarkerName;
        relatedPokemonName = saveData.relatedPokemonName;
        sceneName = saveData.sceneName;
        locationKey = saveData.locationKey;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
        frame = saveData.frame;
        timestamp = saveData.timestamp;
        entryNumber = Mathf.Max(0, saveData.entryNumber);
        read = saveData.read;
        pinned = saveData.pinned;
        blocked = saveData.blocked;
        failureMessage = saveData.failureMessage;
        tags = saveData.tags ?? new List<string>();
        messages = saveData.messages ?? new List<string>();
        values = saveData.values ?? new List<GameEventValue>();
        appliedChains = Mathf.Max(0, saveData.appliedChains);
        blockedChains = Mathf.Max(0, saveData.blockedChains);
        skippedChains = Mathf.Max(0, saveData.skippedChains);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public ChronicleEntryDefinition ResolveDefinition() {
        if(string.IsNullOrWhiteSpace(entryId)) {
            return null;
        }

        return Resources.LoadAll<ChronicleEntryDefinition>("").FirstOrDefault(entry => entry != null && entry.Id == entryId);
    }

    public PlayerChronicleRecordSaveData ToSaveData() {
        return new PlayerChronicleRecordSaveData {
            recordId = recordId,
            entryId = entryId,
            entryName = entryName,
            title = title,
            message = message,
            category = category,
            importance = importance,
            sourceId = sourceId,
            sourceName = sourceName,
            sourceEventId = sourceEventId,
            sourceEventRecordKey = sourceEventRecordKey,
            sourceEventCategory = sourceEventCategory,
            regionId = regionId,
            regionName = regionName,
            mapMarkerId = mapMarkerId,
            mapMarkerName = mapMarkerName,
            relatedPokemonName = relatedPokemonName,
            sceneName = sceneName,
            locationKey = locationKey,
            day = day,
            absoluteHour = absoluteHour,
            frame = frame,
            timestamp = timestamp,
            entryNumber = entryNumber,
            read = read,
            pinned = pinned,
            blocked = blocked,
            failureMessage = failureMessage,
            tags = tags != null ? tags.ToList() : new List<string>(),
            messages = messages != null ? messages.ToList() : new List<string>(),
            values = values != null ? values.Where(value => value != null).Select(value => new GameEventValue(value.key, value.value)).ToList() : new List<GameEventValue>(),
            appliedChains = appliedChains,
            blockedChains = blockedChains,
            skippedChains = skippedChains
        };
    }
}

[Serializable]
public class PlayerChronicleLogSaveData {
    public List<PlayerChronicleRecordSaveData> records = new List<PlayerChronicleRecordSaveData>();
}

[Serializable]
public class PlayerChronicleRecordSaveData {
    public string recordId;
    public string entryId;
    public string entryName;
    public string title;
    public string message;
    public ChronicleCategory category;
    public GameEventImportance importance;
    public string sourceId;
    public string sourceName;
    public string sourceEventId;
    public string sourceEventRecordKey;
    public GameEventCategory sourceEventCategory;
    public string regionId;
    public string regionName;
    public string mapMarkerId;
    public string mapMarkerName;
    public string relatedPokemonName;
    public string sceneName;
    public string locationKey;
    public int day;
    public int absoluteHour;
    public int frame;
    public string timestamp;
    public int entryNumber;
    public bool read;
    public bool pinned;
    public bool blocked;
    public string failureMessage;
    public List<string> tags;
    public List<string> messages;
    public List<GameEventValue> values;
    public int appliedChains;
    public int blockedChains;
    public int skippedChains;
}
