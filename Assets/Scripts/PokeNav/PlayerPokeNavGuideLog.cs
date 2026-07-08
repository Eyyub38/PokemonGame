using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPokeNavGuideLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save state for generic PokeNav guide items.")]
    [SerializeField] List<PokeNavGuideItemState> itemStates = new List<PokeNavGuideItemState>();

    public IReadOnlyList<PokeNavGuideItemState> ItemStates => itemStates;
    public event Action<PokeNavGuideItemState> OnGuideItemChanged;
    public event Action OnGuideLogChanged;

    public PokeNavGuideItemState GetState(PokeNavGuideContentType contentType, string itemId) {
        string key = PokeNavGuideItemRecord.BuildKey(contentType, itemId);
        return string.IsNullOrWhiteSpace(itemId)
            ? null
            : itemStates.FirstOrDefault(state => state != null && state.key == key);
    }

    public PokeNavGuideItemState MarkSeen(PokeNavGuideItemRecord item, string sourceId = null) {
        if(item == null || string.IsNullOrWhiteSpace(item.itemId)) {
            return null;
        }

        var state = GetOrCreateState(item.contentType, item.itemId, item.title);
        state.title = item.title;
        state.contentType = item.contentType;
        state.itemId = item.itemId;
        state.seen = true;
        state.seenCount++;
        state.lastSourceId = sourceId;
        state.lastSeenDay = GetCurrentDay();
        state.lastSeenAbsoluteHour = GetCurrentAbsoluteHour();
        NotifyChanged(state);
        return state;
    }

    public bool MarkRead(PokeNavGuideContentType contentType, string itemId, bool read = true, string title = null) {
        var state = GetOrCreateState(contentType, itemId, title);
        if(state == null || state.read == read) {
            return false;
        }

        state.read = read;
        if(read) {
            state.lastReadAbsoluteHour = GetCurrentAbsoluteHour();
        }

        NotifyChanged(state);
        return true;
    }

    public bool SetPinned(PokeNavGuideContentType contentType, string itemId, bool pinned, string title = null) {
        var state = GetOrCreateState(contentType, itemId, title);
        if(state == null || state.pinned == pinned) {
            return false;
        }

        state.pinned = pinned;
        NotifyChanged(state);
        return true;
    }

    public bool SetDismissed(PokeNavGuideContentType contentType, string itemId, bool dismissed, string title = null) {
        var state = GetOrCreateState(contentType, itemId, title);
        if(state == null || state.dismissed == dismissed) {
            return false;
        }

        state.dismissed = dismissed;
        NotifyChanged(state);
        return true;
    }

    public bool IsSeen(PokeNavGuideContentType contentType, string itemId) {
        return GetState(contentType, itemId)?.seen ?? false;
    }

    public bool IsRead(PokeNavGuideContentType contentType, string itemId) {
        return GetState(contentType, itemId)?.read ?? false;
    }

    public bool IsPinned(PokeNavGuideContentType contentType, string itemId) {
        return GetState(contentType, itemId)?.pinned ?? false;
    }

    public bool IsDismissed(PokeNavGuideContentType contentType, string itemId) {
        return GetState(contentType, itemId)?.dismissed ?? false;
    }

    public int CountStates(PokeNavGuideContentType? contentType = null, bool? seen = null, bool? read = null, bool? pinned = null, bool? dismissed = null, string requiredTag = null) {
        return itemStates.Count(state => state != null
            && (!contentType.HasValue || state.contentType == contentType.Value)
            && (!seen.HasValue || state.seen == seen.Value)
            && (!read.HasValue || state.read == read.Value)
            && (!pinned.HasValue || state.pinned == pinned.Value)
            && (!dismissed.HasValue || state.dismissed == dismissed.Value)
            && (string.IsNullOrWhiteSpace(requiredTag) || state.HasTag(requiredTag)));
    }

    public void ApplyState(PokeNavGuideItemRecord item) {
        if(item == null || string.IsNullOrWhiteSpace(item.itemId)) {
            return;
        }

        var state = GetState(item.contentType, item.itemId);
        if(state == null) {
            return;
        }

        item.read = item.read || state.read;
        item.pinned = item.pinned || state.pinned;
        item.dismissed = item.dismissed || state.dismissed;
        item.lastSeenAbsoluteHour = state.lastSeenAbsoluteHour;
        if(state.tags != null && state.tags.Count > 0) {
            item.tags = item.tags != null
                ? item.tags.Concat(state.tags).Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                : state.tags.ToList();
        }
    }

    PokeNavGuideItemState GetOrCreateState(PokeNavGuideContentType contentType, string itemId, string title) {
        if(string.IsNullOrWhiteSpace(itemId)) {
            return null;
        }

        var state = GetState(contentType, itemId);
        if(state != null) {
            if(!string.IsNullOrWhiteSpace(title)) {
                state.title = title;
            }
            return state;
        }

        state = new PokeNavGuideItemState {
            key = PokeNavGuideItemRecord.BuildKey(contentType, itemId),
            contentType = contentType,
            itemId = itemId,
            title = title
        };
        itemStates.Add(state);
        return state;
    }

    void NotifyChanged(PokeNavGuideItemState state) {
        OnGuideItemChanged?.Invoke(state);
        OnGuideLogChanged?.Invoke();
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerPokeNavGuideLogSaveData {
            itemStates = itemStates.Where(state => state != null).Select(state => state.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerPokeNavGuideLogSaveData;
        itemStates = saveData?.itemStates?.Where(entry => entry != null).Select(entry => entry.Clone()).ToList()
            ?? new List<PokeNavGuideItemState>();
        OnGuideLogChanged?.Invoke();
    }
}

[Serializable]
public class PokeNavGuideItemState {
    [Tooltip("Combined content type and item id key.")]
    public string key;
    [Tooltip("Content type represented by this state.")]
    public PokeNavGuideContentType contentType;
    [Tooltip("Stable source item id.")]
    public string itemId;
    [Tooltip("Saved title for fallback/debug output.")]
    public string title;
    [Tooltip("Whether this guide item has appeared in a guide section.")]
    public bool seen;
    [Tooltip("Whether the player has read/opened this guide item.")]
    public bool read;
    [Tooltip("Whether future UI should pin this guide item.")]
    public bool pinned;
    [Tooltip("Whether future UI should hide this guide item from normal lists.")]
    public bool dismissed;
    [Tooltip("How many times this item has been marked seen.")]
    [Min(0)]
    public int seenCount;
    [Tooltip("Last source or section id that marked this item seen.")]
    public string lastSourceId;
    [Tooltip("In-game day when this item was last seen.")]
    public int lastSeenDay = -1;
    [Tooltip("Absolute in-game hour when this item was last seen.")]
    public int lastSeenAbsoluteHour = -1;
    [Tooltip("Absolute in-game hour when this item was last read.")]
    public int lastReadAbsoluteHour = -1;
    [Tooltip("Free-form tags attached by runtime systems or future UI.")]
    public List<string> tags = new List<string>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public PokeNavGuideItemState Clone() {
        return new PokeNavGuideItemState {
            key = key,
            contentType = contentType,
            itemId = itemId,
            title = title,
            seen = seen,
            read = read,
            pinned = pinned,
            dismissed = dismissed,
            seenCount = Mathf.Max(0, seenCount),
            lastSourceId = lastSourceId,
            lastSeenDay = lastSeenDay,
            lastSeenAbsoluteHour = lastSeenAbsoluteHour,
            lastReadAbsoluteHour = lastReadAbsoluteHour,
            tags = tags != null ? tags.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerPokeNavGuideLogSaveData {
    public List<PokeNavGuideItemState> itemStates = new List<PokeNavGuideItemState>();
}
