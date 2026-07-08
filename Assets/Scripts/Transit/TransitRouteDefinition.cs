using System;
using System.Collections.Generic;
using UnityEngine;

public enum TransitRouteType {
    Walk,
    Bicycle,
    Bus,
    Train,
    Boat,
    Ferry,
    Taxi,
    RidePokemon,
    CableCar,
    Airship,
    Special
}

[CreateAssetMenu(menuName = "Transit/Route Definition")]
public class TransitRouteDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this route. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in future transit UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this route.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad route type used by UI filters, access rules and future vehicle logic.")]
    [SerializeField] TransitRouteType routeType = TransitRouteType.Bus;
    [Tooltip("Free-form tags used by requirements, jobs, dialog conditions and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Endpoints")]
    [Tooltip("Optional origin stop id. Empty means this route can be listed by any stop that references it.")]
    [SerializeField] string originStopId;
    [Tooltip("Destination stop id recorded in the player transit log.")]
    [SerializeField] string destinationStopId;
    [Tooltip("Optional target scene name or scene index label for future scene loading UI.")]
    [SerializeField] string destinationSceneName;
    [Tooltip("Optional destination portal/spawn key for future scene loading UI.")]
    [SerializeField] string destinationPortalId;

    [Header("Unlock Rules")]
    [Tooltip("If enabled, this route is usable without being manually unlocked in PlayerTransitLog.")]
    [SerializeField] bool unlockedByDefault = true;
    [Tooltip("If enabled, PlayerTransitLog must contain this route unless it is unlocked by default.")]
    [SerializeField] bool requiresRouteUnlock;
    [Tooltip("Message shown when route unlock rules block travel.")]
    [SerializeField] string lockedMessage = "This route is not available yet.";

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required to use this route.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this route.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional milestone required before this route can be used.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional skill required before this route can be used.")]
    [SerializeField] PlayerSkillDefinition requiredSkill;
    [Tooltip("Minimum level of the required skill.")]
    [Min(0)]
    [SerializeField] int requiredSkillLevel;
    [Tooltip("Optional world event whose active state gates this route.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for the required world event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("Allowed day periods for this route. Empty means always available.")]
    [SerializeField] List<DayPeriod> allowedPeriods = new List<DayPeriod>();

    [Header("Costs")]
    [Tooltip("Money removed from Wallet when travel starts.")]
    [Min(0f)]
    [SerializeField] float moneyCost;
    [Tooltip("Items removed from the player's inventory when travel starts.")]
    [SerializeField] List<ActivityItemCost> itemCosts = new List<ActivityItemCost>();
    [Tooltip("Survival needs consumed when travel starts, such as energy or stamina.")]
    [SerializeField] List<ActivityNeedCost> needCosts = new List<ActivityNeedCost>();
    [Tooltip("Estimated in-game hours this route takes. Stored in logs/events; clock mutation can be added by UI/scene flow later.")]
    [Min(0)]
    [SerializeField] int estimatedTravelHours;

    [Header("Arrival Rewards")]
    [Tooltip("Trainer XP awarded when travel completes.")]
    [Min(0)]
    [SerializeField] int experienceReward;
    [Tooltip("Progression source used for trainer XP from this route.")]
    [SerializeField] PlayerExperienceSource experienceSource = PlayerExperienceSource.Exploration;
    [Tooltip("Faction reputation changes applied when travel completes.")]
    [SerializeField] List<ReputationChange> reputationRewards = new List<ReputationChange>();
    [Tooltip("Relationship changes applied when travel completes.")]
    [SerializeField] List<RelationshipChange> relationshipRewards = new List<RelationshipChange>();
    [Tooltip("Milestones completed when travel completes.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, passes or licenses granted when travel completes.")]
    [SerializeField] List<TitleGrant> titleRewards = new List<TitleGrant>();

    [Header("Events")]
    [Tooltip("Optional event published when travel starts. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition departedEvent;
    [Tooltip("Optional event published when travel completes. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition arrivedEvent;
    [Tooltip("Optional event published when travel is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, transit route events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, transit route events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public TransitRouteType RouteType => routeType;
    public IReadOnlyList<string> Tags => tags;
    public string OriginStopId => originStopId;
    public string DestinationStopId => destinationStopId;
    public string DestinationSceneName => destinationSceneName;
    public string DestinationPortalId => destinationPortalId;
    public bool UnlockedByDefault => unlockedByDefault;
    public bool RequiresRouteUnlock => requiresRouteUnlock;
    public TitleDefinition RequiredTitle => requiredTitle;
    public ReputationFactionDefinition RequiredFaction => requiredFaction;
    public int RequiredReputation => requiredReputation;
    public MilestoneDefinition RequiredMilestone => requiredMilestone;
    public PlayerSkillDefinition RequiredSkill => requiredSkill;
    public int RequiredSkillLevel => Mathf.Max(0, requiredSkillLevel);
    public WorldEventDefinition RequiredWorldEvent => requiredWorldEvent;
    public float MoneyCost => Mathf.Max(0f, moneyCost);
    public IReadOnlyList<ActivityItemCost> ItemCosts => itemCosts;
    public IReadOnlyList<ActivityNeedCost> NeedCosts => needCosts;
    public int EstimatedTravelHours => Mathf.Max(0, estimatedTravelHours);
    public GameEventDefinition DepartedEvent => departedEvent;
    public GameEventDefinition ArrivedEvent => arrivedEvent;
    public GameEventDefinition BlockedEvent => blockedEvent;

    public bool CanUse(PlayerController player, PlayerTransitLog log, string currentStopId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to use transit.";
            return false;
        }

        if(!CanDepartFrom(currentStopId)) {
            failureMessage = $"{DisplayName} cannot depart from this stop.";
            return false;
        }

        if(!IsUnlocked(log)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is locked." : lockedMessage;
            return false;
        }

        if(!MeetsAccess(player, out failureMessage)) {
            return false;
        }

        if(!CanPayCosts(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool CanDepartFrom(string stopId) {
        if(string.IsNullOrWhiteSpace(originStopId) || string.IsNullOrWhiteSpace(stopId)) {
            return true;
        }

        return string.Equals(originStopId, stopId, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsUnlocked(PlayerTransitLog log) {
        return unlockedByDefault || !requiresRouteUnlock || (log != null && log.HasUnlockedRoute(Id));
    }

    public bool CanPayCosts(PlayerController player, out string failureMessage) {
        if(MoneyCost > 0f && (Wallet.i == null || !Wallet.i.HasMoney(MoneyCost))) {
            failureMessage = $"You need {MoneyCost:0} money to use {DisplayName}.";
            return false;
        }

        var inventory = player != null ? player.GetComponent<Inventory>() : null;
        foreach(var cost in itemCosts) {
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
        foreach(var cost in needCosts) {
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
        foreach(var cost in itemCosts) {
            if(cost != null && cost.item != null && cost.count > 0) {
                inventory?.RemoveItem(cost.item, Mathf.Max(1, cost.count));
            }
        }

        var needs = player != null ? player.GetComponent<SurvivalNeedsController>() : null;
        foreach(var cost in needCosts) {
            if(cost != null && cost.need != null && cost.amount > 0) {
                needs?.ChangeNeed(cost.need, -Mathf.Max(1, cost.amount));
            }
        }

        failureMessage = null;
        return true;
    }

    public void ApplyArrivalEffects(PlayerController player) {
        if(player == null) {
            return;
        }

        if(experienceReward > 0) {
            player.GetComponent<PlayerProgression>()?.AddExperience(experienceReward, experienceSource);
        }

        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationRewards);
        player.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipRewards);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleRewards, player);
    }

    public void PublishDeparted(PlayerController player, string originStopIdOverride) {
        PublishRouteEvent(
            departedEvent,
            $"transit.departed.{Id}",
            $"{DisplayName} departed.",
            GameEventImportance.Info,
            player,
            "departed",
            originStopIdOverride,
            null);
    }

    public void PublishArrived(PlayerController player, string originStopIdOverride) {
        PublishRouteEvent(
            arrivedEvent,
            $"transit.arrived.{Id}",
            $"{DisplayName} arrived.",
            GameEventImportance.Success,
            player,
            "arrived",
            originStopIdOverride,
            null);
    }

    public void PublishBlocked(PlayerController player, string originStopIdOverride, string reason) {
        PublishRouteEvent(
            blockedEvent,
            $"transit.blocked.{Id}",
            string.IsNullOrWhiteSpace(reason) ? $"{DisplayName} is blocked." : reason,
            GameEventImportance.Warning,
            player,
            "blocked",
            originStopIdOverride,
            reason);
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        foreach(var entry in tags) {
            if(string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }

    bool MeetsAccess(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredMilestone != null && !(player.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredSkill != null) {
            int skillLevel = player.GetComponent<PlayerProgression>()?.GetSkillLevel(requiredSkill) ?? 0;
            if(skillLevel < RequiredSkillLevel) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} requires {requiredSkill.DisplayName} level {RequiredSkillLevel}." : lockedMessage;
                return false;
            }
        }

        if(requiredWorldEvent != null) {
            bool active = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredWorldEvent);
            if(active != requiredWorldEventActive) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not running right now." : lockedMessage;
                return false;
            }
        }

        if(allowedPeriods != null && allowedPeriods.Count > 0) {
            DayPeriod currentPeriod = TimeSystem.i != null ? TimeSystem.i.CurrentPeriod : DayPeriod.None;
            if(!allowedPeriods.Contains(currentPeriod)) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not available at this time." : lockedMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    void PublishRouteEvent(
        GameEventDefinition eventDefinition,
        string fallbackId,
        string fallbackMessage,
        GameEventImportance importance,
        PlayerController player,
        string phase,
        string originStopIdOverride,
        string reason
    ) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            fallbackId,
            fallbackMessage,
            GameEventCategory.Transit,
            importance,
            player != null ? player : this,
            "TransitRouteDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("routeId", Id),
            GameEventPublishing.Value("routeName", DisplayName),
            GameEventPublishing.Value("routeType", routeType),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("originStopId", string.IsNullOrWhiteSpace(originStopIdOverride) ? originStopId : originStopIdOverride),
            GameEventPublishing.Value("destinationStopId", destinationStopId),
            GameEventPublishing.Value("destinationSceneName", destinationSceneName),
            GameEventPublishing.Value("destinationPortalId", destinationPortalId),
            GameEventPublishing.Value("moneyCost", MoneyCost),
            GameEventPublishing.Value("estimatedTravelHours", EstimatedTravelHours),
            GameEventPublishing.Value("reason", reason));
    }
}
