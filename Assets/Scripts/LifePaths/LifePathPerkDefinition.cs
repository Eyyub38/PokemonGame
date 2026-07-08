using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Life Paths/Life Path Perk Definition")]
public class LifePathPerkDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this perk. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation of this perk.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Life path this perk belongs to.")]
    [SerializeField] LifePathDefinition lifePath = null;
    [Tooltip("Optional branch id this perk is visually/strategically associated with.")]
    [SerializeField] string branchId = string.Empty;
    [Tooltip("Free-form tags used by requirements, dialog insight and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future perk UI.")]
    [SerializeField] Sprite icon = null;

    [Header("Unlock Cost")]
    [Tooltip("Perk points spent when this perk is unlocked through normal progression.")]
    [Min(0)]
    [SerializeField] int perkPointCost = 1;
    [Tooltip("If enabled, this perk starts unlocked when the player first gains or queries this path.")]
    [SerializeField] bool unlockedByDefault = false;

    [Header("Eligibility")]
    [Tooltip("Minimum total XP required in the owning life path before this perk can be unlocked.")]
    [Min(0)]
    [SerializeField] int requiredPathExperience = 0;
    [Tooltip("Optional branch id whose progress is required.")]
    [SerializeField] string requiredBranchId = string.Empty;
    [Tooltip("Minimum progress required in Required Branch Id.")]
    [Min(0)]
    [SerializeField] int requiredBranchProgress = 0;
    [Tooltip("Optional activity/behavior tag counter required.")]
    [SerializeField] string requiredTag = string.Empty;
    [Tooltip("Minimum count required for Required Tag.")]
    [Min(0)]
    [SerializeField] int requiredTagCount = 0;
    [Tooltip("Other perks that must already be unlocked.")]
    [SerializeField] List<LifePathPerkDefinition> prerequisitePerks = new List<LifePathPerkDefinition>();
    [Tooltip("Reusable requirements that must pass before this perk can be unlocked.")]
    [SerializeField] List<ActivityRequirement> extraRequirements = new List<ActivityRequirement>();

    [Header("Effects")]
    [Tooltip("Rewards/effects applied once when this perk unlocks.")]
    [SerializeField] LifePathPerkEffectDefinition unlockEffects = new LifePathPerkEffectDefinition();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public LifePathDefinition LifePath => lifePath;
    public string BranchId => branchId;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public int PerkPointCost => Mathf.Max(0, perkPointCost);
    public bool UnlockedByDefault => unlockedByDefault;
    public int RequiredPathExperience => Mathf.Max(0, requiredPathExperience);
    public string RequiredBranchId => requiredBranchId;
    public int RequiredBranchProgress => Mathf.Max(0, requiredBranchProgress);
    public string RequiredTag => requiredTag;
    public int RequiredTagCount => Mathf.Max(0, requiredTagCount);
    public IReadOnlyList<LifePathPerkDefinition> PrerequisitePerks => prerequisitePerks != null ? (IReadOnlyList<LifePathPerkDefinition>)prerequisitePerks : Array.Empty<LifePathPerkDefinition>();
    public IReadOnlyList<ActivityRequirement> ExtraRequirements => extraRequirements != null ? (IReadOnlyList<ActivityRequirement>)extraRequirements : Array.Empty<ActivityRequirement>();
    public LifePathPerkEffectDefinition UnlockEffects => unlockEffects;

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

    public bool CanUnlock(PlayerController player, PlayerLifePathLog log, out string failureMessage) {
        if(lifePath == null) {
            failureMessage = $"{DisplayName} has no life path assigned.";
            return false;
        }

        if(log == null) {
            failureMessage = "Player life path log is missing.";
            return false;
        }

        if(log.HasPerk(this)) {
            failureMessage = $"{DisplayName} is already unlocked.";
            return false;
        }

        if(log.GetTotalExperience(lifePath) < RequiredPathExperience) {
            failureMessage = $"{lifePath.DisplayName} needs {RequiredPathExperience} XP.";
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredBranchId) && log.GetBranchProgress(lifePath, requiredBranchId) < RequiredBranchProgress) {
            failureMessage = $"{lifePath.DisplayName} branch {requiredBranchId} needs {RequiredBranchProgress} progress.";
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredTag) && log.GetTagProgress(lifePath, requiredTag) < RequiredTagCount) {
            failureMessage = $"{lifePath.DisplayName} tag {requiredTag} needs {RequiredTagCount} progress.";
            return false;
        }

        foreach(var prerequisite in PrerequisitePerks) {
            if(prerequisite != null && !log.HasPerk(prerequisite)) {
                failureMessage = $"{prerequisite.DisplayName} must be unlocked first.";
                return false;
            }
        }

        foreach(var requirement in ExtraRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public void ApplyUnlockEffects(PlayerController player, string sourceId, UnityEngine.Object context) {
        unlockEffects?.Apply(player, this, sourceId, context != null ? context : this);
    }
}

