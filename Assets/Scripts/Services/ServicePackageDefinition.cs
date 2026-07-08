using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ServicePackageCategory {
    General,
    Clinic,
    Inn,
    Food,
    PokemonCare,
    Training,
    Travel,
    Research,
    Legal,
    Membership,
    Shopping,
    Custom
}

[CreateAssetMenu(menuName = "Services/Service Package Definition")]
public class ServicePackageDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this service package. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing description for this package.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad package category used by requirements, validators and future UI filters.")]
    [SerializeField] ServicePackageCategory category = ServicePackageCategory.General;
    [Tooltip("Free-form tags such as clinic, inn, spa, daycare, professor, police, member or premium.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future UI lists.")]
    [SerializeField] Sprite icon;

    [Header("Price")]
    [Tooltip("Package-level money cost before optional shop catalog and sponsor multipliers. Included services/offers still use their own prices.")]
    [Min(0f)]
    [SerializeField] float basePrice;
    [Tooltip("If enabled, the source shop catalog's buy multiplier is applied to Base Price.")]
    [SerializeField] bool useShopPriceMultiplier = true;
    [Tooltip("If enabled, active sponsor shop-buy multipliers can discount the package Base Price when a shop catalog context is available.")]
    [SerializeField] bool useSponsorDiscounts = true;
    [Tooltip("If enabled, Can Use checks package price plus included service/offer prices before applying anything.")]
    [SerializeField] bool checkCombinedPriceBeforeApplying = true;

    [Header("Repeat")]
    [Tooltip("How often this package can be used.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.Unlimited;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum successful package uses across all sources. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxUseCount;
    [Tooltip("If enabled, successful package uses are saved in PlayerServicePackageLog.")]
    [SerializeField] bool recordHistory = true;
    [Tooltip("If enabled, blocked package attempts are also saved in PlayerServicePackageLog.")]
    [SerializeField] bool recordBlockedAttempts;
    [Tooltip("Optional custom message shown when repeat rules block the package.")]
    [TextArea]
    [SerializeField] string repeatBlockedMessage = string.Empty;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this package can be used.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this package can be used.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this package.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional world event whose active state gates this package.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for Required World Event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("How extra requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this package can be used.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this package is locked by access rules.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This package is not available yet.";

    [Header("Included Services")]
    [Tooltip("Services applied when this package succeeds. Use zero-cost service definitions when the package price should cover the whole bundle.")]
    [SerializeField] List<ServicePackageServiceEntry> services = new List<ServicePackageServiceEntry>();
    [Tooltip("Learnable offers purchased or unlocked through this package. Use zero-price offers when the package price should cover the whole bundle.")]
    [SerializeField] List<ServicePackageLearnableOfferEntry> learnableOffers = new List<ServicePackageLearnableOfferEntry>();

    [Header("Package Rewards")]
    [Tooltip("Title, badge, permit or license grants applied after required package contents succeed.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Milestones completed after required package contents succeed.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Faction reputation changes applied after required package contents succeed.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Personal relationship changes applied after required package contents succeed.")]
    [SerializeField] List<RelationshipChange> relationshipChanges = new List<RelationshipChange>();
    [Tooltip("Lifestyle/playstyle point grants applied after required package contents succeed.")]
    [SerializeField] List<LifestylePointGrant> lifestylePointGrants = new List<LifestylePointGrant>();
    [Tooltip("Career points awarded after required package contents succeed.")]
    [SerializeField] List<CareerPointGrant> careerPointGrants = new List<CareerPointGrant>();
    [Tooltip("Life path XP, branch progress and tag counters awarded after required package contents succeed.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();
    [Tooltip("Organization memberships granted after required package contents succeed.")]
    [SerializeField] List<OrganizationMembershipGrant> organizationMembershipGrants = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points awarded after required package contents succeed.")]
    [SerializeField] List<OrganizationPointGrant> organizationPointGrants = new List<OrganizationPointGrant>();

    [Header("Consequences")]
    [Tooltip("Consequence chains applied after required package contents succeed.")]
    [SerializeField] List<ConsequenceChainDefinition> completedChains = new List<ConsequenceChainDefinition>();
    [Tooltip("Consequence chains applied when the package is blocked.")]
    [SerializeField] List<ConsequenceChainDefinition> blockedChains = new List<ConsequenceChainDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this package succeeds. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("Optional event published when this package is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, generated package events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, generated package events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public ServicePackageCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public float BasePrice => Mathf.Max(0f, basePrice);
    public bool UseShopPriceMultiplier => useShopPriceMultiplier;
    public bool UseSponsorDiscounts => useSponsorDiscounts;
    public bool CheckCombinedPriceBeforeApplying => checkCombinedPriceBeforeApplying;
    public ConsequenceChainRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxUseCount => Mathf.Max(0, maxUseCount);
    public bool RecordHistory => recordHistory;
    public bool RecordBlockedAttempts => recordBlockedAttempts;
    public TitleDefinition RequiredTitle => requiredTitle;
    public MilestoneDefinition RequiredMilestone => requiredMilestone;
    public ReputationFactionDefinition RequiredFaction => requiredFaction;
    public int RequiredReputation => requiredReputation;
    public WorldEventDefinition RequiredWorldEvent => requiredWorldEvent;
    public bool RequiredWorldEventActive => requiredWorldEventActive;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<ServicePackageServiceEntry> Services => services != null ? (IReadOnlyList<ServicePackageServiceEntry>)services : Array.Empty<ServicePackageServiceEntry>();
    public IReadOnlyList<ServicePackageLearnableOfferEntry> LearnableOffers => learnableOffers != null ? (IReadOnlyList<ServicePackageLearnableOfferEntry>)learnableOffers : Array.Empty<ServicePackageLearnableOfferEntry>();
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants != null ? (IReadOnlyList<TitleGrant>)titleGrants : Array.Empty<TitleGrant>();
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete != null ? (IReadOnlyList<MilestoneDefinition>)milestonesToComplete : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges != null ? (IReadOnlyList<ReputationChange>)reputationChanges : Array.Empty<ReputationChange>();
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges != null ? (IReadOnlyList<RelationshipChange>)relationshipChanges : Array.Empty<RelationshipChange>();
    public IReadOnlyList<LifestylePointGrant> LifestylePointGrants => lifestylePointGrants != null ? (IReadOnlyList<LifestylePointGrant>)lifestylePointGrants : Array.Empty<LifestylePointGrant>();
    public IReadOnlyList<CareerPointGrant> CareerPointGrants => careerPointGrants != null ? (IReadOnlyList<CareerPointGrant>)careerPointGrants : Array.Empty<CareerPointGrant>();
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards != null ? (IReadOnlyList<LifePathReward>)lifePathRewards : Array.Empty<LifePathReward>();
    public IReadOnlyList<OrganizationMembershipGrant> OrganizationMembershipGrants => organizationMembershipGrants != null ? (IReadOnlyList<OrganizationMembershipGrant>)organizationMembershipGrants : Array.Empty<OrganizationMembershipGrant>();
    public IReadOnlyList<OrganizationPointGrant> OrganizationPointGrants => organizationPointGrants != null ? (IReadOnlyList<OrganizationPointGrant>)organizationPointGrants : Array.Empty<OrganizationPointGrant>();
    public IReadOnlyList<ConsequenceChainDefinition> CompletedChains => completedChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)completedChains : Array.Empty<ConsequenceChainDefinition>();
    public IReadOnlyList<ConsequenceChainDefinition> BlockedChains => blockedChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)blockedChains : Array.Empty<ConsequenceChainDefinition>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public float GetPackagePrice(PlayerController player, ShopCatalogDefinition shopCatalog = null) {
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

    public float GetExpectedTotalPrice(PlayerController player, ShopCatalogDefinition shopCatalog = null) {
        if(player != null && TryBuildExecutionPlan(player, NormalizeSourceId(null), shopCatalog, out var plan, out _)) {
            return plan.ExpectedTotalPrice;
        }

        float price = GetPackagePrice(player, shopCatalog);
        foreach(var entry in Services) {
            if(entry != null && entry.Service != null) {
                price += entry.Service.MoneyCost * entry.TimesToUse;
            }
        }

        foreach(var entry in LearnableOffers) {
            if(entry != null && entry.Offer != null) {
                price += entry.Offer.GetPrice(player, shopCatalog);
            }
        }

        return Mathf.Max(0f, Mathf.Ceil(price));
    }

    public bool CanUse(PlayerController player, PlayerServicePackageLog log, string sourceId, ShopCatalogDefinition shopCatalog, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to use this package.";
            return false;
        }

        if(!HasAnyPackageEffect()) {
            failureMessage = $"{DisplayName} has no services, offers, rewards or consequences assigned.";
            return false;
        }

        if(log != null && !log.CanUse(this, sourceId, repeatMode, CooldownHours, MaxUseCount, out failureMessage)) {
            if(!string.IsNullOrWhiteSpace(repeatBlockedMessage)) {
                failureMessage = repeatBlockedMessage;
            }
            return false;
        }

        if(!AccessRequirementsMet(player, out failureMessage)) {
            return false;
        }

        if(!ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        if(!TryBuildExecutionPlan(player, sourceId, shopCatalog, out var plan, out failureMessage)) {
            return false;
        }

        float requiredMoney = checkCombinedPriceBeforeApplying ? plan.ExpectedTotalPrice : plan.RequiredTotalPrice;
        if(requiredMoney > 0f && (Wallet.i == null || !Wallet.i.HasMoney(requiredMoney))) {
            failureMessage = $"You need {requiredMoney:0} money for {DisplayName}.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public ServicePackageUseResult Use(PlayerController player, string sourceId = null, string sourceName = null, ShopCatalogDefinition shopCatalog = null, UnityEngine.Object context = null) {
        var result = new ServicePackageUseResult(Id, DisplayName, category, NormalizeSourceId(sourceId), sourceName);
        var unityContext = context != null ? context : this;
        var log = player != null ? player.GetComponent<PlayerServicePackageLog>() ?? player.gameObject.AddComponent<PlayerServicePackageLog>() : null;

        if(!CanUse(player, log, result.sourceId, shopCatalog, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            if(recordBlockedAttempts) {
                log?.RecordUse(this, result);
            }
            ApplyChains(player, blockedChains, result, unityContext);
            PublishPackageEvent(blockedEvent, "blocked", result, player, unityContext, GameEventImportance.Warning);
            return result;
        }

        if(!TryBuildExecutionPlan(player, result.sourceId, shopCatalog, out var plan, out failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            if(recordBlockedAttempts) {
                log?.RecordUse(this, result);
            }
            ApplyChains(player, blockedChains, result, unityContext);
            PublishPackageEvent(blockedEvent, "blocked", result, player, unityContext, GameEventImportance.Warning);
            return result;
        }

        result.requiredTotalPrice = plan.RequiredTotalPrice;
        result.expectedTotalPrice = plan.ExpectedTotalPrice;
        result.skippedServiceCount += plan.skippedServiceCount;
        result.skippedOfferCount += plan.skippedOfferCount;
        result.messages.AddRange(plan.messages.Where(message => !string.IsNullOrWhiteSpace(message)));

        result.packagePricePaid = plan.packagePrice;
        if(result.packagePricePaid > 0f) {
            Wallet.i.TakeMoney(result.packagePricePaid);
        }

        ApplyIncludedServices(player, result, plan, unityContext);
        ApplyIncludedOffers(player, result, plan, shopCatalog, unityContext);

        if(!result.blocked) {
            ApplyPackageRewards(player, result, unityContext);
            ApplyChains(player, completedChains, result, unityContext);
            RecordSponsorBenefit(player, shopCatalog, result);
        } else {
            ApplyChains(player, blockedChains, result, unityContext);
        }

        if(recordHistory || (recordBlockedAttempts && result.blocked)) {
            log?.RecordUse(this, result);
        }

        PublishPackageEvent(
            result.blocked ? blockedEvent : completedEvent,
            result.blocked ? "blocked" : "completed",
            result,
            player,
            unityContext,
            result.blocked ? GameEventImportance.Warning : GameEventImportance.Success);

        return result;
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

    bool TryBuildExecutionPlan(PlayerController player, string sourceId, ShopCatalogDefinition shopCatalog, out ServicePackageExecutionPlan plan, out string failureMessage) {
        plan = new ServicePackageExecutionPlan {
            packagePrice = GetPackagePrice(player, shopCatalog)
        };

        var serviceLog = player.GetComponent<PlayerServiceLog>();
        foreach(var entry in Services) {
            if(entry == null) {
                continue;
            }

            if(entry.Service == null) {
                if(entry.Required) {
                    failureMessage = $"{DisplayName} has a required service slot with no service assigned.";
                    return false;
                }

                plan.SkipService("Optional package service skipped because no service is assigned.");
                continue;
            }

            if(entry.TimesToUse > 1 && entry.Service.RepeatMode != ConsequenceChainRepeatMode.Unlimited) {
                string repeatFailure = $"{entry.Service.DisplayName} cannot safely run multiple times in one package because its repeat mode is {entry.Service.RepeatMode}.";
                if(entry.Required) {
                    failureMessage = repeatFailure;
                    return false;
                }

                plan.SkipService(repeatFailure);
                continue;
            }

            if(!entry.Service.CanUse(player, serviceLog, entry.ResolveProviderId(sourceId), out failureMessage)) {
                if(entry.Required) {
                    return false;
                }

                plan.SkipService($"{entry.Service.DisplayName} skipped: {failureMessage}");
                continue;
            }

            plan.services.Add(entry);
            plan.entryPrice += entry.Service.MoneyCost * entry.TimesToUse;
            if(entry.Required) {
                plan.requiredEntryPrice += entry.Service.MoneyCost * entry.TimesToUse;
            }
        }

        var offerLog = player.GetComponent<PlayerLearnableOfferLog>();
        foreach(var entry in LearnableOffers) {
            if(entry == null) {
                continue;
            }

            if(entry.Offer == null) {
                if(entry.Required) {
                    failureMessage = $"{DisplayName} has a required learnable offer slot with no offer assigned.";
                    return false;
                }

                plan.SkipOffer("Optional package learnable offer skipped because no offer is assigned.");
                continue;
            }

            if(!entry.Offer.CanPurchase(player, offerLog, entry.ResolveSourceId(sourceId), shopCatalog, out failureMessage)) {
                if(entry.Required) {
                    return false;
                }

                plan.SkipOffer($"{entry.Offer.DisplayName} skipped: {failureMessage}");
                continue;
            }

            float price = entry.Offer.GetPrice(player, shopCatalog);
            plan.offers.Add(entry);
            plan.entryPrice += price;
            if(entry.Required) {
                plan.requiredEntryPrice += price;
            }
        }

        failureMessage = null;
        return true;
    }

    void ApplyIncludedServices(PlayerController player, ServicePackageUseResult result, ServicePackageExecutionPlan plan, UnityEngine.Object context) {
        foreach(var entry in plan.services) {
            if(entry == null || entry.Service == null) {
                continue;
            }

            for(int i = 0; i < entry.TimesToUse; i++) {
                var serviceLog = player.GetComponent<PlayerServiceLog>();
                if(!entry.Service.CanUse(player, serviceLog, entry.ResolveProviderId(result.sourceId), out var preflightFailure)) {
                    if(entry.Required) {
                        MarkRequiredEntryFailure(result, entry.Service.DisplayName, preflightFailure);
                        return;
                    }

                    result.skippedServiceCount++;
                    result.messages.Add($"{entry.Service.DisplayName} skipped: {preflightFailure}");
                    break;
                }

                var serviceResult = entry.Service.Use(player, entry.ResolveProviderId(result.sourceId), entry.ResolveProviderName(result.sourceName), context);
                if(serviceResult == null) {
                    if(entry.Required) {
                        MarkRequiredEntryFailure(result, entry.Service.DisplayName, "Service returned no result.");
                        return;
                    }

                    result.skippedServiceCount++;
                    continue;
                }

                result.individualPricePaid += Mathf.Max(0f, serviceResult.moneyPaid);
                if(serviceResult.blocked) {
                    result.blockedServiceCount++;
                    if(!string.IsNullOrWhiteSpace(serviceResult.failureMessage)) {
                        result.messages.Add($"{entry.Service.DisplayName}: {serviceResult.failureMessage}");
                    }

                    if(entry.Required) {
                        MarkRequiredEntryFailure(result, entry.Service.DisplayName, serviceResult.failureMessage);
                        return;
                    }
                } else {
                    result.appliedServiceCount++;
                    result.affectedPokemonCount += Mathf.Max(0, serviceResult.affectedPokemonCount);
                    result.appliedChainCount += Mathf.Max(0, serviceResult.appliedChainCount);
                    result.failedChainCount += Mathf.Max(0, serviceResult.failedChainCount);
                    result.messages.AddRange(serviceResult.messages.Where(message => !string.IsNullOrWhiteSpace(message)));
                }
            }
        }
    }

    void ApplyIncludedOffers(PlayerController player, ServicePackageUseResult result, ServicePackageExecutionPlan plan, ShopCatalogDefinition shopCatalog, UnityEngine.Object context) {
        if(result.blocked) {
            return;
        }

        foreach(var entry in plan.offers) {
            if(entry == null || entry.Offer == null) {
                continue;
            }

            var offerLog = player.GetComponent<PlayerLearnableOfferLog>();
            if(!entry.Offer.CanPurchase(player, offerLog, entry.ResolveSourceId(result.sourceId), shopCatalog, out var preflightFailure)) {
                if(entry.Required) {
                    MarkRequiredEntryFailure(result, entry.Offer.DisplayName, preflightFailure);
                    return;
                }

                result.skippedOfferCount++;
                result.messages.Add($"{entry.Offer.DisplayName} skipped: {preflightFailure}");
                continue;
            }

            var offerResult = entry.Offer.Purchase(player, entry.ResolveSourceId(result.sourceId), shopCatalog, context);
            if(offerResult == null) {
                if(entry.Required) {
                    MarkRequiredEntryFailure(result, entry.Offer.DisplayName, "Offer returned no result.");
                    return;
                }

                result.skippedOfferCount++;
                continue;
            }

            result.individualPricePaid += Mathf.Max(0f, offerResult.pricePaid);
            if(offerResult.blocked) {
                result.blockedOfferCount++;
                if(!string.IsNullOrWhiteSpace(offerResult.failureMessage)) {
                    result.messages.Add($"{entry.Offer.DisplayName}: {offerResult.failureMessage}");
                }

                if(entry.Required) {
                    MarkRequiredEntryFailure(result, entry.Offer.DisplayName, offerResult.failureMessage);
                    return;
                }
            } else {
                result.purchasedOfferCount++;
                result.appliedGrantCount += Mathf.Max(0, offerResult.appliedGrantCount);
            }
        }
    }

    void MarkRequiredEntryFailure(ServicePackageUseResult result, string entryName, string failureMessage) {
        result.blocked = true;
        result.failureMessage = string.IsNullOrWhiteSpace(failureMessage) ? $"{entryName} failed." : failureMessage;
        if(result.HasAppliedChanges) {
            result.partialSuccess = true;
            result.messages.Add($"{entryName} failed after the package had already applied changes.");
        }
    }

    void ApplyPackageRewards(PlayerController player, ServicePackageUseResult result, UnityEngine.Object context) {
        if(player == null) {
            return;
        }

        int beforeTitleCount = player.GetComponent<PlayerTitles>()?.Titles.Count ?? 0;
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, context);
        int afterTitleCount = player.GetComponent<PlayerTitles>()?.Titles.Count ?? beforeTitleCount;
        result.appliedGrantCount += Mathf.Max(0, afterTitleCount - beforeTitleCount);

        var milestones = player.GetComponent<PlayerMilestones>();
        int beforeMilestoneCount = milestones?.CompletedMilestoneIds.Count ?? 0;
        milestones?.CompleteMilestones(milestonesToComplete);
        int afterMilestoneCount = milestones?.CompletedMilestoneIds.Count ?? beforeMilestoneCount;
        result.appliedGrantCount += Mathf.Max(0, afterMilestoneCount - beforeMilestoneCount);

        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        player.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipChanges);
        player.GetComponent<PlayerLifestyleLog>()?.ApplyGrants(lifestylePointGrants, $"service-package:{Id}", DisplayName, context);
        player.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(careerPointGrants, $"service-package:{Id}");
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewards, $"service-package:{Id}", DisplayName, context != null ? context : this);
        player.GetComponent<PlayerOrganizationLog>()?.ApplyMembershipGrants(organizationMembershipGrants, $"service-package:{Id}");
        player.GetComponent<PlayerOrganizationLog>()?.ApplyPointGrants(organizationPointGrants, $"service-package:{Id}");
    }

    void ApplyChains(PlayerController player, IEnumerable<ConsequenceChainDefinition> chains, ServicePackageUseResult result, UnityEngine.Object context) {
        if(player == null || chains == null) {
            return;
        }

        foreach(var chain in chains) {
            if(chain == null) {
                continue;
            }

            var chainResult = chain.Apply(player, new ConsequenceChainContext {
                SourceId = result.sourceId,
                SourceName = string.IsNullOrWhiteSpace(result.sourceName) ? result.packageName : result.sourceName,
                ContextObject = context
            }, context);

            if(chainResult != null) {
                result.appliedChainCount += Mathf.Max(0, chainResult.appliedSteps);
                if(chainResult.blocked || chainResult.failedSteps > 0) {
                    result.failedChainCount++;
                }
            }
        }
    }

    void RecordSponsorBenefit(PlayerController player, ShopCatalogDefinition shopCatalog, ServicePackageUseResult result) {
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

    void PublishPackageEvent(GameEventDefinition eventDefinition, string phase, ServicePackageUseResult result, PlayerController player, UnityEngine.Object context, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"service-package.{phase}.{Id}",
            phase == "blocked" ? $"{DisplayName} blocked." : $"{DisplayName} completed.",
            GameEventCategory.Activity,
            importance,
            context != null ? context : player,
            "ServicePackageDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("packageId", Id),
            GameEventPublishing.Value("packageName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("sourceId", result != null ? result.sourceId : string.Empty),
            GameEventPublishing.Value("sourceName", result != null ? result.sourceName : string.Empty),
            GameEventPublishing.Value("blocked", result != null && result.blocked),
            GameEventPublishing.Value("packagePricePaid", result != null ? result.packagePricePaid : 0f),
            GameEventPublishing.Value("individualPricePaid", result != null ? result.individualPricePaid : 0f),
            GameEventPublishing.Value("appliedServices", result != null ? result.appliedServiceCount : 0),
            GameEventPublishing.Value("purchasedOffers", result != null ? result.purchasedOfferCount : 0));
    }

    bool HasAnyPackageEffect() {
        return Services.Any(entry => entry != null && entry.Service != null)
            || LearnableOffers.Any(entry => entry != null && entry.Offer != null)
            || TitleGrants.Any(entry => entry != null && entry.title != null)
            || MilestonesToComplete.Any(entry => entry != null)
            || ReputationChanges.Any(entry => entry != null && entry.faction != null && entry.amount != 0)
            || RelationshipChanges.Any(entry => entry != null && entry.subject != null && entry.amount != 0)
            || LifestylePointGrants.Any(entry => entry != null && entry.lifestyle != null && entry.points != 0)
            || CareerPointGrants.Any(entry => entry != null && entry.career != null && entry.points > 0)
            || LifePathRewards.Any(entry => entry != null && entry.lifePath != null && entry.HasAnyPayload)
            || OrganizationMembershipGrants.Any(entry => entry != null && entry.organization != null)
            || OrganizationPointGrants.Any(entry => entry != null && entry.organization != null && entry.points > 0)
            || CompletedChains.Any(entry => entry != null);
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "service-package" : sourceId;
    }

    class ServicePackageExecutionPlan {
        public readonly List<ServicePackageServiceEntry> services = new List<ServicePackageServiceEntry>();
        public readonly List<ServicePackageLearnableOfferEntry> offers = new List<ServicePackageLearnableOfferEntry>();
        public readonly List<string> messages = new List<string>();
        public float packagePrice;
        public float entryPrice;
        public float requiredEntryPrice;
        public int skippedServiceCount;
        public int skippedOfferCount;
        public float RequiredTotalPrice => Mathf.Max(0f, Mathf.Ceil(packagePrice + requiredEntryPrice));
        public float ExpectedTotalPrice => Mathf.Max(0f, Mathf.Ceil(packagePrice + entryPrice));

        public void SkipService(string message) {
            skippedServiceCount++;
            if(!string.IsNullOrWhiteSpace(message)) {
                messages.Add(message);
            }
        }

        public void SkipOffer(string message) {
            skippedOfferCount++;
            if(!string.IsNullOrWhiteSpace(message)) {
                messages.Add(message);
            }
        }
    }
}

[Serializable]
public class ServicePackageServiceEntry {
    [Tooltip("Service applied by this package entry.")]
    [SerializeField] ServiceDefinition service;
    [Tooltip("How many times this service is applied when the package runs.")]
    [Min(1)]
    [SerializeField] int timesToUse = 1;
    [Tooltip("If enabled, this service must be usable for the package to start and succeeds/fails the package.")]
    [SerializeField] bool required = true;
    [Tooltip("Optional provider id override passed to the service repeat/log system. Empty uses the package source id.")]
    [SerializeField] string providerIdOverride = string.Empty;
    [Tooltip("Optional provider name override written into service logs. Empty uses the package source name.")]
    [SerializeField] string providerNameOverride = string.Empty;

    public ServiceDefinition Service => service;
    public int TimesToUse => Mathf.Max(1, timesToUse);
    public bool Required => required;
    public string ProviderIdOverride => providerIdOverride;
    public string ProviderNameOverride => providerNameOverride;

    public string ResolveProviderId(string packageSourceId) {
        return string.IsNullOrWhiteSpace(providerIdOverride) ? packageSourceId : providerIdOverride;
    }

    public string ResolveProviderName(string packageSourceName) {
        return string.IsNullOrWhiteSpace(providerNameOverride) ? packageSourceName : providerNameOverride;
    }
}

[Serializable]
public class ServicePackageLearnableOfferEntry {
    [Tooltip("Learnable offer purchased or unlocked by this package entry.")]
    [SerializeField] LearnableOfferDefinition offer;
    [Tooltip("If enabled, this offer must be purchasable for the package to start and succeeds/fails the package.")]
    [SerializeField] bool required = true;
    [Tooltip("Optional source id override passed to the offer repeat/log system. Empty uses the package source id.")]
    [SerializeField] string sourceIdOverride = string.Empty;

    public LearnableOfferDefinition Offer => offer;
    public bool Required => required;
    public string SourceIdOverride => sourceIdOverride;

    public string ResolveSourceId(string packageSourceId) {
        return string.IsNullOrWhiteSpace(sourceIdOverride) ? packageSourceId : sourceIdOverride;
    }
}

public class ServicePackageUseResult {
    public readonly string packageId;
    public readonly string packageName;
    public readonly ServicePackageCategory category;
    public readonly string sourceId;
    public readonly string sourceName;
    public bool blocked;
    public bool partialSuccess;
    public string failureMessage;
    public float expectedTotalPrice;
    public float requiredTotalPrice;
    public float packagePricePaid;
    public float individualPricePaid;
    public int appliedServiceCount;
    public int blockedServiceCount;
    public int skippedServiceCount;
    public int purchasedOfferCount;
    public int blockedOfferCount;
    public int skippedOfferCount;
    public int affectedPokemonCount;
    public int appliedGrantCount;
    public int appliedChainCount;
    public int failedChainCount;
    public readonly List<string> messages = new List<string>();
    public bool HasAppliedChanges => packagePricePaid > 0f
        || individualPricePaid > 0f
        || appliedServiceCount > 0
        || purchasedOfferCount > 0
        || affectedPokemonCount > 0
        || appliedGrantCount > 0
        || appliedChainCount > 0;

    public ServicePackageUseResult(string packageId, string packageName, ServicePackageCategory category, string sourceId, string sourceName) {
        this.packageId = packageId;
        this.packageName = packageName;
        this.category = category;
        this.sourceId = sourceId;
        this.sourceName = sourceName;
    }
}
