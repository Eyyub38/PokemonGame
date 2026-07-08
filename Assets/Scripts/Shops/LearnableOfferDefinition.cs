using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum LearnableOfferCategory {
    General,
    Recipe,
    Permit,
    Research,
    PokeNavInfo,
    MapInfo,
    Training,
    Custom
}

[CreateAssetMenu(menuName = "Shops/Learnable Offer Definition")]
public class LearnableOfferDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this learnable offer. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future shop/tutorial UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing description for this offer.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad category used by filters, requirements and future shop UI.")]
    [SerializeField] LearnableOfferCategory category = LearnableOfferCategory.General;
    [Tooltip("Optional icon used by future shop UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as herbalist, pokeball, bait, police, professor, premium or tutorial.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Pricing")]
    [Tooltip("Base money cost before optional shop catalog and sponsor multipliers. 0 means free.")]
    [Min(0f)]
    [SerializeField] float basePrice;
    [Tooltip("If enabled, the source shop catalog's buy multiplier is applied to Base Price.")]
    [SerializeField] bool useShopPriceMultiplier = true;
    [Tooltip("If enabled, active sponsor shop-buy multipliers can discount this offer when a shop catalog context is available.")]
    [SerializeField] bool useSponsorDiscounts = true;

    [Header("Repeat")]
    [Tooltip("How often this offer can be purchased.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.OnceEver;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum successful purchases across all sources. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxPurchaseCount;
    [Tooltip("If disabled, purchase is blocked when all assigned rewards are already owned or completed.")]
    [SerializeField] bool allowRepurchaseWhenRewardsOwned;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this offer can be purchased.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this offer can be purchased.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this offer.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional known recipe required before this offer can be purchased.")]
    [SerializeField] RecipeDefinition requiredKnownRecipe;
    [Tooltip("Optional world event whose active state gates this offer.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for Required World Event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("How extra requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this offer can be purchased.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this offer is blocked.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This offer is not available yet.";

    [Header("Recipe Grants")]
    [Tooltip("Crafting recipes learned when this offer is purchased.")]
    [SerializeField] List<RecipeGrant> recipeGrants = new List<RecipeGrant>();

    [Header("PokeNav And Map Grants")]
    [Tooltip("Pokemon knowledge granted when this offer is purchased.")]
    [SerializeField] List<PokemonKnowledgeGrant> pokemonKnowledgeGrants = new List<PokemonKnowledgeGrant>();
    [Tooltip("Region cards discovered when this offer is purchased.")]
    [SerializeField] List<RegionInfoDefinition> regionsToDiscover = new List<RegionInfoDefinition>();
    [Tooltip("PokeNav entries discovered when this offer is purchased.")]
    [SerializeField] List<PokeNavEntryDefinition> pokeNavEntriesToDiscover = new List<PokeNavEntryDefinition>();
    [Tooltip("Social posts unlocked when this offer is purchased.")]
    [SerializeField] List<SocialPostDefinition> socialPostsToUnlock = new List<SocialPostDefinition>();
    [Tooltip("Map markers discovered when this offer is purchased.")]
    [SerializeField] List<MapMarkerDefinition> mapMarkersToDiscover = new List<MapMarkerDefinition>();

    [Header("Progression Grants")]
    [Tooltip("Title, badge, permit or license grants applied when this offer is purchased.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Milestones completed when this offer is purchased.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Research progress created or granted when this offer is purchased.")]
    [SerializeField] List<ResearchProgressGrant> researchProgressGrants = new List<ResearchProgressGrant>();

    [Header("Events")]
    [Tooltip("Optional event published when this offer is purchased. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition purchasedEvent;
    [Tooltip("If enabled, purchase events can appear in the notification/event feed.")]
    [SerializeField] bool showPurchaseEventInFeed = true;
    [Tooltip("If enabled, purchase events are written to GameDebugLogger.")]
    [SerializeField] bool writePurchaseEventToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public LearnableOfferCategory Category => category;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public float BasePrice => Mathf.Max(0f, basePrice);
    public bool UseShopPriceMultiplier => useShopPriceMultiplier;
    public bool UseSponsorDiscounts => useSponsorDiscounts;
    public ConsequenceChainRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxPurchaseCount => Mathf.Max(0, maxPurchaseCount);
    public bool AllowRepurchaseWhenRewardsOwned => allowRepurchaseWhenRewardsOwned;
    public TitleDefinition RequiredTitle => requiredTitle;
    public MilestoneDefinition RequiredMilestone => requiredMilestone;
    public ReputationFactionDefinition RequiredFaction => requiredFaction;
    public int RequiredReputation => requiredReputation;
    public RecipeDefinition RequiredKnownRecipe => requiredKnownRecipe;
    public WorldEventDefinition RequiredWorldEvent => requiredWorldEvent;
    public bool RequiredWorldEventActive => requiredWorldEventActive;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<RecipeGrant> RecipeGrants => recipeGrants != null ? (IReadOnlyList<RecipeGrant>)recipeGrants : Array.Empty<RecipeGrant>();
    public IReadOnlyList<PokemonKnowledgeGrant> PokemonKnowledgeGrants => pokemonKnowledgeGrants != null ? (IReadOnlyList<PokemonKnowledgeGrant>)pokemonKnowledgeGrants : Array.Empty<PokemonKnowledgeGrant>();
    public IReadOnlyList<RegionInfoDefinition> RegionsToDiscover => regionsToDiscover != null ? (IReadOnlyList<RegionInfoDefinition>)regionsToDiscover : Array.Empty<RegionInfoDefinition>();
    public IReadOnlyList<PokeNavEntryDefinition> PokeNavEntriesToDiscover => pokeNavEntriesToDiscover != null ? (IReadOnlyList<PokeNavEntryDefinition>)pokeNavEntriesToDiscover : Array.Empty<PokeNavEntryDefinition>();
    public IReadOnlyList<SocialPostDefinition> SocialPostsToUnlock => socialPostsToUnlock != null ? (IReadOnlyList<SocialPostDefinition>)socialPostsToUnlock : Array.Empty<SocialPostDefinition>();
    public IReadOnlyList<MapMarkerDefinition> MapMarkersToDiscover => mapMarkersToDiscover != null ? (IReadOnlyList<MapMarkerDefinition>)mapMarkersToDiscover : Array.Empty<MapMarkerDefinition>();
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants != null ? (IReadOnlyList<TitleGrant>)titleGrants : Array.Empty<TitleGrant>();
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete != null ? (IReadOnlyList<MilestoneDefinition>)milestonesToComplete : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<ResearchProgressGrant> ResearchProgressGrants => researchProgressGrants != null ? (IReadOnlyList<ResearchProgressGrant>)researchProgressGrants : Array.Empty<ResearchProgressGrant>();

    public float GetPrice(PlayerController player, ShopCatalogDefinition shopCatalog = null) {
        float price = BasePrice;
        if(useShopPriceMultiplier && shopCatalog != null) {
            price *= shopCatalog.BuyPriceMultiplier;
        }

        if(useSponsorDiscounts && shopCatalog != null) {
            var sponsorLog = player != null ? player.GetComponent<PlayerSponsorLog>() : null;
            price *= sponsorLog != null ? sponsorLog.GetBestBuyPriceMultiplier(shopCatalog, null) : 1f;
        }

        return Mathf.Max(0f, Mathf.Ceil(price));
    }

    public bool CanPurchase(PlayerController player, PlayerLearnableOfferLog log, string sourceId, ShopCatalogDefinition shopCatalog, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to purchase this offer.";
            return false;
        }

        if(!HasAnyGrant()) {
            failureMessage = $"{DisplayName} has no rewards assigned.";
            return false;
        }

        if(log != null && !log.CanPurchase(this, sourceId, repeatMode, CooldownHours, MaxPurchaseCount, out failureMessage)) {
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

        if(requiredKnownRecipe != null && !(player.GetComponent<PlayerRecipeBook>()?.KnowsRecipe(requiredKnownRecipe) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need to learn {requiredKnownRecipe.DisplayName} first." : lockedMessage;
            return false;
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

        if(!allowRepurchaseWhenRewardsOwned && !HasAnyUnownedGrant(player)) {
            failureMessage = $"{DisplayName} is already learned or unlocked.";
            return false;
        }

        float price = GetPrice(player, shopCatalog);
        if(price > 0f && (Wallet.i == null || !Wallet.i.HasMoney(price))) {
            failureMessage = $"You need {price:0} money for {DisplayName}.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public LearnableOfferPurchaseResult Purchase(PlayerController player, string sourceId, ShopCatalogDefinition shopCatalog = null, UnityEngine.Object context = null) {
        var result = new LearnableOfferPurchaseResult(Id, DisplayName, category, NormalizeSourceId(sourceId));
        var log = player != null ? player.GetComponent<PlayerLearnableOfferLog>() ?? player.gameObject.AddComponent<PlayerLearnableOfferLog>() : null;
        if(!CanPurchase(player, log, result.sourceId, shopCatalog, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            return result;
        }

        result.pricePaid = GetPrice(player, shopCatalog);
        if(result.pricePaid > 0f) {
            Wallet.i.TakeMoney(result.pricePaid);
        }

        ApplyGrants(player, result, context != null ? context : this);
        log?.RecordPurchase(this, result.sourceId, result.pricePaid, result.appliedGrantCount);
        RecordSponsorBenefit(player, shopCatalog, result);
        PublishPurchasedEvent(player, result, context);
        return result;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    bool HasAnyGrant() {
        return RecipeGrants.Any(grant => grant != null && grant.recipe != null)
            || PokemonKnowledgeGrants.Any(grant => grant != null && grant.pokemon != null && grant.level > PokemonKnowledgeLevel.Unknown)
            || RegionsToDiscover.Any(entry => entry != null)
            || PokeNavEntriesToDiscover.Any(entry => entry != null)
            || SocialPostsToUnlock.Any(entry => entry != null)
            || MapMarkersToDiscover.Any(entry => entry != null)
            || TitleGrants.Any(entry => entry != null && entry.title != null)
            || MilestonesToComplete.Any(entry => entry != null)
            || ResearchProgressGrants.Any(entry => entry != null && entry.subject != null);
    }

    bool HasAnyUnownedGrant(PlayerController player) {
        if(player == null) {
            return false;
        }

        var recipeBook = player.GetComponent<PlayerRecipeBook>();
        if(RecipeGrants.Any(grant => grant != null && grant.recipe != null && !(recipeBook?.KnowsRecipe(grant.recipe) ?? false))) {
            return true;
        }

        var pokeNav = player.GetComponent<PlayerPokeNavLog>();
        if(PokemonKnowledgeGrants.Any(grant => grant != null && grant.pokemon != null && (pokeNav == null || pokeNav.GetPokemonKnowledgeLevel(grant.pokemon) < grant.level))) {
            return true;
        }

        if(RegionsToDiscover.Any(region => region != null && !(pokeNav?.HasDiscoveredRegion(region) ?? region.VisibleByDefault))) {
            return true;
        }

        if(PokeNavEntriesToDiscover.Any(entry => entry != null && !(pokeNav?.HasDiscoveredEntry(entry) ?? entry.VisibleByDefault))) {
            return true;
        }

        if(SocialPostsToUnlock.Any(post => post != null && !(pokeNav?.HasUnlockedPost(post) ?? post.VisibleByDefault))) {
            return true;
        }

        var mapLog = player.GetComponent<PlayerMapLog>();
        if(MapMarkersToDiscover.Any(marker => marker != null && !(mapLog?.HasDiscoveredMarker(marker) ?? false))) {
            return true;
        }

        var titles = player.GetComponent<PlayerTitles>();
        if(TitleGrants.Any(grant => grant != null && grant.title != null && !(titles?.HasTitle(grant.title) ?? false))) {
            return true;
        }

        var milestones = player.GetComponent<PlayerMilestones>();
        if(MilestonesToComplete.Any(milestone => milestone != null && !(milestones?.HasMilestone(milestone) ?? false))) {
            return true;
        }

        var research = player.GetComponent<PlayerResearchLog>();
        if(ResearchProgressGrants.Any(grant => grant != null && grant.subject != null && (!grant.completeImmediately || !(research?.IsCompleted(grant.subject) ?? false)))) {
            return true;
        }

        return false;
    }

    void ApplyGrants(PlayerController player, LearnableOfferPurchaseResult result, UnityEngine.Object context) {
        if(player == null) {
            return;
        }

        var recipeBook = player.GetComponent<PlayerRecipeBook>() ?? player.gameObject.AddComponent<PlayerRecipeBook>();
        foreach(var grant in RecipeGrants) {
            if(grant != null && grant.recipe != null && recipeBook.Learn(grant, context)) {
                result.appliedGrantCount++;
            }
        }

        var pokeNav = player.GetComponent<PlayerPokeNavLog>() ?? player.gameObject.AddComponent<PlayerPokeNavLog>();
        foreach(var grant in PokemonKnowledgeGrants) {
            if(grant != null && grant.pokemon != null && grant.level > PokemonKnowledgeLevel.Unknown && pokeNav.RecordPokemonKnowledge(grant.pokemon, grant.level, result.sourceId)) {
                result.appliedGrantCount++;
            }
        }

        foreach(var region in RegionsToDiscover) {
            if(region != null && pokeNav.DiscoverRegion(region, out _)) {
                result.appliedGrantCount++;
            }
        }

        foreach(var entry in PokeNavEntriesToDiscover) {
            if(entry != null && pokeNav.DiscoverEntry(entry, out _)) {
                result.appliedGrantCount++;
            }
        }

        foreach(var post in SocialPostsToUnlock) {
            if(post != null && pokeNav.UnlockPost(post)) {
                result.appliedGrantCount++;
            }
        }

        var mapLog = player.GetComponent<PlayerMapLog>() ?? player.gameObject.AddComponent<PlayerMapLog>();
        foreach(var marker in MapMarkersToDiscover) {
            if(marker != null && mapLog.DiscoverMarker(marker, result.sourceId)) {
                result.appliedGrantCount++;
            }
        }

        int beforeTitleCount = player.GetComponent<PlayerTitles>()?.Titles.Count ?? 0;
        player.GetComponent<PlayerTitles>()?.ApplyGrants(TitleGrants, context);
        int afterTitleCount = player.GetComponent<PlayerTitles>()?.Titles.Count ?? beforeTitleCount;
        result.appliedGrantCount += Mathf.Max(0, afterTitleCount - beforeTitleCount);

        var milestones = player.GetComponent<PlayerMilestones>();
        int beforeMilestoneCount = milestones?.CompletedMilestoneIds.Count ?? 0;
        milestones?.CompleteMilestones(MilestonesToComplete);
        int afterMilestoneCount = milestones?.CompletedMilestoneIds.Count ?? beforeMilestoneCount;
        result.appliedGrantCount += Mathf.Max(0, afterMilestoneCount - beforeMilestoneCount);

        var research = player.GetComponent<PlayerResearchLog>() ?? player.gameObject.AddComponent<PlayerResearchLog>();
        foreach(var grant in ResearchProgressGrants) {
            if(grant == null || grant.subject == null) {
                continue;
            }

            if(grant.completeImmediately) {
                research.AddProgress(grant.subject, grant.subject.RequiredResearchPoints);
                result.appliedGrantCount++;
            } else if(grant.points > 0) {
                research.AddProgress(grant.subject, grant.points);
                result.appliedGrantCount++;
            } else if(grant.createEntryIfMissing) {
                research.GetEntry(grant.subject);
                result.appliedGrantCount++;
            }
        }
    }

    void RecordSponsorBenefit(PlayerController player, ShopCatalogDefinition shopCatalog, LearnableOfferPurchaseResult result) {
        if(!useSponsorDiscounts || shopCatalog == null || result == null) {
            return;
        }

        var sponsorLog = player != null ? player.GetComponent<PlayerSponsorLog>() : null;
        if(sponsorLog == null || !sponsorLog.TryGetBestBuyPriceSponsor(shopCatalog, null, out var sponsor, out float multiplier)) {
            return;
        }

        if(Mathf.Approximately(multiplier, 1f)) {
            return;
        }

        sponsorLog.RecordBenefitUse(sponsor, SponsorBenefitType.ShopBuyPrice, Id, DisplayName, multiplier, result.sourceId);
        sponsor.PublishBenefitUsed(player, result.sourceId, SponsorBenefitType.ShopBuyPrice, Id, DisplayName, multiplier);
    }

    void PublishPurchasedEvent(PlayerController player, LearnableOfferPurchaseResult result, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            purchasedEvent,
            $"learnable-offer.purchased.{Id}",
            $"{DisplayName} purchased.",
            GameEventCategory.Shop,
            GameEventImportance.Success,
            context != null ? context : player,
            "LearnableOfferDefinition",
            GameEventScope.Player,
            showInFeed: showPurchaseEventInFeed,
            writeToDebugLog: writePurchaseEventToDebugLog,
            GameEventPublishing.Value("offerId", Id),
            GameEventPublishing.Value("offerName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("sourceId", result != null ? result.sourceId : string.Empty),
            GameEventPublishing.Value("pricePaid", result != null ? result.pricePaid : 0f),
            GameEventPublishing.Value("appliedGrantCount", result != null ? result.appliedGrantCount : 0));
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "learnable-offer" : sourceId;
    }
}

[Serializable]
public class PokemonKnowledgeGrant {
    [Tooltip("Pokemon whose PokeNav knowledge is updated.")]
    public PokemonBase pokemon;
    [Tooltip("Knowledge level granted for this Pokemon.")]
    public PokemonKnowledgeLevel level = PokemonKnowledgeLevel.Seen;
}

[Serializable]
public class ResearchProgressGrant {
    [Tooltip("Research subject affected by this grant.")]
    public ResearchSubjectDefinition subject;
    [Tooltip("Research points added. 0 can still create an entry when Create Entry If Missing is enabled.")]
    [Min(0)]
    public int points;
    [Tooltip("If enabled, this grant immediately completes the subject.")]
    public bool completeImmediately;
    [Tooltip("If enabled and Points is 0, buying the offer creates a visible research entry without progress.")]
    public bool createEntryIfMissing = true;
}

public class LearnableOfferPurchaseResult {
    public readonly string offerId;
    public readonly string offerName;
    public readonly LearnableOfferCategory category;
    public readonly string sourceId;
    public bool blocked;
    public string failureMessage;
    public float pricePaid;
    public int appliedGrantCount;

    public LearnableOfferPurchaseResult(string offerId, string offerName, LearnableOfferCategory category, string sourceId) {
        this.offerId = offerId;
        this.offerName = offerName;
        this.category = category;
        this.sourceId = sourceId;
    }
}