[Serializable]
public class LifePathPerkEffectDefinition {
    [Tooltip("Trainer XP granted when the perk unlocks.")]
    [Min(0)]
    [SerializeField] int trainerExperience = 0;
    [Tooltip("Progression source used for Trainer XP granted by this perk.")]
    [SerializeField] PlayerExperienceSource experienceSource = PlayerExperienceSource.Career;
    [Tooltip("Titles, badges, permits or medals granted when this perk unlocks.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Milestones completed when this perk unlocks.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Faction reputation changes applied when this perk unlocks.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Relationship changes applied when this perk unlocks.")]
    [SerializeField] List<RelationshipChange> relationshipChanges = new List<RelationshipChange>();
    [Tooltip("Lifestyle/playstyle points granted when this perk unlocks.")]
    [SerializeField] List<LifestylePointGrant> lifestylePointGrants = new List<LifestylePointGrant>();
    [Tooltip("Career points granted when this perk unlocks.")]
    [SerializeField] List<CareerPointGrant> careerPointGrants = new List<CareerPointGrant>();
    [Tooltip("Crafting recipes learned when this perk unlocks.")]
    [SerializeField] List<RecipeGrant> recipeGrants = new List<RecipeGrant>();
    [Tooltip("Organization memberships granted when this perk unlocks.")]
    [SerializeField] List<OrganizationMembershipGrant> organizationMembershipGrants = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points granted when this perk unlocks.")]
    [SerializeField] List<OrganizationPointGrant> organizationPointGrants = new List<OrganizationPointGrant>();
    [Tooltip("Battle rule sets unlocked when this perk unlocks.")]
    [SerializeField] List<BattleRuleSetDefinition> battleRulesToUnlock = new List<BattleRuleSetDefinition>();
    [Tooltip("Contest definitions unlocked when this perk unlocks.")]
    [SerializeField] List<ContestDefinition> contestsToUnlock = new List<ContestDefinition>();
    [Tooltip("Reusable consequence chains applied when this perk unlocks.")]
    [SerializeField] List<ConsequenceChainDefinition> consequenceChains = new List<ConsequenceChainDefinition>();

    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants != null ? (IReadOnlyList<TitleGrant>)titleGrants : Array.Empty<TitleGrant>();
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete != null ? (IReadOnlyList<MilestoneDefinition>)milestonesToComplete : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges != null ? (IReadOnlyList<ReputationChange>)reputationChanges : Array.Empty<ReputationChange>();
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges != null ? (IReadOnlyList<RelationshipChange>)relationshipChanges : Array.Empty<RelationshipChange>();
    public IReadOnlyList<LifestylePointGrant> LifestylePointGrants => lifestylePointGrants != null ? (IReadOnlyList<LifestylePointGrant>)lifestylePointGrants : Array.Empty<LifestylePointGrant>();
    public IReadOnlyList<CareerPointGrant> CareerPointGrants => careerPointGrants != null ? (IReadOnlyList<CareerPointGrant>)careerPointGrants : Array.Empty<CareerPointGrant>();
    public IReadOnlyList<RecipeGrant> RecipeGrants => recipeGrants != null ? (IReadOnlyList<RecipeGrant>)recipeGrants : Array.Empty<RecipeGrant>();
    public IReadOnlyList<OrganizationMembershipGrant> OrganizationMembershipGrants => organizationMembershipGrants != null ? (IReadOnlyList<OrganizationMembershipGrant>)organizationMembershipGrants : Array.Empty<OrganizationMembershipGrant>();
    public IReadOnlyList<OrganizationPointGrant> OrganizationPointGrants => organizationPointGrants != null ? (IReadOnlyList<OrganizationPointGrant>)organizationPointGrants : Array.Empty<OrganizationPointGrant>();
    public IReadOnlyList<BattleRuleSetDefinition> BattleRulesToUnlock => battleRulesToUnlock != null ? (IReadOnlyList<BattleRuleSetDefinition>)battleRulesToUnlock : Array.Empty<BattleRuleSetDefinition>();
    public IReadOnlyList<ContestDefinition> ContestsToUnlock => contestsToUnlock != null ? (IReadOnlyList<ContestDefinition>)contestsToUnlock : Array.Empty<ContestDefinition>();
    public IReadOnlyList<ConsequenceChainDefinition> ConsequenceChains => consequenceChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)consequenceChains : Array.Empty<ConsequenceChainDefinition>();

    public void Apply(PlayerController player, LifePathPerkDefinition perk, string sourceId, UnityEngine.Object context) {
        if(player == null) {
            return;
        }

        if(trainerExperience > 0) {
            player.GetComponent<PlayerProgression>()?.AddExperience(trainerExperience, experienceSource);
        }

        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, context);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        player.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipChanges);
        player.GetComponent<PlayerLifestyleLog>()?.ApplyGrants(lifestylePointGrants, $"life-path-perk:{perk.Id}", perk.DisplayName, context);
        player.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(careerPointGrants, $"life-path-perk:{perk.Id}");
        player.GetComponent<PlayerRecipeBook>()?.ApplyGrants(recipeGrants, context);

        var organizationLog = player.GetComponent<PlayerOrganizationLog>();
        organizationLog?.ApplyMembershipGrants(organizationMembershipGrants, $"life-path-perk:{perk.Id}");
        organizationLog?.ApplyPointGrants(organizationPointGrants, $"life-path-perk:{perk.Id}");

        var battleRuleLog = player.GetComponent<PlayerBattleRuleLog>();
        foreach(var rule in BattleRulesToUnlock) {
            battleRuleLog?.UnlockRuleSet(rule, sourceId ?? perk.Id);
        }

        var contestLog = player.GetComponent<PlayerContestLog>();
        foreach(var contest in ContestsToUnlock) {
            contestLog?.UnlockContest(contest, sourceId ?? perk.Id);
        }

        foreach(var chain in ConsequenceChains) {
            chain?.Apply(player, new ConsequenceChainContext {
                SourceId = sourceId ?? $"life-path-perk:{perk.Id}",
                SourceName = perk.DisplayName,
                ContextObject = context
            }, context);
        }
    }
}
