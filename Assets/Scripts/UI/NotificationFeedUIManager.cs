using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum NotificationFeedUIActionResultKind {
    None,
    Refreshed,
    Published,
    ReadChanged,
    AllReadChanged,
    Removed,
    Cleared,
    Blocked
}

public class NotificationFeedUIManager : MonoBehaviour {
    [Header("Feed")]
    [Tooltip("Notification feed shown by this UI manager. Empty uses NotificationFeed.i or creates one when allowed.")]
    [SerializeField] NotificationFeed feedOverride = null;
    [Tooltip("If enabled, NotificationFeed.Ensure is used when no feed exists.")]
    [SerializeField] bool createMissingFeed = true;

    [Header("Filters")]
    [Tooltip("If enabled, read notifications remain visible in the snapshot.")]
    [SerializeField] bool includeRead = true;
    [Tooltip("If enabled, only pinned notifications are shown.")]
    [SerializeField] bool showOnlyPinned;
    [Tooltip("If enabled, Kind Filter is applied.")]
    [SerializeField] bool useKindFilter;
    [Tooltip("Notification kind shown when Use Kind Filter is enabled.")]
    [SerializeField] NotificationKind kindFilter = NotificationKind.General;
    [Tooltip("If enabled, Channel Filter is applied.")]
    [SerializeField] bool useChannelFilter;
    [Tooltip("Notification channel shown when Use Channel Filter is enabled.")]
    [SerializeField] NotificationChannel channelFilter = NotificationChannel.Gameplay;
    [Tooltip("Minimum priority included in the snapshot.")]
    [SerializeField] NotificationPriority minimumPriority = NotificationPriority.Low;
    [Tooltip("Optional case-insensitive search text matched against title, message, source, kind and channel. Empty disables search filtering.")]
    [SerializeField] string searchText = string.Empty;

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, the UI snapshot refreshes whenever the feed changes.")]
    [SerializeField] bool refreshWhenFeedChanges = true;
    [Tooltip("If enabled, Refresh is called after every successful or blocked action.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("If enabled, pinned notifications are sorted above normal notifications.")]
    [SerializeField] bool sortPinnedFirst = true;
    [Tooltip("If enabled, newest notifications appear first.")]
    [SerializeField] bool newestFirst = true;
    [Tooltip("Maximum notification rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRows = 50;
    [Tooltip("Maximum key/value details copied per notification row. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxValueRowsPerNotification = 8;

    [Header("Manual Publish")]
    [Tooltip("Optional template used by Publish Template From Inspector or TryPublishTemplate when no argument is supplied.")]
    [SerializeField] NotificationDefinition templateToPublish = null;
    [Tooltip("Fallback title used by manual publish actions when no template is supplied.")]
    [SerializeField] string manualTitle = "Notification";
    [Tooltip("Fallback message used by manual publish actions when no template is supplied.")]
    [TextArea]
    [SerializeField] string manualMessage = "Notification message.";
    [Tooltip("Kind used by manual publish actions when no template is supplied.")]
    [SerializeField] NotificationKind manualKind = NotificationKind.General;
    [Tooltip("Priority used by manual publish actions when no template is supplied.")]
    [SerializeField] NotificationPriority manualPriority = NotificationPriority.Normal;
    [Tooltip("Channel used by manual publish actions when no template is supplied.")]
    [SerializeField] NotificationChannel manualChannel = NotificationChannel.Gameplay;
    [Tooltip("Source name saved on manual notifications.")]
    [SerializeField] string manualSource = "NotificationFeedUI";
    [Tooltip("If enabled, manual notifications are pinned.")]
    [SerializeField] bool manualPinned;

    [Header("Debug")]
    [Tooltip("If enabled, successful UI backend actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked UI backend actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    NotificationFeedUIScreenSnapshot currentSnapshot = new NotificationFeedUIScreenSnapshot();
    NotificationFeedUIActionResult lastResult = new NotificationFeedUIActionResult();
    NotificationFeed subscribedFeed;

    public NotificationFeedUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public NotificationFeedUIActionResult LastResult => lastResult;
    public NotificationFeed FeedOverride => feedOverride;
    public bool CreateMissingFeed => createMissingFeed;
    public NotificationDefinition TemplateToPublish => templateToPublish;
    public bool IncludeRead => includeRead;
    public bool ShowOnlyPinned => showOnlyPinned;
    public bool UseKindFilter => useKindFilter;
    public NotificationKind KindFilter => kindFilter;
    public bool UseChannelFilter => useChannelFilter;
    public NotificationChannel ChannelFilter => channelFilter;
    public NotificationPriority MinimumPriority => minimumPriority;
    public int MaxRows => Mathf.Max(0, maxRows);
    public event Action<NotificationFeedUIScreenSnapshot> OnSnapshotChanged;
    public event Action<NotificationFeedUIActionResult> OnActionResult;

    void OnEnable() {
        SubscribeToFeed();
    }

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    void OnDisable() {
        UnsubscribeFromFeed();
    }

    [ContextMenu("Refresh Notification Feed Snapshot")]
    public NotificationFeedUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    [ContextMenu("Publish Template Notification")]
    public void PublishTemplateFromContextMenu() {
        TryPublishTemplate(templateToPublish, null, out _);
    }

    [ContextMenu("Publish Manual Notification")]
    public void PublishManualFromContextMenu() {
        TryPublishManual(manualTitle, manualMessage, out _);
    }

    public NotificationFeedUIScreenSnapshot Refresh() {
        SubscribeToFeed();
        var feed = ResolveFeed();
        var rows = BuildRows(feed).ToList();
        var allEntries = feed != null ? feed.Entries.Where(entry => entry != null).ToList() : new List<NotificationRecord>();

        currentSnapshot = new NotificationFeedUIScreenSnapshot {
            hasFeed = feed != null,
            feedName = feed != null ? feed.name : string.Empty,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour(),
            includeRead = includeRead,
            showOnlyPinned = showOnlyPinned,
            useKindFilter = useKindFilter,
            kindFilter = kindFilter,
            useChannelFilter = useChannelFilter,
            channelFilter = channelFilter,
            minimumPriority = minimumPriority,
            searchText = searchText,
            totalCount = allEntries.Count,
            visibleCount = rows.Count,
            unreadCount = allEntries.Count(entry => !entry.read),
            readCount = allEntries.Count(entry => entry.read),
            pinnedCount = allEntries.Count(entry => entry.pinned),
            criticalCount = allEntries.Count(entry => entry.priority == NotificationPriority.Critical),
            rows = rows,
            lastResult = lastResult
        };

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool TryPublishTemplate(NotificationDefinition definition, string messageOverride, out string feedback) {
        var feed = ResolveFeed();
        if(feed == null) {
            return Block("No notification feed is available.", out feedback);
        }

        definition = definition != null ? definition : templateToPublish;
        if(definition == null) {
            return Block("No notification template was provided.", out feedback);
        }

        var record = feed.AddRecord(definition.CreateRecord(messageOverride, manualSource), definition.WriteToDebugLog);
        return record != null
            ? Succeed(NotificationFeedUIActionResultKind.Published, $"{record.title} published.", out feedback)
            : Block("Notification template could not be published.", out feedback);
    }

    public bool TryPublishManual(string title, string message, out string feedback) {
        var feed = ResolveFeed();
        if(feed == null) {
            return Block("No notification feed is available.", out feedback);
        }

        title = string.IsNullOrWhiteSpace(title) ? manualTitle : title;
        message = string.IsNullOrWhiteSpace(message) ? manualMessage : message;

        var record = NotificationRecord.Create(
            title,
            message,
            manualKind,
            manualPriority,
            manualChannel,
            manualSource,
            pinned: manualPinned);

        record = feed.AddRecord(record);
        return record != null
            ? Succeed(NotificationFeedUIActionResultKind.Published, $"{record.title} published.", out feedback)
            : Block("Manual notification could not be published.", out feedback);
    }

    public bool TryMarkRead(string notificationId, bool read, out string feedback) {
        var feed = ResolveFeed();
        if(feed == null) {
            return Block("No notification feed is available.", out feedback);
        }

        if(string.IsNullOrWhiteSpace(notificationId)) {
            return Block("No notification id was provided.", out feedback);
        }

        if(feed.MarkRead(notificationId, read)) {
            return Succeed(NotificationFeedUIActionResultKind.ReadChanged, read ? "Notification marked read." : "Notification marked unread.", out feedback);
        }

        return Block($"Notification '{notificationId}' could not be found.", out feedback);
    }

    public bool TryMarkAllRead(bool read, out string feedback) {
        var feed = ResolveFeed();
        if(feed == null) {
            return Block("No notification feed is available.", out feedback);
        }

        feed.MarkAllRead(read);
        return Succeed(NotificationFeedUIActionResultKind.AllReadChanged, read ? "All notifications marked read." : "All notifications marked unread.", out feedback);
    }

    public bool TryRemove(string notificationId, bool allowPinned, out string feedback) {
        var feed = ResolveFeed();
        if(feed == null) {
            return Block("No notification feed is available.", out feedback);
        }

        if(string.IsNullOrWhiteSpace(notificationId)) {
            return Block("No notification id was provided.", out feedback);
        }

        if(feed.Remove(notificationId, allowPinned)) {
            return Succeed(NotificationFeedUIActionResultKind.Removed, "Notification removed.", out feedback);
        }

        return Block($"Notification '{notificationId}' could not be removed.", out feedback);
    }

    public bool TryClear(bool includePinned, out string feedback) {
        var feed = ResolveFeed();
        if(feed == null) {
            return Block("No notification feed is available.", out feedback);
        }

        feed.Clear(includePinned);
        return Succeed(NotificationFeedUIActionResultKind.Cleared, includePinned ? "Notification feed cleared." : "Notification feed cleared except pinned entries.", out feedback);
    }

    public NotificationFeedRow FindRow(string notificationId) {
        return currentSnapshot?.rows?
            .FirstOrDefault(row => row != null && string.Equals(row.notificationId, notificationId, StringComparison.OrdinalIgnoreCase));
    }

    IEnumerable<NotificationFeedRow> BuildRows(NotificationFeed feed) {
        if(feed == null) {
            return Enumerable.Empty<NotificationFeedRow>();
        }

        IEnumerable<NotificationFeedIndexedRecord> query = feed.Entries
            .Select((record, index) => new NotificationFeedIndexedRecord(record, index))
            .Where(entry => entry.record != null)
            .Where(entry => includeRead || !entry.record.read)
            .Where(entry => !showOnlyPinned || entry.record.pinned)
            .Where(entry => !useKindFilter || entry.record.kind == kindFilter)
            .Where(entry => !useChannelFilter || entry.record.channel == channelFilter)
            .Where(entry => entry.record.priority >= minimumPriority)
            .Where(entry => MatchesSearch(entry.record));

        if(sortPinnedFirst) {
            var ordered = query.OrderByDescending(entry => entry.record.pinned);
            query = newestFirst
                ? ordered.ThenByDescending(entry => entry.index)
                : ordered.ThenBy(entry => entry.index);
        } else {
            query = newestFirst
                ? query.OrderByDescending(entry => entry.index)
                : query.OrderBy(entry => entry.index);
        }

        var rows = query.Select(entry => NotificationFeedRow.FromRecord(entry.record, entry.index, maxValueRowsPerNotification));
        return maxRows > 0 ? rows.Take(maxRows) : rows;
    }

    bool MatchesSearch(NotificationRecord record) {
        if(record == null || string.IsNullOrWhiteSpace(searchText)) {
            return true;
        }

        string query = searchText.Trim();
        return Contains(record.title, query)
            || Contains(record.message, query)
            || Contains(record.source, query)
            || Contains(record.sourceEventId, query)
            || Contains(record.kind.ToString(), query)
            || Contains(record.channel.ToString(), query)
            || Contains(record.priority.ToString(), query);
    }

    bool Contains(string value, string query) {
        return !string.IsNullOrWhiteSpace(value)
            && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    NotificationFeed ResolveFeed() {
        if(feedOverride != null) {
            return feedOverride;
        }

        if(NotificationFeed.i != null) {
            return NotificationFeed.i;
        }

        if(!createMissingFeed) {
            return FindAnyObjectByType<NotificationFeed>();
        }

        return NotificationFeed.Ensure();
    }

    void SubscribeToFeed() {
        if(!refreshWhenFeedChanges) {
            UnsubscribeFromFeed();
            return;
        }

        var feed = ResolveFeed();
        if(feed == subscribedFeed) {
            return;
        }

        UnsubscribeFromFeed();
        subscribedFeed = feed;
        if(subscribedFeed != null) {
            subscribedFeed.OnFeedChanged += HandleFeedChanged;
        }
    }

    void UnsubscribeFromFeed() {
        if(subscribedFeed != null) {
            subscribedFeed.OnFeedChanged -= HandleFeedChanged;
            subscribedFeed = null;
        }
    }

    void HandleFeedChanged() {
        if(isActiveAndEnabled) {
            Refresh();
        }
    }

    bool Succeed(NotificationFeedUIActionResultKind kind, string message, out string feedback) {
        feedback = message;
        SetLastResult(kind, true, message);
        if(logSuccessfulActions) {
            GameDebug.Success(message, GameDebugCategory.UI, this, "NotificationFeedUIManager");
        }
        return true;
    }

    bool Block(string message, out string feedback) {
        feedback = string.IsNullOrWhiteSpace(message) ? "Notification feed action was blocked." : message;
        SetLastResult(NotificationFeedUIActionResultKind.Blocked, false, feedback);
        if(logBlockedActions) {
            GameDebug.Warning(feedback, GameDebugCategory.UI, this, "NotificationFeedUIManager");
        }
        return false;
    }

    void SetLastResult(NotificationFeedUIActionResultKind kind, bool success, string message) {
        lastResult = new NotificationFeedUIActionResult {
            kind = kind,
            success = success,
            message = message,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour()
        };

        OnActionResult?.Invoke(lastResult);
        if(refreshAfterActions) {
            Refresh();
        }
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    class NotificationFeedIndexedRecord {
        public readonly NotificationRecord record;
        public readonly int index;

        public NotificationFeedIndexedRecord(NotificationRecord record, int index) {
            this.record = record;
            this.index = index;
        }
    }
}

[Serializable]
public class NotificationFeedUIScreenSnapshot {
    [Tooltip("If enabled, a notification feed was resolved for this snapshot.")]
    public bool hasFeed;
    [Tooltip("Resolved feed object name.")]
    public string feedName;
    [Tooltip("Current in-game day.")]
    public int day;
    [Tooltip("Current in-game hour.")]
    public int hour;
    [Tooltip("Current absolute in-game hour.")]
    public int absoluteHour;
    [Tooltip("Whether read notifications are visible in this snapshot.")]
    public bool includeRead;
    [Tooltip("Whether only pinned notifications are visible in this snapshot.")]
    public bool showOnlyPinned;
    [Tooltip("Whether Kind Filter was applied.")]
    public bool useKindFilter;
    [Tooltip("Kind filter copied from the UI manager.")]
    public NotificationKind kindFilter;
    [Tooltip("Whether Channel Filter was applied.")]
    public bool useChannelFilter;
    [Tooltip("Channel filter copied from the UI manager.")]
    public NotificationChannel channelFilter;
    [Tooltip("Minimum priority copied from the UI manager.")]
    public NotificationPriority minimumPriority;
    [Tooltip("Search text copied from the UI manager.")]
    public string searchText;
    [Tooltip("Total notification count in the feed before UI filters.")]
    public int totalCount;
    [Tooltip("Notification rows visible after UI filters.")]
    public int visibleCount;
    [Tooltip("Unread notification count before UI filters.")]
    public int unreadCount;
    [Tooltip("Read notification count before UI filters.")]
    public int readCount;
    [Tooltip("Pinned notification count before UI filters.")]
    public int pinnedCount;
    [Tooltip("Critical notification count before UI filters.")]
    public int criticalCount;
    [Tooltip("Visible notification rows.")]
    public List<NotificationFeedRow> rows = new List<NotificationFeedRow>();
    [Tooltip("Most recent UI backend action result.")]
    public NotificationFeedUIActionResult lastResult;
}

[Serializable]
public class NotificationFeedUIActionResult {
    [Tooltip("Kind of UI backend action that produced this result.")]
    public NotificationFeedUIActionResultKind kind;
    [Tooltip("If enabled, the action succeeded.")]
    public bool success;
    [Tooltip("Readable result, failure or feedback text.")]
    public string message;
    [Tooltip("In-game day when the result was produced.")]
    public int day;
    [Tooltip("In-game hour when the result was produced.")]
    public int hour;
    [Tooltip("Absolute in-game hour when the result was produced.")]
    public int absoluteHour;
}

[Serializable]
public class NotificationFeedRow {
    [Tooltip("Unique notification id used by read/remove actions.")]
    public string notificationId;
    [Tooltip("Original feed order index.")]
    public int feedIndex;
    [Tooltip("Short notification title.")]
    public string title;
    [Tooltip("Main notification text.")]
    public string message;
    [Tooltip("Visual/log group for this notification.")]
    public NotificationKind kind;
    [Tooltip("Importance level used by future UI styling.")]
    public NotificationPriority priority;
    [Tooltip("Notification channel used by future tabs/filters.")]
    public NotificationChannel channel;
    [Tooltip("If this was created from GameEventBus, the source event id is stored here.")]
    public string sourceEventId;
    [Tooltip("System or script that created this notification.")]
    public string source;
    [Tooltip("Scene name when this notification was created.")]
    public string sceneName;
    [Tooltip("Unity frame when this notification was created.")]
    public int frame;
    [Tooltip("Local timestamp when this notification was created.")]
    public string timestamp;
    [Tooltip("If enabled, the player/UI has marked this notification as read.")]
    public bool read;
    [Tooltip("If enabled, this notification is pinned.")]
    public bool pinned;
    [Tooltip("Copied key/value detail count.")]
    public int valueCount;
    [Tooltip("Key/value details copied for future detail panels.")]
    public List<NotificationFeedValueRow> values = new List<NotificationFeedValueRow>();
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static NotificationFeedRow FromRecord(NotificationRecord record, int feedIndex, int maxValueRows) {
        var values = record?.values != null
            ? record.values
                .Where(value => value != null)
                .Select(NotificationFeedValueRow.FromValue)
            : Enumerable.Empty<NotificationFeedValueRow>();

        if(maxValueRows > 0) {
            values = values.Take(maxValueRows);
        }

        return new NotificationFeedRow {
            notificationId = record != null ? record.id : string.Empty,
            feedIndex = feedIndex,
            title = record != null ? record.title : string.Empty,
            message = record != null ? record.message : string.Empty,
            kind = record != null ? record.kind : NotificationKind.General,
            priority = record != null ? record.priority : NotificationPriority.Normal,
            channel = record != null ? record.channel : NotificationChannel.Gameplay,
            sourceEventId = record != null ? record.sourceEventId : string.Empty,
            source = record != null ? record.source : string.Empty,
            sceneName = record != null ? record.sceneName : string.Empty,
            frame = record != null ? record.frame : 0,
            timestamp = record != null ? record.timestamp : string.Empty,
            read = record != null && record.read,
            pinned = record != null && record.pinned,
            valueCount = record?.values != null ? record.values.Count(value => value != null) : 0,
            values = values.ToList(),
            displayText = record != null ? $"{record.title}: {record.message}" : string.Empty
        };
    }
}

[Serializable]
public class NotificationFeedValueRow {
    [Tooltip("Detail key.")]
    public string key;
    [Tooltip("Detail value.")]
    public string value;

    public static NotificationFeedValueRow FromValue(GameEventValue source) {
        return new NotificationFeedValueRow {
            key = source != null ? source.key : string.Empty,
            value = source != null ? source.value : string.Empty
        };
    }
}
