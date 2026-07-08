using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPokeNavFeedLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save PokeNav feed item records unlocked for this player.")]
    [SerializeField] List<PokeNavFeedItemRecord> feedItems = new List<PokeNavFeedItemRecord>();

    public IReadOnlyList<PokeNavFeedItemRecord> FeedItems => feedItems;
    public event Action<PokeNavFeedItemDefinition, PokeNavFeedItemRecord> OnFeedItemUnlocked;
    public event Action<PokeNavFeedItemDefinition> OnFeedItemRead;
    public event Action OnPokeNavFeedChanged;

    public bool CanUnlock(PokeNavFeedItemDefinition item, out string failureMessage) {
        if(item == null) {
            failureMessage = "A PokeNav feed item is required.";
            return false;
        }

        var record = GetRecord(item);
        if(item.RepeatMode == PokeNavFeedRepeatMode.OnceEver && record != null) {
            failureMessage = $"{item.Title} was already unlocked.";
            return false;
        }

        if(item.RepeatMode == PokeNavFeedRepeatMode.RefreshExistingOnly && record == null) {
            failureMessage = $"{item.Title} cannot be refreshed because it is not unlocked.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public PokeNavFeedItemRecord RecordUnlock(PokeNavFeedItemDefinition item, string sourceId = null) {
        if(item == null) {
            return null;
        }

        var record = GetRecord(item);
        if(record == null) {
            record = new PokeNavFeedItemRecord {
                itemId = item.Id,
                title = item.Title,
                feedType = item.FeedType.ToString(),
                sourceName = item.SourceName,
                pinned = item.PinnedByDefault
            };
            feedItems.Add(record);
        }

        record.title = item.Title;
        record.feedType = item.FeedType.ToString();
        record.sourceName = item.SourceName;
        record.priority = item.Priority.ToString();
        record.unlockCount++;
        record.lastSourceId = sourceId;
        record.lastUnlockedDay = GetCurrentDay();
        record.lastUnlockedTotalHour = GetCurrentTotalHour();

        if(item.MarkUnreadOnUnlock) {
            record.read = false;
            record.dismissed = false;
        }

        if(item.ExpiresAfterUnlock && (record.expiresTotalHour < 0 || item.RefreshExpirationOnUnlock)) {
            record.expiresTotalHour = GetCurrentTotalHour() + item.DefaultDurationHours;
        } else if(!item.ExpiresAfterUnlock) {
            record.expiresTotalHour = -1;
        }

        OnFeedItemUnlocked?.Invoke(item, record);
        OnPokeNavFeedChanged?.Invoke();
        return record;
    }

    public bool HasUnlockedItem(PokeNavFeedItemDefinition item) {
        return GetRecord(item) != null;
    }

    public bool HasActiveItem(PokeNavFeedItemDefinition item, out string failureMessage) {
        var record = GetRecord(item);
        if(record == null) {
            failureMessage = $"{item?.Title ?? "Feed item"} is not unlocked.";
            return false;
        }

        return record.IsActive(GetCurrentTotalHour(), out failureMessage);
    }

    public bool IsActiveOrUnowned(PokeNavFeedItemDefinition item, out string failureMessage) {
        var record = GetRecord(item);
        if(record == null) {
            failureMessage = null;
            return true;
        }

        return record.IsActive(GetCurrentTotalHour(), out failureMessage);
    }

    public bool IsRead(PokeNavFeedItemDefinition item) {
        return GetRecord(item)?.read ?? false;
    }

    public bool IsDismissed(PokeNavFeedItemDefinition item) {
        return GetRecord(item)?.dismissed ?? false;
    }

    public bool IsPinned(PokeNavFeedItemDefinition item) {
        var record = GetRecord(item);
        return record != null ? record.pinned : item != null && item.PinnedByDefault;
    }

    public bool MarkRead(PokeNavFeedItemDefinition item, bool read = true) {
        var record = GetRecord(item);
        if(record == null) {
            return false;
        }

        if(record.read == read) {
            return false;
        }

        record.read = read;
        if(read) {
            record.lastReadTotalHour = GetCurrentTotalHour();
            OnFeedItemRead?.Invoke(item);
        }

        OnPokeNavFeedChanged?.Invoke();
        return true;
    }

    public bool SetDismissed(PokeNavFeedItemDefinition item, bool dismissed) {
        var record = GetRecord(item);
        if(record == null || record.dismissed == dismissed) {
            return false;
        }

        record.dismissed = dismissed;
        OnPokeNavFeedChanged?.Invoke();
        return true;
    }

    public bool SetPinned(PokeNavFeedItemDefinition item, bool pinned) {
        var record = GetRecord(item);
        if(record == null || record.pinned == pinned) {
            return false;
        }

        record.pinned = pinned;
        OnPokeNavFeedChanged?.Invoke();
        return true;
    }

    public int GetUnreadCount(string requiredTag = null) {
        return GetAvailableFeedItems(includeRead: false, includeDismissed: false)
            .Count(item => string.IsNullOrWhiteSpace(requiredTag) || item.HasTag(requiredTag));
    }

    public List<PokeNavFeedItemDefinition> GetAvailableFeedItems(bool includeRead = true, bool includeDismissed = false, IEnumerable<PokeNavFeedItemDefinition> feedPool = null) {
        var player = GetComponent<PlayerController>();
        var pool = feedPool ?? Resources.LoadAll<PokeNavFeedItemDefinition>("");

        return pool
            .Where(item => item != null && item.CanShow(player, this, out _))
            .Where(item => includeRead || !IsRead(item))
            .Where(item => includeDismissed || !IsDismissed(item))
            .OrderByDescending(IsPinned)
            .ThenByDescending(item => item.Priority)
            .ThenByDescending(item => GetRecord(item)?.lastUnlockedTotalHour ?? -1)
            .ThenBy(item => item.Title)
            .ToList();
    }

    PokeNavFeedItemRecord GetRecord(PokeNavFeedItemDefinition item) {
        string itemId = item != null ? item.Id : string.Empty;
        return string.IsNullOrWhiteSpace(itemId)
            ? null
            : feedItems.FirstOrDefault(record => record != null && record.itemId == itemId);
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerPokeNavFeedLogSaveData {
            feedItems = feedItems.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerPokeNavFeedLogSaveData;
        feedItems = saveData?.feedItems?.Where(record => record != null).Select(record => record.Clone()).ToList() ?? new List<PokeNavFeedItemRecord>();
        OnPokeNavFeedChanged?.Invoke();
    }
}

[Serializable]
public class PokeNavFeedItemRecord {
    [Tooltip("Saved feed item id.")]
    public string itemId;
    [Tooltip("Saved feed item title for fallback/debug output.")]
    public string title;
    [Tooltip("Saved feed item type.")]
    public string feedType;
    [Tooltip("Saved source/channel name.")]
    public string sourceName;
    [Tooltip("Saved notification priority string.")]
    public string priority;
    [Tooltip("If enabled, future feed UI should treat this item as read.")]
    public bool read;
    [Tooltip("If enabled, future feed UI should hide this item from default views.")]
    public bool dismissed;
    [Tooltip("If enabled, future feed UI should pin this item.")]
    public bool pinned;
    [Tooltip("How many times this feed item has been unlocked or refreshed.")]
    [Min(0)]
    public int unlockCount;
    [Tooltip("Last source id that unlocked or refreshed this item.")]
    public string lastSourceId;
    [Tooltip("In-game day when this item was last unlocked.")]
    public int lastUnlockedDay = -1;
    [Tooltip("In-game total hour when this item was last unlocked.")]
    public int lastUnlockedTotalHour = -1;
    [Tooltip("In-game total hour when this item was last read.")]
    public int lastReadTotalHour = -1;
    [Tooltip("In-game total hour when this item expires. -1 means no expiration.")]
    public int expiresTotalHour = -1;

    public bool IsActive(int currentTotalHour, out string failureMessage) {
        if(expiresTotalHour >= 0 && currentTotalHour >= expiresTotalHour) {
            failureMessage = $"{title} has expired.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public PokeNavFeedItemRecord Clone() {
        return new PokeNavFeedItemRecord {
            itemId = itemId,
            title = title,
            feedType = feedType,
            sourceName = sourceName,
            priority = priority,
            read = read,
            dismissed = dismissed,
            pinned = pinned,
            unlockCount = unlockCount,
            lastSourceId = lastSourceId,
            lastUnlockedDay = lastUnlockedDay,
            lastUnlockedTotalHour = lastUnlockedTotalHour,
            lastReadTotalHour = lastReadTotalHour,
            expiresTotalHour = expiresTotalHour
        };
    }
}

[Serializable]
public class PlayerPokeNavFeedLogSaveData {
    [Tooltip("Saved PokeNav feed item records.")]
    public List<PokeNavFeedItemRecord> feedItems = new List<PokeNavFeedItemRecord>();
}
