using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ShopShelfInteractionType {
    Viewed,
    AddedOffer,
    AddedDefaultOffer,
    AddedFirstVisibleOffer,
    AddedAllVisibleOffers,
    Blocked
}

public class PlayerShopShelfLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum shelf interaction records kept in memory/save data. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 240;
    [Tooltip("Runtime/save shelf interaction history.")]
    [SerializeField] List<ShopShelfInteractionRecord> records = new List<ShopShelfInteractionRecord>();

    public IReadOnlyList<ShopShelfInteractionRecord> Records => records;
    public event Action<ShopShelfInteractionRecord> OnShelfInteractionRecorded;

    public ShopShelfInteractionRecord RecordInteraction(
        ShopShelfDefinition shelf,
        ShopCatalog shop,
        ShopShelfInteractionType interactionType,
        string sourceId,
        ShopCatalogEntry offer = null,
        int bundles = 0,
        bool blocked = false,
        string failureMessage = null
    ) {
        if(shelf == null) {
            return null;
        }

        var record = new ShopShelfInteractionRecord {
            recordId = Guid.NewGuid().ToString("N"),
            shelfId = shelf.Id,
            shelfName = shelf.DisplayName,
            kind = shelf.Kind,
            shopId = shop != null ? shop.ShopId : string.Empty,
            catalogId = shop != null && shop.Catalog != null ? shop.Catalog.Id : string.Empty,
            shopName = shop != null && shop.Catalog != null ? shop.Catalog.DisplayName : string.Empty,
            sourceId = NormalizeSourceId(sourceId),
            interactionType = interactionType,
            offerId = offer != null ? offer.OfferId : string.Empty,
            offerName = offer != null ? offer.DisplayName : string.Empty,
            bundles = Mathf.Max(0, bundles),
            blocked = blocked,
            failureMessage = failureMessage,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            tags = shelf.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };

        records.Add(record);
        TrimHistory();
        OnShelfInteractionRecorded?.Invoke(record);
        return record;
    }

    public bool HasViewed(ShopShelfDefinition shelf = null, string shopId = null, bool includeBlocked = false) {
        return GetInteractionCount(shelf, shopId, ShopShelfInteractionType.Viewed, includeBlocked) > 0;
    }

    public int GetInteractionCount(ShopShelfDefinition shelf = null, string shopId = null, ShopShelfInteractionType? interactionType = null, bool includeBlocked = false) {
        string shelfId = shelf != null ? shelf.Id : null;
        return records.Count(record => Matches(record, shelfId, shopId, interactionType, null, null, includeBlocked));
    }

    public int GetTaggedInteractionCount(string requiredTag, string shopId = null, ShopShelfInteractionType? interactionType = null, bool includeBlocked = false) {
        return records.Count(record => Matches(record, null, shopId, interactionType, requiredTag, null, includeBlocked));
    }

    public int GetOfferAddCount(ShopShelfDefinition shelf = null, string offerId = null, string shopId = null, bool includeBlocked = false) {
        string shelfId = shelf != null ? shelf.Id : null;
        return records.Count(record => Matches(record, shelfId, shopId, null, null, offerId, includeBlocked)
            && (record.interactionType == ShopShelfInteractionType.AddedOffer
                || record.interactionType == ShopShelfInteractionType.AddedDefaultOffer
                || record.interactionType == ShopShelfInteractionType.AddedFirstVisibleOffer
                || record.interactionType == ShopShelfInteractionType.AddedAllVisibleOffers));
    }

    public ShopShelfInteractionRecord GetLastInteraction(ShopShelfDefinition shelf = null, string shopId = null, bool includeBlocked = false) {
        string shelfId = shelf != null ? shelf.Id : null;
        return records
            .Where(record => Matches(record, shelfId, shopId, null, null, null, includeBlocked))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .FirstOrDefault();
    }

    public int GetHoursSinceLastInteraction(ShopShelfDefinition shelf = null, string shopId = null, bool includeBlocked = false) {
        var lastInteraction = GetLastInteraction(shelf, shopId, includeBlocked);
        return lastInteraction != null ? Mathf.Max(0, GetCurrentAbsoluteHour() - lastInteraction.absoluteHour) : -1;
    }

    public void ClearHistoryBefore(int absoluteHour) {
        records.RemoveAll(record => record != null && record.absoluteHour < absoluteHour);
    }

    bool Matches(
        ShopShelfInteractionRecord record,
        string shelfId,
        string shopId,
        ShopShelfInteractionType? interactionType,
        string requiredTag,
        string offerId,
        bool includeBlocked
    ) {
        return record != null
            && (includeBlocked || !record.blocked)
            && (string.IsNullOrWhiteSpace(shelfId) || record.shelfId == shelfId)
            && (string.IsNullOrWhiteSpace(shopId) || record.shopId == shopId)
            && (!interactionType.HasValue || record.interactionType == interactionType.Value)
            && (string.IsNullOrWhiteSpace(requiredTag) || record.HasTag(requiredTag))
            && (string.IsNullOrWhiteSpace(offerId) || record.offerId == offerId);
    }

    void TrimHistory() {
        if(maxHistoryRecords <= 0 || records.Count <= maxHistoryRecords) {
            return;
        }

        records = records
            .Where(record => record != null)
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .Take(maxHistoryRecords)
            .OrderBy(record => record.absoluteHour)
            .ThenBy(record => record.day)
            .ToList();
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "shop-shelf" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerShopShelfLogSaveData {
            records = records.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerShopShelfLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => record.Clone()).ToList()
            ?? new List<ShopShelfInteractionRecord>();
        TrimHistory();
    }
}

