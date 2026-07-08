using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CareerCategory {
    Trainer,
    Researcher,
    Breeder,
    Farmer,
    Ranger,
    Medic,
    Merchant,
    Performer,
    Officer,
    Explorer,
    Crafter,
    Custom
}

public enum CareerJoinMode {
    FreeJoin,
    RequiresAccess,
    MentorOnly,
    StoryOnly
}

[CreateAssetMenu(menuName = "Careers/Career Path Definition")]
public class CareerPathDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this career path. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in future career UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer or player-facing explanation of this career path.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad category used by filters, access checks and future UI styling.")]
    [SerializeField] CareerCategory category = CareerCategory.Trainer;
    [Tooltip("Free-form tags used by dialog, activities and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Joining")]
    [Tooltip("If enabled, the player knows about this career without unlocking it first.")]
    [SerializeField] bool unlockedByDefault = true;
    [Tooltip("How this career can be joined.")]
    [SerializeField] CareerJoinMode joinMode = CareerJoinMode.FreeJoin;
    [Tooltip("If enabled, gaining career points automatically joins the career when access allows it.")]
    [SerializeField] bool autoJoinOnPointGain = true;
    [Tooltip("If enabled, the player may be active in this career at the same time as other careers.")]
    [SerializeField] bool canRunAlongsideOtherCareers = true;
    [Tooltip("Message shown when this career cannot be joined and no more specific reason exists.")]
    [SerializeField] string lockedMessage = "This career path is not available yet.";

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this career can be joined.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this career can be joined.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this career.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum reputation required with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional player skill required before this career can be joined.")]
    [SerializeField] PlayerSkillDefinition requiredSkill;
    [Tooltip("Minimum level required for Required Skill.")]
    [Min(0)]
    [SerializeField] int requiredSkillLevel;
    [Tooltip("Optional calendar event that must be active before this career can be joined.")]
    [SerializeField] CalendarEventDefinition requiredActiveCalendarEvent;

    [Header("Progression")]
    [Tooltip("Ranks available in this career. Higher Min Points means later career rank.")]
    [SerializeField] List<CareerRankDefinition> ranks = new List<CareerRankDefinition>();
    [Tooltip("Trainer XP source used by rank rewards unless a rank overrides it.")]
    [SerializeField] PlayerExperienceSource defaultExperienceSource = PlayerExperienceSource.Career;

    [Header("Events")]
    [Tooltip("Optional event published when this career is unlocked.")]
    [SerializeField] GameEventDefinition unlockedEvent;
    [Tooltip("Optional event published when this career is joined.")]
    [SerializeField] GameEventDefinition joinedEvent;
    [Tooltip("Optional event published when this career rank increases.")]
    [SerializeField] GameEventDefinition rankUpEvent;
    [Tooltip("If enabled, career events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, career events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public CareerCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public bool UnlockedByDefault => unlockedByDefault;
    public CareerJoinMode JoinMode => joinMode;
    public bool AutoJoinOnPointGain => autoJoinOnPointGain;
    public bool CanRunAlongsideOtherCareers => canRunAlongsideOtherCareers;
    public IReadOnlyList<CareerRankDefinition> Ranks => ranks;
    public PlayerExperienceSource DefaultExperienceSource => defaultExperienceSource;

    public bool CanJoin(PlayerController player, bool viaMentor, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to join this career.";
            return false;
        }

        var log = player.GetComponent<PlayerCareerLog>();
        if(!unlockedByDefault && !(log?.HasUnlockedCareer(this) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not unlocked." : lockedMessage;
            return false;
        }

        if(joinMode == CareerJoinMode.MentorOnly && !viaMentor) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} requires a mentor." : lockedMessage;
            return false;
        }

        if(joinMode == CareerJoinMode.StoryOnly && !viaMentor) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is story-gated." : lockedMessage;
            return false;
        }

        if(!PassesAccess(player, out failureMessage)) {
            return false;
        }

        if(!canRunAlongsideOtherCareers && log != null && log.HasAnyJoinedCareerExcept(this)) {
            failureMessage = "Another exclusive career is already active.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public CareerRankDefinition GetRankForPoints(int points) {
        points = Mathf.Max(0, points);
        return GetOrderedRanks()
            .Where(rank => rank != null && points >= rank.MinPoints)
            .OrderByDescending(rank => rank.MinPoints)
            .FirstOrDefault();
    }

    public int GetRankIndex(CareerRankDefinition rank) {
        if(rank == null) {
            return -1;
        }

        return GetOrderedRanks().IndexOf(rank);
    }

    public CareerRankDefinition GetNextRank(int points) {
        points = Mathf.Max(0, points);
        return GetOrderedRanks()
            .Where(rank => rank != null && points < rank.MinPoints)
            .OrderBy(rank => rank.MinPoints)
            .FirstOrDefault();
    }

    public List<CareerRankDefinition> GetRanksReached(int points) {
        points = Mathf.Max(0, points);
        return GetOrderedRanks()
            .Where(rank => rank != null && points >= rank.MinPoints)
            .ToList();
    }

    public void ApplyRankRewards(PlayerController player, CareerRankDefinition rank, string source = null) {
        rank?.ApplyRewards(player, this, source);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishUnlocked(PlayerController player, string source = null) {
        PublishCareerEvent(unlockedEvent, "unlocked", $"{DisplayName} unlocked.", GameEventImportance.Success, player, null, 0, source);
    }

    public void PublishJoined(PlayerController player, string source = null) {
        PublishCareerEvent(joinedEvent, "joined", $"{DisplayName} joined.", GameEventImportance.Success, player, null, 0, source);
    }

    public void PublishRankUp(PlayerController player, CareerRankDefinition rank, int points, string source = null) {
        string rankName = rank != null ? rank.DisplayName : "Rank";
        PublishCareerEvent(rankUpEvent, "rank-up", $"{DisplayName} reached {rankName}.", GameEventImportance.Success, player, rank, points, source);
    }

    bool PassesAccess(PlayerController player, out string failureMessage) {
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

        if(requiredSkill != null && (player.GetComponent<PlayerProgression>()?.GetSkillLevel(requiredSkill) ?? 0) < requiredSkillLevel) {
            failureMessage = $"You need {requiredSkill.DisplayName} level {requiredSkillLevel}.";
            return false;
        }

        if(requiredActiveCalendarEvent != null && !requiredActiveCalendarEvent.IsActiveNow()) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{requiredActiveCalendarEvent.Title} is not active right now." : lockedMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }

    List<CareerRankDefinition> GetOrderedRanks() {
        return (ranks ?? new List<CareerRankDefinition>())
            .Where(rank => rank != null)
            .OrderBy(rank => rank.MinPoints)
            .ToList();
    }

    void PublishCareerEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, CareerRankDefinition rank, int points, string source) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"career.{phase}.{Id}",
            message,
            GameEventCategory.Career,
            importance,
            player != null ? player : this,
            "CareerPathDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("careerId", Id),
            GameEventPublishing.Value("careerName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("rankId", rank != null ? rank.Id : string.Empty),
            GameEventPublishing.Value("rankName", rank != null ? rank.DisplayName : string.Empty),
            GameEventPublishing.Value("points", points),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("source", source));
    }
}

