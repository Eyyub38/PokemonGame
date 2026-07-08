using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerShopBasketLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum checkout records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxCheckoutRecords = 200;
    [Tooltip("Current active basket state. Empty means the player has no active basket.")]
    [SerializeField] ShopBasketState activeBasket;
    [Tooltip("Runtime/save checkout history for basket purchases.")]
    [SerializeField] List<ShopBasketCheckoutRecord> checkoutRecords = new List<ShopBasketCheckoutRecord>();

    public ShopBasketState ActiveBasket => activeBasket;
    public IReadOnlyList<ShopBasketCheckoutRecord> CheckoutRecords => checkoutRecords;
    public bool HasActiveBasket => activeBasket != null && !string.IsNullOrWhiteSpace(activeBasket.shopId);
    public event Action<ShopBasketState> OnBasketChanged;
    public event Action<ShopBasketCheckoutRecord> OnBasketCheckedOut;

    public bool BeginBasket(ShopCatalog shop, bool clearExisting = false, string sourceId = null) {
        if(shop == null || shop.Catalog == null) {
            return false;
        }

        if(activeBasket == null || activeBasket.shopId != shop.ShopId || clearExisting) {
            activeBasket = new ShopBasketState {
                shopId = shop.ShopId,
                catalogId = shop.Catalog.Id,
                shopName = shop.Catalog.DisplayName,
                sourceId = NormalizeSourceId(sourceId),
                startedDay = GetCurrentDay(),
                startedAbsoluteHour = GetCurrentAbsoluteHour(),
                lines = new List<ShopBasketLineState>()
            };
        } else {
            activeBasket.sourceId = NormalizeSourceId(sourceId);
            activeBasket.shopName = shop.Catalog.DisplayName;
            activeBasket.catalogId = shop.Catalog.Id;
        }

        NotifyBasketChanged();
        return true;
    }

    public bool AddOffer(ShopCatalog shop, ShopCatalogEntry entry, int bundles, string sourceId, out string failureMessage) {
        failureMessage = null;
        if(shop == null || entry == null) {
            failureMessage = "No shop offer selected.";
            return false;
        }

        bundles = Mathf.Max(1, bundles);
        if(!BeginBasket(shop, clearExisting: false, sourceId: sourceId)) {
            failureMessage = "Could not start a shop basket.";
            return false;
        }

        var player = GetComponent<PlayerController>();
        if(player == null) {
            failureMessage = "A player is required to update a basket.";
            return false;
        }

        if(!shop.GetAvailableOffers(player).Contains(entry)) {
            failureMessage = $"{entry.DisplayName} is not available in {shop.Catalog.DisplayName}.";
            return false;
        }

        int existingBundles = GetBundleCount(entry.OfferId);
        var ledger = player.GetComponent<PlayerShopLedger>();
        if(entry.HasStockLimit && !entry.HasEnoughStock(shop.ShopId, ledger, existingBundles + bundles)) {
            failureMessage = $"{entry.DisplayName} does not have enough stock for this basket.";
            return false;
        }

        var line = activeBasket.lines.FirstOrDefault(item => item != null && item.offerId == entry.OfferId);
        if(line == null) {
            line = CreateLine(shop, player, entry, bundles);
            activeBasket.lines.Add(line);
        } else {
            line.bundles += bundles;
            RefreshLineSnapshot(shop, player, entry, line);
        }

        activeBasket.lastUpdatedAbsoluteHour = GetCurrentAbsoluteHour();
        NotifyBasketChanged();
        return true;
    }

    public bool SetOfferBundles(ShopCatalog shop, string offerId, int bundles, out string failureMessage) {
        failureMessage = null;
        if(!HasActiveBasket || string.IsNullOrWhiteSpace(offerId)) {
            failureMessage = "No active basket line selected.";
            return false;
        }

        var line = activeBasket.lines.FirstOrDefault(item => item != null && item.offerId == offerId);
        if(line == null) {
            failureMessage = "Basket does not contain that offer.";
            return false;
        }

        if(bundles <= 0) {
            activeBasket.lines.Remove(line);
            NotifyBasketChanged();
            return true;
        }

        var entry = shop != null ? shop.FindOffer(offerId) : null;
        if(entry != null) {
            var player = GetComponent<PlayerController>();
            var ledger = player != null ? player.GetComponent<PlayerShopLedger>() : null;
            if(entry.HasStockLimit && !entry.HasEnoughStock(shop.ShopId, ledger, bundles)) {
                failureMessage = $"{entry.DisplayName} does not have enough stock for this basket.";
                return false;
            }

            line.bundles = Mathf.Max(1, bundles);
            RefreshLineSnapshot(shop, player, entry, line);
        } else {
            line.bundles = Mathf.Max(1, bundles);
        }

        activeBasket.lastUpdatedAbsoluteHour = GetCurrentAbsoluteHour();
        NotifyBasketChanged();
        return true;
    }

    public bool RemoveOffer(string offerId, int bundles = 1) {
        if(!HasActiveBasket || string.IsNullOrWhiteSpace(offerId)) {
            return false;
        }

        var line = activeBasket.lines.FirstOrDefault(item => item != null && item.offerId == offerId);
        if(line == null) {
            return false;
        }

        line.bundles -= Mathf.Max(1, bundles);
        if(line.bundles <= 0) {
            activeBasket.lines.Remove(line);
        }

        activeBasket.lastUpdatedAbsoluteHour = GetCurrentAbsoluteHour();
        NotifyBasketChanged();
        return true;
    }

    public bool ClearBasket(string sourceId = null) {
        if(activeBasket == null) {
            return false;
        }

        activeBasket = null;
        NotifyBasketChanged();
        return true;
    }

    public bool ContainsOffer(string offerId) {
        return HasActiveBasket
            && !string.IsNullOrWhiteSpace(offerId)
            && activeBasket.lines.Any(line => line != null && line.offerId == offerId);
    }

    public int GetLineCount() {
        return HasActiveBasket ? activeBasket.lines.Count(line => line != null) : 0;
    }

    public int GetBundleCount(string offerId = null) {
        if(!HasActiveBasket) {
            return 0;
        }

        return activeBasket.lines
            .Where(line => line != null && (string.IsNullOrWhiteSpace(offerId) || line.offerId == offerId))
            .Sum(line => Mathf.Max(0, line.bundles));
    }

    public float GetSnapshotTotal() {
        return HasActiveBasket
            ? activeBasket.lines.Where(line => line != null).Sum(line => Mathf.Max(0f, line.lineTotal))
            : 0f;
    }

    public float GetCurrentTotal(ShopCatalog shop, out string failureMessage) {
        failureMessage = null;
        if(!HasActiveBasket) {
            return 0f;
        }

        if(shop == null) {
            failureMessage = "No shop selected.";
            return GetSnapshotTotal();
        }

        var player = GetComponent<PlayerController>();
        float total = 0f;
        foreach(var line in activeBasket.lines) {
            if(line == null) {
                continue;
            }

            var entry = shop.FindOffer(line.offerId);
            if(entry == null) {
                failureMessage = $"Shop no longer contains offer '{line.offerId}'.";
                return total;
            }

            total += shop.GetBuyPrice(player, entry, line.bundles);
        }

        return total;
    }

    public ShopBasketCheckoutRecord FindCheckoutRecord(string recordId) {
        if(string.IsNullOrWhiteSpace(recordId)) {
            return null;
        }

        return checkoutRecords.FirstOrDefault(record => record != null && record.recordId == recordId);
    }

    public ShopBasketCheckoutRecord GetLatestCheckoutRecord(string shopId = null) {
        return checkoutRecords
            .Where(record => record != null && (string.IsNullOrWhiteSpace(shopId) || record.shopId == shopId))
            .OrderByDescending(record => record.absoluteHour)
            .FirstOrDefault();
    }

    public bool CanAffordCurrentBasket(ShopCatalog shop) {
        float total = GetCurrentTotal(shop, out _);
        return total <= 0f || (Wallet.i != null && Wallet.i.HasMoney(total));
    }

    public bool TryCheckout(ShopCatalog shop, string sourceId, out ShopBasketCheckoutRecord record, out string failureMessage) {
        return TryCheckout(shop, sourceId, null, out record, out failureMessage);
    }

    public bool TryCheckout(ShopCatalog shop, string sourceId, ShopPaymentRuleDefinition paymentRule, out ShopBasketCheckoutRecord record, out string failureMessage) {
        record = null;
        if(!HasActiveBasket || activeBasket.lines.Count == 0) {
            failureMessage = "The basket is empty.";
            return false;
        }

        if(shop == null) {
            failureMessage = "No shop selected.";
            return false;
        }

        if(activeBasket.shopId != shop.ShopId) {
            failureMessage = $"Basket belongs to {activeBasket.shopName}, not {shop.Catalog.DisplayName}.";
            return false;
        }

        var cart = BuildCart(shop, out failureMessage);
        if(cart == null) {
            return false;
        }

        var player = GetComponent<PlayerController>();
        ShopCheckoutPaymentQuote quote = null;
        if(paymentRule != null && !paymentRule.TryBuildPaymentQuote(player, shop, this, NormalizeSourceId(sourceId), out quote, out failureMessage)) {
            return false;
        }

        if(!shop.TryCheckoutCart(player, cart, quote, out var checkoutResult, out failureMessage)) {
            paymentRule?.PublishBlocked(quote, player, shop, NormalizeSourceId(sourceId), failureMessage);
            return false;
        }

        paymentRule?.PublishPaid(quote, player, shop, NormalizeSourceId(sourceId));

        record = new ShopBasketCheckoutRecord {
            recordId = Guid.NewGuid().ToString("N"),
            shopId = activeBasket.shopId,
            catalogId = activeBasket.catalogId,
            shopName = activeBasket.shopName,
            sourceId = NormalizeSourceId(sourceId),
            totalPrice = checkoutResult.amountPaid,
            itemSubtotal = checkoutResult.itemSubtotal,
            serviceFee = checkoutResult.serviceFee,
            discountTotal = checkoutResult.discountTotal,
            amountDue = checkoutResult.amountDue,
            amountPaid = checkoutResult.amountPaid,
            paymentMode = checkoutResult.paymentMode.ToString(),
            paymentRuleId = checkoutResult.paymentRuleId,
            paymentRuleName = checkoutResult.paymentRuleName,
            terminalKind = checkoutResult.terminalKind.ToString(),
            paymentMessage = checkoutResult.paymentMessage,
            lineCount = GetLineCount(),
            bundleCount = GetBundleCount(),
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            lines = checkoutResult.lines != null && checkoutResult.lines.Count > 0
                ? checkoutResult.lines.Where(line => line != null).Select(line => line.Clone()).ToList()
                : activeBasket.lines.Where(line => line != null).Select(line => line.Clone()).ToList()
        };

        checkoutRecords.Add(record);
        TrimHistory();
        activeBasket = null;
        OnBasketCheckedOut?.Invoke(record);
        NotifyBasketChanged();
        PublishCheckoutEvent(record);
        return true;
    }

    public ShopCart BuildCart(ShopCatalog shop, out string failureMessage) {
        failureMessage = null;
        if(!HasActiveBasket) {
            failureMessage = "The basket is empty.";
            return null;
        }

        if(shop == null) {
            failureMessage = "No shop selected.";
            return null;
        }

        var cart = shop.CreateCart();
        foreach(var line in activeBasket.lines) {
            if(line == null) {
                continue;
            }

            var entry = shop.FindOffer(line.offerId);
            if(entry == null) {
                failureMessage = $"Shop no longer contains offer '{line.offerId}'.";
                return null;
            }

            cart.Add(entry, line.bundles);
        }

        return cart;
    }

    ShopBasketLineState CreateLine(ShopCatalog shop, PlayerController player, ShopCatalogEntry entry, int bundles) {
        var line = new ShopBasketLineState {
            offerId = entry.OfferId,
            itemId = entry.GetItem() != null ? entry.GetItem().name : string.Empty,
            itemName = entry.DisplayName,
            bundles = Mathf.Max(1, bundles),
            itemsPerBundle = entry.GetBundleCount()
        };
        RefreshLineSnapshot(shop, player, entry, line);
        return line;
    }

    void RefreshLineSnapshot(ShopCatalog shop, PlayerController player, ShopCatalogEntry entry, ShopBasketLineState line) {
        if(shop == null || entry == null || line == null) {
            return;
        }

        line.itemId = entry.GetItem() != null ? entry.GetItem().name : line.itemId;
        line.itemName = entry.DisplayName;
        line.itemsPerBundle = entry.GetBundleCount();
        line.unitBundlePrice = shop.GetBuyPrice(player, entry, 1);
        line.lineTotal = shop.GetBuyPrice(player, entry, line.bundles);
    }

    void TrimHistory() {
        if(maxCheckoutRecords <= 0 || checkoutRecords.Count <= maxCheckoutRecords) {
            return;
        }

        checkoutRecords = checkoutRecords
            .Where(record => record != null)
            .OrderByDescending(record => record.absoluteHour)
            .Take(maxCheckoutRecords)
            .OrderBy(record => record.absoluteHour)
            .ToList();
    }

    void NotifyBasketChanged() {
        OnBasketChanged?.Invoke(activeBasket);
    }

    void PublishCheckoutEvent(ShopBasketCheckoutRecord record) {
        if(record == null) {
            return;
        }

        GameEventPublishing.PublishOptional(
            null,
            $"shop.basket.checkout.{record.shopId}",
            $"{record.shopName} basket checked out.",
            GameEventCategory.Shop,
            GameEventImportance.Success,
            this,
            "PlayerShopBasketLog",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("shopId", record.shopId),
            GameEventPublishing.Value("shopName", record.shopName),
            GameEventPublishing.Value("lineCount", record.lineCount),
            GameEventPublishing.Value("bundleCount", record.bundleCount),
            GameEventPublishing.Value("totalPrice", record.totalPrice),
            GameEventPublishing.Value("itemSubtotal", record.itemSubtotal),
            GameEventPublishing.Value("serviceFee", record.serviceFee),
            GameEventPublishing.Value("discountTotal", record.discountTotal),
            GameEventPublishing.Value("amountDue", record.amountDue),
            GameEventPublishing.Value("amountPaid", record.amountPaid),
            GameEventPublishing.Value("paymentMode", record.paymentMode),
            GameEventPublishing.Value("paymentRuleId", record.paymentRuleId),
            GameEventPublishing.Value("terminalKind", record.terminalKind));
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "shop-basket" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerShopBasketLogSaveData {
            activeBasket = activeBasket != null ? activeBasket.Clone() : null,
            checkoutRecords = checkoutRecords.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerShopBasketLogSaveData;
        activeBasket = saveData?.activeBasket != null ? saveData.activeBasket.Clone() : null;
        checkoutRecords = saveData?.checkoutRecords?.Where(record => record != null).Select(record => record.Clone()).ToList()
            ?? new List<ShopBasketCheckoutRecord>();
        TrimHistory();
        NotifyBasketChanged();
    }
}

