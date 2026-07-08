using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ShopCatalogType {
    GeneralMarket,
    PokeMart,
    Specialist,
    Apothecary,
    PokeballSmith,
    BaitShop,
    FarmerMarket,
    TransitVendor,
    PoliceSupply,
    ResearchSupply
}

[CreateAssetMenu(menuName = "Shops/Shop Catalog Definition")]
public class ShopCatalogDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this shop catalog. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in shop UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this shop catalog.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad catalog type used by filters, UI and future shop rules.")]
    [SerializeField] ShopCatalogType catalogType = ShopCatalogType.GeneralMarket;
    [Tooltip("Free-form tags used by shop UI, access rules and future world logic.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Pricing")]
    [Tooltip("Multiplier applied to all buy prices in this catalog.")]
    [Min(0f)]
    [SerializeField] float buyPriceMultiplier = 1f;
    [Tooltip("Multiplier applied to all sell prices in this catalog.")]
    [Min(0f)]
    [SerializeField] float sellPriceMultiplier = 1f;
    [Tooltip("If enabled, this shop accepts item selling through ShopCatalog.")]
    [SerializeField] bool allowSelling = true;

    [Header("Access")]
    [Tooltip("Optional title, badge or permit required to use this catalog.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this catalog.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Message used when catalog-level access is blocked.")]
    [SerializeField] string lockedMessage = "This shop is not available yet.";

    [Header("Stock")]
    [Tooltip("Items/models sold by this catalog.")]
    [SerializeField] List<ShopCatalogEntry> entries = new List<ShopCatalogEntry>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public ShopCatalogType CatalogType => catalogType;
    public IReadOnlyList<string> Tags => tags;
    public float BuyPriceMultiplier => Mathf.Max(0f, buyPriceMultiplier);
    public float SellPriceMultiplier => Mathf.Max(0f, sellPriceMultiplier);
    public bool AllowSelling => allowSelling;
    public TitleDefinition RequiredTitle => requiredTitle;
    public ReputationFactionDefinition RequiredFaction => requiredFaction;
    public int RequiredReputation => requiredReputation;
    public string LockedMessage => lockedMessage;
    public IReadOnlyList<ShopCatalogEntry> Entries => entries;

    public bool IsUnlocked(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public List<ShopCatalogEntry> GetAvailableEntries(PlayerController player, string shopId, PlayerShopLedger ledger) {
        if(!IsUnlocked(player, out _)) {
            return new List<ShopCatalogEntry>();
        }

        return (entries ?? new List<ShopCatalogEntry>())
            .Where(e => e != null && e.GetItem() != null)
            .Where(e => !e.HiddenWhenLocked || e.IsUnlocked(player, out _))
            .Where(e => e.GetRemainingStock(shopId, ledger) != 0)
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.DisplayName)
            .ToList();
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        foreach(var entry in tags) {
            if(string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }
}

[System.Serializable]
public class ShopCatalogEntry {
    [Header("Identity")]
    [Tooltip("Stable offer id inside this catalog. Empty uses model id or item asset name.")]
    public string id;
    [Tooltip("Optional sort order used by future shop UI.")]
    public int sortOrder;

    [Header("Product")]
    [Tooltip("Model/brand metadata sold by this offer.")]
    public ItemModelDefinition model;
    [Tooltip("Direct item override. If assigned, this item is sold instead of the model item.")]
    public ItemBase itemOverride;
    [Tooltip("How many inventory items are granted per purchase bundle.")]
    [Min(1)]
    public int bundleCount = 1;

    [Header("Pricing")]
    [Tooltip("Offer-specific price override for one bundle. 0 uses model/item price.")]
    [Min(0f)]
    public float priceOverride;
    [Tooltip("Offer-specific multiplier applied after catalog and model multipliers.")]
    [Min(0f)]
    public float priceMultiplier = 1f;

    [Header("Stock")]
    [Tooltip("Maximum number of bundles available in the selected stock period. 0 means unlimited.")]
    [Min(0)]
    public int stockLimit;
    [Tooltip("How stock limits are counted for this offer.")]
    public ShopStockLimitPeriod stockLimitPeriod = ShopStockLimitPeriod.None;

    [Header("Access")]
    [Tooltip("Optional title, badge or permit required before this offer is available.")]
    public TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this offer.")]
    public ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    public int requiredReputation;
    [Tooltip("Optional recipe the player must know before this offer appears.")]
    public RecipeDefinition requiredKnownRecipe;
    [Tooltip("Message used when this offer is locked.")]
    public string lockedMessage = "This item is not available yet.";
    [Tooltip("If enabled, locked offers are hidden from GetAvailableEntries.")]
    public bool hiddenWhenLocked = true;

    public string OfferId {
        get {
            if(!string.IsNullOrWhiteSpace(id)) {
                return id;
            }

            if(model != null) {
                return model.Id;
            }

            var item = GetItem();
            return item != null ? item.name : "offer";
        }
    }
    public string DisplayName => model != null ? model.DisplayName : GetItem() != null ? GetItem().Name : OfferId;
    public int SortOrder => sortOrder;
    public bool HiddenWhenLocked => hiddenWhenLocked;
    public bool HasStockLimit => stockLimit > 0 && stockLimitPeriod != ShopStockLimitPeriod.None;

    public ItemBase GetItem() {
        return itemOverride != null ? itemOverride : model != null ? model.Item : null;
    }

    public int GetBundleCount() {
        return Mathf.Max(1, bundleCount);
    }

    public float GetBuyPrice(float catalogMultiplier) {
        if(priceOverride > 0f) {
            return Mathf.Max(0f, Mathf.Ceil(priceOverride * Mathf.Max(0f, priceMultiplier) * Mathf.Max(0f, catalogMultiplier)));
        }

        if(model != null) {
            return Mathf.Max(0f, Mathf.Ceil(model.GetBuyPrice(catalogMultiplier) * Mathf.Max(0f, priceMultiplier)));
        }

        var item = GetItem();
        return item != null
            ? Mathf.Max(0f, Mathf.Ceil(item.Price * Mathf.Max(0f, priceMultiplier) * Mathf.Max(0f, catalogMultiplier)))
            : 0f;
    }

    public int GetRemainingStock(string shopId, PlayerShopLedger ledger) {
        if(!HasStockLimit) {
            return -1;
        }

        int purchased = ledger != null ? ledger.GetPurchasedCount(shopId, OfferId, stockLimitPeriod) : 0;
        return Mathf.Max(0, stockLimit - purchased);
    }

    public bool HasEnoughStock(string shopId, PlayerShopLedger ledger, int count) {
        if(!HasStockLimit) {
            return true;
        }

        return ledger != null && GetRemainingStock(shopId, ledger) >= Mathf.Max(1, count);
    }

    public bool IsUnlocked(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredKnownRecipe != null && !(player?.GetComponent<PlayerRecipeBook>()?.KnowsRecipe(requiredKnownRecipe) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need to learn {requiredKnownRecipe.DisplayName} first." : lockedMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }
}
