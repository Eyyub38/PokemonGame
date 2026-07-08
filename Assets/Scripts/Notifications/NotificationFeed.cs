using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NotificationFeed : MonoBehaviour, ISavable {
    [Header("Lifetime")]
    [Tooltip("If enabled, this feed survives scene loads.")]
    [SerializeField] bool dontDestroyOnLoad = true;

    [Header("Game Event Capture")]
    [Tooltip("If enabled, GameEventBus records marked Show In Feed are converted into notification records.")]
    [SerializeField] bool captureGameEvents = true;
    [Tooltip("If enabled, existing GameEventBus history is replayed into this feed on enable.")]
    [SerializeField] bool replayEventHistoryOnEnable = true;
    [Tooltip("If enabled, GameEventBus records hidden from feed can still be captured.")]
    [SerializeField] bool includeHiddenGameEvents;
    [Tooltip("Only game events at or above this importance become notifications.")]
    [SerializeField] GameEventImportance minimumEventImportance = GameEventImportance.Info;
    [Tooltip("Optional category allow-list. Empty means all categories can be captured.")]
    [SerializeField] List<GameEventCategory> capturedCategories = new List<GameEventCategory>();

    [Header("History")]
    [Tooltip("Maximum number of notification records kept in memory.")]
    [Min(1)]
    [SerializeField] int maxEntries = 200;
    [Tooltip("If enabled, CaptureState includes notification history when this component is saved by SavableEntity.")]
    [SerializeField] bool saveHistory;
    [Tooltip("Runtime notification records. Future UI can read this list.")]
    [SerializeField] List<NotificationRecord> entries = new List<NotificationRecord>();

    [Header("Debug")]
    [Tooltip("If enabled, every notification is mirrored into GameDebugLogger.")]
    [SerializeField] bool writeAllNotificationsToDebug;

    readonly HashSet<string> capturedEventRecordIds = new HashSet<string>();

    public static NotificationFeed i { get; private set; }
    public static event Action<NotificationRecord> GlobalNotificationAdded;

    public IReadOnlyList<NotificationRecord> Entries => entries;
    public int UnreadCount => entries.Count(e => e != null && !e.read);

    public event Action<NotificationRecord> OnNotificationAdded;
    public event Action OnFeedChanged;

    void Awake() {
        if(i != null && i != this) {
            Destroy(gameObject);
            return;
        }

        i = this;
        if(dontDestroyOnLoad) {
            DontDestroyOnLoad(gameObject);
        }
    }

    void OnEnable() {
        if(captureGameEvents) {
            GameEventBus.Subscribe(HandleGameEvent, replayEventHistoryOnEnable);
        }
    }

    void OnDisable() {
        GameEventBus.Unsubscribe(HandleGameEvent);
    }

    public static NotificationFeed Ensure() {
        if(i != null) {
            return i;
        }

        var existing = FindAnyObjectByType<NotificationFeed>();
        if(existing != null) {
            i = existing;
            return i;
        }

        var go = new GameObject("NotificationFeed");
        return go.AddComponent<NotificationFeed>();
    }

    public static NotificationRecord Publish(
        NotificationDefinition definition,
        string messageOverride = null,
        string source = null,
        IEnumerable<GameEventValue> values = null
    ) {
        if(definition == null) {
            return Publish(
                "Notification",
                messageOverride,
                NotificationKind.General,
                NotificationPriority.Normal,
                NotificationChannel.Gameplay,
                source,
                values: values);
        }

        var record = definition.CreateRecord(messageOverride, source, values);
        Ensure().Add(record, definition.WriteToDebugLog);
        return record;
    }

    public static NotificationRecord Publish(
        string title,
        string message,
        NotificationKind kind = NotificationKind.General,
        NotificationPriority priority = NotificationPriority.Normal,
        NotificationChannel channel = NotificationChannel.Gameplay,
        string source = null,
        string sourceEventId = null,
        string sourceEventRecordKey = null,
        bool pinned = false,
        IEnumerable<GameEventValue> values = null,
        bool writeToDebugLog = false
    ) {
        var record = NotificationRecord.Create(title, message, kind, priority, channel, sourceEventId, sourceEventRecordKey, source, pinned, values);
        Ensure().Add(record, writeToDebugLog);
        return record;
    }

    public IReadOnlyList<NotificationRecord> GetEntries(
        NotificationKind? kind = null,
        NotificationChannel? channel = null,
        NotificationPriority? minimumPriority = null,
        bool includeRead = true
    ) {
        return entries
            .Where(e => e != null)
            .Where(e => includeRead || !e.read)
            .Where(e => !kind.HasValue || e.kind == kind.Value)
            .Where(e => !channel.HasValue || e.channel == channel.Value)
            .Where(e => !minimumPriority.HasValue || e.priority >= minimumPriority.Value)
            .ToList();
    }

    public NotificationRecord AddRecord(NotificationRecord record, bool writeToDebugLog = false) {
        Add(record, writeToDebugLog);
        return record;
    }

    public bool MarkRead(string notificationId, bool read = true) {
        var entry = entries.FirstOrDefault(e => e != null && e.id == notificationId);
        if(entry == null) {
            return false;
        }

        entry.read = read;
        OnFeedChanged?.Invoke();
        return true;
    }

    public void MarkAllRead(bool read = true) {
        foreach(var entry in entries) {
            if(entry != null) {
                entry.read = read;
            }
        }

        OnFeedChanged?.Invoke();
    }

    public void Clear(bool includePinned = false) {
        entries.RemoveAll(e => e == null || includePinned || !e.pinned);
        RebuildCapturedEventIndex();
        OnFeedChanged?.Invoke();
    }

    public bool Remove(string notificationId, bool allowPinned = false) {
        int removed = entries.RemoveAll(e => e != null && e.id == notificationId && (allowPinned || !e.pinned));
        if(removed <= 0) {
            return false;
        }

        RebuildCapturedEventIndex();
        OnFeedChanged?.Invoke();
        return true;
    }

    public object CaptureState() {
        if(!saveHistory) {
            return new NotificationFeedSaveData();
        }

        return new NotificationFeedSaveData() {
            entries = entries
                .Where(e => e != null)
                .Select(CloneRecord)
                .ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as NotificationFeedSaveData;
        entries = saveData?.entries ?? new List<NotificationRecord>();
        RebuildCapturedEventIndex();
        OnFeedChanged?.Invoke();
    }

    void HandleGameEvent(GameEventRecord gameEvent) {
        if(!ShouldCapture(gameEvent)) {
            return;
        }

        capturedEventRecordIds.Add(GetEventRecordKey(gameEvent));
        Add(CreateFromGameEvent(gameEvent), writeToDebugLog: false);
    }

    bool ShouldCapture(GameEventRecord gameEvent) {
        if(gameEvent == null) {
            return false;
        }

        string key = GetEventRecordKey(gameEvent);
        if(capturedEventRecordIds.Contains(key)) {
            return false;
        }

        if(!includeHiddenGameEvents && !gameEvent.showInFeed) {
            return false;
        }

        if(gameEvent.importance < minimumEventImportance) {
            return false;
        }

        return capturedCategories.Count == 0 || capturedCategories.Contains(gameEvent.category);
    }

    NotificationRecord CreateFromGameEvent(GameEventRecord gameEvent) {
        var values = new List<GameEventValue>();
        if(gameEvent.values != null) {
            values.AddRange(gameEvent.values);
        }

        values.Add(new GameEventValue("eventId", gameEvent.id));
        values.Add(new GameEventValue("eventCategory", gameEvent.category.ToString()));
        values.Add(new GameEventValue("eventImportance", gameEvent.importance.ToString()));

        return NotificationRecord.Create(
            string.IsNullOrWhiteSpace(gameEvent.displayName) ? gameEvent.id : gameEvent.displayName,
            string.IsNullOrWhiteSpace(gameEvent.message) ? gameEvent.displayName : gameEvent.message,
            ToNotificationKind(gameEvent.category),
            ToNotificationPriority(gameEvent.importance),
            ToNotificationChannel(gameEvent),
            gameEvent.id,
            GetEventRecordKey(gameEvent),
            gameEvent.source,
            pinned: false,
            values: values);
    }

    void Add(NotificationRecord record, bool writeToDebugLog) {
        if(record == null) {
            return;
        }

        entries.Add(record);
        TrimHistory();

        if(writeAllNotificationsToDebug || writeToDebugLog) {
            WriteToDebug(record);
        }

        OnNotificationAdded?.Invoke(record);
        GlobalNotificationAdded?.Invoke(record);
        OnFeedChanged?.Invoke();
    }

    void TrimHistory() {
        int limit = Mathf.Max(1, maxEntries);
        while(entries.Count > limit) {
            int index = entries.FindIndex(e => e == null || !e.pinned);
            if(index < 0) {
                index = 0;
            }
            entries.RemoveAt(index);
        }

        RebuildCapturedEventIndex();
    }

    void RebuildCapturedEventIndex() {
        capturedEventRecordIds.Clear();
        foreach(var entry in entries) {
            if(entry != null && !string.IsNullOrWhiteSpace(entry.sourceEventRecordKey)) {
                capturedEventRecordIds.Add(entry.sourceEventRecordKey);
            }
        }
    }

    string GetEventRecordKey(GameEventRecord gameEvent) {
        if(gameEvent == null) {
            return string.Empty;
        }

        return $"{gameEvent.id}|{gameEvent.frame}|{gameEvent.timestamp}";
    }

    NotificationKind ToNotificationKind(GameEventCategory category) {
        return category switch {
            GameEventCategory.Battle => NotificationKind.Battle,
            GameEventCategory.Encounter => NotificationKind.Activity,
            GameEventCategory.Activity => NotificationKind.Activity,
            GameEventCategory.Farming => NotificationKind.Activity,
            GameEventCategory.Resource => NotificationKind.Activity,
            GameEventCategory.Crafting => NotificationKind.Item,
            GameEventCategory.PokemonCare => NotificationKind.Activity,
            GameEventCategory.Research => NotificationKind.Activity,
            GameEventCategory.Quest => NotificationKind.Quest,
            GameEventCategory.Inventory => NotificationKind.Item,
            GameEventCategory.Shop => NotificationKind.Item,
            GameEventCategory.Job => NotificationKind.Quest,
            GameEventCategory.Dialogue => NotificationKind.Dialogue,
            GameEventCategory.NPC => NotificationKind.NPC,
            GameEventCategory.Companion => NotificationKind.Companion,
            GameEventCategory.Survival => NotificationKind.Survival,
            GameEventCategory.RPG => NotificationKind.Progression,
            GameEventCategory.Reputation => NotificationKind.Social,
            GameEventCategory.Relationship => NotificationKind.Social,
            GameEventCategory.WorldEvent => NotificationKind.World,
            GameEventCategory.Milestone => NotificationKind.Progression,
            GameEventCategory.PokeNav => NotificationKind.Social,
            GameEventCategory.Map => NotificationKind.World,
            GameEventCategory.Rumor => NotificationKind.Social,
            GameEventCategory.Calendar => NotificationKind.World,
            GameEventCategory.BattleRule => NotificationKind.Battle,
            GameEventCategory.Contest => NotificationKind.Activity,
            GameEventCategory.Career => NotificationKind.Progression,
            GameEventCategory.Organization => NotificationKind.Social,
            GameEventCategory.Assignment => NotificationKind.Quest,
            GameEventCategory.Access => NotificationKind.System,
            GameEventCategory.Law => NotificationKind.System,
            GameEventCategory.Investigation => NotificationKind.Quest,
            GameEventCategory.WorldDiscovery => NotificationKind.World,
            GameEventCategory.LocationVisit => NotificationKind.World,
            GameEventCategory.Chronicle => NotificationKind.World,
            GameEventCategory.Navigation => NotificationKind.World,
            GameEventCategory.AreaProfile => NotificationKind.World,
            GameEventCategory.Debug => NotificationKind.Debug,
            GameEventCategory.System => NotificationKind.System,
            _ => NotificationKind.General
        };
    }

    NotificationPriority ToNotificationPriority(GameEventImportance importance) {
        return importance switch {
            GameEventImportance.Trace => NotificationPriority.Low,
            GameEventImportance.Success => NotificationPriority.Normal,
            GameEventImportance.Warning => NotificationPriority.High,
            GameEventImportance.Error => NotificationPriority.Critical,
            _ => NotificationPriority.Normal
        };
    }

    NotificationChannel ToNotificationChannel(GameEventRecord gameEvent) {
        if(gameEvent.category == GameEventCategory.Battle) {
            return NotificationChannel.Combat;
        }

        if(gameEvent.category == GameEventCategory.Quest
            || gameEvent.category == GameEventCategory.Dialogue
            || gameEvent.category == GameEventCategory.NPC
            || gameEvent.category == GameEventCategory.WorldEvent
            || gameEvent.category == GameEventCategory.Milestone
            || gameEvent.category == GameEventCategory.PokeNav
            || gameEvent.category == GameEventCategory.Map
            || gameEvent.category == GameEventCategory.Rumor
            || gameEvent.category == GameEventCategory.Calendar
            || gameEvent.category == GameEventCategory.WorldDiscovery
            || gameEvent.category == GameEventCategory.LocationVisit
            || gameEvent.category == GameEventCategory.Chronicle
            || gameEvent.category == GameEventCategory.Navigation
            || gameEvent.category == GameEventCategory.AreaProfile) {
            return NotificationChannel.Story;
        }

        if(gameEvent.category == GameEventCategory.BattleRule) {
            return NotificationChannel.Combat;
        }

        if(gameEvent.category == GameEventCategory.Contest) {
            return NotificationChannel.Gameplay;
        }

        if(gameEvent.category == GameEventCategory.Career) {
            return NotificationChannel.Gameplay;
        }

        if(gameEvent.category == GameEventCategory.Organization) {
            return NotificationChannel.Story;
        }

        if(gameEvent.category == GameEventCategory.Assignment) {
            return NotificationChannel.Story;
        }

        if(gameEvent.category == GameEventCategory.Access) {
            return NotificationChannel.System;
        }

        if(gameEvent.category == GameEventCategory.Law) {
            return NotificationChannel.Story;
        }

        if(gameEvent.category == GameEventCategory.Investigation) {
            return NotificationChannel.Story;
        }

        if(gameEvent.category == GameEventCategory.Debug || gameEvent.importance == GameEventImportance.Error) {
            return NotificationChannel.Debug;
        }

        if(gameEvent.category == GameEventCategory.System || gameEvent.category == GameEventCategory.SaveLoad) {
            return NotificationChannel.System;
        }

        return NotificationChannel.Gameplay;
    }

    void WriteToDebug(NotificationRecord record) {
        GameDebugLogger.Ensure().Record(
            ToDebugSeverity(record.priority),
            ToDebugCategory(record.kind),
            record.message,
            this,
            string.IsNullOrWhiteSpace(record.source) ? "NotificationFeed" : record.source);
    }

    GameDebugSeverity ToDebugSeverity(NotificationPriority priority) {
        return priority switch {
            NotificationPriority.Low => GameDebugSeverity.Trace,
            NotificationPriority.High => GameDebugSeverity.Warning,
            NotificationPriority.Critical => GameDebugSeverity.Error,
            _ => GameDebugSeverity.Info
        };
    }

    GameDebugCategory ToDebugCategory(NotificationKind kind) {
        return kind switch {
            NotificationKind.Battle => GameDebugCategory.Battle,
            NotificationKind.Activity => GameDebugCategory.Activity,
            NotificationKind.Item => GameDebugCategory.Inventory,
            NotificationKind.Quest => GameDebugCategory.Quest,
            NotificationKind.Survival => GameDebugCategory.General,
            NotificationKind.Debug => GameDebugCategory.Validation,
            _ => GameDebugCategory.General
        };
    }

    NotificationRecord CloneRecord(NotificationRecord record) {
        return new NotificationRecord() {
            id = record.id,
            title = record.title,
            message = record.message,
            kind = record.kind,
            priority = record.priority,
            channel = record.channel,
            sourceEventId = record.sourceEventId,
            sourceEventRecordKey = record.sourceEventRecordKey,
            source = record.source,
            sceneName = record.sceneName,
            frame = record.frame,
            timestamp = record.timestamp,
            read = record.read,
            pinned = record.pinned,
            values = record.values != null ? new List<GameEventValue>(record.values) : new List<GameEventValue>()
        };
    }
}

[Serializable]
public class NotificationFeedSaveData {
    public List<NotificationRecord> entries = new List<NotificationRecord>();
}