[Serializable]
public class ShopBasketState {
    [Tooltip("Shop instance id this basket belongs to.")]
    public string shopId;
    [Tooltip("Catalog definition id this basket belongs to.")]
    public string catalogId;
    [Tooltip("Shop display name copied for fallback/debug output.")]
    public string shopName;
    [Tooltip("Source id that started or last refreshed this basket.")]
    public string sourceId;
    [Tooltip("In-game day when this basket started.")]
    public int startedDay;
    [Tooltip("Absolute in-game hour when this basket started.")]
    public int startedAbsoluteHour;
    [Tooltip("Absolute in-game hour when this basket last changed.")]
    public int lastUpdatedAbsoluteHour;
    [Tooltip("Offer lines currently in this basket.")]
    public List<ShopBasketLineState> lines = new List<ShopBasketLineState>();

    public ShopBasketState Clone() {
        return new ShopBasketState {
            shopId = shopId,
            catalogId = catalogId,
            shopName = shopName,
            sourceId = sourceId,
            startedDay = startedDay,
            startedAbsoluteHour = startedAbsoluteHour,
            lastUpdatedAbsoluteHour = lastUpdatedAbsoluteHour,
            lines = lines != null ? lines.Where(line => line != null).Select(line => line.Clone()).ToList() : new List<ShopBasketLineState>()
        };
    }
}

