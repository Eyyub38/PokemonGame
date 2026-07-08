using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RegionTravelMode {
    Walk,
    Bicycle,
    Train,
    Boat,
    Ferry,
    Airship,
    Flight,
    RidePokemon,
    Portal,
    Special
}

[CreateAssetMenu(menuName = "World Regions/Region Travel Route")]
public class RegionTravelRouteDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this region route. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future region travel UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing description for this route.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad travel mode used by filters and future UI.")]
    [SerializeField] RegionTravelMode travelMode = RegionTravelMode.Train;
    [Tooltip("Free-form tags such as ferry, league, night-only, airport or postgame.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Endpoints")]
    [Tooltip("Origin world region. Empty means this route can be offered from any region/point.")]
    [SerializeField] WorldRegionDefinition originRegion;
    [Tooltip("Destination world region.")]
    [SerializeField] WorldRegionDefinition destinationRegion;
    [Tooltip("If enabled, PlayerWorldRegionLog.CurrentRegion must match Origin Region before this route can be used.")]
    [SerializeField] bool requireCurrentOriginRegion = true;
    [Tooltip("Optional origin point id used for logs and future UI.")]
    [SerializeField] string originPointId = string.Empty;
    [Tooltip("Optional destination scene override. Empty uses Destination Region default scene.")]
    [SerializeField] string destinationSceneName = string.Empty;
    [Tooltip("Optional destination spawn/portal override. Empty uses Destination Region default spawn point.")]
    [SerializeField] string destinationSpawnPointId = string.Empty;

    [Header("Unlock And Repeat")]
    [Tooltip("If enabled, this route is usable without being unlocked in PlayerWorldRegionLog.")]
    [SerializeField] bool unlockedByDefault = true;
    [Tooltip("If enabled, PlayerWorldRegionLog must contain this route unless it is unlocked by default.")]
    [SerializeField] bool requiresRouteUnlock;
    [Tooltip("How often this route can be used. Daily and Cooldown use the route/source id.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.Unlimited;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum successful travels on this route. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxTravelCount;
    [Tooltip("Optional custom message shown when unlock or repeat rules block this route.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This regional route is not available yet.";

    [Header("Schedule")]
    [Tooltip("Allowed day periods. Empty means always available.")]
    [SerializeField] List<DayPeriod> allowedPeriods = new List<DayPeriod>();
    [Tooltip("Allowed weekdays. Empty means every weekday is valid.")]
    [SerializeField] List<WeekDay> allowedWeekDays = new List<WeekDay>();
    [Tooltip("Estimated in-game hours this route takes. Stored in logs/events; time mutation can be handled by scene/UI flow later.")]
    [Min(0)]
    [SerializeField] int estimatedTravelHours;

    [Header("Travel Policy")]
    [Tooltip("Optional policy that exposes selectable travel choices such as full party, one Pokemon only or challenge travel.")]
    [SerializeField] RegionTravelPolicyDefinition travelPolicy;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required to use this route.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required to use this route.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this route.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional skill required to use this route.")]
    [SerializeField] PlayerSkillDefinition requiredSkill;
    [Tooltip("Minimum level of the required skill.")]
    [Min(0)]
    [SerializeField] int requiredSkillLevel;
    [Tooltip("Optional world event whose active state gates this route.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for the required world event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("How custom requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Additional requirements checked before this route can be used.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Costs")]
    [Tooltip("Money removed from Wallet when travel starts.")]
    [Min(0f)]
    [SerializeField] float moneyCost;
    [Tooltip("Items removed from inventory when travel starts.")]
    [SerializeField] List<ActivityItemCost> itemCosts = new List<ActivityItemCost>();
    [Tooltip("Survival needs consumed when travel starts.")]
    [SerializeField] List<ActivityNeedCost> needCosts = new List<ActivityNeedCost>();

    [Header("Arrival And Challenge")]
    [Tooltip("Optional challenge profile started after arrival.")]
    [SerializeField] RegionChallengeProfileDefinition challengeProfile;
    [Tooltip("If enabled, Destination Region discovery and linked content are applied on arrival.")]
    [SerializeField] bool discoverDestinationOnArrival = true;
    [Tooltip("If enabled, this route becomes unlocked after first successful travel.")]
    [SerializeField] bool unlockRouteAfterTravel = true;

    [Header("Arrival Rewards")]
    [Tooltip("Trainer XP awarded after arrival.")]
    [Min(0)]
    [SerializeField] int experienceReward;
    [Tooltip("Progression source used for trainer XP from this regional travel.")]
    [SerializeField] PlayerExperienceSource experienceSource = PlayerExperienceSource.Exploration;
    [Tooltip("Titles, passes or licenses granted after arrival.")]
    [SerializeField] List<TitleGrant> titleRewards = new List<TitleGrant>();
    [Tooltip("Milestones completed after arrival.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Faction reputation changes applied after arrival.")]
    [SerializeField] List<ReputationChange> reputationRewards = new List<ReputationChange>();
    [Tooltip("Relationship changes applied after arrival.")]
    [SerializeField] List<RelationshipChange> relationshipRewards = new List<RelationshipChange>();
    [Tooltip("Lifestyle/playstyle points granted after arrival.")]
    [SerializeField] List<LifestylePointGrant> lifestyleRewards = new List<LifestylePointGrant>();

    [Header("Events")]
    [Tooltip("Optional event published when travel starts. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition departedEvent;
    [Tooltip("Optional event published when travel completes. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition arrivedEvent;
    [Tooltip("Optional event published when travel is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, generated route events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, generated route events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public RegionTravelMode TravelMode => travelMode;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public WorldRegionDefinition OriginRegion => originRegion;
    public WorldRegionDefinition DestinationRegion => destinationRegion;
    public bool RequireCurrentOriginRegion => requireCurrentOriginRegion;
    public string OriginPointId => originPointId;
    public string DestinationSceneName => string.IsNullOrWhiteSpace(destinationSceneName) && destinationRegion != null ? destinationRegion.DefaultSceneName : destinationSceneName;
    public string DestinationSpawnPointId => string.IsNullOrWhiteSpace(destinationSpawnPointId) && destinationRegion != null ? destinationRegion.DefaultSpawnPointId : destinationSpawnPointId;
    public bool UnlockedByDefault => unlockedByDefault;
    public bool RequiresRouteUnlock => requiresRouteUnlock;
    public ConsequenceChainRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxTravelCount => Mathf.Max(0, maxTravelCount);
    public IReadOnlyList<DayPeriod> AllowedPeriods => allowedPeriods != null ? (IReadOnlyList<DayPeriod>)allowedPeriods : Array.Empty<DayPeriod>();
    public IReadOnlyList<WeekDay> AllowedWeekDays => allowedWeekDays != null ? (IReadOnlyList<WeekDay>)allowedWeekDays : Array.Empty<WeekDay>();
    public int EstimatedTravelHours => Mathf.Max(0, estimatedTravelHours);
    public RegionTravelPolicyDefinition TravelPolicy => travelPolicy;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public float MoneyCost => Mathf.Max(0f, moneyCost);
    public IReadOnlyList<ActivityItemCost> ItemCosts => itemCosts != null ? (IReadOnlyList<ActivityItemCost>)itemCosts : Array.Empty<ActivityItemCost>();
    public IReadOnlyList<ActivityNeedCost> NeedCosts => needCosts != null ? (IReadOnlyList<ActivityNeedCost>)needCosts : Array.Empty<ActivityNeedCost>();
    public RegionChallengeProfileDefinition ChallengeProfile => challengeProfile;
    public bool DiscoverDestinationOnArrival => discoverDestinationOnArrival;
    public bool UnlockRouteAfterTravel => unlockRouteAfterTravel;
    public IReadOnlyList<TitleGrant> TitleRewards => titleRewards != null ? (IReadOnlyList<TitleGrant>)titleRewards : Array.Empty<TitleGrant>();
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete != null ? (IReadOnlyList<MilestoneDefinition>)milestonesToComplete : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<ReputationChange> ReputationRewards => reputationRewards != null ? (IReadOnlyList<ReputationChange>)reputationRewards : Array.Empty<ReputationChange>();
    public IReadOnlyList<RelationshipChange> RelationshipRewards => relationshipRewards != null ? (IReadOnlyList<RelationshipChange>)relationshipRewards : Array.Empty<RelationshipChange>();
    public IReadOnlyList<LifestylePointGrant> LifestyleRewards => lifestyleRewards != null ? (IReadOnlyList<LifestylePointGrant>)lifestyleRewards : Array.Empty<LifestylePointGrant>();

    public bool CanUse(PlayerController player, PlayerWorldRegionLog log, string sourceId, out string failureMessage) {
        return CanUse(player, log, sourceId, ResolveTravelPolicyOption(null), null, out failureMessage);
    }

    public bool CanUse(PlayerController player, PlayerWorldRegionLog log, string sourceId, RegionTravelPolicyOption policyOption, Pokemon selectedPokemon, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to use regional travel.";
            return false;
        }

        if(destinationRegion == null) {
            failureMessage = "This route has no destination region.";
            return false;
        }

        if(requireCurrentOriginRegion && originRegion != null && log != null && !log.IsCurrentRegion(originRegion)) {
            failureMessage = $"{DisplayName} cannot depart from the current region.";
            return false;
        }

        if(!IsUnlocked(log)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is locked." : lockedMessage;
            return false;
        }

        if(log != null && !log.CanTravel(this, sourceId, repeatMode, CooldownHours, MaxTravelCount, out failureMessage)) {
            if(!string.IsNullOrWhiteSpace(lockedMessage)) {
                failureMessage = lockedMessage;
            }
            return false;
        }

        if(!IsScheduledNow(out failureMessage)) {
            return false;
        }

        if(!MeetsAccess(player, out failureMessage)) {
            return false;
        }

        if(!RequirementsMet(player, out failureMessage)) {
            return false;
        }

        if(policyOption != null && !policyOption.CanUse(player, this, log, selectedPokemon, out failureMessage)) {
            return false;
        }

        if(!destinationRegion.CanEnter(player, out failureMessage)) {
            return false;
        }

        var resolvedChallenge = policyOption != null ? policyOption.ResolveChallengeProfile(this) : challengeProfile;
        if(resolvedChallenge != null && !resolvedChallenge.CanStart(player, out failureMessage)) {
            return false;
        }

        if(!CanPayCosts(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public RegionTravelPolicyOption ResolveTravelPolicyOption(string optionId) {
        return travelPolicy != null ? travelPolicy.GetOption(optionId) : null;
    }

    public bool IsUnlocked(PlayerWorldRegionLog log) {
        return unlockedByDefault || !requiresRouteUnlock || (log != null && log.HasUnlockedRoute(this));
    }

    public bool CanPayCosts(PlayerController player, out string failureMessage) {
        if(MoneyCost > 0f && (Wallet.i == null || !Wallet.i.HasMoney(MoneyCost))) {
            failureMessage = $"You need {MoneyCost:0} money to use {DisplayName}.";
            return false;
        }

        var inventory = player != null ? player.GetComponent<Inventory>() : null;
        foreach(var cost in ItemCosts) {
            if(cost == null || cost.item == null || cost.count <= 0) {
                continue;
            }

            int count = Mathf.Max(1, cost.count);
            if(inventory == null || !inventory.HasItemEnough(cost.item, count)) {
                failureMessage = $"You need {count} {cost.item.Name} to use {DisplayName}.";
                return false;
            }
        }

        var needs = player != null ? player.GetComponent<SurvivalNeedsController>() : null;
        foreach(var cost in NeedCosts) {
            if(cost == null || cost.need == null || cost.amount <= 0) {
                continue;
            }

            int amount = Mathf.Max(1, cost.amount);
            var need = needs != null ? needs.GetNeed(cost.need) : null;
            if(need == null || need.CurrentValue < amount) {
                failureMessage = $"You do not have enough {cost.need.DisplayName} for {DisplayName}.";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool TryPayCosts(PlayerController player, out string failureMessage) {
        if(!CanPayCosts(player, out failureMessage)) {
            return false;
        }

        if(MoneyCost > 0f) {
            Wallet.i.TakeMoney(MoneyCost);
        }

        var inventory = player != null ? player.GetComponent<Inventory>() : null;
        foreach(var cost in ItemCosts) {
            if(cost != null && cost.item != null && cost.count > 0) {
                inventory?.RemoveItem(cost.item, Mathf.Max(1, cost.count));
            }
        }

        var needs = player != null ? player.GetComponent<SurvivalNeedsController>() : null;
        foreach(var cost in NeedCosts) {
            if(cost != null && cost.need != null && cost.amount > 0) {
                needs?.ChangeNeed(cost.need, -Mathf.Max(1, cost.amount));
            }
        }

        failureMessage = null;
        return true;
    }

    public void ApplyArrivalRewards(PlayerController player, UnityEngine.Object context = null) {
        if(player == null) {
            return;
        }

        if(experienceReward > 0) {
            player.GetComponent<PlayerProgression>()?.AddExperience(experienceReward, experienceSource);
        }

        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleRewards, context);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationRewards);
        player.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipRewards);
        player.GetComponent<PlayerLifestyleLog>()?.ApplyGrants(lifestyleRewards, $"region-route:{Id}", DisplayName, context);
    }

    public void PublishDeparted(PlayerController player, RegionTravelResult result, UnityEngine.Object context = null) {
        PublishRouteEvent(departedEvent, "departed", GameEventImportance.Info, player, result, context);
    }

    public void PublishArrived(PlayerController player, RegionTravelResult result, UnityEngine.Object context = null) {
        PublishRouteEvent(arrivedEvent, "arrived", GameEventImportance.Success, player, result, context);
    }

    public void PublishBlocked(PlayerController player, RegionTravelResult result, UnityEngine.Object context = null) {
        PublishRouteEvent(blockedEvent, "blocked", GameEventImportance.Warning, player, result, context);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    bool MeetsAccess(PlayerController player, out string failureMessage) {
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

        if(requiredSkill != null) {
            int level = player.GetComponent<PlayerProgression>()?.GetSkillLevel(requiredSkill) ?? 0;
            if(level < Mathf.Max(0, requiredSkillLevel)) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredSkill.DisplayName} level {requiredSkillLevel}." : lockedMessage;
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

    bool RequirementsMet(PlayerController player, out string failureMessage) {
        var activeRequirements = requirements?.Where(requirement => requirement != null).ToList() ?? new List<ActivityRequirement>();
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

    bool IsScheduledNow(out string failureMessage) {
        if(AllowedPeriods.Count > 0) {
            DayPeriod currentPeriod = TimeSystem.i != null ? TimeSystem.i.CurrentPeriod : DayPeriod.None;
            if(!AllowedPeriods.Contains(currentPeriod)) {
                failureMessage = $"{DisplayName} is not available at this time of day.";
                return false;
            }
        }

        if(AllowedWeekDays.Count > 0 && TimeSystem.i != null) {
            var weekDay = (WeekDay)((Mathf.Max(1, TimeSystem.i.Day) - 1) % 7);
            if(!AllowedWeekDays.Contains(weekDay)) {
                failureMessage = $"{DisplayName} is not available today.";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    void PublishRouteEvent(GameEventDefinition eventDefinition, string phase, GameEventImportance importance, PlayerController player, RegionTravelResult result, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"region-route.{phase}.{Id}",
            phase == "blocked" ? $"{DisplayName} blocked." : $"{DisplayName} {phase}.",
            GameEventCategory.Transit,
            importance,
            context != null ? context : player,
            "RegionTravelRouteDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("routeId", Id),
            GameEventPublishing.Value("routeName", DisplayName),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("originRegionId", originRegion != null ? originRegion.Id : string.Empty),
            GameEventPublishing.Value("destinationRegionId", destinationRegion != null ? destinationRegion.Id : string.Empty),
            GameEventPublishing.Value("sourceId", result != null ? result.sourceId : string.Empty),
            GameEventPublishing.Value("blocked", result != null && result.blocked),
            GameEventPublishing.Value("challengeStarted", result != null && result.challengeStarted),
            GameEventPublishing.Value("policyId", result != null ? result.policyId : string.Empty),
            GameEventPublishing.Value("policyOptionId", result != null ? result.policyOptionId : string.Empty),
            GameEventPublishing.Value("partyTransferMode", result != null ? result.partyTransferMode : RegionPartyTransferMode.KeepCurrentParty));
    }
}

public class RegionTravelResult {
    public readonly string routeId;
    public readonly string routeName;
    public readonly string sourceId;
    public readonly string sourceName;
    public string originRegionId;
    public string originRegionName;
    public string destinationRegionId;
    public string destinationRegionName;
    public string destinationSceneName;
    public string destinationSpawnPointId;
    public bool blocked;
    public string failureMessage;
    public bool costsPaid;
    public bool destinationDiscovered;
    public bool challengeStarted;
    public string challengeId;
    public string challengeName;
    public string policyId;
    public string policyName;
    public string policyOptionId;
    public string policyOptionName;
    public string selectedPokemonId;
    public string selectedPokemonName;
    public RegionPartyTransferMode partyTransferMode = RegionPartyTransferMode.KeepCurrentParty;
    public int estimatedTravelHours;
    public readonly List<string> messages = new List<string>();

    public RegionTravelResult(RegionTravelRouteDefinition route, string sourceId, string sourceName) {
        routeId = route != null ? route.Id : string.Empty;
        routeName = route != null ? route.DisplayName : string.Empty;
        this.sourceId = sourceId;
        this.sourceName = sourceName;
        originRegionId = route != null && route.OriginRegion != null ? route.OriginRegion.Id : string.Empty;
        originRegionName = route != null && route.OriginRegion != null ? route.OriginRegion.DisplayName : string.Empty;
        destinationRegionId = route != null && route.DestinationRegion != null ? route.DestinationRegion.Id : string.Empty;
        destinationRegionName = route != null && route.DestinationRegion != null ? route.DestinationRegion.DisplayName : string.Empty;
        destinationSceneName = route != null ? route.DestinationSceneName : string.Empty;
        destinationSpawnPointId = route != null ? route.DestinationSpawnPointId : string.Empty;
        estimatedTravelHours = route != null ? route.EstimatedTravelHours : 0;
    }
}
