using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SponsorKind {
    Brand,
    Shop,
    Competition,
    Venue,
    Research,
    Police,
    Transit,
    Care,
    Custom
}

public enum SponsorGrantMode {
    AlwaysRefreshOrAdd,
    OnceEver,
    RefreshExistingOnly
}

public enum SponsorBenefitType {
    ShopBuyPrice,
    ShopSellPrice,
    CompetitionPrize,
    AssignmentReward,
    Custom
}

[CreateAssetMenu(menuName = "Sponsors/Sponsor Definition")]
public class SponsorDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this sponsor. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future sponsor, shop or competition UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this sponsor agreement.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad sponsor kind used by filters and future UI styling.")]
    [SerializeField] SponsorKind kind = SponsorKind.Brand;
    [Tooltip("Optional icon used by future sponsor UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as kanto, market, medicine, frontier, professor, police or premium.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Shop and Brand Filters")]
    [Tooltip("Optional exact item brand this sponsor applies to. Empty allows any brand.")]
    [SerializeField] ItemBrandDefinition itemBrandFilter;
    [Tooltip("Optional exact shop catalog this sponsor applies to. Empty allows any catalog.")]
    [SerializeField] ShopCatalogDefinition shopCatalogFilter;
    [Tooltip("If enabled, the sponsor only applies to the selected shop catalog type.")]
    [SerializeField] bool filterShopCatalogType;
    [Tooltip("Shop catalog type required when Filter Shop Catalog Type is enabled.")]
    [SerializeField] ShopCatalogType shopCatalogTypeFilter = ShopCatalogType.GeneralMarket;
    [Tooltip("Catalog tags required before the sponsor applies. Empty means no catalog tag filter.")]
    [SerializeField] List<string> requiredCatalogTags = new List<string>();
    [Tooltip("How required catalog tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode catalogTagMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("If enabled, the sponsor only applies to the selected item model category.")]
    [SerializeField] bool filterItemModelCategory;
    [Tooltip("Item model category required when Filter Item Model Category is enabled.")]
    [SerializeField] ItemModelCategory itemModelCategoryFilter = ItemModelCategory.General;
    [Tooltip("Item model or brand tags required before the sponsor applies. Empty means no item tag filter.")]
    [SerializeField] List<string> requiredItemModelTags = new List<string>();
    [Tooltip("How required item model tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode itemModelTagMatchMode = ConsequenceRequirementMatchMode.All;

    [Header("Competition Filters")]
    [Tooltip("Optional exact competition this sponsor is associated with. Empty allows any competition.")]
    [SerializeField] CompetitionDefinition competitionFilter;
    [Tooltip("Optional exact venue this sponsor is associated with. Empty allows any venue.")]
    [SerializeField] CompetitionVenueDefinition venueFilter;
    [Tooltip("Optional region this sponsor is associated with. Empty allows any region.")]
    [SerializeField] WorldRegionDefinition regionFilter;

    [Header("Benefits")]
    [Tooltip("Multiplier applied to matching shop buy prices. 0.8 means 20 percent discount.")]
    [Min(0f)]
    [SerializeField] float buyPriceMultiplier = 1f;
    [Tooltip("Multiplier applied to matching shop sell values. 1.2 means 20 percent more money when selling.")]
    [Min(0f)]
    [SerializeField] float sellPriceMultiplier = 1f;
    [Tooltip("Multiplier future competition systems can use for sponsor prize money.")]
    [Min(0f)]
    [SerializeField] float prizeMoneyMultiplier = 1f;
    [Tooltip("Sponsor points granted each time this sponsor is awarded or refreshed.")]
    [Min(0)]
    [SerializeField] int sponsorPointsOnGrant;

    [Header("Grant Rules")]
    [Tooltip("How repeated grants of this sponsor are handled.")]
    [SerializeField] SponsorGrantMode grantMode = SponsorGrantMode.AlwaysRefreshOrAdd;
    [Tooltip("If enabled, this sponsor expires after Default Duration Hours.")]
    [SerializeField] bool expires;
    [Tooltip("In-game hours this sponsor remains active when Expires is enabled.")]
    [Min(0)]
    [SerializeField] int defaultDurationHours = 72;
    [Tooltip("If enabled, granting this sponsor again refreshes its expiration time.")]
    [SerializeField] bool refreshExpirationOnGrant = true;

    [Header("Access")]
    [Tooltip("How grant requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this sponsor can be granted.")]
    [SerializeField] List<ActivityRequirement> grantRequirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this sponsor cannot be granted or used.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This sponsor is not available yet.";

    [Header("Events")]
    [Tooltip("Optional event published when this sponsor is granted.")]
    [SerializeField] GameEventDefinition grantedEvent;
    [Tooltip("Optional event published when this sponsor benefit is used.")]
    [SerializeField] GameEventDefinition benefitUsedEvent;
    [Tooltip("If enabled, sponsor events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, sponsor events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public SponsorKind Kind => kind;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public ItemBrandDefinition ItemBrandFilter => itemBrandFilter;
    public ShopCatalogDefinition ShopCatalogFilter => shopCatalogFilter;
    public bool FilterShopCatalogType => filterShopCatalogType;
    public ShopCatalogType ShopCatalogTypeFilter => shopCatalogTypeFilter;
    public IReadOnlyList<string> RequiredCatalogTags => requiredCatalogTags != null ? (IReadOnlyList<string>)requiredCatalogTags : Array.Empty<string>();
    public bool FilterItemModelCategory => filterItemModelCategory;
    public ItemModelCategory ItemModelCategoryFilter => itemModelCategoryFilter;
    public IReadOnlyList<string> RequiredItemModelTags => requiredItemModelTags != null ? (IReadOnlyList<string>)requiredItemModelTags : Array.Empty<string>();
    public CompetitionDefinition CompetitionFilter => competitionFilter;
    public CompetitionVenueDefinition VenueFilter => venueFilter;
    public WorldRegionDefinition RegionFilter => regionFilter;
    public float BuyPriceMultiplier => Mathf.Max(0f, buyPriceMultiplier);
    public float SellPriceMultiplier => Mathf.Max(0f, sellPriceMultiplier);
    public float PrizeMoneyMultiplier => Mathf.Max(0f, prizeMoneyMultiplier);
    public int SponsorPointsOnGrant => Mathf.Max(0, sponsorPointsOnGrant);
    public SponsorGrantMode GrantMode => grantMode;
    public bool Expires => expires;
    public int DefaultDurationHours => Mathf.Max(0, defaultDurationHours);
    public bool RefreshExpirationOnGrant => refreshExpirationOnGrant;
    public IReadOnlyList<ActivityRequirement> GrantRequirements => grantRequirements != null ? (IReadOnlyList<ActivityRequirement>)grantRequirements : Array.Empty<ActivityRequirement>();

    public bool CanGrant(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to grant sponsors.";
            return false;
        }

        var log = player.GetComponent<PlayerSponsorLog>();
        if(log == null && grantMode == SponsorGrantMode.RefreshExistingOnly) {
            failureMessage = $"{DisplayName} cannot be refreshed because it is not owned.";
            return false;
        }

        if(log != null && !log.CanGrant(this, out failureMessage)) {
            return false;
        }

        if(!RequirementsMet(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryGrant(PlayerController player, string sourceId, out PlayerSponsorRecord record, out string failureMessage) {
        record = null;
        if(!CanGrant(player, out failureMessage)) {
            return false;
        }

        var log = player.GetComponent<PlayerSponsorLog>() ?? player.gameObject.AddComponent<PlayerSponsorLog>();
        record = log.RecordGrant(this, sourceId);
        PublishSponsorEvent(grantedEvent, "granted", $"{DisplayName} sponsorship granted.", GameEventImportance.Success, player, sourceId, SponsorBenefitType.Custom, null, null, 1f);
        failureMessage = null;
        return true;
    }

    public bool AppliesToShop(ShopCatalogDefinition catalog, ShopCatalogEntry entry) {
        if(shopCatalogFilter != null && catalog != shopCatalogFilter) {
            return false;
        }

        if(filterShopCatalogType && (catalog == null || catalog.CatalogType != shopCatalogTypeFilter)) {
            return false;
        }

        if(!MatchesTags(requiredCatalogTags, catalogTagMatchMode, tag => catalog != null && catalog.HasTag(tag))) {
            return false;
        }

        var model = entry != null ? entry.model : null;
        if(itemBrandFilter != null && (model == null || model.Brand != itemBrandFilter)) {
            return false;
        }

        if(filterItemModelCategory && (model == null || model.Category != itemModelCategoryFilter)) {
            return false;
        }

        if(!MatchesTags(requiredItemModelTags, itemModelTagMatchMode, tag => model != null && model.HasTag(tag))) {
            return false;
        }

        return true;
    }

    public bool MatchesCompetition(CompetitionDefinition competition, CompetitionVenueDefinition venue, WorldRegionDefinition region = null) {
        if(competitionFilter != null && competition != competitionFilter) {
            return false;
        }

        if(venueFilter != null && venue != venueFilter) {
            return false;
        }

        if(regionFilter != null && region != regionFilter) {
            return false;
        }

        return true;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishBenefitUsed(PlayerController player, string sourceId, SponsorBenefitType benefitType, string targetId, string targetName, float multiplier) {
        PublishSponsorEvent(benefitUsedEvent, "benefit-used", $"{DisplayName} sponsor benefit used.", GameEventImportance.Info, player, sourceId, benefitType, targetId, targetName, multiplier);
    }

    bool RequirementsMet(PlayerController player, out string failureMessage) {
        var activeRequirements = grantRequirements?.Where(requirement => requirement != null).ToList() ?? new List<ActivityRequirement>();
        if(activeRequirements.Count == 0) {
            failureMessage = null;
            return true;
        }

        if(requirementMatchMode == ConsequenceRequirementMatchMode.Any) {
            foreach(var requirement in activeRequirements) {
                if(requirement.IsMet(player)) {
                    failureMessage = null;
                    return true;
                }
            }

            failureMessage = activeRequirements.FirstOrDefault()?.FailureMessage ?? lockedMessage;
            return false;
        }

        foreach(var requirement in activeRequirements) {
            if(!requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
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

    void PublishSponsorEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, string sourceId, SponsorBenefitType benefitType, string targetId, string targetName, float multiplier) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"sponsor.{phase}.{Id}.{sourceId}",
            message,
            GameEventCategory.Shop,
            importance,
            player != null ? player : this,
            "SponsorDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("sponsorId", Id),
            GameEventPublishing.Value("sponsorName", DisplayName),
            GameEventPublishing.Value("kind", kind),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("benefitType", benefitType),
            GameEventPublishing.Value("targetId", targetId),
            GameEventPublishing.Value("targetName", targetName),
            GameEventPublishing.Value("multiplier", multiplier),
            GameEventPublishing.Value("sourceId", sourceId));
    }
}