[Serializable]
public class CareerRankDefinition {
    [Tooltip("Stable rank id used by save/debug output. Empty uses Display Name.")]
    public string id;
    [Tooltip("Name shown for this career rank.")]
    public string displayName;
    [Tooltip("Designer or player-facing explanation of this rank.")]
    [TextArea]
    public string description;
    [Tooltip("Career points required to reach this rank.")]
    [Min(0)]
    public int minPoints;
    [Tooltip("Trainer XP granted when this rank is reached.")]
    [Min(0)]
    public int trainerExperience;
    [Tooltip("If enabled, this rank uses Experience Source instead of the career default.")]
    public bool overrideExperienceSource;
    [Tooltip("Progression source used for trainer XP when Override Experience Source is enabled.")]
    public PlayerExperienceSource experienceSource = PlayerExperienceSource.Career;
    [Tooltip("Titles, badges, permits or ranks granted when this career rank is reached.")]
    public List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Crafting recipes learned when this career rank is reached.")]
    public List<RecipeGrant> recipeGrants = new List<RecipeGrant>();
    [Tooltip("Faction reputation changes applied when this career rank is reached.")]
    public List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Milestones completed when this career rank is reached.")]
    public List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Battle rule sets unlocked when this career rank is reached.")]
    public List<BattleRuleSetDefinition> battleRulesToUnlock = new List<BattleRuleSetDefinition>();
    [Tooltip("Contests unlocked when this career rank is reached.")]
    public List<ContestDefinition> contestsToUnlock = new List<ContestDefinition>();
    [Tooltip("Calendar events unlocked when this career rank is reached.")]
    public List<CalendarEventDefinition> calendarEventsToUnlock = new List<CalendarEventDefinition>();
    [Tooltip("Organization memberships granted when this career rank is reached.")]
    public List<OrganizationMembershipGrant> organizationMembershipRewards = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points awarded when this career rank is reached.")]
    public List<OrganizationPointGrant> organizationPointRewards = new List<OrganizationPointGrant>();

    public string Id => string.IsNullOrWhiteSpace(id) ? DisplayName : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
    public int MinPoints => Mathf.Max(0, minPoints);

    public void ApplyRewards(PlayerController player, CareerPathDefinition career, string source = null) {
        if(player == null) {
            return;
        }

        var experience = overrideExperienceSource ? experienceSource : career.DefaultExperienceSource;
        if(trainerExperience > 0) {
            player.GetComponent<PlayerProgression>()?.AddExperience(trainerExperience, experience);
        }

        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, career);
        player.GetComponent<PlayerRecipeBook>()?.ApplyGrants(recipeGrants, career);
        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);

        var battleLog = player.GetComponent<PlayerBattleRuleLog>();
        foreach(var rule in battleRulesToUnlock) {
            battleLog?.UnlockRuleSet(rule, source ?? career.Id);
        }

        var contestLog = player.GetComponent<PlayerContestLog>();
        foreach(var contest in contestsToUnlock) {
            contestLog?.UnlockContest(contest, source ?? career.Id);
        }

        var calendarLog = player.GetComponent<PlayerCalendarLog>();
        foreach(var calendarEvent in calendarEventsToUnlock) {
            calendarLog?.UnlockEvent(calendarEvent, source ?? career.Id);
        }

        var organizationLog = player.GetComponent<PlayerOrganizationLog>();
        organizationLog?.ApplyMembershipGrants(organizationMembershipRewards, $"career-rank:{career.Id}:{Id}");
        organizationLog?.ApplyPointGrants(organizationPointRewards, $"career-rank:{career.Id}:{Id}");
    }
}

[Serializable]
public class CareerPointGrant {
    [Tooltip("Career path that receives points.")]
    public CareerPathDefinition career;
    [Tooltip("Career points to add. Negative values are ignored by PlayerCareerLog.")]
    [Min(0)]
    public int points = 1;
    [Tooltip("Short source/reason stored in debug and save data.")]
    public string source;
}
