using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerLearnableOfferLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save purchase records for learnable offers.")]
    [SerializeField] List<LearnableOfferPurchaseRecord> purchases = new List<LearnableOfferPurchaseRecord>();

    public IReadOnlyList<LearnableOfferPurchaseRecord> Purchases => purchases;
    public event Action<LearnableOfferPurchaseRecord> OnOfferPurchased;

    public bool CanPurchase(LearnableOfferDefinition offer, string sourceId, ConsequenceChainRepeatMode repeatMode, int cooldownHours, int maxPurchaseCount, out string failureMessage) {
        if(offer == null) {
            failureMessage = "No learnable offer selected.";
            return false;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId);
        int successfulCount = GetPurchaseCount(offer, includeBlocked: false);
        if(maxPurchaseCount > 0 && successfulCount >= maxPurchaseCount) {
            failureMessage = $"{offer.DisplayName} has reached its purchase limit.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && successfulCount > 0) {
            failureMessage = $"{offer.DisplayName} has already been purchased.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetPurchaseCount(offer, normalizedSourceId, includeBlocked: false) > 0) {
            failureMessage = $"{offer.DisplayName} has already been purchased from this source.";
            return false;
        }

        var lastSourcePurchase = GetLastPurchase(offer, normalizedSourceId, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastSourcePurchase != null && lastSourcePurchase.day == GetCurrentDay()) {
            failureMessage = $"{offer.DisplayName} can only be purchased once per day from this source.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastSourcePurchase != null) {
            int elapsed = GetCurrentAbsoluteHour() - lastSourcePurchase.absoluteHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{offer.DisplayName} will be available again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public LearnableOfferPurchaseRecord RecordPurchase(LearnableOfferDefinition offer, string sourceId, float pricePaid, int appliedGrantCount, bool blocked = false, string failureMessage = null) {
        if(offer == null) {
            return null;
        }

        var record = new LearnableOfferPurchaseRecord {
            offerId = offer.Id,
            offerName = offer.DisplayName,
            category = offer.Category,
            sourceId = NormalizeSourceId(sourceId),
            pricePaid = Mathf.Max(0f, pricePaid),
            appliedGrantCount = Mathf.Max(0, appliedGrantCount),
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            blocked = blocked,
            failureMessage = failureMessage,
            tags = offer.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };

        purchases.Add(record);
        OnOfferPurchased?.Invoke(record);
        return record;
    }

    public bool HasPurchased(LearnableOfferDefinition offer = null, string sourceId = null, bool includeBlocked = false) {
        return GetPurchaseCount(offer, sourceId, includeBlocked: includeBlocked) > 0;
    }

    public int GetPurchaseCount(LearnableOfferDefinition offer = null, string sourceId = null, LearnableOfferCategory? category = null, string requiredTag = null, bool includeBlocked = false) {
        string offerId = offer != null ? offer.Id : null;
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return purchases.Count(record => Matches(record, offerId, normalizedSourceId, category, requiredTag, includeBlocked));
    }

    public LearnableOfferPurchaseRecord GetLastPurchase(LearnableOfferDefinition offer = null, string sourceId = null, LearnableOfferCategory? category = null, string requiredTag = null, bool includeBlocked = false) {
        string offerId = offer != null ? offer.Id : null;
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return purchases
            .Where(record => Matches(record, offerId, normalizedSourceId, category, requiredTag, includeBlocked))
            .OrderByDescending(record => record.absoluteHour)
            .FirstOrDefault();
    }

    bool Matches(LearnableOfferPurchaseRecord record, string offerId, string sourceId, LearnableOfferCategory? category, string requiredTag, bool includeBlocked) {
        return record != null
            && (includeBlocked || !record.blocked)
            && (string.IsNullOrWhiteSpace(offerId) || record.offerId == offerId)
            && (string.IsNullOrWhiteSpace(sourceId) || record.sourceId == sourceId)
            && (!category.HasValue || record.category == category.Value)
            && (string.IsNullOrWhiteSpace(requiredTag) || record.HasTag(requiredTag));
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "learnable-offer" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerLearnableOfferLogSaveData {
            purchases = purchases.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerLearnableOfferLogSaveData;
        purchases = saveData?.purchases?.Where(record => record != null).Select(record => record.Clone()).ToList()
            ?? new List<LearnableOfferPurchaseRecord>();
    }
}

[Serializable]
public class LearnableOfferPurchaseRecord {
    [Tooltip("Offer id purchased or attempted.")]
    public string offerId;
    [Tooltip("Offer display name copied at purchase time.")]
    public string offerName;
    [Tooltip("Offer category copied at purchase time.")]
    public LearnableOfferCategory category;
    [Tooltip("Source id that sold this offer.")]
    public string sourceId;
    [Tooltip("Money paid for this offer.")]
    public float pricePaid;
    [Tooltip("Number of grants that changed player state.")]
    public int appliedGrantCount;
    [Tooltip("In-game day when this record was created.")]
    public int day;
    [Tooltip("Absolute in-game hour when this record was created.")]
    public int absoluteHour;
    [Tooltip("If enabled, this record represents a blocked attempt.")]
    public bool blocked;
    [Tooltip("Blocked attempt reason, if any.")]
    public string failureMessage;
    [Tooltip("Offer tags copied at purchase time.")]
    public List<string> tags = new List<string>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public LearnableOfferPurchaseRecord Clone() {
        return new LearnableOfferPurchaseRecord {
            offerId = offerId,
            offerName = offerName,
            category = category,
            sourceId = sourceId,
            pricePaid = pricePaid,
            appliedGrantCount = appliedGrantCount,
            day = day,
            absoluteHour = absoluteHour,
            blocked = blocked,
            failureMessage = failureMessage,
            tags = tags != null ? tags.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerLearnableOfferLogSaveData {
    public List<LearnableOfferPurchaseRecord> purchases = new List<LearnableOfferPurchaseRecord>();
}
