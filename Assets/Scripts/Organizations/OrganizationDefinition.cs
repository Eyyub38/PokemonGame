using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum OrganizationCategory {
    ProfessorLab,
    Police,
    Guild,
    BreederAssociation,
    ContestLeague,
    RangerUnion,
    MerchantCompany,
    TransitCompany,
    FarmCoop,
    Clinic,
    Club,
    Custom
}

public enum OrganizationJoinMode {
    FreeJoin,
    RequiresAccess,
    InvitationOnly,
    StoryOnly
}

[CreateAssetMenu(menuName = "Organizations/Organization Definition")]
public class OrganizationDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this organization. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in future organization UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer or player-facing explanation of this organization.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad organization category used by filters, access checks and future UI styling.")]
    [SerializeField] OrganizationCategory category = OrganizationCategory.Club;
    [Tooltip("Free-form tags used by dialog, access rules and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional exclusive group id. Organizations in the same non-empty group can block one another.")]
    [SerializeField] string exclusiveGroup;

    [Header("Joining")]
    [Tooltip("If enabled, the player knows about this organization without unlocking it first.")]
    [SerializeField] bool unlockedByDefault = true;
    [Tooltip("How this organization can be joined.")]
    [SerializeField] OrganizationJoinMode joinMode = OrganizationJoinMode.FreeJoin;
    [Tooltip("If enabled, gaining organization points can automatically join when access allows it.")]
    [SerializeField] bool autoJoinOnPointGain = true;
    [Tooltip("If enabled, this organization can be joined alongside organizations in the same Exclusive Group.")]
    [SerializeField] bool canRunAlongsideExclusiveGroup = true;
    [Tooltip("If enabled, joins are permanent unless a grant overrides duration.")]
    [SerializeField] bool permanentByDefault = true;
    [Tooltip("Default temporary membership duration in in-game hours. 0 means one hour if the membership is temporary.")]
    [Min(0)]
    [SerializeField] int defaultDurationHours;
    [Tooltip("If disabled, temporary grants are treated as permanent membership.")]
    [SerializeField] bool canBeTemporary = true;
    [Tooltip("Message shown when this organization cannot be joined and no more specific reason exists.")]
    [SerializeField] string lockedMessage = "This organization is not available yet.";

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this organization can be joined.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this organization can be joined.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this organization.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum reputation required with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional career path required before this organization can be joined.")]
    [SerializeField] CareerPathDefinition requiredCareer;
    [Tooltip("Minimum career rank index required for Required Career.")]
    [Min(0)]
    [SerializeField] int requiredCareerRankIndex;
    [Tooltip("Optional calendar event that must be active before this organization can be joined.")]
    [SerializeField] CalendarEventDefinition requiredActiveCalendarEvent;

    [Header("Ranks")]
    [Tooltip("Ranks available inside this organization. Higher Min Points means later rank.")]
    [SerializeField] List<OrganizationRankDefinition> ranks = new List<OrganizationRankDefinition>();

    [Header("Linked Content")]
    [Tooltip("Job boards associated with this organization.")]
    [SerializeField] List<JobBoardDefinition> linkedJobBoards = new List<JobBoardDefinition>();
    [Tooltip("Activities associated with this organization.")]
    [SerializeField] List<ActivityDefinition> linkedActivities = new List<ActivityDefinition>();
    [Tooltip("Shops associated with this organization.")]
    [SerializeField] List<ShopCatalogDefinition> linkedShops = new List<ShopCatalogDefinition>();
    [Tooltip("Battle challenges associated with this organization.")]
    [SerializeField] List<BattleChallengeDefinition> linkedBattleChallenges = new List<BattleChallengeDefinition>();
    [Tooltip("Contests associated with this organization.")]
    [SerializeField] List<ContestDefinition> linkedContests = new List<ContestDefinition>();

    [Header("Join Rewards")]
    [Tooltip("Titles, badges, permits or ranks granted when this organization is joined.")]
    [SerializeField] List<TitleGrant> joinTitleGrants = new List<TitleGrant>();
    [Tooltip("Careers unlocked when this organization is joined.")]
    [SerializeField] List<CareerPathDefinition> careersToUnlockOnJoin = new List<CareerPathDefinition>();
    [Tooltip("Careers joined as mentor/story joins when this organization is joined.")]
    [SerializeField] List<CareerPathDefinition> careersToJoinOnJoin = new List<CareerPathDefinition>();
    [Tooltip("Career points awarded when this organization is joined.")]
    [SerializeField] List<CareerPointGrant> joinCareerPointRewards = new List<CareerPointGrant>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this organization is joined.")]
    [SerializeField] List<LifePathReward> joinLifePathRewards = new List<LifePathReward>();
    [Tooltip("Organization points awarded immediately when this organization is joined.")]
    [Min(0)]
    [SerializeField] int joinOrganizationPoints;

    [Header("Events")]
    [Tooltip("Optional event published when this organization is unlocked.")]
    [SerializeField] GameEventDefinition unlockedEvent;
    [Tooltip("Optional event published when this organization is joined.")]
    [SerializeField] GameEventDefinition joinedEvent;
    [Tooltip("Optional event published when this organization rank increases.")]
    [SerializeField] GameEventDefinition rankUpEvent;
    [Tooltip("Optional event published when this organization membership expires.")]
    [SerializeField] GameEventDefinition expiredEvent;
    [Tooltip("If enabled, organization events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, organization events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public OrganizationCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public string ExclusiveGroup => exclusiveGroup;
    public bool UnlockedByDefault => unlockedByDefault;
    public OrganizationJoinMode JoinMode => joinMode;
    public bool AutoJoinOnPointGain => autoJoinOnPointGain;
    public bool CanRunAlongsideExclusiveGroup => canRunAlongsideExclusiveGroup;
    public bool PermanentByDefault => permanentByDefault;
    public int DefaultDurationHours => Mathf.Max(0, defaultDurationHours);
    public bool CanBeTemporary => canBeTemporary;
    public IReadOnlyList<OrganizationRankDefinition> Ranks => ranks;
    public IReadOnlyList<JobBoardDefinition> LinkedJobBoards => linkedJobBoards;
    public IReadOnlyList<ActivityDefinition> LinkedActivities => linkedActivities;
    public IReadOnlyList<ShopCatalogDefinition> LinkedShops => linkedShops;
    public IReadOnlyList<BattleChallengeDefinition> LinkedBattleChallenges => linkedBattleChallenges;
    public IReadOnlyList<ContestDefinition> LinkedContests => linkedContests;
    public IReadOnlyList<LifePathReward> JoinLifePathRewards => joinLifePathRewards;
    public int JoinOrganizationPoints => Mathf.Max(0, joinOrganizationPoints);

    public bool CanJoin(PlayerController player, bool viaInvitation, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to join this organization.";
            return false;
        }

        var log = player.GetComponent<PlayerOrganizationLog>();
        if(!unlockedByDefault && !(log?.HasUnlockedOrganization(this) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not unlocked." : lockedMessage;
            return false;
        }

        if((joinMode == OrganizationJoinMode.InvitationOnly || joinMode == OrganizationJoinMode.StoryOnly) && !viaInvitation) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} requires an invitation." : lockedMessage;
            return false;
        }

        if(!PassesAccess(player, out failureMessage)) {
            return false;
        }

        if(!canRunAlongsideExclusiveGroup && !string.IsNullOrWhiteSpace(exclusiveGroup) && log != null && log.HasActiveExclusiveMembership(exclusiveGroup, this)) {
            failureMessage = "Another exclusive organization membership is already active.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public int ResolveDurationHours(bool grantPermanently, int durationHours) {
        if(grantPermanently || !canBeTemporary) {
            return -1;
        }

        if(durationHours > 0) {
            return durationHours;
        }

        if(DefaultDurationHours > 0) {
            return DefaultDurationHours;
        }

        return permanentByDefault ? -1 : 1;
    }

    public OrganizationRankDefinition GetRankForPoints(int points) {
        points = Mathf.Max(0, points);
        return GetOrderedRanks()
            .Where(rank => rank != null && points >= rank.MinPoints)
            .OrderByDescending(rank => rank.MinPoints)
            .FirstOrDefault();
    }

    public int GetRankIndex(OrganizationRankDefinition rank) {
        if(rank == null) {
            return -1;
        }

        return GetOrderedRanks().IndexOf(rank);
    }

    public List<OrganizationRankDefinition> GetRanksReached(int points) {
        points = Mathf.Max(0, points);
        return GetOrderedRanks()
            .Where(rank => rank != null && points >= rank.MinPoints)
            .ToList();
    }

    public void ApplyJoinRewards(PlayerController player, string source = null) {
        if(player == null) {
            return;
        }

        player.GetComponent<PlayerTitles>()?.ApplyGrants(joinTitleGrants, this);

        var careerLog = player.GetComponent<PlayerCareerLog>();
        foreach(var career in careersToUnlockOnJoin) {
            careerLog?.UnlockCareer(career, source ?? Id);
        }

        foreach(var career in careersToJoinOnJoin) {
            careerLog?.JoinCareer(career, viaMentor: true, source ?? Id, out _);
        }

        careerLog?.ApplyPointGrants(joinCareerPointRewards, $"organization:{Id}");
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(joinLifePathRewards, $"organization:{Id}", DisplayName, this);
    }

    public void ApplyRankRewards(PlayerController player, OrganizationRankDefinition rank, string source = null) {
        rank?.ApplyRewards(player, this, source);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishUnlocked(PlayerController player, string source = null) {
        PublishOrganizationEvent(unlockedEvent, "unlocked", $"{DisplayName} unlocked.", GameEventImportance.Success, player, null, 0, source);
    }

    public void PublishJoined(PlayerController player, string source = null) {
        PublishOrganizationEvent(joinedEvent, "joined", $"{DisplayName} joined.", GameEventImportance.Success, player, null, 0, source);
    }

    public void PublishRankUp(PlayerController player, OrganizationRankDefinition rank, int points, string source = null) {
        string rankName = rank != null ? rank.DisplayName : "Rank";
        PublishOrganizationEvent(rankUpEvent, "rank-up", $"{DisplayName} reached {rankName}.", GameEventImportance.Success, player, rank, points, source);
    }

    public void PublishExpired(PlayerController player, string source = null) {
        PublishOrganizationEvent(expiredEvent, "expired", $"{DisplayName} membership expired.", GameEventImportance.Info, player, null, 0, source);
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

        if(requiredCareer != null && !(player.GetComponent<PlayerCareerLog>()?.HasReachedRank(requiredCareer, requiredCareerRankIndex) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more progress in {requiredCareer.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredActiveCalendarEvent != null && !requiredActiveCalendarEvent.IsActiveNow()) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{requiredActiveCalendarEvent.Title} is not active right now." : lockedMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }

    List<OrganizationRankDefinition> GetOrderedRanks() {
        return (ranks ?? new List<OrganizationRankDefinition>())
            .Where(rank => rank != null)
            .OrderBy(rank => rank.MinPoints)
            .ToList();
    }

    void PublishOrganizationEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, OrganizationRankDefinition rank, int points, string source) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"organization.{phase}.{Id}",
            message,
            GameEventCategory.Organization,
            importance,
            player != null ? player : this,
            "OrganizationDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("organizationId", Id),
            GameEventPublishing.Value("organizationName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("rankId", rank != null ? rank.Id : string.Empty),
            GameEventPublishing.Value("rankName", rank != null ? rank.DisplayName : string.Empty),
            GameEventPublishing.Value("points", points),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("source", source));
    }
}

[Serializable]
public class OrganizationRankDefinition {
    [Tooltip("Stable rank id used by save/debug output. Empty uses Display Name.")]
    public string id;
    [Tooltip("Name shown for this organization rank.")]
    public string displayName;
    [Tooltip("Designer or player-facing explanation of this rank.")]
    [TextArea]
    public string description;
    [Tooltip("Organization points required to reach this rank.")]
    [Min(0)]
    public int minPoints;
    [Tooltip("Titles, badges, permits or ranks granted when this rank is reached.")]
    public List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Careers unlocked when this organization rank is reached.")]
    public List<CareerPathDefinition> careersToUnlock = new List<CareerPathDefinition>();
    [Tooltip("Careers joined as mentor/story joins when this organization rank is reached.")]
    public List<CareerPathDefinition> careersToJoin = new List<CareerPathDefinition>();
    [Tooltip("Career points awarded when this organization rank is reached.")]
    public List<CareerPointGrant> careerPointRewards = new List<CareerPointGrant>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this organization rank is reached.")]
    public List<LifePathReward> lifePathRewards = new List<LifePathReward>();
    [Tooltip("Faction reputation changes applied when this organization rank is reached.")]
    public List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Milestones completed when this organization rank is reached.")]
    public List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Battle rule sets unlocked when this organization rank is reached.")]
    public List<BattleRuleSetDefinition> battleRulesToUnlock = new List<BattleRuleSetDefinition>();
    [Tooltip("Contests unlocked when this organization rank is reached.")]
    public List<ContestDefinition> contestsToUnlock = new List<ContestDefinition>();
    [Tooltip("Calendar events unlocked when this organization rank is reached.")]
    public List<CalendarEventDefinition> calendarEventsToUnlock = new List<CalendarEventDefinition>();
    [Tooltip("Transit routes unlocked when this organization rank is reached.")]
    public List<TransitRouteDefinition> transitRoutesToUnlock = new List<TransitRouteDefinition>();

    public string Id => string.IsNullOrWhiteSpace(id) ? DisplayName : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
    public int MinPoints => Mathf.Max(0, minPoints);

    public void ApplyRewards(PlayerController player, OrganizationDefinition organization, string source = null) {
        if(player == null) {
            return;
        }

        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, organization);

        var careerLog = player.GetComponent<PlayerCareerLog>();
        foreach(var career in careersToUnlock) {
            careerLog?.UnlockCareer(career, source ?? organization.Id);
        }

        foreach(var career in careersToJoin) {
            careerLog?.JoinCareer(career, viaMentor: true, source ?? organization.Id, out _);
        }

        careerLog?.ApplyPointGrants(careerPointRewards, $"organization-rank:{organization.Id}:{Id}");
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewards, $"organization-rank:{organization.Id}:{Id}", $"{organization.DisplayName} {DisplayName}", organization);
        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);

        var battleLog = player.GetComponent<PlayerBattleRuleLog>();
        foreach(var rule in battleRulesToUnlock) {
            battleLog?.UnlockRuleSet(rule, source ?? organization.Id);
        }

        var contestLog = player.GetComponent<PlayerContestLog>();
        foreach(var contest in contestsToUnlock) {
            contestLog?.UnlockContest(contest, source ?? organization.Id);
        }

        var calendarLog = player.GetComponent<PlayerCalendarLog>();
        foreach(var calendarEvent in calendarEventsToUnlock) {
            calendarLog?.UnlockEvent(calendarEvent, source ?? organization.Id);
        }

        var transitLog = player.GetComponent<PlayerTransitLog>();
        foreach(var route in transitRoutesToUnlock) {
            transitLog?.UnlockRoute(route, source ?? organization.Id);
        }
    }
}

