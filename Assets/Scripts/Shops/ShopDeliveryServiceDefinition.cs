using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ShopDeliveryCourierType {
    Pidgey,
    Pelipper,
    BikeCourier,
    TrainerCourier,
    TransitCourier,
    Custom
}

public enum ShopDeliveryFulfillmentMode {
    AutoToInventoryWhenDue,
    ClaimAtDestination
}

public enum ShopDeliverySourceAction {
    PreviewOrder,
    PlaceOrder,
    ClaimDueDeliveries,
    CancelLatestPending
}

[CreateAssetMenu(menuName = "Shops/Delivery Service Definition")]
public class ShopDeliveryServiceDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this delivery service. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future delivery UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation of how this delivery service works.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Courier flavor used by UI, filtering and balancing.")]
    [SerializeField] ShopDeliveryCourierType courierType = ShopDeliveryCourierType.Pidgey;
    [Tooltip("Optional icon used by future delivery UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as pidgey, premium, city, rural, fast, shop, mart or region name.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Catalog Filters")]
    [Tooltip("If assigned, this delivery service only works with this exact shop catalog.")]
    [SerializeField] ShopCatalogDefinition requiredCatalog;
    [Tooltip("If not empty, this delivery service only works with these catalog types.")]
    [SerializeField] List<ShopCatalogType> allowedCatalogTypes = new List<ShopCatalogType>();
    [Tooltip("Catalog tags required before this delivery service can be used. Empty means no catalog tag filter.")]
    [SerializeField] List<string> requiredCatalogTags = new List<string>();
    [Tooltip("How required catalog tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode catalogTagMatchMode = ConsequenceRequirementMatchMode.All;

    [Header("Basket Limits")]
    [Tooltip("Maximum basket line count accepted by this service. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxLineCount;
    [Tooltip("Maximum total bundle count accepted by this service. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxBundleCount;
    [Tooltip("Minimum item subtotal required before delivery can be used. 0 means no minimum.")]
    [Min(0f)]
    [SerializeField] float minimumSubtotal;
    [Tooltip("Maximum item subtotal accepted by this service. 0 means unlimited.")]
    [Min(0f)]
    [SerializeField] float maximumSubtotal;
    [Tooltip("If enabled, the active basket is cleared after a delivery order is placed.")]
    [SerializeField] bool clearBasketOnOrder = true;

    [Header("Destination")]
    [Tooltip("How this order is fulfilled after the delivery time passes.")]
    [SerializeField] ShopDeliveryFulfillmentMode fulfillmentMode = ShopDeliveryFulfillmentMode.AutoToInventoryWhenDue;
    [Tooltip("If enabled, destination id must not be empty.")]
    [SerializeField] bool requireDestinationId;
    [Tooltip("If assigned, delivery is accepted only for this exact destination marker.")]
    [SerializeField] MapMarkerDefinition requiredDestinationMarker;
    [Tooltip("If assigned, delivery is accepted only for this destination region.")]
    [SerializeField] RegionInfoDefinition requiredDestinationRegion;
    [Tooltip("Destination tags required before delivery can be used. Source destination tags can come from marker, region or source custom tags.")]
    [SerializeField] List<string> requiredDestinationTags = new List<string>();
    [Tooltip("How required destination tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode destinationTagMatchMode = ConsequenceRequirementMatchMode.All;

    [Header("Timing")]
    [Tooltip("Base in-game hours before the delivery becomes available.")]
    [Min(0)]
    [SerializeField] int baseDeliveryHours = 3;
    [Tooltip("Additional in-game hours added per purchased bundle.")]
    [Min(0f)]
    [SerializeField] float hoursPerBundle;
    [Tooltip("Minimum in-game delivery duration after all calculations.")]
    [Min(0)]
    [SerializeField] int minimumDeliveryHours = 1;
    [Tooltip("If enabled, calculated delivery hours are rounded up.")]
    [SerializeField] bool roundDeliveryHoursUp = true;

    [Header("Pricing")]
    [Tooltip("Flat delivery fee added to item subtotal.")]
    [Min(0f)]
    [SerializeField] float flatDeliveryFee = 10f;
    [Tooltip("Percentage delivery fee added from item subtotal. 0.10 means +10%.")]
    [Min(0f)]
    [SerializeField] float percentageDeliveryFee;
    [Tooltip("If subtotal reaches this value, delivery fee is waived. 0 disables free-delivery threshold.")]
    [Min(0f)]
    [SerializeField] float freeDeliveryMinimumSubtotal;
    [Tooltip("If enabled, final delivery fee and total due are rounded up.")]
    [SerializeField] bool roundFeeUp = true;
    [Tooltip("If enabled, Wallet funds are checked and charged when placing an order.")]
    [SerializeField] bool chargeWalletOnOrder = true;

    [Header("Cancellation")]
    [Tooltip("If enabled, pending orders can be cancelled before delivery.")]
    [SerializeField] bool allowCancellation = true;
    [Tooltip("In-game hours after placing the order during which cancellation is allowed. 0 means until delivery is due.")]
    [Min(0)]
    [SerializeField] int cancellationWindowHours = 1;
    [Tooltip("Percentage of paid total refunded on cancellation. 1.0 means full refund.")]
    [Range(0f, 1f)]
    [SerializeField] float cancellationRefundPercent = 0.75f;
    [Tooltip("If enabled, stock-limited offers are restored when a pending delivery is cancelled.")]
    [SerializeField] bool restoreLimitedStockOnCancel = true;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this delivery service can be used.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this delivery service can be used.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this delivery service.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional world event whose active state gates this delivery service.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for Required World Event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("How extra requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this delivery service can be used.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this delivery service is locked.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This delivery service is not available.";

    [Header("Events")]
    [Tooltip("Optional event published when an order is placed.")]
    [SerializeField] GameEventDefinition orderPlacedEvent;
    [Tooltip("Optional event published when an order is delivered.")]
    [SerializeField] GameEventDefinition deliveredEvent;
    [Tooltip("Optional event published when an order is cancelled.")]
    [SerializeField] GameEventDefinition cancelledEvent;
    [Tooltip("Optional event published when delivery is blocked.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, delivery events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, delivery events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public ShopDeliveryCourierType CourierType => courierType;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public ShopCatalogDefinition RequiredCatalog => requiredCatalog;
    public IReadOnlyList<ShopCatalogType> AllowedCatalogTypes => allowedCatalogTypes != null ? (IReadOnlyList<ShopCatalogType>)allowedCatalogTypes : Array.Empty<ShopCatalogType>();
    public IReadOnlyList<string> RequiredCatalogTags => requiredCatalogTags != null ? (IReadOnlyList<string>)requiredCatalogTags : Array.Empty<string>();
    public int MaxLineCount => Mathf.Max(0, maxLineCount);
    public int MaxBundleCount => Mathf.Max(0, maxBundleCount);
    public float MinimumSubtotal => Mathf.Max(0f, minimumSubtotal);
    public float MaximumSubtotal => Mathf.Max(0f, maximumSubtotal);
    public bool ClearBasketOnOrder => clearBasketOnOrder;
    public ShopDeliveryFulfillmentMode FulfillmentMode => fulfillmentMode;
    public bool RequireDestinationId => requireDestinationId;
    public MapMarkerDefinition RequiredDestinationMarker => requiredDestinationMarker;
    public RegionInfoDefinition RequiredDestinationRegion => requiredDestinationRegion;
    public IReadOnlyList<string> RequiredDestinationTags => requiredDestinationTags != null ? (IReadOnlyList<string>)requiredDestinationTags : Array.Empty<string>();
    public int BaseDeliveryHours => Mathf.Max(0, baseDeliveryHours);
    public float HoursPerBundle => Mathf.Max(0f, hoursPerBundle);
    public int MinimumDeliveryHours => Mathf.Max(0, minimumDeliveryHours);
    public float FlatDeliveryFee => Mathf.Max(0f, flatDeliveryFee);
    public float PercentageDeliveryFee => Mathf.Max(0f, percentageDeliveryFee);
    public float FreeDeliveryMinimumSubtotal => Mathf.Max(0f, freeDeliveryMinimumSubtotal);
    public bool ChargeWalletOnOrder => chargeWalletOnOrder;
    public bool AllowCancellation => allowCancellation;
    public int CancellationWindowHours => Mathf.Max(0, cancellationWindowHours);
    public float CancellationRefundPercent => Mathf.Clamp01(cancellationRefundPercent);
    public bool RestoreLimitedStockOnCancel => restoreLimitedStockOnCancel;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public bool TryBuildQuote(
        PlayerController player,
        ShopCatalog shop,
        PlayerShopBasketLog basketLog,
        ShopDeliveryContext context,
        out ShopDeliveryQuote quote,
        out string failureMessage
    ) {
        quote = null;
        if(!CanUse(player, shop, basketLog, context, out failureMessage)) {
            PublishDeliveryEvent(blockedEvent, "blocked", null, player, shop, context, GameEventImportance.Warning, failureMessage);
            return false;
        }

        var lines = BuildOrderLines(player, shop, basketLog, out failureMessage);
        if(lines.Count == 0) {
            failureMessage ??= "The delivery basket is empty.";
            PublishDeliveryEvent(blockedEvent, "blocked", null, player, shop, context, GameEventImportance.Warning, failureMessage);
            return false;
        }

        float itemSubtotal = lines.Sum(line => Mathf.Max(0f, line.lineTotal));
        int lineCount = lines.Count;
        int bundleCount = lines.Sum(line => Mathf.Max(0, line.bundles));
        if(!ValidateBasketTotals(itemSubtotal, lineCount, bundleCount, out failureMessage)) {
            PublishDeliveryEvent(blockedEvent, "blocked", null, player, shop, context, GameEventImportance.Warning, failureMessage);
            return false;
        }

        float deliveryFee = CalculateDeliveryFee(itemSubtotal);
        float totalDue = itemSubtotal + deliveryFee;
        if(roundFeeUp) {
            deliveryFee = Mathf.Ceil(deliveryFee);
            totalDue = Mathf.Ceil(totalDue);
        }

        int durationHours = CalculateDeliveryHours(bundleCount);
        int currentHour = GetCurrentAbsoluteHour();
        quote = new ShopDeliveryQuote {
            serviceId = Id,
            serviceName = DisplayName,
            courierType = courierType,
            fulfillmentMode = fulfillmentMode,
            shopId = shop.ShopId,
            catalogId = shop.Catalog.Id,
            shopName = shop.Catalog.DisplayName,
            sourceId = context != null ? context.SourceId : Id,
            destinationId = context != null ? context.DestinationId : string.Empty,
            destinationName = context != null ? context.DestinationName : string.Empty,
            destinationRegionId = context != null && context.Region != null ? context.Region.Id : string.Empty,
            destinationRegionName = context != null && context.Region != null ? context.Region.DisplayName : string.Empty,
            itemSubtotal = itemSubtotal,
            deliveryFee = deliveryFee,
            totalDue = totalDue,
            deliveryHours = durationHours,
            placedAbsoluteHour = currentHour,
            dueAbsoluteHour = currentHour + durationHours,
            cancellationDeadlineAbsoluteHour = GetCancellationDeadline(currentHour, currentHour + durationHours),
            cancellationRefundAmount = Mathf.Floor(totalDue * CancellationRefundPercent),
            allowCancellation = allowCancellation,
            restoreLimitedStockOnCancel = restoreLimitedStockOnCancel,
            lines = lines
        };

        if(chargeWalletOnOrder && totalDue > 0f && (Wallet.i == null || !Wallet.i.HasMoney(totalDue))) {
            failureMessage = $"You need {totalDue:0} money for {DisplayName}.";
            PublishDeliveryEvent(blockedEvent, "blocked", quote, player, shop, context, GameEventImportance.Warning, failureMessage);
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryPlaceOrder(
        PlayerController player,
        ShopCatalog shop,
        PlayerShopBasketLog basketLog,
        PlayerDeliveryLog deliveryLog,
        ShopDeliveryContext context,
        out ShopDeliveryOrderRecord record,
        out string failureMessage
    ) {
        record = null;
        if(!TryBuildQuote(player, shop, basketLog, context, out var quote, out failureMessage)) {
            return false;
        }

        if(chargeWalletOnOrder && quote.totalDue > 0f) {
            Wallet.i.TakeMoney(quote.totalDue);
        }

        var ledger = player.GetComponent<PlayerShopLedger>() ?? player.gameObject.AddComponent<PlayerShopLedger>();
        foreach(var line in quote.lines) {
            if(line != null && line.hasStockLimit) {
                ledger.RecordPurchase(quote.shopId, line.offerId, line.stockPeriod, line.bundles);
            }
        }

        if(clearBasketOnOrder) {
            basketLog?.ClearBasket(quote.sourceId);
        }

        deliveryLog ??= player.gameObject.AddComponent<PlayerDeliveryLog>();
        record = deliveryLog.RecordOrder(this, quote, context);
        PublishDeliveryEvent(orderPlacedEvent, "ordered", quote, player, shop, context, GameEventImportance.Success, null);
        failureMessage = null;
        return true;
    }

    public bool CanUse(PlayerController player, ShopCatalog shop, PlayerShopBasketLog basketLog, ShopDeliveryContext context, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for delivery orders.";
            return false;
        }

        if(shop == null || shop.Catalog == null) {
            failureMessage = "A shop catalog is required for delivery orders.";
            return false;
        }

        if(basketLog == null || !basketLog.HasActiveBasket || basketLog.GetLineCount() == 0) {
            failureMessage = "The delivery basket is empty.";
            return false;
        }

        if(basketLog.ActiveBasket.shopId != shop.ShopId) {
            failureMessage = $"Basket belongs to {basketLog.ActiveBasket.shopName}, not {shop.Catalog.DisplayName}.";
            return false;
        }

        if(requiredCatalog != null && shop.Catalog != requiredCatalog) {
            failureMessage = $"{DisplayName} cannot deliver from this shop.";
            return false;
        }

        if(allowedCatalogTypes.Count > 0 && !allowedCatalogTypes.Contains(shop.Catalog.CatalogType)) {
            failureMessage = $"{DisplayName} cannot deliver from this shop type.";
            return false;
        }

        if(!MatchesTags(requiredCatalogTags, catalogTagMatchMode, tag => shop.Catalog.HasTag(tag))) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not available for this shop." : lockedMessage;
            return false;
        }

        if(!CanDeliverTo(context, out failureMessage)) {
            return false;
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

        var inventory = player.GetComponent<Inventory>();
        if(inventory == null) {
            failureMessage = "The player has no inventory for delivered items.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool CanDeliverTo(ShopDeliveryContext context, out string failureMessage) {
        if(requireDestinationId && (context == null || string.IsNullOrWhiteSpace(context.DestinationId))) {
            failureMessage = $"{DisplayName} requires a delivery destination.";
            return false;
        }

        if(requiredDestinationMarker != null && (context == null || context.Marker != requiredDestinationMarker)) {
            failureMessage = $"{DisplayName} cannot deliver to this marker.";
            return false;
        }

        if(requiredDestinationRegion != null && (context == null || context.Region != requiredDestinationRegion)) {
            failureMessage = $"{DisplayName} cannot deliver to this region.";
            return false;
        }

        if(!MatchesTags(requiredDestinationTags, destinationTagMatchMode, tag => context != null && context.HasDestinationTag(tag))) {
            failureMessage = $"{DisplayName} cannot deliver to this destination.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    List<ShopDeliveryOrderLine> BuildOrderLines(PlayerController player, ShopCatalog shop, PlayerShopBasketLog basketLog, out string failureMessage) {
        failureMessage = null;
        var lines = new List<ShopDeliveryOrderLine>();
        if(shop == null || basketLog == null || !basketLog.HasActiveBasket) {
            failureMessage = "No delivery basket is available.";
            return lines;
        }

        if(!shop.Catalog.IsUnlocked(player, out failureMessage)) {
            return lines;
        }

        var ledger = player.GetComponent<PlayerShopLedger>() ?? player.gameObject.AddComponent<PlayerShopLedger>();
        foreach(var basketLine in basketLog.ActiveBasket.lines) {
            if(basketLine == null) {
                continue;
            }

            var entry = shop.FindOffer(basketLine.offerId);
            if(entry == null) {
                failureMessage = $"Shop no longer contains offer '{basketLine.offerId}'.";
                return new List<ShopDeliveryOrderLine>();
            }

            if(entry.GetItem() == null) {
                failureMessage = $"{entry.DisplayName} has no item to deliver.";
                return new List<ShopDeliveryOrderLine>();
            }

            if(!entry.IsUnlocked(player, out failureMessage)) {
                return new List<ShopDeliveryOrderLine>();
            }

            int bundles = Mathf.Max(1, basketLine.bundles);
            if(entry.HasStockLimit && !entry.HasEnoughStock(shop.ShopId, ledger, bundles)) {
                failureMessage = $"{entry.DisplayName} is out of stock.";
                return new List<ShopDeliveryOrderLine>();
            }

            int itemCount = entry.GetBundleCount() * bundles;
            float lineTotal = shop.GetBuyPrice(player, entry, bundles);
            var item = entry.GetItem();
            lines.Add(new ShopDeliveryOrderLine {
                offerId = entry.OfferId,
                offerName = entry.DisplayName,
                itemAssetName = item.name,
                itemName = item.Name,
                bundles = bundles,
                itemsPerBundle = entry.GetBundleCount(),
                itemCount = itemCount,
                unitBundlePrice = bundles > 0 ? Mathf.Max(0f, lineTotal / bundles) : lineTotal,
                lineTotal = lineTotal,
                hasStockLimit = entry.HasStockLimit,
                stockPeriod = entry.stockLimitPeriod
            });
        }

        failureMessage = null;
        return lines;
    }

    bool ValidateBasketTotals(float itemSubtotal, int lineCount, int bundleCount, out string failureMessage) {
        if(MaxLineCount > 0 && lineCount > MaxLineCount) {
            failureMessage = $"{DisplayName} accepts at most {MaxLineCount} basket line(s).";
            return false;
        }

        if(MaxBundleCount > 0 && bundleCount > MaxBundleCount) {
            failureMessage = $"{DisplayName} accepts at most {MaxBundleCount} bundle(s).";
            return false;
        }

        if(MinimumSubtotal > 0f && itemSubtotal < MinimumSubtotal) {
            failureMessage = $"{DisplayName} requires at least {MinimumSubtotal:0} money in the basket.";
            return false;
        }

        if(MaximumSubtotal > 0f && itemSubtotal > MaximumSubtotal) {
            failureMessage = $"{DisplayName} accepts baskets up to {MaximumSubtotal:0} money.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    float CalculateDeliveryFee(float itemSubtotal) {
        if(freeDeliveryMinimumSubtotal > 0f && itemSubtotal >= freeDeliveryMinimumSubtotal) {
            return 0f;
        }

        return Mathf.Max(0f, flatDeliveryFee + itemSubtotal * percentageDeliveryFee);
    }

    int CalculateDeliveryHours(int bundleCount) {
        float hours = BaseDeliveryHours + Mathf.Max(0, bundleCount) * HoursPerBundle;
        int rounded = roundDeliveryHoursUp ? Mathf.CeilToInt(hours) : Mathf.FloorToInt(hours);
        return Mathf.Max(MinimumDeliveryHours, rounded);
    }

    int GetCancellationDeadline(int placedHour, int dueHour) {
        if(!allowCancellation) {
            return placedHour;
        }

        if(cancellationWindowHours <= 0) {
            return dueHour;
        }

        return Mathf.Min(dueHour, placedHour + cancellationWindowHours);
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

    internal void PublishDelivered(ShopDeliveryOrderRecord record, PlayerController player, UnityEngine.Object context) {
        PublishDeliveryEvent(deliveredEvent, "delivered", record != null ? record.ToEventSnapshot() : null, player, null, null, GameEventImportance.Success, null, context);
    }

    internal void PublishCancelled(ShopDeliveryOrderRecord record, PlayerController player, UnityEngine.Object context) {
        PublishDeliveryEvent(cancelledEvent, "cancelled", record != null ? record.ToEventSnapshot() : null, player, null, null, GameEventImportance.Warning, null, context);
    }

    void PublishDeliveryEvent(GameEventDefinition eventDefinition, string phase, ShopDeliveryQuote quote, PlayerController player, ShopCatalog shop, ShopDeliveryContext context, GameEventImportance importance, string failureMessage) {
        PublishDeliveryEvent(eventDefinition, phase, quote != null ? quote.ToEventSnapshot() : null, player, shop, context, importance, failureMessage, this);
    }

    void PublishDeliveryEvent(GameEventDefinition eventDefinition, string phase, ShopDeliveryEventSnapshot snapshot, PlayerController player, ShopCatalog shop, ShopDeliveryContext context, GameEventImportance importance, string failureMessage, UnityEngine.Object eventContext) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"shop.delivery.{phase}.{Id}.{snapshot?.shopId ?? shop?.ShopId ?? "shop"}",
            !string.IsNullOrWhiteSpace(failureMessage) ? failureMessage : $"{DisplayName} delivery {phase}.",
            GameEventCategory.Shop,
            importance,
            eventContext != null ? eventContext : player != null ? player : this,
            "ShopDeliveryServiceDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("deliveryServiceId", Id),
            GameEventPublishing.Value("deliveryServiceName", DisplayName),
            GameEventPublishing.Value("courierType", courierType),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("shopId", snapshot?.shopId ?? shop?.ShopId ?? string.Empty),
            GameEventPublishing.Value("sourceId", snapshot?.sourceId ?? context?.SourceId ?? string.Empty),
            GameEventPublishing.Value("destinationId", snapshot?.destinationId ?? context?.DestinationId ?? string.Empty),
            GameEventPublishing.Value("itemSubtotal", snapshot?.itemSubtotal ?? 0f),
            GameEventPublishing.Value("deliveryFee", snapshot?.deliveryFee ?? 0f),
            GameEventPublishing.Value("totalPaid", snapshot?.totalPaid ?? 0f),
            GameEventPublishing.Value("deliveryHours", snapshot?.deliveryHours ?? 0),
            GameEventPublishing.Value("dueAbsoluteHour", snapshot?.dueAbsoluteHour ?? 0));
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }
}

public class ShopDeliverySource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id written into delivery orders. Empty uses service id or this GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Readable source name for debug/future UI. Empty uses service display name or this GameObject name.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Delivery")]
    [Tooltip("Shop whose active basket can be converted into a delivery order.")]
    [SerializeField] ShopCatalog shop;
    [Tooltip("Delivery service used by this source.")]
    [SerializeField] ShopDeliveryServiceDefinition deliveryService;
    [Tooltip("Optional explicit player. Empty uses the triggering/interacting player or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("Action applied when this source is triggered.")]
    [SerializeField] ShopDeliverySourceAction triggerAction = ShopDeliverySourceAction.PlaceOrder;
    [Tooltip("Action applied when an Interactable flow calls Interact.")]
    [SerializeField] ShopDeliverySourceAction interactAction = ShopDeliverySourceAction.PlaceOrder;
    [Tooltip("If enabled, player trigger applies Trigger Action.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Destination")]
    [Tooltip("Destination id written into orders. Empty uses destination marker id, region id or this GameObject name.")]
    [SerializeField] string destinationId = string.Empty;
    [Tooltip("Destination display name written into orders. Empty uses marker, region or source name.")]
    [SerializeField] string destinationName = string.Empty;
    [Tooltip("Optional map marker representing the delivery destination.")]
    [SerializeField] MapMarkerDefinition destinationMarker;
    [Tooltip("Optional region representing the delivery destination.")]
    [SerializeField] RegionInfoDefinition destinationRegion;
    [Tooltip("Optional scene name associated with this destination.")]
    [SerializeField] string destinationSceneName = string.Empty;
    [Tooltip("Optional portal/spawn key associated with this destination.")]
    [SerializeField] string destinationPortalId = string.Empty;
    [Tooltip("Extra free-form destination tags used by delivery service filters.")]
    [SerializeField] List<string> destinationTags = new List<string>();

    [Header("Feedback")]
    [Tooltip("If enabled, result text is shown through the existing DialogManager when available.")]
    [SerializeField] bool showDialogFeedback;
    [Tooltip("If enabled, blocked delivery actions are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful delivery actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public string SourceId => ResolveSourceId();
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : deliveryService != null ? deliveryService.DisplayName : gameObject.name;
    public ShopCatalog Shop => shop;
    public ShopDeliveryServiceDefinition DeliveryService => deliveryService;
    public ShopDeliverySourceAction TriggerAction => triggerAction;
    public ShopDeliverySourceAction InteractAction => interactAction;
    public MapMarkerDefinition DestinationMarker => destinationMarker;
    public RegionInfoDefinition DestinationRegion => destinationRegion;
    public IReadOnlyList<string> DestinationTags => destinationTags != null ? (IReadOnlyList<string>)destinationTags : Array.Empty<string>();

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

    public bool TryPreviewOrder(PlayerController player, out ShopDeliveryQuote quote, out string failureMessage) {
        player = ResolvePlayer(player);
        quote = null;
        if(!ResolveOrderContext(player, out var basketLog, out var deliveryLog, out var context, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(!deliveryService.TryBuildQuote(player, shop, basketLog, context, out quote, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        return true;
    }

    public bool TryPlaceOrder(PlayerController player, out ShopDeliveryOrderRecord record, out string failureMessage) {
        player = ResolvePlayer(player);
        record = null;
        if(!ResolveOrderContext(player, out var basketLog, out var deliveryLog, out var context, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(!deliveryService.TryPlaceOrder(player, shop, basketLog, deliveryLog, context, out record, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{DisplayName} placed delivery order for {record.totalPaid:0}.", GameDebugCategory.Shop, this, "ShopDeliverySource");
        }

        return true;
    }

    public bool TryClaimDueDeliveries(PlayerController player, out List<ShopDeliveryOrderRecord> delivered, out string failureMessage) {
        player = ResolvePlayer(player);
        delivered = new List<ShopDeliveryOrderRecord>();
        if(player == null) {
            failureMessage = "A player is required for delivery claims.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        var deliveryLog = player.GetComponent<PlayerDeliveryLog>();
        if(deliveryLog == null) {
            failureMessage = "Player has no delivery log.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(!deliveryLog.TryDeliverDueOrders(player, ResolveDestinationId(), allowAutoOrders: false, allowClaimOrders: true, out delivered, out failureMessage, this)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{DisplayName} claimed {delivered.Count} delivery order(s).", GameDebugCategory.Shop, this, "ShopDeliverySource");
        }

        return true;
    }

    public bool TryCancelLatestPending(PlayerController player, out ShopDeliveryOrderRecord cancelled, out string failureMessage) {
        player = ResolvePlayer(player);
        cancelled = null;
        if(player == null) {
            failureMessage = "A player is required for delivery cancellation.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        var deliveryLog = player.GetComponent<PlayerDeliveryLog>();
        var latest = deliveryLog != null ? deliveryLog.GetLatestPendingOrder(shop != null ? shop.ShopId : null) : null;
        if(latest == null) {
            failureMessage = "No pending delivery order was found.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(!deliveryLog.TryCancelOrder(player, latest.orderId, out cancelled, out failureMessage, this)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{DisplayName} cancelled delivery order.", GameDebugCategory.Shop, this, "ShopDeliverySource");
        }

        return true;
    }

    bool ApplyAction(ShopDeliverySourceAction action, PlayerController player, out string feedback) {
        feedback = null;
        switch(action) {
            case ShopDeliverySourceAction.PreviewOrder:
                if(TryPreviewOrder(player, out var quote, out feedback)) {
                    feedback = quote != null ? quote.BuildSummary() : "Delivery quote ready.";
                    return true;
                }
                return false;
            case ShopDeliverySourceAction.ClaimDueDeliveries:
                if(TryClaimDueDeliveries(player, out var delivered, out feedback)) {
                    feedback = $"Claimed {delivered.Count} delivery order(s).";
                    return true;
                }
                return false;
            case ShopDeliverySourceAction.CancelLatestPending:
                if(TryCancelLatestPending(player, out var cancelled, out feedback)) {
                    feedback = cancelled != null ? $"Cancelled delivery order and refunded {cancelled.refundAmount:0}." : "Delivery order cancelled.";
                    return true;
                }
                return false;
            default:
                if(TryPlaceOrder(player, out var record, out feedback)) {
                    feedback = record != null ? $"Delivery order placed. Due in {record.deliveryHours} hour(s)." : "Delivery order placed.";
                    return true;
                }
                return false;
        }
    }

    bool ResolveOrderContext(PlayerController player, out PlayerShopBasketLog basketLog, out PlayerDeliveryLog deliveryLog, out ShopDeliveryContext context, out string failureMessage) {
        basketLog = null;
        deliveryLog = null;
        context = null;
        if(player == null) {
            failureMessage = "A player is required for delivery orders.";
            return false;
        }

        if(shop == null) {
            failureMessage = "Delivery source has no shop assigned.";
            return false;
        }

        if(deliveryService == null) {
            failureMessage = "Delivery source has no service assigned.";
            return false;
        }

        basketLog = player.GetComponent<PlayerShopBasketLog>();
        if(basketLog == null) {
            failureMessage = "Player has no active shop basket.";
            return false;
        }

        deliveryLog = player.GetComponent<PlayerDeliveryLog>() ?? player.gameObject.AddComponent<PlayerDeliveryLog>();
        context = BuildContext();
        failureMessage = null;
        return true;
    }

    ShopDeliveryContext BuildContext() {
        return new ShopDeliveryContext(
            ResolveSourceId(),
            ResolveDestinationId(),
            ResolveDestinationName(),
            destinationMarker,
            destinationRegion,
            destinationSceneName,
            destinationPortalId,
            destinationTags);
    }

    void RecordBlocked(PlayerController player, string failureMessage) {
        if(logBlockedAttempts && !string.IsNullOrWhiteSpace(failureMessage)) {
            GameDebug.Warning(failureMessage, GameDebugCategory.Shop, player != null ? player : this, "ShopDeliverySource");
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

        if(deliveryService != null) {
            return $"delivery:{deliveryService.Id}";
        }

        return gameObject.name;
    }

    string ResolveDestinationId() {
        if(!string.IsNullOrWhiteSpace(destinationId)) {
            return destinationId;
        }

        if(destinationMarker != null) {
            return destinationMarker.Id;
        }

        if(destinationRegion != null) {
            return destinationRegion.Id;
        }

        return gameObject.name;
    }

    string ResolveDestinationName() {
        if(!string.IsNullOrWhiteSpace(destinationName)) {
            return destinationName;
        }

        if(destinationMarker != null) {
            return destinationMarker.DisplayName;
        }

        if(destinationRegion != null) {
            return destinationRegion.DisplayName;
        }

        return DisplayName;
    }
}

public class PlayerDeliveryLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum delivery order records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRecords = 200;
    [Tooltip("If enabled, due Auto To Inventory delivery orders are delivered when time changes.")]
    [SerializeField] bool autoDeliverDueOrders = true;
    [Tooltip("Runtime/save history of delivery orders.")]
    [SerializeField] List<ShopDeliveryOrderRecord> orders = new List<ShopDeliveryOrderRecord>();

    public IReadOnlyList<ShopDeliveryOrderRecord> Orders => orders;
    public event Action<ShopDeliveryOrderRecord> OnDeliveryOrderPlaced;
    public event Action<ShopDeliveryOrderRecord> OnDeliveryOrderDelivered;
    public event Action<ShopDeliveryOrderRecord> OnDeliveryOrderCancelled;
    public event Action OnDeliveryLogChanged;

    void OnEnable() {
        SubscribeToTime();
    }

    void OnDisable() {
        UnsubscribeFromTime();
    }

    public ShopDeliveryOrderRecord RecordOrder(ShopDeliveryServiceDefinition service, ShopDeliveryQuote quote, ShopDeliveryContext context) {
        if(service == null || quote == null) {
            return null;
        }

        var record = new ShopDeliveryOrderRecord(quote) {
            orderId = Guid.NewGuid().ToString("N"),
            serviceId = service.Id,
            serviceName = service.DisplayName,
            courierType = service.CourierType,
            fulfillmentMode = service.FulfillmentMode,
            sourceId = context != null ? context.SourceId : quote.sourceId,
            destinationId = context != null ? context.DestinationId : quote.destinationId,
            destinationName = context != null ? context.DestinationName : quote.destinationName,
            destinationRegionId = context != null && context.Region != null ? context.Region.Id : quote.destinationRegionId,
            destinationRegionName = context != null && context.Region != null ? context.Region.DisplayName : quote.destinationRegionName
        };

        orders.Add(record);
        TrimHistory();
        OnDeliveryOrderPlaced?.Invoke(record);
        OnDeliveryLogChanged?.Invoke();
        return record;
    }

    public bool TryDeliverDueOrders(
        PlayerController player,
        string destinationId,
        bool allowAutoOrders,
        bool allowClaimOrders,
        out List<ShopDeliveryOrderRecord> delivered,
        out string failureMessage,
        UnityEngine.Object context = null
    ) {
        delivered = new List<ShopDeliveryOrderRecord>();
        player ??= GetComponent<PlayerController>();
        if(player == null) {
            failureMessage = "A player is required to deliver orders.";
            return false;
        }

        int currentHour = GetCurrentAbsoluteHour();
        var dueOrders = orders
            .Where(order => order != null && order.IsPending && order.IsDue(currentHour))
            .Where(order => (allowAutoOrders && order.fulfillmentMode == ShopDeliveryFulfillmentMode.AutoToInventoryWhenDue)
                || (allowClaimOrders && order.fulfillmentMode == ShopDeliveryFulfillmentMode.ClaimAtDestination && order.MatchesDestination(destinationId)))
            .OrderBy(order => order.dueAbsoluteHour)
            .ToList();

        foreach(var order in dueOrders) {
            if(TryDeliverOrder(player, order.orderId, out var deliveredOrder, out _, context)) {
                delivered.Add(deliveredOrder);
            }
        }

        if(delivered.Count == 0) {
            failureMessage = "No due delivery orders were found.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryDeliverOrder(PlayerController player, string orderId, out ShopDeliveryOrderRecord deliveredOrder, out string failureMessage, UnityEngine.Object context = null) {
        deliveredOrder = null;
        player ??= GetComponent<PlayerController>();
        var order = FindOrder(orderId);
        if(order == null) {
            failureMessage = "Delivery order was not found.";
            return false;
        }

        if(!order.IsPending) {
            failureMessage = "Delivery order is not pending.";
            return false;
        }

        if(!order.IsDue(GetCurrentAbsoluteHour())) {
            failureMessage = "Delivery order is not due yet.";
            return false;
        }

        var inventory = player != null ? player.GetComponent<Inventory>() : null;
        if(inventory == null) {
            failureMessage = "The player has no inventory for delivered items.";
            return false;
        }

        foreach(var line in order.lines) {
            if(line == null) {
                continue;
            }

            var item = line.ResolveItem();
            if(item == null) {
                failureMessage = $"Could not resolve delivered item '{line.itemName}'.";
                return false;
            }
        }

        foreach(var line in order.lines) {
            if(line == null) {
                continue;
            }

            inventory.AddItem(line.ResolveItem(), Mathf.Max(1, line.itemCount));
        }

        order.delivered = true;
        order.deliveredDay = GetCurrentDay();
        order.deliveredAbsoluteHour = GetCurrentAbsoluteHour();
        deliveredOrder = order;
        OnDeliveryOrderDelivered?.Invoke(order);
        OnDeliveryLogChanged?.Invoke();
        order.ResolveService()?.PublishDelivered(order, player, context != null ? context : this);
        failureMessage = null;
        return true;
    }

    public bool TryCancelOrder(PlayerController player, string orderId, out ShopDeliveryOrderRecord cancelledOrder, out string failureMessage, UnityEngine.Object context = null) {
        cancelledOrder = null;
        player ??= GetComponent<PlayerController>();
        var order = FindOrder(orderId);
        if(order == null) {
            failureMessage = "Delivery order was not found.";
            return false;
        }

        if(!order.IsPending) {
            failureMessage = "Only pending delivery orders can be cancelled.";
            return false;
        }

        if(!order.allowCancellation) {
            failureMessage = "This delivery order cannot be cancelled.";
            return false;
        }

        int currentHour = GetCurrentAbsoluteHour();
        if(currentHour > order.cancellationDeadlineAbsoluteHour) {
            failureMessage = "The cancellation window has closed.";
            return false;
        }

        if(order.refundAmount > 0f) {
            Wallet.i?.AddMoney(order.refundAmount);
        }

        if(order.restoreLimitedStockOnCancel) {
            var ledger = player != null ? player.GetComponent<PlayerShopLedger>() : null;
            foreach(var line in order.lines) {
                if(line != null && line.hasStockLimit) {
                    ledger?.RecordReturn(order.shopId, line.offerId, line.stockPeriod, line.bundles);
                }
            }
        }

        order.cancelled = true;
        order.cancelledDay = GetCurrentDay();
        order.cancelledAbsoluteHour = currentHour;
        cancelledOrder = order;
        OnDeliveryOrderCancelled?.Invoke(order);
        OnDeliveryLogChanged?.Invoke();
        order.ResolveService()?.PublishCancelled(order, player, context != null ? context : this);
        failureMessage = null;
        return true;
    }

    public ShopDeliveryOrderRecord FindOrder(string orderId) {
        if(string.IsNullOrWhiteSpace(orderId)) {
            return null;
        }

        return orders.FirstOrDefault(order => order != null && order.orderId == orderId);
    }

    public ShopDeliveryOrderRecord GetLatestPendingOrder(string shopId = null) {
        return orders
            .Where(order => order != null && order.IsPending && (string.IsNullOrWhiteSpace(shopId) || order.shopId == shopId))
            .OrderByDescending(order => order.placedAbsoluteHour)
            .FirstOrDefault();
    }

    public int GetPendingOrderCount(string shopId = null) {
        return orders.Count(order => order != null && order.IsPending && (string.IsNullOrWhiteSpace(shopId) || order.shopId == shopId));
    }

    void HandleTimeChanged() {
        if(!autoDeliverDueOrders) {
            return;
        }

        TryDeliverDueOrders(GetComponent<PlayerController>(), null, allowAutoOrders: true, allowClaimOrders: false, out _, out _, this);
    }

    void SubscribeToTime() {
        if(TimeSystem.i == null) {
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleTimeChanged;
        TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        TimeSystem.i.OnDayChanged += HandleTimeChanged;
    }

    void UnsubscribeFromTime() {
        if(TimeSystem.i == null) {
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleTimeChanged;
    }

    void TrimHistory() {
        if(maxRecords <= 0 || orders.Count <= maxRecords) {
            return;
        }

        orders = orders
            .Where(order => order != null)
            .OrderByDescending(order => order.placedAbsoluteHour)
            .Take(maxRecords)
            .OrderBy(order => order.placedAbsoluteHour)
            .ToList();
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerDeliveryLogSaveData {
            orders = orders.Where(order => order != null).Select(order => order.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerDeliveryLogSaveData;
        orders = saveData?.orders?.Where(order => order != null).Select(order => order.Clone()).ToList()
            ?? new List<ShopDeliveryOrderRecord>();
        TrimHistory();
        OnDeliveryLogChanged?.Invoke();
    }
}

public class ShopDeliveryContext {
    public string SourceId { get; }
    public string DestinationId { get; }
    public string DestinationName { get; }
    public MapMarkerDefinition Marker { get; }
    public RegionInfoDefinition Region { get; }
    public string SceneName { get; }
    public string PortalId { get; }
    public IReadOnlyList<string> Tags => tags;

    readonly List<string> tags = new List<string>();

    public ShopDeliveryContext(
        string sourceId,
        string destinationId,
        string destinationName,
        MapMarkerDefinition marker,
        RegionInfoDefinition region,
        string sceneName,
        string portalId,
        IEnumerable<string> extraTags
    ) {
        SourceId = string.IsNullOrWhiteSpace(sourceId) ? "delivery" : sourceId;
        Marker = marker;
        Region = region;
        DestinationId = !string.IsNullOrWhiteSpace(destinationId) ? destinationId : marker != null ? marker.Id : region != null ? region.Id : string.Empty;
        DestinationName = !string.IsNullOrWhiteSpace(destinationName) ? destinationName : marker != null ? marker.DisplayName : region != null ? region.DisplayName : DestinationId;
        SceneName = sceneName ?? string.Empty;
        PortalId = portalId ?? string.Empty;
        AddTagRange(extraTags);
        AddTagRange(marker != null ? marker.Tags : null);
        AddTagRange(region != null ? region.Tags : null);
    }

    public bool HasDestinationTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    void AddTagRange(IEnumerable<string> values) {
        if(values == null) {
            return;
        }

        foreach(var value in values) {
            if(!string.IsNullOrWhiteSpace(value) && !tags.Any(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase))) {
                tags.Add(value);
            }
        }
    }
}

[Serializable]
public class ShopDeliveryQuote {
    [Tooltip("Delivery service id that produced this quote.")]
    public string serviceId;
    [Tooltip("Delivery service display name that produced this quote.")]
    public string serviceName;
    [Tooltip("Courier type used by this quote.")]
    public ShopDeliveryCourierType courierType;
    [Tooltip("How this order is fulfilled once due.")]
    public ShopDeliveryFulfillmentMode fulfillmentMode;
    [Tooltip("Shop instance id used by this quote.")]
    public string shopId;
    [Tooltip("Catalog definition id used by this quote.")]
    public string catalogId;
    [Tooltip("Shop display name copied for fallback/debug output.")]
    public string shopName;
    [Tooltip("Source id that requested this quote.")]
    public string sourceId;
    [Tooltip("Destination id used by this quote.")]
    public string destinationId;
    [Tooltip("Destination display name used by this quote.")]
    public string destinationName;
    [Tooltip("Destination region id used by this quote.")]
    public string destinationRegionId;
    [Tooltip("Destination region display name used by this quote.")]
    public string destinationRegionName;
    [Tooltip("Basket item subtotal before delivery fee.")]
    public float itemSubtotal;
    [Tooltip("Delivery fee charged by this quote.")]
    public float deliveryFee;
    [Tooltip("Total order amount due.")]
    public float totalDue;
    [Tooltip("In-game hours until this order is due.")]
    public int deliveryHours;
    [Tooltip("Absolute in-game hour when this quote was created.")]
    public int placedAbsoluteHour;
    [Tooltip("Absolute in-game hour when this order becomes due.")]
    public int dueAbsoluteHour;
    [Tooltip("Absolute in-game hour after which cancellation is blocked.")]
    public int cancellationDeadlineAbsoluteHour;
    [Tooltip("Money refunded if this order is cancelled inside the cancellation window.")]
    public float cancellationRefundAmount;
    [Tooltip("If enabled, this order can be cancelled while pending and inside the cancellation window.")]
    public bool allowCancellation;
    [Tooltip("If enabled, limited stock is restored when this order is cancelled.")]
    public bool restoreLimitedStockOnCancel;
    [Tooltip("Line-level delivery quote details.")]
    public List<ShopDeliveryOrderLine> lines = new List<ShopDeliveryOrderLine>();

    public string BuildSummary() {
        return $"{serviceName}: items {itemSubtotal:0}, fee {deliveryFee:0}, total {totalDue:0}, due in {deliveryHours} hour(s).";
    }

    public ShopDeliveryEventSnapshot ToEventSnapshot() {
        return new ShopDeliveryEventSnapshot {
            shopId = shopId,
            sourceId = sourceId,
            destinationId = destinationId,
            itemSubtotal = itemSubtotal,
            deliveryFee = deliveryFee,
            totalPaid = totalDue,
            deliveryHours = deliveryHours,
            dueAbsoluteHour = dueAbsoluteHour
        };
    }
}

[Serializable]
public class ShopDeliveryOrderRecord {
    [Tooltip("Unique runtime/save id for this delivery order.")]
    public string orderId;
    [Tooltip("Delivery service id used by this order.")]
    public string serviceId;
    [Tooltip("Delivery service display name copied for fallback/debug output.")]
    public string serviceName;
    [Tooltip("Courier type used by this order.")]
    public ShopDeliveryCourierType courierType;
    [Tooltip("How this order is fulfilled once due.")]
    public ShopDeliveryFulfillmentMode fulfillmentMode;
    [Tooltip("Shop instance id used by this order.")]
    public string shopId;
    [Tooltip("Catalog definition id used by this order.")]
    public string catalogId;
    [Tooltip("Shop display name copied for fallback/debug output.")]
    public string shopName;
    [Tooltip("Source id that placed this order.")]
    public string sourceId;
    [Tooltip("Destination id for this order.")]
    public string destinationId;
    [Tooltip("Destination display name for this order.")]
    public string destinationName;
    [Tooltip("Destination region id for this order.")]
    public string destinationRegionId;
    [Tooltip("Destination region display name for this order.")]
    public string destinationRegionName;
    [Tooltip("Basket item subtotal paid for this order.")]
    public float itemSubtotal;
    [Tooltip("Delivery fee paid for this order.")]
    public float deliveryFee;
    [Tooltip("Total money paid for this order.")]
    public float totalPaid;
    [Tooltip("Money refunded if this order is cancelled.")]
    public float refundAmount;
    [Tooltip("In-game hours until this order is due.")]
    public int deliveryHours;
    [Tooltip("In-game day when this order was placed.")]
    public int placedDay;
    [Tooltip("Absolute in-game hour when this order was placed.")]
    public int placedAbsoluteHour;
    [Tooltip("Absolute in-game hour when this order becomes due.")]
    public int dueAbsoluteHour;
    [Tooltip("If enabled, this order can be cancelled while pending and inside the cancellation window.")]
    public bool allowCancellation;
    [Tooltip("Absolute in-game hour after which cancellation is blocked.")]
    public int cancellationDeadlineAbsoluteHour;
    [Tooltip("If enabled, limited stock is restored when this order is cancelled.")]
    public bool restoreLimitedStockOnCancel;
    [Tooltip("If enabled, the order has been delivered.")]
    public bool delivered;
    [Tooltip("In-game day when this order was delivered.")]
    public int deliveredDay = -1;
    [Tooltip("Absolute in-game hour when this order was delivered.")]
    public int deliveredAbsoluteHour = -1;
    [Tooltip("If enabled, the order was cancelled.")]
    public bool cancelled;
    [Tooltip("In-game day when this order was cancelled.")]
    public int cancelledDay = -1;
    [Tooltip("Absolute in-game hour when this order was cancelled.")]
    public int cancelledAbsoluteHour = -1;
    [Tooltip("Line-level delivery order details.")]
    public List<ShopDeliveryOrderLine> lines = new List<ShopDeliveryOrderLine>();

    public bool IsPending => !delivered && !cancelled;

    public ShopDeliveryOrderRecord() {
    }

    public ShopDeliveryOrderRecord(ShopDeliveryQuote quote) {
        if(quote == null) {
            return;
        }

        serviceId = quote.serviceId;
        serviceName = quote.serviceName;
        courierType = quote.courierType;
        fulfillmentMode = quote.fulfillmentMode;
        shopId = quote.shopId;
        catalogId = quote.catalogId;
        shopName = quote.shopName;
        sourceId = quote.sourceId;
        destinationId = quote.destinationId;
        destinationName = quote.destinationName;
        destinationRegionId = quote.destinationRegionId;
        destinationRegionName = quote.destinationRegionName;
        itemSubtotal = quote.itemSubtotal;
        deliveryFee = quote.deliveryFee;
        totalPaid = quote.totalDue;
        refundAmount = quote.cancellationRefundAmount;
        deliveryHours = quote.deliveryHours;
        placedDay = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
        placedAbsoluteHour = quote.placedAbsoluteHour;
        dueAbsoluteHour = quote.dueAbsoluteHour;
        allowCancellation = quote.allowCancellation;
        cancellationDeadlineAbsoluteHour = quote.cancellationDeadlineAbsoluteHour;
        restoreLimitedStockOnCancel = quote.restoreLimitedStockOnCancel;
        lines = quote.lines != null ? quote.lines.Where(line => line != null).Select(line => line.Clone()).ToList() : new List<ShopDeliveryOrderLine>();
    }

    public bool IsDue(int currentAbsoluteHour) {
        return currentAbsoluteHour >= dueAbsoluteHour;
    }

    public bool MatchesDestination(string destinationIdFilter) {
        return string.IsNullOrWhiteSpace(destinationIdFilter)
            || string.Equals(destinationId, destinationIdFilter, StringComparison.OrdinalIgnoreCase);
    }

    public ShopDeliveryServiceDefinition ResolveService() {
        if(string.IsNullOrWhiteSpace(serviceId)) {
            return null;
        }

        return Resources.LoadAll<ShopDeliveryServiceDefinition>("").FirstOrDefault(service => service != null && service.Id == serviceId);
    }

    public ShopDeliveryEventSnapshot ToEventSnapshot() {
        return new ShopDeliveryEventSnapshot {
            shopId = shopId,
            sourceId = sourceId,
            destinationId = destinationId,
            itemSubtotal = itemSubtotal,
            deliveryFee = deliveryFee,
            totalPaid = totalPaid,
            deliveryHours = deliveryHours,
            dueAbsoluteHour = dueAbsoluteHour
        };
    }

    public ShopDeliveryOrderRecord Clone() {
        return new ShopDeliveryOrderRecord {
            orderId = orderId,
            serviceId = serviceId,
            serviceName = serviceName,
            courierType = courierType,
            fulfillmentMode = fulfillmentMode,
            shopId = shopId,
            catalogId = catalogId,
            shopName = shopName,
            sourceId = sourceId,
            destinationId = destinationId,
            destinationName = destinationName,
            destinationRegionId = destinationRegionId,
            destinationRegionName = destinationRegionName,
            itemSubtotal = itemSubtotal,
            deliveryFee = deliveryFee,
            totalPaid = totalPaid,
            refundAmount = refundAmount,
            deliveryHours = deliveryHours,
            placedDay = placedDay,
            placedAbsoluteHour = placedAbsoluteHour,
            dueAbsoluteHour = dueAbsoluteHour,
            allowCancellation = allowCancellation,
            cancellationDeadlineAbsoluteHour = cancellationDeadlineAbsoluteHour,
            restoreLimitedStockOnCancel = restoreLimitedStockOnCancel,
            delivered = delivered,
            deliveredDay = deliveredDay,
            deliveredAbsoluteHour = deliveredAbsoluteHour,
            cancelled = cancelled,
            cancelledDay = cancelledDay,
            cancelledAbsoluteHour = cancelledAbsoluteHour,
            lines = lines != null ? lines.Where(line => line != null).Select(line => line.Clone()).ToList() : new List<ShopDeliveryOrderLine>()
        };
    }
}

[Serializable]
public class ShopDeliveryOrderLine {
    [Tooltip("Offer id selected from the shop catalog.")]
    public string offerId;
    [Tooltip("Offer display name copied for fallback/debug output.")]
    public string offerName;
    [Tooltip("Inventory item asset name used to resolve the delivered item.")]
    public string itemAssetName;
    [Tooltip("Item display name copied for fallback/debug output.")]
    public string itemName;
    [Tooltip("Number of purchase bundles ordered.")]
    [Min(1)]
    public int bundles = 1;
    [Tooltip("How many inventory items each bundle grants.")]
    [Min(1)]
    public int itemsPerBundle = 1;
    [Tooltip("Total inventory item count delivered by this line.")]
    [Min(1)]
    public int itemCount = 1;
    [Tooltip("Price for one bundle at order time.")]
    public float unitBundlePrice;
    [Tooltip("Line total at order time.")]
    public float lineTotal;
    [Tooltip("If enabled, this line reserved limited stock in PlayerShopLedger.")]
    public bool hasStockLimit;
    [Tooltip("Stock period used by this offer.")]
    public ShopStockLimitPeriod stockPeriod;

    public ItemBase ResolveItem() {
        return !string.IsNullOrWhiteSpace(itemAssetName) ? ItemDB.GetObjectByName(itemAssetName) : null;
    }

    public ShopDeliveryOrderLine Clone() {
        return new ShopDeliveryOrderLine {
            offerId = offerId,
            offerName = offerName,
            itemAssetName = itemAssetName,
            itemName = itemName,
            bundles = Mathf.Max(1, bundles),
            itemsPerBundle = Mathf.Max(1, itemsPerBundle),
            itemCount = Mathf.Max(1, itemCount),
            unitBundlePrice = unitBundlePrice,
            lineTotal = lineTotal,
            hasStockLimit = hasStockLimit,
            stockPeriod = stockPeriod
        };
    }
}

public class ShopDeliveryEventSnapshot {
    public string shopId;
    public string sourceId;
    public string destinationId;
    public float itemSubtotal;
    public float deliveryFee;
    public float totalPaid;
    public int deliveryHours;
    public int dueAbsoluteHour;
}

[Serializable]
public class PlayerDeliveryLogSaveData {
    [Tooltip("Saved delivery orders.")]
    public List<ShopDeliveryOrderRecord> orders;
}