[Serializable]
public class ShopBasketLineState {
    [Tooltip("Offer id selected from the shop catalog.")]
    public string offerId;
    [Tooltip("Inventory item asset id/name copied from the offer.")]
    public string itemId;
    [Tooltip("Display name copied from the offer.")]
    public string itemName;
    [Tooltip("Number of purchase bundles in the basket.")]
    [Min(1)]
    public int bundles = 1;
    [Tooltip("How many inventory items each bundle grants.")]
    [Min(1)]
    public int itemsPerBundle = 1;
    [Tooltip("Current/snapshot price for one bundle after catalog, model and sponsor modifiers.")]
    public float unitBundlePrice;
    [Tooltip("Current/snapshot total price for this line.")]
    public float lineTotal;

    public ShopBasketLineState Clone() {
        return new ShopBasketLineState {
            offerId = offerId,
            itemId = itemId,
            itemName = itemName,
            bundles = Mathf.Max(1, bundles),
            itemsPerBundle = Mathf.Max(1, itemsPerBundle),
            unitBundlePrice = unitBundlePrice,
            lineTotal = lineTotal
        };
    }
}

[Serializable]
public class ShopBasketCheckoutRecord {
    [Tooltip("Unique runtime/save id for this checkout.")]
    public string recordId;
    [Tooltip("Shop instance id used by this checkout.")]
    public string shopId;
    [Tooltip("Catalog definition id used by this checkout.")]
    public string catalogId;
    [Tooltip("Shop display name copied at checkout time.")]
    public string shopName;
    [Tooltip("Source id that requested checkout.")]
    public string sourceId;
    [Tooltip("Total checkout price after modifiers.")]
    public float totalPrice;
    [Tooltip("Basket item subtotal before checkout fees or discounts.")]
    public float itemSubtotal;
    [Tooltip("Checkout fee applied by the payment rule.")]
    public float serviceFee;
    [Tooltip("Discount or waiver applied by the payment rule.")]
    public float discountTotal;
    [Tooltip("Total amount due after fees and discounts.")]
    public float amountDue;
    [Tooltip("Amount charged to Wallet. Free/manual approval checkouts can be 0.")]
    public float amountPaid;
    [Tooltip("Payment mode used by this checkout record.")]
    public string paymentMode;
    [Tooltip("Payment rule id used by this checkout record.")]
    public string paymentRuleId;
    [Tooltip("Payment rule display name used by this checkout record.")]
    public string paymentRuleName;
    [Tooltip("Terminal kind used by this checkout record.")]
    public string terminalKind;
    [Tooltip("Readable payment summary or note copied from the payment quote.")]
    public string paymentMessage;
    [Tooltip("Number of basket lines checked out.")]
    public int lineCount;
    [Tooltip("Total purchase bundles checked out.")]
    public int bundleCount;
    [Tooltip("In-game day when checkout occurred.")]
    public int day;
    [Tooltip("Absolute in-game hour when checkout occurred.")]
    public int absoluteHour;
    [Tooltip("Lines checked out.")]
    public List<ShopBasketLineState> lines = new List<ShopBasketLineState>();

    public ShopBasketCheckoutRecord Clone() {
        return new ShopBasketCheckoutRecord {
            recordId = recordId,
            shopId = shopId,
            catalogId = catalogId,
            shopName = shopName,
            sourceId = sourceId,
            totalPrice = totalPrice,
            itemSubtotal = itemSubtotal,
            serviceFee = serviceFee,
            discountTotal = discountTotal,
            amountDue = amountDue,
            amountPaid = amountPaid,
            paymentMode = paymentMode,
            paymentRuleId = paymentRuleId,
            paymentRuleName = paymentRuleName,
            terminalKind = terminalKind,
            paymentMessage = paymentMessage,
            lineCount = lineCount,
            bundleCount = bundleCount,
            day = day,
            absoluteHour = absoluteHour,
            lines = lines != null ? lines.Where(line => line != null).Select(line => line.Clone()).ToList() : new List<ShopBasketLineState>()
        };
    }
}

[Serializable]
public class PlayerShopBasketLogSaveData {
    public ShopBasketState activeBasket;
    public List<ShopBasketCheckoutRecord> checkoutRecords;
}
