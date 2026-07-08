using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Activities/Activity Definition")]
public class ActivityDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this activity. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI and debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this activity.")]
    [TextArea][SerializeField] string description;
    [Tooltip("Free-form tags used by area modifiers, requirements and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Progression")]
    [Tooltip("Which progression source this activity uses for trainer XP multipliers.")]
    [SerializeField] PlayerExperienceSource experienceSource = PlayerExperienceSource.Exploration;
    [Tooltip("Base trainer XP granted when the activity completes.")]
    [Min(0)]
    [SerializeField] int baseExperience = 10;
    [Tooltip("Optional skill used by activity-specific systems as a bonus source.")]
    [SerializeField] PlayerSkillDefinition bonusSkill;

    [Header("Repeat Rules")]
    [Tooltip("How many times this activity can be completed per in-game day. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int dailyLimit;
    [Tooltip("How many in-game hours must pass before this activity can be repeated. 0 means no cooldown.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Optional custom message shown when the daily limit blocks the activity.")]
    [SerializeField] string dailyLimitMessage;
    [Tooltip("Optional custom message shown when cooldown blocks the activity.")]
    [SerializeField] string cooldownMessage;

    [Header("Area Rules")]
    [Tooltip("If enabled, this activity can only be performed inside an Activity Zone that allows it.")]
    [SerializeField] bool requiresActivityZone = true;
    [Tooltip("Optional message shown when the player is not inside a valid area for this activity.")]
    [SerializeField] string noValidAreaMessage;

    [Header("Costs")]
    [Tooltip("Items removed from the inventory when the activity successfully starts.")]
    [SerializeField] List<ActivityItemCost> itemCosts = new List<ActivityItemCost>();
    [Tooltip("Tool durability consumed when the activity successfully starts.")]
    [SerializeField] List<ActivityToolCost> toolCosts = new List<ActivityToolCost>();
    [Tooltip("Survival need values consumed when the activity successfully starts.")]
    [SerializeField] List<ActivityNeedCost> needCosts = new List<ActivityNeedCost>();

    [Header("Rewards")]
    [Tooltip("Faction reputation changes applied when this activity completes.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Personal relationship changes applied when this activity completes.")]
    [SerializeField] List<RelationshipChange> relationshipChanges = new List<RelationshipChange>();
    [Tooltip("Milestones completed when this activity completes.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Career points awarded when this activity completes.")]
    [SerializeField] List<CareerPointGrant> careerPointRewards = new List<CareerPointGrant>();
    [Tooltip("Life path XP, branch progress and tag counters awarded when this activity completes.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();
    [Tooltip("Organization memberships granted when this activity completes.")]
    [SerializeField] List<OrganizationMembershipGrant> organizationMembershipRewards = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points awarded when this activity completes.")]
    [SerializeField] List<OrganizationPointGrant> organizationPointRewards = new List<OrganizationPointGrant>();
    [Tooltip("Random or conditional extra outcomes rolled after this activity completes.")]
    [SerializeField] List<ActivityOutcomeDefinition> outcomes = new List<ActivityOutcomeDefinition>();

    [Header("Requirements")]
    [Tooltip("All requirements that must pass before the activity can be performed.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Events")]
    [Tooltip("Optional event published when this activity completes through ApplyRewards. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition completedEvent;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags;
    public PlayerExperienceSource ExperienceSource => experienceSource;
    public int BaseExperience => baseExperience;
    public PlayerSkillDefinition BonusSkill => bonusSkill;
    public int DailyLimit => Mathf.Max(0, dailyLimit);
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public bool RequiresActivityZone => requiresActivityZone;
    public IReadOnlyList<ActivityItemCost> ItemCosts => itemCosts;
    public IReadOnlyList<ActivityToolCost> ToolCosts => toolCosts;
    public IReadOnlyList<ActivityNeedCost> NeedCosts => needCosts;
    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges;
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges;
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete;
    public IReadOnlyList<CareerPointGrant> CareerPointRewards => careerPointRewards;
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards;
    public IReadOnlyList<OrganizationMembershipGrant> OrganizationMembershipRewards => organizationMembershipRewards;
    public IReadOnlyList<OrganizationPointGrant> OrganizationPointRewards => organizationPointRewards;
    public IReadOnlyList<ActivityOutcomeDefinition> Outcomes => outcomes;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements;
    public GameEventDefinition CompletedEvent => completedEvent;

    public bool CanPerform(PlayerController player, out string failureMessage) {
        if(!PlayerActivityContext.CanPerform(this, player, out failureMessage)) {
            return false;
        }

        foreach(var requirement in requirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        if(WorldEventManager.i != null && WorldEventManager.i.IsActivityBlocked(this, out failureMessage)) {
            return false;
        }

        var conditionLog = player != null ? player.GetComponent<PlayerWorldConditionLog>() : null;
        if(conditionLog != null && conditionLog.IsActivityBlocked(this, out failureMessage)) {
            return false;
        }

        var journal = player != null ? player.GetComponent<PlayerActivityJournal>() : null;
        if(journal != null && !journal.CanPerform(this, DailyLimit, CooldownHours, out failureMessage, out var blockReason)) {
            if(blockReason == ActivityJournalBlockReason.DailyLimit && !string.IsNullOrWhiteSpace(dailyLimitMessage)) {
                failureMessage = dailyLimitMessage;
            } else if(blockReason == ActivityJournalBlockReason.Cooldown && !string.IsNullOrWhiteSpace(cooldownMessage)) {
                failureMessage = cooldownMessage;
            }
            return false;
        }

        if(!CanPayCosts(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
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

    public string GetNoValidAreaMessage() {
        if(!string.IsNullOrWhiteSpace(noValidAreaMessage)) {
            return noValidAreaMessage;
        }

        return $"{DisplayName} can only be done in a suitable activity area.";
    }

    public bool CanPayCosts(PlayerController player, out string failureMessage) {
        var inventory = Inventory.GetInventory();
        foreach(var cost in itemCosts) {
            if(cost == null || cost.item == null || cost.count <= 0) {
                continue;
            }

            int itemCost = PlayerActivityContext.ModifyItemCost(this, cost.count);
            if(itemCost <= 0) {
                continue;
            }

            if(inventory == null || !inventory.HasItemEnough(cost.item, itemCost)) {
                failureMessage = $"You need {itemCost} {cost.item.Name} to do {DisplayName}.";
                return false;
            }
        }

        var toolInventory = player != null ? player.GetComponent<PlayerToolInventory>() : null;
        foreach(var cost in toolCosts) {
            if(cost == null || cost.tool == null || cost.durabilityCost <= 0) {
                continue;
            }

            int durabilityCost = PlayerActivityContext.ModifyToolDurabilityCost(this, cost.durabilityCost);
            if(toolInventory == null || !toolInventory.HasTool(cost.tool, 1, durabilityCost)) {
                failureMessage = $"{cost.tool.DisplayName} does not have enough durability for {DisplayName}.";
                return false;
            }
        }

        var survivalNeeds = player != null ? player.GetComponent<SurvivalNeedsController>() : null;
        foreach(var cost in needCosts) {
            if(cost == null || cost.need == null || cost.amount <= 0) {
                continue;
            }

            int needCost = PlayerActivityContext.ModifyNeedCost(this, cost.amount);
            if(needCost <= 0) {
                continue;
            }

            var need = survivalNeeds?.GetNeed(cost.need);
            if(need == null || need.CurrentValue < needCost) {
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

        var inventory = Inventory.GetInventory();
        foreach(var cost in itemCosts) {
            if(cost != null && cost.item != null && cost.count > 0) {
                inventory?.RemoveItem(cost.item, PlayerActivityContext.ModifyItemCost(this, cost.count));
            }
        }

        var toolInventory = player != null ? player.GetComponent<PlayerToolInventory>() : null;
        foreach(var cost in toolCosts) {
            if(cost != null && cost.tool != null && cost.durabilityCost > 0) {
                toolInventory?.ConsumeDurability(cost.tool, PlayerActivityContext.ModifyToolDurabilityCost(this, cost.durabilityCost));
            }
        }

        var survivalNeeds = player != null ? player.GetComponent<SurvivalNeedsController>() : null;
        foreach(var cost in needCosts) {
            if(cost != null && cost.need != null && cost.amount > 0) {
                survivalNeeds?.ChangeNeed(cost.need, -PlayerActivityContext.ModifyNeedCost(this, cost.amount));
            }
        }

        failureMessage = null;
        return true;
    }

    public void ApplyRewards(PlayerController player) {
        if(player == null) {
            return;
        }

        int experienceReward = PlayerActivityContext.ModifyExperience(this, baseExperience);
        experienceReward = WorldEventManager.i != null
            ? WorldEventManager.i.ModifyExperience(this, experienceReward)
            : experienceReward;

        player.GetComponent<PlayerProgression>()?.AddExperience(experienceReward, experienceSource);

        if(WorldEventManager.i != null) {
            WorldEventManager.i.ApplyActivityReputation(player, this);
        } else {
            player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        }

        RecordCompletion(player);
        CompleteMilestones(player);
        ApplyCareerPointRewards(player);
        ApplyLifePathRewards(player);
        ApplyOrganizationRewards(player);
        ApplyOutcomes(player);
        PublishCompletedEvent(player, experienceReward, "ActivityDefinition");
    }

    public void ApplyRelationshipRewards(PlayerController player) {
        player?.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipChanges);
    }

    public void CompleteMilestones(PlayerController player) {
        player?.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
    }

    public void ApplyCareerPointRewards(PlayerController player) {
        player?.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(careerPointRewards, $"activity:{Id}");
    }

    public void ApplyLifePathRewards(PlayerController player) {
        player?.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewards, $"activity:{Id}", DisplayName, this);
    }

    public void ApplyOrganizationRewards(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerOrganizationLog>() : null;
        log?.ApplyMembershipGrants(organizationMembershipRewards, $"activity:{Id}");
        log?.ApplyPointGrants(organizationPointRewards, $"activity:{Id}");
    }

    public void ApplyOutcomes(PlayerController player) {
        if(player == null) {
            return;
        }

        foreach(var outcome in outcomes) {
            outcome?.TryApply(player);
        }
    }

    public void RecordCompletion(PlayerController player) {
        player?.GetComponent<PlayerActivityJournal>()?.RecordCompletion(this);
        player?.GetComponent<PlayerLifestyleLog>()?.RecordActivityCompletion(this);
    }

    public void PublishCompletedEvent(PlayerController player, int experienceReward, string source) {
        GameEventPublishing.PublishOptional(
            completedEvent,
            $"activity.completed.{Id}",
            $"{DisplayName} completed.",
            GameEventCategory.Activity,
            GameEventImportance.Success,
            player,
            source,
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("activityId", Id),
            GameEventPublishing.Value("activityName", DisplayName),
            GameEventPublishing.Value("experience", experienceReward),
            GameEventPublishing.Value("experienceSource", experienceSource));
    }
}
