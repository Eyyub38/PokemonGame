using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ShopRefundSourceAction {
    PreviewRefund,
    Refund
}

[CreateAssetMenu(menuName = "Shops/Return Policy Definition")]
public class ShopReturnPolicyDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this return policy. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future refund UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing description for this return policy.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Optional icon used by future refund UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as mart, premium, no-questions, strict, clinic, police or region name.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Catalog Filters")]
    [Tooltip("If assigned, refunds are accepted only for this exact catalog.")]
    [SerializeField] ShopCatalogDefinition requiredCatalog;
    [Tooltip("If not empty, refunds are accepted only for these catalog types.")]
    [SerializeField] List<ShopCatalogType> allowedCatalogTypes = new List<ShopCatalogType>();
    [Tooltip("Catalog tags required before this return policy can be used. Empty means no catalog tag filter.")]
    [SerializeField] List<string> requiredCatalogTags = new List<string>();
    [Tooltip("How required catalog tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode catalogTagMatchMode = ConsequenceRequirementMatchMode.All;

    [Header("Receipt Rules")]
    [Tooltip("If enabled, receipt shop id must match the current shop id.")]
    [SerializeField] bool requireSameShop = true;
    [Tooltip("If enabled, full receipt refunds are allowed when no offer id is specified.")]
    [SerializeField] bool allowFullReceiptRefund = true;
    [Tooltip("If enabled, a single offer line from the receipt can be refunded.")]
    [SerializeField] bool allowLineRefund = true;
    [Tooltip("If enabled, fewer bundles than the original line can be refunded.")]
    [SerializeField] bool allowPartialLineRefunds = true;
    [Tooltip("In-game hours after checkout during which refund is allowed. 0 means no time limit.")]
    [Min(0)]
    [SerializeField] int returnWindowHours = 72;
    [Tooltip("If enabled, the player must still have returned items in inventory.")]
    [SerializeField] bool requireItemsInInventory = true;
    [Tooltip("If enabled, returned stock-limited offers reduce the player's purchased stock count.")]
    [SerializeField] bool restoreLimitedStockOnRefund = true;
    [Tooltip("If enabled, receipts with no Wallet amount paid cannot be refunded for money.")]
    [SerializeField] bool requirePositiveAmountPaid = true;
    [Tooltip("Payment modes accepted by this return policy. Empty allows any mode.")]
    [SerializeField] List<ShopPaymentMode> allowedPaymentModes = new List<ShopPaymentMode>();

    [Header("Refund Amount")]
    [Tooltip("Maximum percentage of the item line total refunded. 1.0 means 100%, 0.5 means 50%.")]
    [Min(0f)]
    [SerializeField] float maxRefundPercent = 1f;
    [Tooltip("Flat restocking fee subtracted from the refund total.")]
    [Min(0f)]
    [SerializeField] float flatRestockingFee;
    [Tooltip("Percentage restocking fee subtracted from the gross refund. 0.10 means 10%.")]
    [Min(0f)]
    [SerializeField] float percentageRestockingFee;
    [Tooltip("If enabled, final refund money is rounded down with Mathf.Floor.")]
    [SerializeField] bool roundRefundDown = true;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this return policy can be used.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this return policy can be used.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this return policy.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional world event whose active state gates this return policy.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for Required World Event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("How extra requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this return policy can be used.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this return policy is locked.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This return policy is not available.";

    [Header("Events")]
    [Tooltip("Optional event published when a refund succeeds.")]
    [SerializeField] GameEventDefinition refundedEvent;
    [Tooltip("Optional event published when a refund is blocked.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, refund events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, refund events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public ShopCatalogDefinition RequiredCatalog => requiredCatalog;
    public IReadOnlyList<ShopCatalogType> AllowedCatalogTypes => allowedCatalogTypes != null ? (IReadOnlyList<ShopCatalogType>)allowedCatalogTypes : Array.Empty<ShopCatalogType>();
    public IReadOnlyList<string> RequiredCatalogTags => requiredCatalogTags != null ? (IReadOnlyList<string>)requiredCatalogTags : Array.Empty<string>();
    public bool RequireSameShop => requireSameShop;
    public bool AllowFullReceiptRefund => allowFullReceiptRefund;
    public bool AllowLineRefund => allowLineRefund;
    public bool AllowPartialLineRefunds => allowPartialLineRefunds;
    public int ReturnWindowHours => Mathf.Max(0, returnWindowHours);
    public bool RequireItemsInInventory => requireItemsInInventory;
    public bool RestoreLimitedStockOnRefund => restoreLimitedStockOnRefund;
    public bool RequirePositiveAmountPaid => requirePositiveAmountPaid;
    public IReadOnlyList<ShopPaymentMode> AllowedPaymentModes => allowedPaymentModes != null ? (IReadOnlyList<ShopPaymentMode>)allowedPaymentModes : Array.Empty<ShopPaymentMode>();
    public float MaxRefundPercent => Mathf.Clamp01(maxRefundPercent);
    public float FlatRestockingFee => Mathf.Max(0f, flatRestockingFee);
    public float PercentageRestockingFee => Mathf.Max(0f, percentageRestockingFee);
    public bool RoundRefundDown => roundRefundDown;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public bool TryBuildRefundQuote(
        PlayerController player,
        ShopCatalog shop,
        PlayerShopBasketLog basketLog,
        PlayerShopReceiptLog receiptLog,
        ShopBasketCheckoutRecord checkoutRecord,
        string offerId,
        int bundles,
        string sourceId,
        out ShopRefundQuote quote,
        out string failureMessage
    ) {
        quote = null;
        if(!CanUse(player, shop, basketLog, receiptLog, checkoutRecord, offerId, bundles, out failureMessage)) {
            PublishRefundEvent(blockedEvent, "blocked", null, player, shop, sourceId, GameEventImportance.Warning, failureMessage);
            return false;
        }

        var lines = BuildRefundLines(shop, receiptLog, checkoutRecord, offerId, bundles, out failureMessage);
        if(lines.Count == 0) {
            failureMessage ??= "No refundable receipt lines were found.";
            PublishRefundEvent(blockedEvent, "blocked", null, player, shop, sourceId, GameEventImportance.Warning, failureMessage);
            return false;
        }

        float grossRefund = lines.Sum(line => Mathf.Max(0f, line.grossRefund));
        float restockingFee = FlatRestockingFee + grossRefund * PercentageRestockingFee;
        float amountRefunded = Mathf.Max(0f, grossRefund * MaxRefundPercent - restockingFee);
        if(roundRefundDown) {
            amountRefunded = Mathf.Floor(amountRefunded);
            restockingFee = Mathf.Floor(restockingFee);
        }

        quote = new ShopRefundQuote {
            returnPolicyId = Id,
            returnPolicyName = DisplayName,
            checkoutRecordId = checkoutRecord.recordId,
            shopId = checkoutRecord.shopId,
            sourceId = string.IsNullOrWhiteSpace(sourceId) ? Id : sourceId,
            grossRefund = grossRefund,
            restockingFee = Mathf.Max(0f, restockingFee),
            amountRefunded = amountRefunded,
            requestedOfferId = offerId,
            removeItemsFromInventory = requireItemsInInventory,
            restoreLimitedStock = restoreLimitedStockOnRefund,
            lines = lines,
            message = $"{DisplayName}: refund {amountRefunded:0}."
        };

        failureMessage = null;
        return true;
    }

    public bool CanUse(PlayerController player, ShopCatalog shop, PlayerShopBasketLog basketLog, PlayerShopReceiptLog receiptLog, ShopBasketCheckoutRecord checkoutRecord, string offerId, int bundles, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for refunds.";
            return false;
        }

        if(shop == null || shop.Catalog == null) {
            failureMessage = "A shop catalog is required for refunds.";
            return false;
        }

        if(basketLog == null) {
            failureMessage = "Player has no checkout receipt log.";
            return false;
        }

        if(receiptLog == null) {
            failureMessage = "Player has no refund log.";
            return false;
        }

        if(checkoutRecord == null) {
            failureMessage = "Checkout receipt was not found.";
            return false;
        }

        if(requireSameShop && checkoutRecord.shopId != shop.ShopId) {
            failureMessage = $"Receipt belongs to {checkoutRecord.shopName}, not this shop.";
            return false;
        }

        if(requiredCatalog != null && shop.Catalog != requiredCatalog) {
            failureMessage = $"{DisplayName} cannot be used at this shop.";
            return false;
        }

        if(allowedCatalogTypes != null && allowedCatalogTypes.Count > 0 && !allowedCatalogTypes.Contains(shop.Catalog.CatalogType)) {
            failureMessage = $"{DisplayName} cannot be used for this shop type.";
            return false;
        }

        if(!MatchesTags(requiredCatalogTags, catalogTagMatchMode, tag => shop.Catalog.HasTag(tag))) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not available for this shop." : lockedMessage;
            return false;
        }

        if(!string.IsNullOrWhiteSpace(offerId) && !allowLineRefund) {
            failureMessage = $"{DisplayName} does not allow line-item refunds.";
            return false;
        }

        if(string.IsNullOrWhiteSpace(offerId) && !allowFullReceiptRefund) {
            failureMessage = $"{DisplayName} does not allow full receipt refunds.";
            return false;
        }

        if(requirePositiveAmountPaid && checkoutRecord.amountPaid <= 0f) {
            failureMessage = "This receipt did not charge wallet money, so it cannot be refunded for money.";
            return false;
        }

        if(ReturnWindowHours > 0) {
            int ageHours = GetCurrentAbsoluteHour() - checkoutRecord.absoluteHour;
            if(ageHours > ReturnWindowHours) {
                failureMessage = $"{DisplayName} only accepts returns within {ReturnWindowHours} hour(s).";
                return false;
            }
        }

        if(allowedPaymentModes != null && allowedPaymentModes.Count > 0) {
            bool matchesPaymentMode = allowedPaymentModes.Any(mode => string.Equals(mode.ToString(), checkoutRecord.paymentMode, StringComparison.OrdinalIgnoreCase));
            if(!matchesPaymentMode) {
                failureMessage = $"{DisplayName} does not accept {checkoutRecord.paymentMode} receipts.";
                return false;
            }
        }

        if(requiredTitle != null && !(player.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredMilestone != null && !(player.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredWorldEvent != null) {
            bool active = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredWorldEvent);
            if(active != requiredWorldEventActive) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not available right now." : lockedMessage;
                return false;
            }
        }

        if(!ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        BuildRefundLines(shop, receiptLog, checkoutRecord, offerId, bundles, out failureMessage);
        return string.IsNullOrWhiteSpace(failureMessage);
    }

    public void PublishRefunded(ShopRefundQuote quote, PlayerController player, ShopCatalog shop, string sourceId) {
        PublishRefundEvent(refundedEvent, "refunded", quote, player, shop, sourceId, GameEventImportance.Success, null);
    }

    public void PublishBlocked(ShopRefundQuote quote, PlayerController player, ShopCatalog shop, string sourceId, string failureMessage) {
        PublishRefundEvent(blockedEvent, "blocked", quote, player, shop, sourceId, GameEventImportance.Warning, failureMessage);
    }

    List<ShopRefundLineRecord> BuildRefundLines(ShopCatalog shop, PlayerShopReceiptLog receiptLog, ShopBasketCheckoutRecord checkoutRecord, string offerId, int bundles, out string failureMessage) {
        failureMessage = null;
        var results = new List<ShopRefundLineRecord>();
        var sourceLines = checkoutRecord.lines ?? new List<ShopBasketLineState>();
        var selectedLines = string.IsNullOrWhiteSpace(offerId)
            ? sourceLines
            : sourceLines.Where(line => line != null && line.offerId == offerId).ToList();

        if(selectedLines.Count == 0) {
            failureMessage = string.IsNullOrWhiteSpace(offerId) ? "Receipt has no refundable lines." : $"Receipt does not contain offer '{offerId}'.";
            return results;
        }

        foreach(var line in selectedLines) {
            if(line == null || string.IsNullOrWhiteSpace(line.offerId)) {
                continue;
            }

            int purchasedBundles = Mathf.Max(0, line.bundles);
            int alreadyRefunded = receiptLog.GetRefundedBundles(checkoutRecord.recordId, line.offerId);
            int availableBundles = Mathf.Max(0, purchasedBundles - alreadyRefunded);
            if(availableBundles <= 0) {
                continue;
            }

            int requestedBundles = string.IsNullOrWhiteSpace(offerId) || bundles <= 0 ? availableBundles : Mathf.Min(bundles, availableBundles);
            if(requestedBundles <= 0) {
                continue;
            }

            if(!allowPartialLineRefunds && requestedBundles < availableBundles) {
                failureMessage = $"{DisplayName} does not allow partial bundle refunds.";
                return new List<ShopRefundLineRecord>();
            }

            var entry = shop.FindOffer(line.offerId);
            var item = entry != null ? entry.GetItem() : null;
            int itemsPerBundle = Mathf.Max(1, line.itemsPerBundle);
            int itemCount = requestedBundles * itemsPerBundle;
            if(requireItemsInInventory) {
                var inventory = Inventory.GetInventory();
                if(item == null) {
                    failureMessage = $"Cannot resolve returned item for offer '{line.offerId}'.";
                    return new List<ShopRefundLineRecord>();
                }

                if(inventory == null || !inventory.HasItemEnough(item, itemCount)) {
                    failureMessage = $"You do not have enough {line.itemName} to refund.";
                    return new List<ShopRefundLineRecord>();
                }
            }

            float unitPrice = line.unitBundlePrice > 0f
                ? line.unitBundlePrice
                : purchasedBundles > 0 ? line.lineTotal / purchasedBundles : 0f;
            results.Add(new ShopRefundLineRecord {
                offerId = line.offerId,
                itemId = line.itemId,
                itemName = line.itemName,
                bundles = requestedBundles,
                itemsPerBundle = itemsPerBundle,
                itemCount = itemCount,
                unitBundlePrice = unitPrice,
                grossRefund = Mathf.Max(0f, unitPrice * requestedBundles)
            });
        }

        if(results.Count == 0) {
            failureMessage = "All matching receipt lines were already refunded.";
        }

        return results;
    }

    bool MatchesTags(List<string> requiredTags, ConsequenceRequirementMatchMode matchMode, Func<string, bool> hasTag) {
        var activeTags = requiredTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() ?? new List<string>();
        if(activeTags.Count == 0) {
            return true;
        }

        if(matchMode == ConsequenceRequirementMatchMode.Any) {
            return activeTags.Any(hasTag);
        }

        return activeTags.All(hasTag);
    }

    void PublishRefundEvent(GameEventDefinition eventDefinition, string phase, ShopRefundQuote quote, PlayerController player, ShopCatalog shop, string sourceId, GameEventImportance importance, string failureMessage) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"shop.refund.{phase}.{Id}.{shop?.ShopId ?? "shop"}",
            !string.IsNullOrWhiteSpace(failureMessage) ? failureMessage : $"{DisplayName} refund {phase}.",
            GameEventCategory.Shop,
            importance,
            player != null ? player : this,
            "ShopReturnPolicyDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("returnPolicyId", Id),
            GameEventPublishing.Value("returnPolicyName", DisplayName),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("shopId", shop != null ? shop.ShopId : string.Empty),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("checkoutRecordId", quote != null ? quote.checkoutRecordId : string.Empty),
            GameEventPublishing.Value("grossRefund", quote != null ? quote.grossRefund : 0f),
            GameEventPublishing.Value("restockingFee", quote != null ? quote.restockingFee : 0f),
            GameEventPublishing.Value("amountRefunded", quote != null ? quote.amountRefunded : 0f));
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }
}

public class PlayerShopReceiptLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum refund records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRefundRecords = 200;
    [Tooltip("Runtime/save refund history.")]
    [SerializeField] List<ShopRefundRecord> refundRecords = new List<ShopRefundRecord>();

    public IReadOnlyList<ShopRefundRecord> RefundRecords => refundRecords;
    public event Action<ShopRefundRecord> OnRefundRecorded;

    public int GetRefundedBundles(string checkoutRecordId, string offerId) {
        if(string.IsNullOrWhiteSpace(checkoutRecordId) || string.IsNullOrWhiteSpace(offerId)) {
            return 0;
        }

        return refundRecords
            .Where(record => record != null && record.checkoutRecordId == checkoutRecordId)
            .SelectMany(record => record.lines ?? new List<ShopRefundLineRecord>())
            .Where(line => line != null && line.offerId == offerId)
            .Sum(line => Mathf.Max(0, line.bundles));
    }

    public bool TryRefund(ShopReturnPolicyDefinition policy, ShopCatalog shop, string checkoutRecordId, string offerId, int bundles, string sourceId, out ShopRefundRecord refundRecord, out string failureMessage) {
        refundRecord = null;
        if(policy == null) {
            failureMessage = "No return policy assigned.";
            return false;
        }

        var player = GetComponent<PlayerController>();
        var basketLog = GetComponent<PlayerShopBasketLog>();
        if(basketLog == null) {
            failureMessage = "Player has no checkout receipt history.";
            return false;
        }

        var checkoutRecord = basketLog.FindCheckoutRecord(checkoutRecordId);
        if(checkoutRecord == null) {
            failureMessage = "Checkout receipt was not found.";
            return false;
        }

        if(!policy.TryBuildRefundQuote(player, shop, basketLog, this, checkoutRecord, offerId, bundles, sourceId, out var quote, out failureMessage)) {
            return false;
        }

        if(!ApplyRefund(shop, checkoutRecord, quote, out failureMessage)) {
            policy.PublishBlocked(quote, player, shop, sourceId, failureMessage);
            return false;
        }

        refundRecord = new ShopRefundRecord {
            recordId = Guid.NewGuid().ToString("N"),
            checkoutRecordId = checkoutRecord.recordId,
            shopId = checkoutRecord.shopId,
            catalogId = checkoutRecord.catalogId,
            shopName = checkoutRecord.shopName,
            sourceId = NormalizeSourceId(sourceId),
            returnPolicyId = quote.returnPolicyId,
            returnPolicyName = quote.returnPolicyName,
            grossRefund = quote.grossRefund,
            restockingFee = quote.restockingFee,
            amountRefunded = quote.amountRefunded,
            requestedOfferId = quote.requestedOfferId,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            lines = quote.lines.Where(line => line != null).Select(line => line.Clone()).ToList()
        };

        refundRecords.Add(refundRecord);
        TrimHistory();
        OnRefundRecorded?.Invoke(refundRecord);
        policy.PublishRefunded(quote, player, shop, sourceId);
        PublishRefundEvent(refundRecord);
        failureMessage = null;
        return true;
    }

    bool ApplyRefund(ShopCatalog shop, ShopBasketCheckoutRecord checkoutRecord, ShopRefundQuote quote, out string failureMessage) {
        failureMessage = null;
        if(quote.removeItemsFromInventory) {
            var inventory = GetComponent<Inventory>() ?? Inventory.GetInventory();
            if(inventory == null) {
                failureMessage = "Player has no inventory for returned items.";
                return false;
            }

            foreach(var line in quote.lines) {
                if(line == null) {
                    continue;
                }

                var entry = shop != null ? shop.FindOffer(line.offerId) : null;
                var item = entry != null ? entry.GetItem() : null;
                if(item == null) {
                    failureMessage = $"Cannot resolve returned item for offer '{line.offerId}'.";
                    return false;
                }

                if(!inventory.HasItemEnough(item, line.itemCount)) {
                    failureMessage = $"You do not have enough {line.itemName} to refund.";
                    return false;
                }
            }

            foreach(var line in quote.lines) {
                var entry = shop.FindOffer(line.offerId);
                var item = entry.GetItem();
                inventory.RemoveItem(item, line.itemCount);

                if(quote.restoreLimitedStock && entry.HasStockLimit) {
                    GetComponent<PlayerShopLedger>()?.RecordReturn(checkoutRecord.shopId, entry.OfferId, entry.stockLimitPeriod, line.bundles);
                }
            }
        }

        if(quote.amountRefunded > 0f) {
            Wallet.i?.AddMoney(quote.amountRefunded);
        }

        return true;
    }

    void TrimHistory() {
        if(maxRefundRecords <= 0 || refundRecords.Count <= maxRefundRecords) {
            return;
        }

        refundRecords = refundRecords
            .Where(record => record != null)
            .OrderByDescending(record => record.absoluteHour)
            .Take(maxRefundRecords)
            .OrderBy(record => record.absoluteHour)
            .ToList();
    }

    void PublishRefundEvent(ShopRefundRecord record) {
        if(record == null) {
            return;
        }

        GameEventPublishing.PublishOptional(
            null,
            $"shop.refund.record.{record.shopId}.{record.recordId}",
            $"{record.shopName} refund recorded.",
            GameEventCategory.Shop,
            GameEventImportance.Success,
            this,
            "PlayerShopReceiptLog",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("shopId", record.shopId),
            GameEventPublishing.Value("shopName", record.shopName),
            GameEventPublishing.Value("checkoutRecordId", record.checkoutRecordId),
            GameEventPublishing.Value("amountRefunded", record.amountRefunded),
            GameEventPublishing.Value("lineCount", record.lines != null ? record.lines.Count : 0));
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "shop-refund" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerShopReceiptLogSaveData {
            refundRecords = refundRecords.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerShopReceiptLogSaveData;
        refundRecords = saveData?.refundRecords?.Where(record => record != null).Select(record => record.Clone()).ToList()
            ?? new List<ShopRefundRecord>();
        TrimHistory();
    }
}

public class ShopRefundSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id written into refund records. Empty uses return policy id or this GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Readable source name for debug/future UI. Empty uses return policy display name or this GameObject name.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Refund")]
    [Tooltip("Shop whose receipts and catalog entries this refund source uses.")]
    [SerializeField] ShopCatalog shop;
    [Tooltip("Return policy that validates and prices refunds.")]
    [SerializeField] ShopReturnPolicyDefinition returnPolicy;
    [Tooltip("Optional explicit player. Empty uses the triggering/interacting player or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("Checkout record id to refund. Empty can use the latest checkout when Use Latest Checkout If Empty is enabled.")]
    [SerializeField] string checkoutRecordId = string.Empty;
    [Tooltip("Optional offer id to refund from the receipt. Empty refunds the whole receipt when policy allows it.")]
    [SerializeField] string offerId = string.Empty;
    [Tooltip("Requested bundle count for a line refund. 0 refunds all available bundles for the selected line.")]
    [Min(0)]
    [SerializeField] int bundles;
    [Tooltip("If enabled and Checkout Record Id is empty, the latest checkout for this shop is used.")]
    [SerializeField] bool useLatestCheckoutIfEmpty = true;
    [Tooltip("Action applied when this source is triggered.")]
    [SerializeField] ShopRefundSourceAction triggerAction = ShopRefundSourceAction.Refund;
    [Tooltip("Action applied when an Interactable flow calls Interact.")]
    [SerializeField] ShopRefundSourceAction interactAction = ShopRefundSourceAction.Refund;
    [Tooltip("If enabled, player trigger applies Trigger Action.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Feedback")]
    [Tooltip("If enabled, result text is shown through the existing DialogManager when available.")]
    [SerializeField] bool showDialogFeedback;
    [Tooltip("If enabled, blocked refund actions are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful refund actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public string SourceId => ResolveSourceId();
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : returnPolicy != null ? returnPolicy.DisplayName : gameObject.name;
    public ShopCatalog Shop => shop;
    public ShopReturnPolicyDefinition ReturnPolicy => returnPolicy;
    public ShopRefundSourceAction TriggerAction => triggerAction;
    public ShopRefundSourceAction InteractAction => interactAction;

    public void OnPlayerTriggered(PlayerController player) {
        if(!applyOnPlayerTrigger) {
            return;
        }

        ApplyAction(triggerAction, ResolvePlayer(player), out _);
    }

    public IEnumerator Interact(Transform initiator) {
        var player = ResolvePlayer(initiator != null ? initiator.GetComponent<PlayerController>() : null);
        ApplyAction(interactAction, player, out var feedback);
        if(showDialogFeedback && DialogManager.i != null && !string.IsNullOrWhiteSpace(feedback)) {
            yield return DialogManager.i.ShowDialogText(feedback);
        }
    }

    public bool TryPreviewRefund(PlayerController player, out ShopRefundQuote quote, out string failureMessage) {
        player = ResolvePlayer(player);
        quote = null;
        if(!ResolveRefundContext(player, out var basketLog, out var receiptLog, out var recordId, out failureMessage)) {
            RecordBlocked(player, failureMessage, null);
            return false;
        }

        var checkoutRecord = basketLog.FindCheckoutRecord(recordId);
        if(!returnPolicy.TryBuildRefundQuote(player, shop, basketLog, receiptLog, checkoutRecord, offerId, bundles, ResolveSourceId(), out quote, out failureMessage)) {
            RecordBlocked(player, failureMessage, quote);
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryRefund(PlayerController player, out ShopRefundRecord record, out string failureMessage) {
        player = ResolvePlayer(player);
        record = null;
        if(!ResolveRefundContext(player, out _, out var receiptLog, out var recordId, out failureMessage)) {
            RecordBlocked(player, failureMessage, null);
            return false;
        }

        if(!receiptLog.TryRefund(returnPolicy, shop, recordId, offerId, bundles, ResolveSourceId(), out record, out failureMessage)) {
            RecordBlocked(player, failureMessage, null);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{DisplayName} refunded {record.amountRefunded:0}.", GameDebugCategory.Shop, this, "ShopRefundSource");
        }

        return true;
    }

    bool ApplyAction(ShopRefundSourceAction action, PlayerController player, out string feedback) {
        feedback = null;
        if(action == ShopRefundSourceAction.PreviewRefund) {
            if(TryPreviewRefund(player, out var quote, out feedback)) {
                feedback = quote != null ? quote.BuildSummary() : "Refund quote ready.";
                return true;
            }
            return false;
        }

        if(TryRefund(player, out var record, out feedback)) {
            feedback = record != null ? $"Refund completed for {record.amountRefunded:0}." : "Refund completed.";
            return true;
        }

        return false;
    }

    bool ResolveRefundContext(PlayerController player, out PlayerShopBasketLog basketLog, out PlayerShopReceiptLog receiptLog, out string recordId, out string failureMessage) {
        basketLog = null;
        receiptLog = null;
        recordId = checkoutRecordId;
        if(player == null) {
            failureMessage = "A player is required for refunds.";
            return false;
        }

        if(shop == null) {
            failureMessage = "Refund source has no shop assigned.";
            return false;
        }

        if(returnPolicy == null) {
            failureMessage = "Refund source has no return policy assigned.";
            return false;
        }

        basketLog = player.GetComponent<PlayerShopBasketLog>();
        if(basketLog == null) {
            failureMessage = "Player has no checkout receipt history.";
            return false;
        }

        if(string.IsNullOrWhiteSpace(recordId) && useLatestCheckoutIfEmpty) {
            recordId = basketLog.GetLatestCheckoutRecord(shop.ShopId)?.recordId;
        }

        if(string.IsNullOrWhiteSpace(recordId)) {
            failureMessage = "No checkout receipt id was selected.";
            return false;
        }

        receiptLog = player.GetComponent<PlayerShopReceiptLog>() ?? player.gameObject.AddComponent<PlayerShopReceiptLog>();
        failureMessage = null;
        return true;
    }

    void RecordBlocked(PlayerController player, string failureMessage, ShopRefundQuote quote) {
        returnPolicy?.PublishBlocked(quote, player, shop, ResolveSourceId(), failureMessage);
        if(logBlockedAttempts && !string.IsNullOrWhiteSpace(failureMessage)) {
            GameDebug.Warning(failureMessage, GameDebugCategory.Shop, player != null ? player : this, "ShopRefundSource");
        }
    }

    PlayerController ResolvePlayer(PlayerController player) {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(player != null) {
            return player;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }

    string ResolveSourceId() {
        if(!string.IsNullOrWhiteSpace(sourceId)) {
            return sourceId;
        }

        if(returnPolicy != null) {
            return $"refund:{returnPolicy.Id}";
        }

        return gameObject.name;
    }
}

[Serializable]
public class ShopRefundQuote {
    [Tooltip("Return policy id that produced this quote.")]
    public string returnPolicyId;
    [Tooltip("Return policy display name that produced this quote.")]
    public string returnPolicyName;
    [Tooltip("Checkout record id being refunded.")]
    public string checkoutRecordId;
    [Tooltip("Shop id copied from the receipt.")]
    public string shopId;
    [Tooltip("Source id requesting the refund.")]
    public string sourceId;
    [Tooltip("Optional requested offer id. Empty means full receipt refund.")]
    public string requestedOfferId;
    [Tooltip("Gross refund before restocking fee and policy percentage.")]
    public float grossRefund;
    [Tooltip("Restocking fee subtracted from this refund.")]
    public float restockingFee;
    [Tooltip("Final money returned to Wallet.")]
    public float amountRefunded;
    [Tooltip("If enabled, refunded items are removed from inventory.")]
    public bool removeItemsFromInventory;
    [Tooltip("If enabled, stock-limited returned offers reduce the player's purchased stock count.")]
    public bool restoreLimitedStock;
    [Tooltip("Short readable refund quote message.")]
    public string message;
    [Tooltip("Line-level refund details.")]
    public List<ShopRefundLineRecord> lines = new List<ShopRefundLineRecord>();

    public string BuildSummary() {
        return $"{returnPolicyName}: gross {grossRefund:0}, fee {restockingFee:0}, refund {amountRefunded:0}.";
    }
}

[Serializable]
public class ShopRefundRecord {
    [Tooltip("Unique runtime/save id for this refund.")]
    public string recordId;
    [Tooltip("Checkout record id refunded by this record.")]
    public string checkoutRecordId;
    [Tooltip("Shop instance id copied from the receipt.")]
    public string shopId;
    [Tooltip("Catalog definition id copied from the receipt.")]
    public string catalogId;
    [Tooltip("Shop display name copied from the receipt.")]
    public string shopName;
    [Tooltip("Source id that requested this refund.")]
    public string sourceId;
    [Tooltip("Return policy id used by this refund.")]
    public string returnPolicyId;
    [Tooltip("Return policy display name used by this refund.")]
    public string returnPolicyName;
    [Tooltip("Optional requested offer id. Empty means full receipt refund.")]
    public string requestedOfferId;
    [Tooltip("Gross refund before restocking fee and policy percentage.")]
    public float grossRefund;
    [Tooltip("Restocking fee subtracted from this refund.")]
    public float restockingFee;
    [Tooltip("Final money returned to Wallet.")]
    public float amountRefunded;
    [Tooltip("In-game day when refund occurred.")]
    public int day;
    [Tooltip("Absolute in-game hour when refund occurred.")]
    public int absoluteHour;
    [Tooltip("Line-level refund details.")]
    public List<ShopRefundLineRecord> lines = new List<ShopRefundLineRecord>();

    public ShopRefundRecord Clone() {
        return new ShopRefundRecord {
            recordId = recordId,
            checkoutRecordId = checkoutRecordId,
            shopId = shopId,
            catalogId = catalogId,
            shopName = shopName,
            sourceId = sourceId,
            returnPolicyId = returnPolicyId,
            returnPolicyName = returnPolicyName,
            requestedOfferId = requestedOfferId,
            grossRefund = grossRefund,
            restockingFee = restockingFee,
            amountRefunded = amountRefunded,
            day = day,
            absoluteHour = absoluteHour,
            lines = lines != null ? lines.Where(line => line != null).Select(line => line.Clone()).ToList() : new List<ShopRefundLineRecord>()
        };
    }
}

[Serializable]
public class ShopRefundLineRecord {
    [Tooltip("Offer id refunded from the receipt.")]
    public string offerId;
    [Tooltip("Inventory item asset id/name copied from the receipt.")]
    public string itemId;
    [Tooltip("Display name copied from the receipt.")]
    public string itemName;
    [Tooltip("Number of purchase bundles refunded.")]
    public int bundles;
    [Tooltip("How many inventory items each bundle contained.")]
    public int itemsPerBundle;
    [Tooltip("Total inventory item count removed by this refund line.")]
    public int itemCount;
    [Tooltip("Unit bundle price copied from the receipt line.")]
    public float unitBundlePrice;
    [Tooltip("Gross refund for this line before policy reductions.")]
    public float grossRefund;

    public ShopRefundLineRecord Clone() {
        return new ShopRefundLineRecord {
            offerId = offerId,
            itemId = itemId,
            itemName = itemName,
            bundles = Mathf.Max(0, bundles),
            itemsPerBundle = Mathf.Max(1, itemsPerBundle),
            itemCount = Mathf.Max(0, itemCount),
            unitBundlePrice = unitBundlePrice,
            grossRefund = grossRefund
        };
    }
}

[Serializable]
public class PlayerShopReceiptLogSaveData {
    public List<ShopRefundRecord> refundRecords;
}
