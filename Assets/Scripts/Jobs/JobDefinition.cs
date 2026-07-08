using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum JobType {
    General,
    Police,
    Research,
    Delivery,
    Bounty,
    Patrol,
    Gathering,
    Crafting,
    Farming,
    PokemonCare,
    Encounter,
    Social
}

public enum JobRepeatMode {
    Once,
    Repeatable,
    Daily,
    CooldownHours
}

public enum JobObjectiveType {
    HaveItem,
    CompleteActivity,
    EncounterSeen,
    EncounterBattleStarted,
    EncounterCaptured,
    EncounterStealthCaptured,
    KnowRecipe,
    HasTitle,
    CompleteMilestone,
    ShopPurchaseCount,
    PlayerLevel,
    ReputationAtLeast,
    TransitRouteUnlocked,
    TransitStopUnlocked,
    TransitRouteTravelCount,
    TransitRouteTagTravelCount,
    TransitAnyTravelCount
}

[CreateAssetMenu(menuName = "Jobs/Job Definition")]
public class JobDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this job. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this job.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad job type used by boards, filters and future UI.")]
    [SerializeField] JobType jobType = JobType.General;
    [Tooltip("Free-form tags used by boards, access rules and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Repeat Rules")]
    [Tooltip("How often this job can be accepted/completed.")]
    [SerializeField] JobRepeatMode repeatMode = JobRepeatMode.Once;
    [Tooltip("Cooldown in in-game hours when repeat mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("If enabled, the same job cannot be accepted while already active.")]
    [SerializeField] bool blockDuplicateActiveJob = true;

    [Header("Access")]
    [Tooltip("Optional title, badge or permit required before this job can be accepted.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this job.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional milestone required before this job can be accepted.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional skill required before this job can be accepted.")]
    [SerializeField] PlayerSkillDefinition requiredSkill;
    [Tooltip("Minimum level of the required skill.")]
    [Min(0)]
    [SerializeField] int requiredSkillLevel;
    [Tooltip("Message shown when access rules block this job.")]
    [SerializeField] string lockedMessage = "This job is not available yet.";

    [Header("Objectives")]
    [Tooltip("Objectives that must all be complete before this job can be turned in.")]
    [SerializeField] List<JobObjective> objectives = new List<JobObjective>();

    [Header("Rewards")]
    [Tooltip("Money awarded when this job completes.")]
    [Min(0f)]
    [SerializeField] float moneyReward;
    [Tooltip("Items awarded when this job completes.")]
    [SerializeField] List<JobItemReward> itemRewards = new List<JobItemReward>();
    [Tooltip("Trainer XP awarded when this job completes.")]
    [Min(0)]
    [SerializeField] int experienceReward;
    [Tooltip("Progression source used for trainer XP.")]
    [SerializeField] PlayerExperienceSource experienceSource = PlayerExperienceSource.Quest;
    [Tooltip("Faction reputation changes awarded on completion.")]
    [SerializeField] List<ReputationChange> reputationRewards = new List<ReputationChange>();
    [Tooltip("Relationship changes awarded on completion.")]
    [SerializeField] List<RelationshipChange> relationshipRewards = new List<RelationshipChange>();
    [Tooltip("Milestones completed when this job completes.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, badges or permits granted when this job completes.")]
    [SerializeField] List<TitleGrant> titleRewards = new List<TitleGrant>();
    [Tooltip("Crafting recipes learned when this job completes.")]
    [SerializeField] List<RecipeGrant> recipeRewards = new List<RecipeGrant>();
    [Tooltip("Career points awarded when this job completes.")]
    [SerializeField] List<CareerPointGrant> careerPointRewards = new List<CareerPointGrant>();
    [Tooltip("Life path XP, branch progress and tag counters awarded when this job completes.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();
    [Tooltip("Organization memberships granted when this job completes.")]
    [SerializeField] List<OrganizationMembershipGrant> organizationMembershipRewards = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points awarded when this job completes.")]
    [SerializeField] List<OrganizationPointGrant> organizationPointRewards = new List<OrganizationPointGrant>();

    [Header("Events")]
    [Tooltip("Optional event published when this job is accepted. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition acceptedEvent;
    [Tooltip("Optional event published when this job completes. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("If enabled, job events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, job events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public JobType JobType => jobType;
    public IReadOnlyList<string> Tags => tags;
    public JobRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public bool BlockDuplicateActiveJob => blockDuplicateActiveJob;
    public TitleDefinition RequiredTitle => requiredTitle;
    public ReputationFactionDefinition RequiredFaction => requiredFaction;
    public int RequiredReputation => requiredReputation;
    public MilestoneDefinition RequiredMilestone => requiredMilestone;
    public IReadOnlyList<JobObjective> Objectives => objectives;
    public IReadOnlyList<JobItemReward> ItemRewards => itemRewards;
    public IReadOnlyList<CareerPointGrant> CareerPointRewards => careerPointRewards;
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards;
    public IReadOnlyList<OrganizationMembershipGrant> OrganizationMembershipRewards => organizationMembershipRewards;
    public IReadOnlyList<OrganizationPointGrant> OrganizationPointRewards => organizationPointRewards;
    public GameEventDefinition AcceptedEvent => acceptedEvent;
    public GameEventDefinition CompletedEvent => completedEvent;

    public bool CanAccept(PlayerController player, PlayerJobLog log, string boardId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to accept jobs.";
            return false;
        }

        if(blockDuplicateActiveJob && log != null && log.HasActiveJob(this, boardId)) {
            failureMessage = $"{DisplayName} is already active.";
            return false;
        }

        if(!MeetsAccess(player, out failureMessage)) {
            return false;
        }

        if(log != null && !log.CanAccept(this, boardId, repeatMode, CooldownHours, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool IsCompleted(PlayerController player, PlayerJobState state, out string failureMessage) {
        foreach(var objective in objectives) {
            if(objective == null) {
                continue;
            }

            int index = objectives.IndexOf(objective);
            if(!objective.IsComplete(player, state, index)) {
                failureMessage = objective.GetProgressText(player, state, index);
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool TryComplete(PlayerController player, PlayerJobState state, out string failureMessage) {
        if(!IsCompleted(player, state, out failureMessage)) {
            return false;
        }

        if(!TryConsumeObjectiveCosts(player, state, out failureMessage)) {
            return false;
        }

        ApplyRewards(player);
        PublishCompleted(player);
        failureMessage = null;
        return true;
    }

    public List<JobObjectiveBaseline> CaptureBaselines(PlayerController player) {
        var baselines = new List<JobObjectiveBaseline>();
        for(int i = 0; i < objectives.Count; i++) {
            var objective = objectives[i];
            baselines.Add(new JobObjectiveBaseline {
                objectiveIndex = i,
                baseline = objective != null && objective.UseBaselineProgress ? objective.GetCurrentCount(player) : 0
            });
        }

        return baselines;
    }

    public void PublishAccepted(PlayerController player, string boardId) {
        GameEventPublishing.PublishOptional(
            acceptedEvent,
            $"job.accepted.{Id}",
            $"{DisplayName} accepted.",
            GameEventCategory.Job,
            GameEventImportance.Info,
            player,
            "JobDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("jobId", Id),
            GameEventPublishing.Value("jobName", DisplayName),
            GameEventPublishing.Value("jobType", jobType),
            GameEventPublishing.Value("boardId", boardId));
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
            if(skillLevel < requiredSkillLevel) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} requires {requiredSkill.DisplayName} level {requiredSkillLevel}." : lockedMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    bool TryConsumeObjectiveCosts(PlayerController player, PlayerJobState state, out string failureMessage) {
        foreach(var objective in objectives) {
            if(objective == null) {
                continue;
            }

            int index = objectives.IndexOf(objective);
            if(!objective.TryConsumeCompletionCost(player, state, index, out failureMessage)) {
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    void ApplyRewards(PlayerController player) {
        if(player == null) {
            return;
        }

        if(moneyReward > 0f) {
            Wallet.i?.AddMoney(moneyReward);
        }

        var inventory = player.GetComponent<Inventory>() ?? Inventory.GetInventory();
        foreach(var reward in itemRewards) {
            if(reward != null && reward.item != null && reward.count > 0) {
                inventory?.AddItem(reward.item, reward.count);
            }
        }

        if(experienceReward > 0) {
            player.GetComponent<PlayerProgression>()?.AddExperience(experienceReward, experienceSource);
        }

        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationRewards);
        player.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipRewards);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleRewards, player);
        player.GetComponent<PlayerRecipeBook>()?.ApplyGrants(recipeRewards, player);
        player.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(careerPointRewards, $"job:{Id}");
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewards, $"job:{Id}", DisplayName, this);
        var organizationLog = player.GetComponent<PlayerOrganizationLog>();
        organizationLog?.ApplyMembershipGrants(organizationMembershipRewards, $"job:{Id}");
        organizationLog?.ApplyPointGrants(organizationPointRewards, $"job:{Id}");
    }

    void PublishCompleted(PlayerController player) {
        GameEventPublishing.PublishOptional(
            completedEvent,
            $"job.completed.{Id}",
            $"{DisplayName} completed.",
            GameEventCategory.Job,
            GameEventImportance.Success,
            player,
            "JobDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("jobId", Id),
            GameEventPublishing.Value("jobName", DisplayName),
            GameEventPublishing.Value("jobType", jobType),
            GameEventPublishing.Value("money", moneyReward),
            GameEventPublishing.Value("experience", experienceReward));
    }
}

[System.Serializable]
public class JobObjective {
    [Header("Rule")]
    [Tooltip("What this objective checks.")]
    public JobObjectiveType type = JobObjectiveType.HaveItem;
    [Tooltip("Target count/value required for completion.")]
    [Min(1)]
    public int requiredCount = 1;
    [Tooltip("If enabled for Have Item objectives, required items are removed when the job completes.")]
    public bool consumeItemsOnComplete;
    [Tooltip("Optional text shown by future UI instead of auto-generated progress text.")]
    public string progressLabel;

    [Header("Definitions")]
    [Tooltip("Item checked by Have Item objectives.")]
    public ItemBase item;
    [Tooltip("Activity checked by Complete Activity objectives.")]
    public ActivityDefinition activity;
    [Tooltip("Pokemon checked by encounter objectives.")]
    public PokemonBase pokemon;
    [Tooltip("Encounter source filter used by encounter objectives.")]
    public EncounterSourceType encounterSourceType = EncounterSourceType.Any;
    [Tooltip("Recipe checked by Know Recipe objectives.")]
    public RecipeDefinition recipe;
    [Tooltip("Title checked by Has Title objectives.")]
    public TitleDefinition title;
    [Tooltip("Milestone checked by Complete Milestone objectives.")]
    public MilestoneDefinition milestone;
    [Tooltip("Faction checked by Reputation At Least objectives.")]
    public ReputationFactionDefinition faction;
    [Tooltip("Transit route checked by transit objectives.")]
    public TransitRouteDefinition transitRoute;
    [Tooltip("Transit stop checked by transit objectives.")]
    public TransitStopDefinition transitStop;
    [Tooltip("Transit route tag checked by Transit Route Tag Travel Count objectives.")]
    public string transitRouteTag;
    [Tooltip("Origin stop id filter used by Transit Route Travel Count objectives.")]
    public string transitOriginStopId;
    [Tooltip("Destination stop id filter used by Transit Route Travel Count objectives.")]
    public string transitDestinationStopId;

    [Header("Shop")]
    [Tooltip("Shop id checked by Shop Purchase Count objectives.")]
    public string shopId;
    [Tooltip("Offer id checked by Shop Purchase Count objectives.")]
    public string shopOfferId;
    [Tooltip("Purchase period checked by Shop Purchase Count objectives.")]
    public ShopStockLimitPeriod shopPurchasePeriod = ShopStockLimitPeriod.Total;

    public bool UseBaselineProgress => type == JobObjectiveType.CompleteActivity
        || type == JobObjectiveType.EncounterSeen
        || type == JobObjectiveType.EncounterBattleStarted
        || type == JobObjectiveType.EncounterCaptured
        || type == JobObjectiveType.EncounterStealthCaptured
        || type == JobObjectiveType.ShopPurchaseCount
        || type == JobObjectiveType.TransitRouteTravelCount
        || type == JobObjectiveType.TransitRouteTagTravelCount
        || type == JobObjectiveType.TransitAnyTravelCount;

    public bool IsComplete(PlayerController player, PlayerJobState state, int index) {
        return GetProgress(player, state, index) >= Mathf.Max(1, requiredCount);
    }

    public int GetProgress(PlayerController player, PlayerJobState state, int index) {
        int current = GetCurrentCount(player);
        int baseline = UseBaselineProgress ? state?.GetBaseline(index) ?? 0 : 0;
        return Mathf.Max(0, current - baseline);
    }

    public int GetCurrentCount(PlayerController player) {
        switch(type) {
            case JobObjectiveType.HaveItem:
                return item != null ? player?.GetComponent<Inventory>()?.GetItemCount(item) ?? Inventory.GetInventory()?.GetItemCount(item) ?? 0 : 0;
            case JobObjectiveType.CompleteActivity:
                return activity != null ? player?.GetComponent<PlayerActivityJournal>()?.GetLifetimeCompletions(activity) ?? 0 : 0;
            case JobObjectiveType.EncounterSeen:
                return GetEncounterCount(player, EncounterLogCountType.Seen);
            case JobObjectiveType.EncounterBattleStarted:
                return GetEncounterCount(player, EncounterLogCountType.BattleStarted);
            case JobObjectiveType.EncounterCaptured:
                return GetEncounterCount(player, EncounterLogCountType.Captured);
            case JobObjectiveType.EncounterStealthCaptured:
                return GetEncounterCount(player, EncounterLogCountType.StealthCaptured);
            case JobObjectiveType.KnowRecipe:
                return recipe != null && (player?.GetComponent<PlayerRecipeBook>()?.KnowsRecipe(recipe) ?? false) ? requiredCount : 0;
            case JobObjectiveType.HasTitle:
                return title != null && (player?.GetComponent<PlayerTitles>()?.HasTitle(title) ?? false) ? requiredCount : 0;
            case JobObjectiveType.CompleteMilestone:
                return milestone != null && (player?.GetComponent<PlayerMilestones>()?.HasMilestone(milestone) ?? false) ? requiredCount : 0;
            case JobObjectiveType.ShopPurchaseCount:
                return player?.GetComponent<PlayerShopLedger>()?.GetPurchasedCount(shopId, shopOfferId, shopPurchasePeriod) ?? 0;
            case JobObjectiveType.PlayerLevel:
                return player?.GetComponent<PlayerProgression>()?.Level ?? 0;
            case JobObjectiveType.ReputationAtLeast:
                return faction != null ? player?.GetComponent<PlayerReputation>()?.GetReputation(faction) ?? 0 : 0;
            case JobObjectiveType.TransitRouteUnlocked:
                return transitRoute != null && (player?.GetComponent<PlayerTransitLog>()?.HasUnlockedRoute(transitRoute) ?? false) ? requiredCount : 0;
            case JobObjectiveType.TransitStopUnlocked:
                return transitStop != null && (player?.GetComponent<PlayerTransitLog>()?.HasUnlockedStop(transitStop) ?? false) ? requiredCount : 0;
            case JobObjectiveType.TransitRouteTravelCount:
                return player?.GetComponent<PlayerTransitLog>()?.GetTravelCount(transitRoute, transitOriginStopId, transitDestinationStopId) ?? 0;
            case JobObjectiveType.TransitRouteTagTravelCount:
                return player?.GetComponent<PlayerTransitLog>()?.GetTravelCountWithTag(transitRouteTag) ?? 0;
            case JobObjectiveType.TransitAnyTravelCount:
                return player?.GetComponent<PlayerTransitLog>()?.GetTotalTravelCount() ?? 0;
            default:
                return 0;
        }
    }

    public string GetProgressText(PlayerController player, PlayerJobState state, int index) {
        if(!string.IsNullOrWhiteSpace(progressLabel)) {
            return $"{progressLabel}: {GetProgress(player, state, index)}/{requiredCount}";
        }

        return $"{type}: {GetProgress(player, state, index)}/{requiredCount}";
    }

    public bool TryConsumeCompletionCost(PlayerController player, PlayerJobState state, int index, out string failureMessage) {
        if(type != JobObjectiveType.HaveItem || !consumeItemsOnComplete || item == null) {
            failureMessage = null;
            return true;
        }

        var inventory = player?.GetComponent<Inventory>() ?? Inventory.GetInventory();
        int count = Mathf.Max(1, requiredCount);
        if(inventory == null || !inventory.HasItemEnough(item, count)) {
            failureMessage = $"You need {count} {item.Name}.";
            return false;
        }

        inventory.RemoveItem(item, count);
        failureMessage = null;
        return true;
    }

    int GetEncounterCount(PlayerController player, EncounterLogCountType countType) {
        return player?.GetComponent<PlayerEncounterLog>()?.GetCount(pokemon, encounterSourceType, countType) ?? 0;
    }
}

[System.Serializable]
public class JobItemReward {
    [Tooltip("Item awarded by this job.")]
    public ItemBase item;
    [Tooltip("Amount of this item awarded.")]
    [Min(1)]
    public int count = 1;
}

[System.Serializable]
public class JobObjectiveBaseline {
    [Tooltip("Objective index this baseline belongs to.")]
    public int objectiveIndex;
    [Tooltip("Runtime count captured when the job was accepted.")]
    public int baseline;
}
