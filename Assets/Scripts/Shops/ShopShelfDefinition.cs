using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ShopShelfKind {
    General,
    Medicine,
    Pokeball,
    Bait,
    Food,
    Tools,
    Crafting,
    PokemonCare,
    Premium,
    Seasonal,
    Clearance,
    Custom
}

public enum ShopShelfSortMode {
    CatalogOrder,
    DisplayName,
    PriceAscending,
    PriceDescending,
    QualityDescending,
    EffectivenessDescending
}

[CreateAssetMenu(menuName = "Shops/Shop Shelf Definition")]
public class ShopShelfDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this shelf. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future market shelf UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing description for this shelf.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad shelf kind used by filters, requirements and future UI styling.")]
    [SerializeField] ShopShelfKind kind = ShopShelfKind.General;
    [Tooltip("Optional icon used by future shelf UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as kanto, medicine, premium, sale, professor, police or local.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this shelf can be browsed.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this shelf can be browsed.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this shelf.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional world event whose active state gates this shelf.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for Required World Event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("How extra browse requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this shelf can be browsed.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this shelf is locked.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This shelf is not available yet.";

    [Header("Catalog Filters")]
    [Tooltip("If enabled, the shelf only works with the selected shop catalog type.")]
    [SerializeField] bool filterShopCatalogType;
    [Tooltip("Shop catalog type required when Filter Shop Catalog Type is enabled.")]
    [SerializeField] ShopCatalogType shopCatalogTypeFilter = ShopCatalogType.GeneralMarket;
    [Tooltip("Catalog tags required before this shelf applies. Empty means no catalog tag filter.")]
    [SerializeField] List<string> requiredCatalogTags = new List<string>();
    [Tooltip("How required catalog tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode catalogTagMatchMode = ConsequenceRequirementMatchMode.All;

    [Header("Offer Selection")]
    [Tooltip("If enabled, explicit offer ids below are included when they exist in the assigned shop.")]
    [SerializeField] bool includeExplicitOffers = true;
    [Tooltip("Offer ids from the assigned shop catalog that should appear on this shelf.")]
    [SerializeField] List<string> explicitOfferIds = new List<string>();
    [Tooltip("If enabled, catalog offers matching the filter fields below are included.")]
    [SerializeField] bool includeFilteredCatalogOffers = true;
    [Tooltip("If enabled, only currently available/unlocked/in-stock offers are returned.")]
    [SerializeField] bool onlyAvailableOffers = true;
    [Tooltip("Optional model categories allowed on this shelf. Empty allows any model category.")]
    [SerializeField] List<ItemModelCategory> allowedModelCategories = new List<ItemModelCategory>();
    [Tooltip("Optional brand required for model-backed offers. Empty allows any brand.")]
    [SerializeField] ItemBrandDefinition requiredBrand;
    [Tooltip("Offer/model/brand tags required before an offer appears. Empty means no offer tag filter.")]
    [SerializeField] List<string> requiredOfferTags = new List<string>();
    [Tooltip("How required offer tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode offerTagMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Minimum model quality tier required. 0 means no minimum.")]
    [Min(0)]
    [SerializeField] int minimumQualityTier;
    [Tooltip("Maximum model quality tier allowed. 0 means no maximum.")]
    [Min(0)]
    [SerializeField] int maximumQualityTier;
    [Tooltip("Maximum number of offers returned by GetShelfOffers. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxDisplayedOffers;
    [Tooltip("How offers are ordered after filtering.")]
    [SerializeField] ShopShelfSortMode sortMode = ShopShelfSortMode.CatalogOrder;

    [Header("Default Add")]
    [Tooltip("Offer id used by Add Default Offer actions. Empty uses the first visible offer.")]
    [SerializeField] string defaultOfferId = string.Empty;
    [Tooltip("Default bundle count used by Add Default Offer and Add First Visible Offer actions.")]
    [Min(1)]
    [SerializeField] int defaultBundles = 1;

    [Header("Events")]
    [Tooltip("Optional event published when this shelf is viewed. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition viewedEvent;
    [Tooltip("Optional event published when this shelf adds an offer to the basket. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition offerAddedEvent;
    [Tooltip("Optional event published when this shelf action is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, shelf events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, shelf events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public ShopShelfKind Kind => kind;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public TitleDefinition RequiredTitle => requiredTitle;
    public MilestoneDefinition RequiredMilestone => requiredMilestone;
    public ReputationFactionDefinition RequiredFaction => requiredFaction;
    public int RequiredReputation => requiredReputation;
    public WorldEventDefinition RequiredWorldEvent => requiredWorldEvent;
    public bool RequiredWorldEventActive => requiredWorldEventActive;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public bool FilterShopCatalogType => filterShopCatalogType;
    public ShopCatalogType ShopCatalogTypeFilter => shopCatalogTypeFilter;
    public IReadOnlyList<string> RequiredCatalogTags => requiredCatalogTags != null ? (IReadOnlyList<string>)requiredCatalogTags : Array.Empty<string>();
    public bool IncludeExplicitOffers => includeExplicitOffers;
    public IReadOnlyList<string> ExplicitOfferIds => explicitOfferIds != null ? (IReadOnlyList<string>)explicitOfferIds : Array.Empty<string>();
    public bool IncludeFilteredCatalogOffers => includeFilteredCatalogOffers;
    public bool OnlyAvailableOffers => onlyAvailableOffers;
    public IReadOnlyList<ItemModelCategory> AllowedModelCategories => allowedModelCategories != null ? (IReadOnlyList<ItemModelCategory>)allowedModelCategories : Array.Empty<ItemModelCategory>();
    public ItemBrandDefinition RequiredBrand => requiredBrand;
    public IReadOnlyList<string> RequiredOfferTags => requiredOfferTags != null ? (IReadOnlyList<string>)requiredOfferTags : Array.Empty<string>();
    public int MinimumQualityTier => Mathf.Max(0, minimumQualityTier);
    public int MaximumQualityTier => Mathf.Max(0, maximumQualityTier);
    public int MaxDisplayedOffers => Mathf.Max(0, maxDisplayedOffers);
    public ShopShelfSortMode SortMode => sortMode;
    public string DefaultOfferId => defaultOfferId;
    public int DefaultBundles => Mathf.Max(1, defaultBundles);

    public bool CanBrowse(PlayerController player, ShopCatalog shop, out string failureMessage) {
        if(shop == null || shop.Catalog == null) {
            failureMessage = "A shop catalog is required to browse this shelf.";
            return false;
        }

        if(filterShopCatalogType && shop.Catalog.CatalogType != shopCatalogTypeFilter) {
            failureMessage = $"{DisplayName} is not available in this shop type.";
            return false;
        }

        if(!MatchesTags(requiredCatalogTags, catalogTagMatchMode, tag => shop.Catalog.HasTag(tag))) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not available in this shop." : lockedMessage;
            return false;
        }

        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredMilestone != null && !(player?.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
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

        failureMessage = null;
        return true;
    }

    public List<ShopCatalogEntry> GetShelfOffers(ShopCatalog shop, PlayerController player, out string failureMessage) {
        if(!CanBrowse(player, shop, out failureMessage)) {
            return new List<ShopCatalogEntry>();
        }

        var candidates = onlyAvailableOffers
            ? shop.GetAvailableOffers(player)
            : shop.Catalog.Entries.Where(entry => entry != null && entry.GetItem() != null).ToList();

        var selected = new List<ShopCatalogEntry>();
        if(includeExplicitOffers) {
            foreach(var offerId in ExplicitOfferIds.Where(id => !string.IsNullOrWhiteSpace(id))) {
                var entry = candidates.FirstOrDefault(item => item != null && item.OfferId == offerId);
                if(entry != null && !selected.Contains(entry)) {
                    selected.Add(entry);
                }
            }
        }

        if(includeFilteredCatalogOffers) {
            foreach(var entry in candidates) {
                if(entry != null && MatchesOffer(entry) && !selected.Contains(entry)) {
                    selected.Add(entry);
                }
            }
        }

        selected = SortOffers(shop, player, selected).ToList();
        if(MaxDisplayedOffers > 0) {
            selected = selected.Take(MaxDisplayedOffers).ToList();
        }

        failureMessage = null;
        return selected;
    }

    public ShopCatalogEntry GetDefaultOffer(ShopCatalog shop, PlayerController player, out string failureMessage) {
        var offers = GetShelfOffers(shop, player, out failureMessage);
        if(offers.Count == 0) {
            failureMessage ??= "This shelf has no visible offers.";
            return null;
        }

        if(!string.IsNullOrWhiteSpace(defaultOfferId)) {
            var defaultOffer = offers.FirstOrDefault(offer => offer != null && offer.OfferId == defaultOfferId);
            if(defaultOffer != null) {
                return defaultOffer;
            }
        }

        return offers.FirstOrDefault();
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishViewed(PlayerController player, ShopCatalog shop, UnityEngine.Object context) {
        PublishShelfEvent(viewedEvent, "viewed", $"{DisplayName} viewed.", GameEventImportance.Info, player, shop, null, 0, context);
    }

    public void PublishOfferAdded(PlayerController player, ShopCatalog shop, ShopCatalogEntry entry, int bundles, UnityEngine.Object context) {
        PublishShelfEvent(offerAddedEvent, "offer-added", $"{entry?.DisplayName ?? "Offer"} added from {DisplayName}.", GameEventImportance.Success, player, shop, entry, bundles, context);
    }

    public void PublishBlocked(PlayerController player, ShopCatalog shop, string failureMessage, UnityEngine.Object context) {
        PublishShelfEvent(blockedEvent, "blocked", string.IsNullOrWhiteSpace(failureMessage) ? $"{DisplayName} blocked." : failureMessage, GameEventImportance.Warning, player, shop, null, 0, context);
    }

    bool MatchesOffer(ShopCatalogEntry entry) {
        if(entry == null || entry.GetItem() == null) {
            return false;
        }

        var model = entry.model;
        if(allowedModelCategories != null && allowedModelCategories.Count > 0) {
            if(model == null || !allowedModelCategories.Contains(model.Category)) {
                return false;
            }
        }

        if(requiredBrand != null && (model == null || model.Brand != requiredBrand)) {
            return false;
        }

        if(MinimumQualityTier > 0 && (model == null || model.QualityTier < MinimumQualityTier)) {
            return false;
        }

        if(MaximumQualityTier > 0 && model != null && model.QualityTier > MaximumQualityTier) {
            return false;
        }

        return MatchesTags(requiredOfferTags, offerTagMatchMode, tag => OfferHasTag(entry, tag));
    }

    bool OfferHasTag(ShopCatalogEntry entry, string tag) {
        if(string.IsNullOrWhiteSpace(tag) || entry == null) {
            return false;
        }

        if(string.Equals(entry.OfferId, tag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.DisplayName, tag, StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        var item = entry.GetItem();
        if(item != null
            && (string.Equals(item.name, tag, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.Name, tag, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.ItemType.ToString(), tag, StringComparison.OrdinalIgnoreCase))) {
            return true;
        }

        return entry.model != null && entry.model.HasTag(tag);
    }

    IEnumerable<ShopCatalogEntry> SortOffers(ShopCatalog shop, PlayerController player, List<ShopCatalogEntry> offers) {
        switch(sortMode) {
            case ShopShelfSortMode.DisplayName:
                return offers.OrderBy(entry => entry.DisplayName);
            case ShopShelfSortMode.PriceAscending:
                return offers.OrderBy(entry => shop.GetBuyPrice(player, entry, 1)).ThenBy(entry => entry.DisplayName);
            case ShopShelfSortMode.PriceDescending:
                return offers.OrderByDescending(entry => shop.GetBuyPrice(player, entry, 1)).ThenBy(entry => entry.DisplayName);
            case ShopShelfSortMode.QualityDescending:
                return offers.OrderByDescending(entry => entry.model != null ? entry.model.QualityTier : 0).ThenBy(entry => entry.DisplayName);
            case ShopShelfSortMode.EffectivenessDescending:
                return offers.OrderByDescending(entry => entry.model != null ? entry.model.EffectivenessMultiplier : 0f).ThenBy(entry => entry.DisplayName);
            default:
                return offers.OrderBy(entry => entry.SortOrder).ThenBy(entry => entry.DisplayName);
        }
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

    void PublishShelfEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, ShopCatalog shop, ShopCatalogEntry entry, int bundles, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"shop.shelf.{phase}.{Id}.{shop?.ShopId ?? "shop"}",
            message,
            GameEventCategory.Shop,
            importance,
            context != null ? context : player,
            "ShopShelfDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("shelfId", Id),
            GameEventPublishing.Value("shelfName", DisplayName),
            GameEventPublishing.Value("kind", kind),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("shopId", shop != null ? shop.ShopId : string.Empty),
            GameEventPublishing.Value("offerId", entry != null ? entry.OfferId : string.Empty),
            GameEventPublishing.Value("offerName", entry != null ? entry.DisplayName : string.Empty),
            GameEventPublishing.Value("bundles", bundles));
    }
}