[Serializable]
public class ShopShelfInteractionRecord {
    [Tooltip("Unique runtime/save id for this shelf interaction.")]
    public string recordId;
    [Tooltip("Saved shelf id.")]
    public string shelfId;
    [Tooltip("Saved shelf display name.")]
    public string shelfName;
    [Tooltip("Shelf kind at interaction time.")]
    public ShopShelfKind kind;
    [Tooltip("Shop instance id used by this interaction.")]
    public string shopId;
    [Tooltip("Catalog definition id used by this interaction.")]
    public string catalogId;
    [Tooltip("Shop display name copied for fallback/debug output.")]
    public string shopName;
    [Tooltip("Source id that recorded this interaction.")]
    public string sourceId;
    [Tooltip("Interaction type recorded for this shelf.")]
    public ShopShelfInteractionType interactionType;
    [Tooltip("Offer id affected by this interaction, if any.")]
    public string offerId;
    [Tooltip("Offer display name copied at interaction time.")]
    public string offerName;
    [Tooltip("Bundle count affected by this interaction.")]
    [Min(0)]
    public int bundles;
    [Tooltip("Whether this record is a blocked attempt.")]
    public bool blocked;
    [Tooltip("Failure message stored for blocked attempts.")]
    public string failureMessage;
    [Tooltip("In-game day when this interaction happened.")]
    public int day;
    [Tooltip("Absolute in-game hour when this interaction happened.")]
    public int absoluteHour;
    [Tooltip("Shelf tags copied at interaction time.")]
    public List<string> tags = new List<string>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public ShopShelfInteractionRecord Clone() {
        return new ShopShelfInteractionRecord {
            recordId = recordId,
            shelfId = shelfId,
            shelfName = shelfName,
            kind = kind,
            shopId = shopId,
            catalogId = catalogId,
            shopName = shopName,
            sourceId = sourceId,
            interactionType = interactionType,
            offerId = offerId,
            offerName = offerName,
            bundles = bundles,
            blocked = blocked,
            failureMessage = failureMessage,
            day = day,
            absoluteHour = absoluteHour,
            tags = tags != null ? tags.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerShopShelfLogSaveData {
    public List<ShopShelfInteractionRecord> records = new List<ShopShelfInteractionRecord>();
}
