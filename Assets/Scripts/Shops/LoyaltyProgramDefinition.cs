using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum LoyaltyProgramKind {
    General,
    Shop,
    Brand,
    Clinic,
    Inn,
    PokemonCare,
    Research,
    Police,
    Transit,
    Regional,
    Custom
}

public enum LoyaltyProgramGrantMode {
    JoinOrRefresh,
    OnceEver,
    RefreshExistingOnly
}

public enum LoyaltyPointSourceKind {
    Manual,
    ShopPurchase,
    ShopSell,
    Service,
    ServicePackage,
    LearnableOffer,
    Assignment,
    Custom
}

[CreateAssetMenu(menuName = "Shops/Loyalty Program Definition")]
public class LoyaltyProgramDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this loyalty program. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future membership/shop UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing description for this loyalty program.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad program kind used by filters, requirements and future UI.")]
    [SerializeField] LoyaltyProgramKind kind = LoyaltyProgramKind.General;
    [Tooltip("Optional icon used by future membership/shop UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as kanto, market, clinic, inn, premium, herbalist or professor.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Shop And Item Filters")]
    [Tooltip("Optional exact shop catalog this program applies to. Empty allows any catalog.")]
    [SerializeField] ShopCatalogDefinition shopCatalogFilter;
    [Tooltip("If enabled, the program only applies to the selected shop catalog type.")]
    [SerializeField] bool filterShopCatalogType;
    [Tooltip("Shop catalog type required when Filter Shop Catalog Type is enabled.")]
    [SerializeField] ShopCatalogType shopCatalogTypeFilter = ShopCatalogType.GeneralMarket;
    [Tooltip("Catalog tags required before the program applies. Empty means no catalog tag filter.")]
    [SerializeField] List<string> requiredCatalogTags = new List<string>();
    [Tooltip("How required catalog tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode catalogTagMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Optional exact item brand this program applies to. Empty allows any brand.")]
    [SerializeField] ItemBrandDefinition itemBrandFilter;
    [Tooltip("If enabled, the program only applies to the selected item model category.")]
    [SerializeField] bool filterItemModelCategory;
    [Tooltip("Item model category required when Filter Item Model Category is enabled.")]
    [SerializeField] ItemModelCategory itemModelCategoryFilter = ItemModelCategory.General;
    [Tooltip("Item model or brand tags required before the program applies. Empty means no item tag filter.")]
    [SerializeField] List<string> requiredItemModelTags = new List<string>();
    [Tooltip("How required item model tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode itemModelTagMatchMode = ConsequenceRequirementMatchMode.All;

    [Header("Joining")]
    [Tooltip("How repeated grants or refreshes of this program are handled.")]
    [SerializeField] LoyaltyProgramGrantMode grantMode = LoyaltyProgramGrantMode.JoinOrRefresh;
    [Tooltip("Money paid when joining or refreshing this program. 0 means free.")]
    [Min(0f)]
    [SerializeField] float joinCost;
    [Tooltip("Starting points granted when this program is joined for the first time.")]
    [Min(0)]
    [SerializeField] int startingPoints;
    [Tooltip("Points granted whenever an existing membership is refreshed.")]
    [Min(0)]
    [SerializeField] int refreshPoints;
    [Tooltip("If enabled, this membership expires after Default Duration Hours.")]
    [SerializeField] bool expires;
    [Tooltip("In-game hours this membership remains active when Expires is enabled.")]
    [Min(0)]
    [SerializeField] int defaultDurationHours = 168;
    [Tooltip("If enabled, joining again refreshes expiration time.")]
    [SerializeField] bool refreshExpirationOnGrant = true;
    [Tooltip("If enabled, the player can be auto-joined when a matching shop purchase/sell would grant points.")]
    [SerializeField] bool autoJoinOnFirstPointGain;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this program can be joined.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this program can be joined.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this program.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional world event whose active state gates this program.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for Required World Event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("How extra join requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this program can be joined.")]
    [SerializeField] List<ActivityRequirement> joinRequirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this program is locked.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This membership is not available yet.";

    [Header("Point Rules")]
    [Tooltip("If enabled, matching shop purchases can grant points.")]
    [SerializeField] bool earnFromShopPurchases = true;
    [Tooltip("Points awarded for each full money unit spent on matching shop purchases.")]
    [Min(0f)]
    [SerializeField] float pointsPerMoneySpent = 0.05f;
    [Tooltip("Flat points awarded once per matching shop purchase transaction.")]
    [Min(0)]
    [SerializeField] int flatPointsPerPurchase;
    [Tooltip("Points awarded per purchased bundle in matching shop purchases.")]
    [Min(0)]
    [SerializeField] int pointsPerPurchasedBundle;
    [Tooltip("If enabled, matching shop sells can grant points.")]
    [SerializeField] bool earnFromShopSells;
    [Tooltip("Points awarded for each full money unit gained from matching item sells.")]
    [Min(0f)]
    [SerializeField] float pointsPerMoneySold;
    [Tooltip("Flat points awarded once per matching shop sell transaction.")]
    [Min(0)]
    [SerializeField] int flatPointsPerSell;

    [Header("Tiers")]
    [Tooltip("Tier definitions sorted by minimum points at runtime. Higher tier benefits can discount buying or improve selling.")]
    [SerializeField] List<LoyaltyTierDefinition> tiers = new List<LoyaltyTierDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this membership is joined or refreshed.")]
    [SerializeField] GameEventDefinition joinedEvent;
    [Tooltip("Optional event published when loyalty points are gained.")]
    [SerializeField] GameEventDefinition pointsGainedEvent;
    [Tooltip("Optional event published when a new tier is unlocked.")]
    [SerializeField] GameEventDefinition tierUnlockedEvent;
    [Tooltip("If enabled, loyalty events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, loyalty events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public LoyaltyProgramKind Kind => kind;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public ShopCatalogDefinition ShopCatalogFilter => shopCatalogFilter;
    public bool FilterShopCatalogType => filterShopCatalogType;
    public ShopCatalogType ShopCatalogTypeFilter => shopCatalogTypeFilter;
    public IReadOnlyList<string> RequiredCatalogTags => requiredCatalogTags != null ? (IReadOnlyList<string>)requiredCatalogTags : Array.Empty<string>();
    public ItemBrandDefinition ItemBrandFilter => itemBrandFilter;
    public bool FilterItemModelCategory => filterItemModelCategory;
    public ItemModelCategory ItemModelCategoryFilter => itemModelCategoryFilter;
    public IReadOnlyList<string> RequiredItemModelTags => requiredItemModelTags != null ? (IReadOnlyList<string>)requiredItemModelTags : Array.Empty<string>();
    public LoyaltyProgramGrantMode GrantMode => grantMode;
    public float JoinCost => Mathf.Max(0f, joinCost);
    public int StartingPoints => Mathf.Max(0, startingPoints);
    public int RefreshPoints => Mathf.Max(0, refreshPoints);
    public bool Expires => expires;
    public int DefaultDurationHours => Mathf.Max(0, defaultDurationHours);
    public bool RefreshExpirationOnGrant => refreshExpirationOnGrant;
    public bool AutoJoinOnFirstPointGain => autoJoinOnFirstPointGain;
    public TitleDefinition RequiredTitle => requiredTitle;
    public MilestoneDefinition RequiredMilestone => requiredMilestone;
    public ReputationFactionDefinition RequiredFaction => requiredFaction;
    public int RequiredReputation => requiredReputation;
    public WorldEventDefinition RequiredWorldEvent => requiredWorldEvent;
    public bool RequiredWorldEventActive => requiredWorldEventActive;
    public IReadOnlyList<ActivityRequirement> JoinRequirements => joinRequirements != null ? (IReadOnlyList<ActivityRequirement>)joinRequirements : Array.Empty<ActivityRequirement>();
    public bool EarnFromShopPurchases => earnFromShopPurchases;
    public float PointsPerMoneySpent => Mathf.Max(0f, pointsPerMoneySpent);
    public int FlatPointsPerPurchase => Mathf.Max(0, flatPointsPerPurchase);
    public int PointsPerPurchasedBundle => Mathf.Max(0, pointsPerPurchasedBundle);
    public bool EarnFromShopSells => earnFromShopSells;
    public float PointsPerMoneySold => Mathf.Max(0f, pointsPerMoneySold);
    public int FlatPointsPerSell => Mathf.Max(0, flatPointsPerSell);
    public IReadOnlyList<LoyaltyTierDefinition> Tiers => tiers != null ? (IReadOnlyList<LoyaltyTierDefinition>)tiers : Array.Empty<LoyaltyTierDefinition>();

    public bool CanJoin(PlayerController player, PlayerLoyaltyLog log, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to join loyalty programs.";
            return false;
        }

        if(log == null && grantMode == LoyaltyProgramGrantMode.RefreshExistingOnly) {
            failureMessage = $"{DisplayName} can only refresh an existing membership.";
            return false;
        }

        if(log != null && !log.CanJoin(this, out failureMessage)) {
            return false;
        }

        if(!AccessRequirementsMet(player, out failureMessage)) {
            return false;
        }

        if(!ConsequenceChainDefinition.RequirementsMet(player, joinRequirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        if(JoinCost > 0f && (Wallet.i == null || !Wallet.i.HasMoney(JoinCost))) {
            failureMessage = $"You need {JoinCost:0} money to join {DisplayName}.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryJoin(PlayerController player, string sourceId, out PlayerLoyaltyRecord record, out string failureMessage) {
        record = null;
        var log = player != null ? player.GetComponent<PlayerLoyaltyLog>() ?? player.gameObject.AddComponent<PlayerLoyaltyLog>() : null;
        if(!CanJoin(player, log, out failureMessage)) {
            return false;
        }

        if(JoinCost > 0f) {
            Wallet.i.TakeMoney(JoinCost);
        }

        record = log.RecordJoin(this, sourceId, JoinCost);
        PublishLoyaltyEvent(joinedEvent, "joined", $"{DisplayName} membership joined.", GameEventImportance.Success, player, sourceId, 0, record != null ? record.CurrentTierId : string.Empty);
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

    public LoyaltyTierDefinition GetTierForPoints(int points) {
        LoyaltyTierDefinition selected = null;
        foreach(var tier in Tiers.Where(tier => tier != null).OrderBy(tier => tier.MinimumPoints)) {
            if(points >= tier.MinimumPoints) {
                selected = tier;
            }
        }

        return selected;
    }

    public int GetTierIndexForPoints(int points) {
        var ordered = Tiers.Where(tier => tier != null).OrderBy(tier => tier.MinimumPoints).ToList();
        int index = -1;
        for(int i = 0; i < ordered.Count; i++) {
            if(points >= ordered[i].MinimumPoints) {
                index = i;
            }
        }

        return index;
    }

    public float GetBuyPriceMultiplier(int points) {
        return GetTierForPoints(points)?.BuyPriceMultiplier ?? 1f;
    }

    public float GetSellPriceMultiplier(int points) {
        return GetTierForPoints(points)?.SellPriceMultiplier ?? 1f;
    }

    public int CalculateShopPurchasePoints(float moneySpent, int bundles, int currentPoints) {
        if(!earnFromShopPurchases) {
            return 0;
        }

        float multiplier = GetTierForPoints(currentPoints)?.PointEarnMultiplier ?? 1f;
        float rawPoints = flatPointsPerPurchase
            + Mathf.Max(0f, moneySpent) * PointsPerMoneySpent
            + Mathf.Max(1, bundles) * PointsPerPurchasedBundle;
        return Mathf.Max(0, Mathf.FloorToInt(rawPoints * Mathf.Max(0f, multiplier)));
    }

    public int CalculateShopSellPoints(float moneyGained, int currentPoints) {
        if(!earnFromShopSells) {
            return 0;
        }

        float multiplier = GetTierForPoints(currentPoints)?.PointEarnMultiplier ?? 1f;
        float rawPoints = flatPointsPerSell + Mathf.Max(0f, moneyGained) * PointsPerMoneySold;
        return Mathf.Max(0, Mathf.FloorToInt(rawPoints * Mathf.Max(0f, multiplier)));
    }

    public void ApplyTierRewards(PlayerController player, PlayerLoyaltyRecord record, UnityEngine.Object context) {
        if(player == null || record == null) {
            return;
        }

        var ordered = Tiers.Where(tier => tier != null).OrderBy(tier => tier.MinimumPoints).ToList();
        foreach(var tier in ordered) {
            if(record.points < tier.MinimumPoints || record.HasUnlockedTier(tier.TierId)) {
                continue;
            }

            record.unlockedTierIds ??= new List<string>();
            record.unlockedTierIds.Add(tier.TierId);
            player.GetComponent<PlayerTitles>()?.ApplyGrants(tier.TitleGrants, context);
            player.GetComponent<PlayerMilestones>()?.CompleteMilestones(tier.MilestonesToComplete);
            player.GetComponent<PlayerReputation>()?.ApplyChanges(tier.ReputationChanges);
            player.GetComponent<PlayerLifestyleLog>()?.ApplyGrants(tier.LifestylePointGrants, $"loyalty:{Id}:{tier.TierId}", tier.DisplayName, context);
            PublishLoyaltyEvent(tierUnlockedEvent, "tier-unlocked", $"{tier.DisplayName} unlocked.", GameEventImportance.Success, player, record.sourceId, 0, tier.TierId);
        }

        var currentTier = GetTierForPoints(record.points);
        record.currentTierId = currentTier != null ? currentTier.TierId : string.Empty;
        record.currentTierName = currentTier != null ? currentTier.DisplayName : string.Empty;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishPointsGained(PlayerController player, string sourceId, int points, string tierId) {
        PublishLoyaltyEvent(pointsGainedEvent, "points-gained", $"{DisplayName} gained {points} point(s).", GameEventImportance.Info, player, sourceId, points, tierId);
    }

    bool AccessRequirementsMet(PlayerController player, out string failureMessage) {
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

    void PublishLoyaltyEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, string sourceId, int points, string tierId) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"loyalty.{phase}.{Id}.{sourceId}",
            message,
            GameEventCategory.Shop,
            importance,
            player != null ? player : this,
            "LoyaltyProgramDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("programId", Id),
            GameEventPublishing.Value("programName", DisplayName),
            GameEventPublishing.Value("kind", kind),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("points", points),
            GameEventPublishing.Value("tierId", tierId));
    }
}

[Serializable]
public class LoyaltyTierDefinition {
    [Header("Identity")]
    [Tooltip("Stable tier id inside this loyalty program. Empty uses Display Name or minimum points.")]
    [SerializeField] string tierId = string.Empty;
    [Tooltip("Name shown in future loyalty UI. Empty uses Tier Id.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing description for this tier.")]
    [TextArea]
    [SerializeField] string description = string.Empty;

    [Header("Threshold")]
    [Tooltip("Total points required to unlock this tier.")]
    [Min(0)]
    [SerializeField] int minimumPoints;

    [Header("Benefits")]
    [Tooltip("Multiplier applied to matching shop buy prices while this tier is active. 0.9 means 10 percent discount.")]
    [Min(0f)]
    [SerializeField] float buyPriceMultiplier = 1f;
    [Tooltip("Multiplier applied to matching shop sell values while this tier is active. 1.1 means 10 percent more money.")]
    [Min(0f)]
    [SerializeField] float sellPriceMultiplier = 1f;
    [Tooltip("Multiplier applied to future point gains for this program.")]
    [Min(0f)]
    [SerializeField] float pointEarnMultiplier = 1f;

    [Header("Unlock Rewards")]
    [Tooltip("Title, badge, permit or license grants applied once when this tier is unlocked.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Milestones completed once when this tier is unlocked.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Faction reputation changes applied once when this tier is unlocked.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Lifestyle/playstyle point grants applied once when this tier is unlocked.")]
    [SerializeField] List<LifestylePointGrant> lifestylePointGrants = new List<LifestylePointGrant>();

    public string TierId => string.IsNullOrWhiteSpace(tierId) ? (!string.IsNullOrWhiteSpace(displayName) ? displayName : $"tier-{MinimumPoints}") : tierId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? TierId : displayName;
    public string Description => description;
    public int MinimumPoints => Mathf.Max(0, minimumPoints);
    public float BuyPriceMultiplier => Mathf.Max(0f, buyPriceMultiplier);
    public float SellPriceMultiplier => Mathf.Max(0f, sellPriceMultiplier);
    public float PointEarnMultiplier => Mathf.Max(0f, pointEarnMultiplier);
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants != null ? (IReadOnlyList<TitleGrant>)titleGrants : Array.Empty<TitleGrant>();
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete != null ? (IReadOnlyList<MilestoneDefinition>)milestonesToComplete : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges != null ? (IReadOnlyList<ReputationChange>)reputationChanges : Array.Empty<ReputationChange>();
    public IReadOnlyList<LifestylePointGrant> LifestylePointGrants => lifestylePointGrants != null ? (IReadOnlyList<LifestylePointGrant>)lifestylePointGrants : Array.Empty<LifestylePointGrant>();
}
