using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum InvestigationCaseCategory {
    General,
    PoliceCase,
    MissingPokemon,
    Theft,
    Poaching,
    RestrictedArea,
    ProfessorResearch,
    FieldStudy,
    Mystery,
    OrganizationCase,
    Custom
}

[CreateAssetMenu(menuName = "Investigations/Case Definition")]
public class InvestigationCaseDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this case. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in debug and future UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing explanation of this case.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad case category used by filters, requirements and future UI.")]
    [SerializeField] InvestigationCaseCategory category = InvestigationCaseCategory.General;
    [Tooltip("Free-form tags used by requirements, dialog and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Priority used by future UI sorting. Higher priority appears first.")]
    [SerializeField] int priority;

    [Header("Access")]
    [Tooltip("If enabled, this case can be started without being unlocked first.")]
    [SerializeField] bool unlockedByDefault = true;
    [Tooltip("Optional access profile required before this case can be started.")]
    [SerializeField] AccessProfileDefinition requiredAccessProfile;
    [Tooltip("Requirements checked before this case can be started.")]
    [SerializeField] List<ActivityRequirement> startRequirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when this case cannot be started.")]
    [SerializeField] string lockedMessage = "This case is not available yet.";

    [Header("Clues")]
    [Tooltip("Clues connected to this case.")]
    [SerializeField] List<InvestigationClueRule> clues = new List<InvestigationClueRule>();
    [Tooltip("If enabled, all clues marked Required For Completion must be discovered before completion.")]
    [SerializeField] bool requireRequiredClues = true;
    [Tooltip("Minimum discovered clue count required to complete this case.")]
    [Min(0)]
    [SerializeField] int requiredClueCount;
    [Tooltip("Minimum evidence points required to complete this case.")]
    [Min(0)]
    [SerializeField] int requiredEvidencePoints;

    [Header("Stages")]
    [Tooltip("Stage definitions used to calculate case progress. First matching highest threshold wins.")]
    [SerializeField] List<InvestigationStageDefinition> stages = new List<InvestigationStageDefinition>();

    [Header("Completion")]
    [Tooltip("Additional requirements checked before the case can be completed.")]
    [SerializeField] List<ActivityRequirement> completionRequirements = new List<ActivityRequirement>();
    [Tooltip("If enabled, the case completes automatically when completion requirements pass after clue discovery.")]
    [SerializeField] bool autoCompleteWhenReady;
    [Tooltip("Message shown when this case is completed.")]
    [SerializeField] string completedMessage;

    [Header("Completion Rewards")]
    [Tooltip("Money awarded when this case completes.")]
    [Min(0f)]
    [SerializeField] float moneyReward;
    [Tooltip("Items awarded when this case completes.")]
    [SerializeField] List<InvestigationItemReward> itemRewards = new List<InvestigationItemReward>();
    [Tooltip("Trainer XP awarded when this case completes.")]
    [Min(0)]
    [SerializeField] int experienceReward;
    [Tooltip("Progression source used for trainer XP.")]
    [SerializeField] PlayerExperienceSource experienceSource = PlayerExperienceSource.Quest;
    [Tooltip("Faction reputation changes awarded when this case completes.")]
    [SerializeField] List<ReputationChange> reputationRewards = new List<ReputationChange>();
    [Tooltip("Relationship changes awarded when this case completes.")]
    [SerializeField] List<RelationshipChange> relationshipRewards = new List<RelationshipChange>();
    [Tooltip("Milestones completed when this case completes.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, badges, permits or ranks granted when this case completes.")]
    [SerializeField] List<TitleGrant> titleRewards = new List<TitleGrant>();
    [Tooltip("Crafting recipes learned when this case completes.")]
    [SerializeField] List<RecipeGrant> recipeRewards = new List<RecipeGrant>();
    [Tooltip("Career points awarded when this case completes.")]
    [SerializeField] List<CareerPointGrant> careerPointRewards = new List<CareerPointGrant>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this case completes.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();
    [Tooltip("Organization memberships granted when this case completes.")]
    [SerializeField] List<OrganizationMembershipGrant> organizationMembershipRewards = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points awarded when this case completes.")]
    [SerializeField] List<OrganizationPointGrant> organizationPointRewards = new List<OrganizationPointGrant>();
    [Tooltip("Research progress awarded when this case completes.")]
    [SerializeField] List<ResearchProgressReward> researchRewards = new List<ResearchProgressReward>();

    [Header("Events")]
    [Tooltip("Optional event published when this case is unlocked.")]
    [SerializeField] GameEventDefinition unlockedEvent;
    [Tooltip("Optional event published when this case starts.")]
    [SerializeField] GameEventDefinition startedEvent;
    [Tooltip("Optional event published when this case completes.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("If enabled, case events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, case events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public InvestigationCaseCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public int Priority => priority;
    public bool UnlockedByDefault => unlockedByDefault;
    public IReadOnlyList<ActivityRequirement> StartRequirements => startRequirements;
    public IReadOnlyList<InvestigationClueRule> Clues => clues;
    public IReadOnlyList<InvestigationStageDefinition> Stages => stages;
    public IReadOnlyList<ActivityRequirement> CompletionRequirements => completionRequirements;
    public bool AutoCompleteWhenReady => autoCompleteWhenReady;
    public IReadOnlyList<InvestigationItemReward> ItemRewards => itemRewards;
    public IReadOnlyList<ReputationChange> ReputationRewards => reputationRewards;
    public IReadOnlyList<RelationshipChange> RelationshipRewards => relationshipRewards;
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete;
    public IReadOnlyList<TitleGrant> TitleRewards => titleRewards;
    public IReadOnlyList<RecipeGrant> RecipeRewards => recipeRewards;
    public IReadOnlyList<CareerPointGrant> CareerPointRewards => careerPointRewards;
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards;
    public IReadOnlyList<OrganizationMembershipGrant> OrganizationMembershipRewards => organizationMembershipRewards;
    public IReadOnlyList<OrganizationPointGrant> OrganizationPointRewards => organizationPointRewards;
    public IReadOnlyList<ResearchProgressReward> ResearchRewards => researchRewards;

    public bool CanStart(PlayerController player, PlayerInvestigationLog log, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to start cases.";
            return false;
        }

        if(!unlockedByDefault && !(log?.HasUnlockedCase(this) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not unlocked." : lockedMessage;
            return false;
        }

        if(log != null && log.HasActiveCase(this)) {
            failureMessage = $"{DisplayName} is already active.";
            return false;
        }

        if(log != null && log.HasCompletedCase(this)) {
            failureMessage = $"{DisplayName} has already been completed.";
            return false;
        }

        if(requiredAccessProfile != null && !requiredAccessProfile.CanAccess(player, out failureMessage)) {
            return false;
        }

        foreach(var requirement in startRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool CanDiscoverClue(PlayerController player, InvestigationClueDefinition clue, out string failureMessage) {
        var rule = GetClueRule(clue);
        if(rule == null || clue == null) {
            failureMessage = "This clue is not part of the case.";
            return false;
        }

        if(!clue.CanDiscover(player, out failureMessage)) {
            return false;
        }

        foreach(var requirement in rule.extraRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool CanComplete(PlayerController player, PlayerInvestigationState state, out string failureMessage) {
        if(state == null) {
            failureMessage = $"{DisplayName} is not active.";
            return false;
        }

        if(requiredClueCount > 0 && state.GetDiscoveredClueCount() < requiredClueCount) {
            failureMessage = $"More clues are needed for {DisplayName}.";
            return false;
        }

        if(requiredEvidencePoints > 0 && state.evidencePoints < requiredEvidencePoints) {
            failureMessage = $"More evidence is needed for {DisplayName}.";
            return false;
        }

        if(requireRequiredClues) {
            foreach(var rule in clues) {
                if(rule != null && rule.requiredForCompletion && rule.clue != null && !state.HasClue(rule.clue.Id)) {
                    failureMessage = $"{rule.clue.DisplayName} is required.";
                    return false;
                }
            }
        }

        foreach(var requirement in completionRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public int GetEvidencePointsForClue(InvestigationClueDefinition clue) {
        var rule = GetClueRule(clue);
        return rule != null && rule.overrideEvidencePoints ? Mathf.Max(0, rule.evidencePointsOverride) : clue != null ? clue.EvidencePoints : 0;
    }

    public InvestigationStageDefinition GetStageFor(PlayerInvestigationState state) {
        int evidence = state != null ? state.evidencePoints : 0;
        int clueCount = state != null ? state.GetDiscoveredClueCount() : 0;
        return stages
            .Where(stage => stage != null && stage.IsReached(evidence, clueCount))
            .OrderByDescending(stage => stage.requiredEvidencePoints)
            .ThenByDescending(stage => stage.requiredClueCount)
            .FirstOrDefault();
    }

    public int GetStageIndex(PlayerInvestigationState state) {
        var stage = GetStageFor(state);
        return stage != null ? stages.IndexOf(stage) : -1;
    }

    public InvestigationClueRule GetClueRule(InvestigationClueDefinition clue) {
        return clue != null ? clues.FirstOrDefault(rule => rule != null && rule.clue == clue) : null;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void ApplyCompletionRewards(PlayerController player) {
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
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleRewards, this);
        player.GetComponent<PlayerRecipeBook>()?.ApplyGrants(recipeRewards, this);
        player.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(careerPointRewards, $"investigation:{Id}");
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewards, $"investigation:{Id}", DisplayName, this);

        var organizationLog = player.GetComponent<PlayerOrganizationLog>();
        organizationLog?.ApplyMembershipGrants(organizationMembershipRewards, $"investigation:{Id}");
        organizationLog?.ApplyPointGrants(organizationPointRewards, $"investigation:{Id}");

        var researchLog = player.GetComponent<PlayerResearchLog>();
        foreach(var reward in researchRewards) {
            reward?.Apply(researchLog);
        }
    }

    public void PublishUnlocked(PlayerController player, string sourceId = null) {
        PublishCaseEvent(unlockedEvent, "unlocked", $"{DisplayName} unlocked.", GameEventImportance.Success, player, sourceId);
    }

    public void PublishStarted(PlayerController player, string sourceId = null) {
        PublishCaseEvent(startedEvent, "started", $"{DisplayName} started.", GameEventImportance.Info, player, sourceId);
    }

    public void PublishCompleted(PlayerController player, string sourceId = null) {
        string message = string.IsNullOrWhiteSpace(completedMessage) ? $"{DisplayName} completed." : completedMessage;
        PublishCaseEvent(completedEvent, "completed", message, GameEventImportance.Success, player, sourceId);
    }

    void PublishCaseEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, string sourceId) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"investigation.{phase}.{Id}",
            message,
            GameEventCategory.Investigation,
            importance,
            player != null ? player : this,
            "InvestigationCaseDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("caseId", Id),
            GameEventPublishing.Value("caseName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId));
    }
}

[Serializable]
public class InvestigationClueRule {
    [Tooltip("Clue connected to this case.")]
    public InvestigationClueDefinition clue;
    [Tooltip("If enabled, this clue must be discovered before the case can complete.")]
    public bool requiredForCompletion;
    [Tooltip("If enabled, this rule overrides the clue's default evidence points.")]
    public bool overrideEvidencePoints;
    [Tooltip("Evidence points used when Override Evidence Points is enabled.")]
    [Min(0)]
    public int evidencePointsOverride;
    [Tooltip("Extra requirements checked before this clue can be discovered in this case.")]
    public List<ActivityRequirement> extraRequirements = new List<ActivityRequirement>();
}

[Serializable]
public class InvestigationStageDefinition {
    [Tooltip("Stable stage id used by debug/future UI.")]
    public string id;
    [Tooltip("Stage name shown in debug/future UI.")]
    public string displayName;
    [Tooltip("Evidence points required for this stage.")]
    [Min(0)]
    public int requiredEvidencePoints;
    [Tooltip("Discovered clue count required for this stage.")]
    [Min(0)]
    public int requiredClueCount;

    public bool IsReached(int evidencePoints, int clueCount) {
        return evidencePoints >= Mathf.Max(0, requiredEvidencePoints)
            && clueCount >= Mathf.Max(0, requiredClueCount);
    }
}

[Serializable]
public class InvestigationItemReward {
    [Tooltip("Item awarded by this investigation case.")]
    public ItemBase item;
    [Tooltip("Amount of this item awarded.")]
    [Min(1)]
    public int count = 1;
}
