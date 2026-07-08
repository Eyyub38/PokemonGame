using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopCatalog : MonoBehaviour {
    [Header("Catalog")]
    [Tooltip("Catalog definition that drives stock, prices and access rules.")]
    [SerializeField] ShopCatalogDefinition catalog;
    [Tooltip("Optional save/id override for this shop instance. Empty uses catalog id or GameObject name.")]
    [SerializeField] string shopInstanceId;
    [Tooltip("If enabled, the GameObject name is used when no explicit shop instance id exists.")]
    [SerializeField] bool fallbackToGameObjectName = true;

    public ShopCatalogDefinition Catalog => catalog;
    public string ShopId {
        get {
            if(!string.IsNullOrWhiteSpace(shopInstanceId)) {
                return shopInstanceId;
            }

            if(catalog != null) {
                return catalog.Id;
            }

            return fallbackToGameObjectName ? name : "shop";
        }
    }

    public List<ShopCatalogEntry> GetAvailableOffers(PlayerController player) {
        if(catalog == null) {
            return new List<ShopCatalogEntry>();
        }

        var ledger = player != null ? player.GetComponent<PlayerShopLedger>() : null;
        return catalog.GetAvailableEntries(player, ShopId, ledger);
    }

    public ShopCart CreateCart() {
        return new ShopCart();
    }

    public ShopCatalogEntry FindOffer(string offerId) {
        if(catalog == null || catalog.Entries == null || string.IsNullOrWhiteSpace(offerId)) {
            return null;
        }

        return catalog.Entries.FirstOrDefault(entry => entry != null && entry.OfferId == offerId);
    }

    public float GetBuyPrice(ShopCatalogEntry entry, int bundles = 1) {
        if(entry == null || catalog == null) {
            return 0f;
        }

        return entry.GetBuyPrice(catalog.BuyPriceMultiplier) * Mathf.Max(1, bundles);
    }

    public float GetBuyPrice(PlayerController player, ShopCatalogEntry entry, int bundles = 1) {
        float basePrice = GetBuyPrice(entry, bundles);
        if(basePrice <= 0f) {
            return 0f;
        }

        var sponsorLog = player != null ? player.GetComponent<PlayerSponsorLog>() : null;
        var loyaltyLog = player != null ? player.GetComponent<PlayerLoyaltyLog>() : null;
        float multiplier = sponsorLog != null ? sponsorLog.GetBestBuyPriceMultiplier(catalog, entry) : 1f;
        multiplier *= loyaltyLog != null ? loyaltyLog.GetBestBuyPriceMultiplier(catalog, entry) : 1f;
        return Mathf.Max(0f, Mathf.Ceil(basePrice * multiplier));
    }

    public float GetSellPrice(ItemBase item, int count = 1) {
        if(item == null || catalog == null) {
            return 0f;
        }

        return Mathf.Max(0f, Mathf.Floor(item.Price * 0.5f * catalog.SellPriceMultiplier * Mathf.Max(1, count)));
    }

    public float GetSellPrice(PlayerController player, ItemBase item, int count = 1) {
        float basePrice = GetSellPrice(item, count);
        if(basePrice <= 0f) {
            return 0f;
        }

        var sponsorLog = player != null ? player.GetComponent<PlayerSponsorLog>() : null;
        var loyaltyLog = player != null ? player.GetComponent<PlayerLoyaltyLog>() : null;
        float multiplier = sponsorLog != null ? sponsorLog.GetBestSellPriceMultiplier(catalog) : 1f;
        multiplier *= loyaltyLog != null ? loyaltyLog.GetBestSellPriceMultiplier(catalog) : 1f;
        return Mathf.Max(0f, Mathf.Floor(basePrice * multiplier));
    }

    public bool CanBuy(PlayerController player, ShopCatalogEntry entry, int bundles, out string failureMessage) {
        return CanBuy(player, entry, bundles, checkMoney: true, out failureMessage);
    }

    public bool TryBuy(PlayerController player, ShopCatalogEntry entry, int bundles, out string failureMessage) {
        if(!CanBuy(player, entry, bundles, out failureMessage)) {
            return false;
        }

        float totalPrice = GetBuyPrice(player, entry, bundles);
        if(totalPrice > 0f) {
            Wallet.i.TakeMoney(totalPrice);
        }

        CompletePurchase(player, entry, bundles, totalPrice);
        failureMessage = null;
        return true;
    }

    public bool TryCheckoutCart(PlayerController player, ShopCart cart, out string failureMessage) {
        return TryCheckoutCart(player, cart, null, out _, out failureMessage);
    }

    public bool TryCheckoutCart(PlayerController player, ShopCart cart, ShopCheckoutPaymentQuote paymentQuote, out ShopCheckoutCartResult checkoutResult, out string failureMessage) {
        checkoutResult = null;
        if(cart == null || cart.Lines.Count == 0) {
            failureMessage = "The cart is empty.";
            return false;
        }

        var checkoutLines = new List<ShopCheckoutLineSnapshot>();
        float totalPrice = 0f;
        foreach(var line in cart.Lines) {
            if(line == null) {
                failureMessage = "The cart has an empty line.";
                return false;
            }

            if(!CanBuy(player, line.entry, line.bundles, checkMoney: false, out failureMessage)) {
                return false;
            }

            float linePrice = GetBuyPrice(player, line.entry, line.bundles);
            checkoutLines.Add(new ShopCheckoutLineSnapshot(line.entry, line.bundles, linePrice));
            totalPrice += linePrice;
        }

        var quote = paymentQuote ?? ShopCheckoutPaymentQuote.CreateDefault(totalPrice, $"shop:{ShopId}");
        if(!quote.MatchesSubtotal(totalPrice)) {
            failureMessage = "Basket price changed before checkout. Please refresh the checkout quote.";
            return false;
        }

        float amountToCharge = quote.AmountToCharge;
        if(amountToCharge > 0f && (Wallet.i == null || !Wallet.i.HasMoney(amountToCharge))) {
            failureMessage = $"You need {amountToCharge:0} money for this cart.";
            return false;
        }

        if(amountToCharge > 0f) {
            Wallet.i.TakeMoney(amountToCharge);
        }

        foreach(var line in checkoutLines) {
            CompletePurchase(player, line.entry, line.bundles, line.totalPrice);
        }

        checkoutResult = new ShopCheckoutCartResult {
            itemSubtotal = totalPrice,
            serviceFee = quote.serviceFee,
            discountTotal = quote.discountTotal,
            amountDue = quote.amountDue,
            amountPaid = amountToCharge,
            paymentRuleId = quote.paymentRuleId,
            paymentRuleName = quote.paymentRuleName,
            terminalKind = quote.terminalKind,
            paymentMode = quote.paymentMode,
            paymentMessage = quote.message,
            lines = checkoutLines.Select(line => line.ToBasketLineState()).ToList()
        };

        cart.Clear();
        failureMessage = null;
        return true;
    }

    public bool CanSell(PlayerController player, ItemBase item, int count, out string failureMessage) {
        if(catalog == null) {
            failureMessage = "No shop catalog assigned.";
            return false;
        }

        if(!catalog.AllowSelling) {
            failureMessage = $"{catalog.DisplayName} is not buying items.";
            return false;
        }

        if(item == null) {
            failureMessage = "No item selected.";
            return false;
        }

        if(!item.IsSellable) {
            failureMessage = $"{item.Name} cannot be sold.";
            return false;
        }

        var inventory = player != null ? player.GetComponent<Inventory>() : Inventory.GetInventory();
        if(inventory == null || !inventory.HasItemEnough(item, Mathf.Max(1, count))) {
            failureMessage = $"You do not have enough {item.Name}.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TrySell(PlayerController player, ItemBase item, int count, out float moneyGained, out string failureMessage) {
        moneyGained = 0f;
        count = Mathf.Max(1, count);
        if(!CanSell(player, item, count, out failureMessage)) {
            return false;
        }

        var inventory = player != null ? player.GetComponent<Inventory>() : Inventory.GetInventory();
        moneyGained = GetSellPrice(player, item, count);
        inventory.RemoveItem(item, count);
        Wallet.i?.AddMoney(moneyGained);
        RecordSellSponsorBenefit(player);
        RecordSellLoyaltyPoints(player, item, count, moneyGained);
        PublishShopEvent("sold", player, item.Name, count, moneyGained, null);
        failureMessage = null;
        return true;
    }

    bool CanBuy(PlayerController player, ShopCatalogEntry entry, int bundles, bool checkMoney, out string failureMessage) {
        bundles = Mathf.Max(1, bundles);
        if(catalog == null) {
            failureMessage = "No shop catalog assigned.";
            return false;
        }

        if(player == null) {
            failureMessage = "A player is required to buy items.";
            return false;
        }

        if(!catalog.IsUnlocked(player, out failureMessage)) {
            return false;
        }

        if(entry == null || entry.GetItem() == null) {
            failureMessage = "No shop offer selected.";
            return false;
        }

        if(!catalog.Entries.Contains(entry)) {
            failureMessage = $"{entry.DisplayName} is not sold by {catalog.DisplayName}.";
            return false;
        }

        if(!entry.IsUnlocked(player, out failureMessage)) {
            return false;
        }

        var ledger = player.GetComponent<PlayerShopLedger>();
        if(entry.HasStockLimit && ledger == null) {
            failureMessage = "The player has no shop ledger for limited stock.";
            return false;
        }

        if(!entry.HasEnoughStock(ShopId, ledger, bundles)) {
            failureMessage = $"{entry.DisplayName} is out of stock.";
            return false;
        }

        if(player.GetComponent<Inventory>() == null) {
            failureMessage = "The player has no inventory.";
            return false;
        }

        float totalPrice = GetBuyPrice(player, entry, bundles);
        if(checkMoney && totalPrice > 0f && (Wallet.i == null || !Wallet.i.HasMoney(totalPrice))) {
            failureMessage = $"You need {totalPrice:0} money to buy {entry.DisplayName}.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    void CompletePurchase(PlayerController player, ShopCatalogEntry entry, int bundles, float totalPrice) {
        int bundleCount = entry.GetBundleCount();
        int itemCount = bundleCount * Mathf.Max(1, bundles);
        player.GetComponent<Inventory>().AddItem(entry.GetItem(), itemCount);

        if(entry.HasStockLimit) {
            player.GetComponent<PlayerShopLedger>()?.RecordPurchase(ShopId, entry.OfferId, entry.stockLimitPeriod, bundles);
        }

        RecordBuySponsorBenefit(player, entry);
        RecordBuyLoyaltyPoints(player, entry, bundles, totalPrice);
        PublishShopEvent("purchased", player, entry.DisplayName, itemCount, totalPrice, entry);
    }

    void RecordBuySponsorBenefit(PlayerController player, ShopCatalogEntry entry) {
        var sponsorLog = player != null ? player.GetComponent<PlayerSponsorLog>() : null;
        if(sponsorLog == null || !sponsorLog.TryGetBestBuyPriceSponsor(catalog, entry, out var sponsor, out float multiplier)) {
            return;
        }

        if(Mathf.Approximately(multiplier, 1f)) {
            return;
        }

        sponsorLog.RecordBenefitUse(sponsor, SponsorBenefitType.ShopBuyPrice, ShopId, catalog != null ? catalog.DisplayName : name, multiplier, $"shop:{ShopId}");
        sponsor.PublishBenefitUsed(player, $"shop:{ShopId}", SponsorBenefitType.ShopBuyPrice, ShopId, catalog != null ? catalog.DisplayName : name, multiplier);
    }

    void RecordSellSponsorBenefit(PlayerController player) {
        var sponsorLog = player != null ? player.GetComponent<PlayerSponsorLog>() : null;
        if(sponsorLog == null || !sponsorLog.TryGetBestSellPriceSponsor(catalog, out var sponsor, out float multiplier)) {
            return;
        }

        if(Mathf.Approximately(multiplier, 1f)) {
            return;
        }

        sponsorLog.RecordBenefitUse(sponsor, SponsorBenefitType.ShopSellPrice, ShopId, catalog != null ? catalog.DisplayName : name, multiplier, $"shop:{ShopId}");
        sponsor.PublishBenefitUsed(player, $"shop:{ShopId}", SponsorBenefitType.ShopSellPrice, ShopId, catalog != null ? catalog.DisplayName : name, multiplier);
    }

    void RecordBuyLoyaltyPoints(PlayerController player, ShopCatalogEntry entry, int bundles, float totalPrice) {
        var loyaltyLog = player != null ? player.GetComponent<PlayerLoyaltyLog>() : null;
        if(loyaltyLog == null || catalog == null || entry == null) {
            return;
        }

        loyaltyLog.RecordShopPurchase(catalog, entry, ShopId, totalPrice, Mathf.Max(1, bundles));
    }

    void RecordSellLoyaltyPoints(PlayerController player, ItemBase item, int count, float moneyGained) {
        var loyaltyLog = player != null ? player.GetComponent<PlayerLoyaltyLog>() : null;
        if(loyaltyLog == null || catalog == null || item == null) {
            return;
        }

        loyaltyLog.RecordShopSell(catalog, ShopId, item, Mathf.Max(1, count), moneyGained);
    }

    void PublishShopEvent(string phase, PlayerController player, string itemName, int count, float money, ShopCatalogEntry entry) {
        GameEventPublishing.PublishOptional(
            null,
            $"shop.{phase}.{ShopId}.{entry?.OfferId ?? itemName}",
            $"{itemName} {phase}.",
            GameEventCategory.Shop,
            GameEventImportance.Success,
            player != null ? player : this,
            "ShopCatalog",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("shopId", ShopId),
            GameEventPublishing.Value("catalog", catalog != null ? catalog.DisplayName : name),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("itemName", itemName),
            GameEventPublishing.Value("count", count),
            GameEventPublishing.Value("money", money));
    }
}

class ShopCheckoutLineSnapshot {
    public readonly ShopCatalogEntry entry;
    public readonly int bundles;
    public readonly float totalPrice;

    public ShopCheckoutLineSnapshot(ShopCatalogEntry entry, int bundles, float totalPrice) {
        this.entry = entry;
        this.bundles = Mathf.Max(1, bundles);
        this.totalPrice = Mathf.Max(0f, totalPrice);
    }

    public ShopBasketLineState ToBasketLineState() {
        int bundleCount = entry != null ? entry.GetBundleCount() : 1;
        int safeBundles = Mathf.Max(1, bundles);
        return new ShopBasketLineState {
            offerId = entry != null ? entry.OfferId : string.Empty,
            itemId = entry != null && entry.GetItem() != null ? entry.GetItem().name : string.Empty,
            itemName = entry != null ? entry.DisplayName : string.Empty,
            bundles = safeBundles,
            itemsPerBundle = bundleCount,
            unitBundlePrice = safeBundles > 0 ? Mathf.Max(0f, totalPrice / safeBundles) : totalPrice,
            lineTotal = totalPrice
        };
    }
}

[Serializable]
public class ShopCart {
    [Tooltip("Runtime cart lines. UI can bind to this list before checkout.")]
    [SerializeField] List<ShopCartLine> lines = new List<ShopCartLine>();

    public IReadOnlyList<ShopCartLine> Lines => lines;

    public void Add(ShopCatalogEntry entry, int bundles = 1) {
        if(entry == null) {
            return;
        }

        bundles = Mathf.Max(1, bundles);
        var line = lines.FirstOrDefault(l => l != null && l.entry == entry);
        if(line == null) {
            lines.Add(new ShopCartLine {
                entry = entry,
                bundles = bundles
            });
        } else {
            line.bundles += bundles;
        }
    }

    public void Remove(ShopCatalogEntry entry, int bundles = 1) {
        var line = lines.FirstOrDefault(l => l != null && l.entry == entry);
        if(line == null) {
            return;
        }

        line.bundles -= Mathf.Max(1, bundles);
        if(line.bundles <= 0) {
            lines.Remove(line);
        }
    }

    public void Clear() {
        lines.Clear();
    }

    public float GetTotalPrice(ShopCatalog shop) {
        if(shop == null) {
            return 0f;
        }

        return lines.Sum(line => line != null ? shop.GetBuyPrice(line.entry, line.bundles) : 0f);
    }

    public float GetTotalPrice(ShopCatalog shop, PlayerController player) {
        if(shop == null) {
            return 0f;
        }

        return lines.Sum(line => line != null ? shop.GetBuyPrice(player, line.entry, line.bundles) : 0f);
    }

    public bool TryCheckout(ShopCatalog shop, PlayerController player, out string failureMessage) {
        if(shop == null) {
            failureMessage = "No shop selected.";
            return false;
        }

        return shop.TryCheckoutCart(player, this, out failureMessage);
    }
}

[Serializable]
public class ShopCartLine {
    [Tooltip("Offer selected in this cart line.")]
    public ShopCatalogEntry entry;
    [Tooltip("Number of purchase bundles selected.")]
    [Min(1)]
    public int bundles = 1;
}