[Serializable]
public class OrganizationMembershipGrant {
    [Tooltip("Organization that receives a membership grant.")]
    public OrganizationDefinition organization;
    [Tooltip("If enabled, this grant is permanent. If disabled, it uses duration/default hours when the organization allows temporary membership.")]
    public bool grantPermanently = true;
    [Tooltip("Temporary membership duration in in-game hours. 0 uses the organization default duration.")]
    [Min(0)]
    public int durationHours;
    [Tooltip("Organization points added immediately after joining or refreshing membership.")]
    [Min(0)]
    public int initialPoints;
    [Tooltip("Short source/reason stored in save/debug data.")]
    public string source;
    [Tooltip("If enabled, existing active membership is refreshed/replaced with this grant.")]
    public bool refreshExisting = true;
    [Tooltip("If enabled, invitation/story-only join modes accept this grant as an invitation.")]
    public bool countsAsInvitation = true;
}

[Serializable]
public class OrganizationPointGrant {
    [Tooltip("Organization that receives points.")]
    public OrganizationDefinition organization;
    [Tooltip("Organization points to add. Negative values are ignored by PlayerOrganizationLog.")]
    [Min(0)]
    public int points = 1;
    [Tooltip("Short source/reason stored in debug and save data.")]
    public string source;
    [Tooltip("If enabled, points can auto-join the organization when the organization allows it.")]
    public bool autoJoinIfAllowed = true;
}
