using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AssignmentCategory {
    General,
    PoliceCase,
    PolicePatrol,
    Investigation,
    ProfessorTask,
    FieldResearch,
    ResearchProject,
    PermitTask,
    Delivery,
    Bounty,
    OrganizationTask,
    Custom
}

public enum AssignmentRepeatMode {
    Once,
    Repeatable,
    Daily,
    CooldownHours
}

[CreateAssetMenu(menuName = "Assignments/Assignment Definition")]
public class AssignmentDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this assignment. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in future assignment UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer or player-facing explanation of this assignment.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad assignment category used by filters, access checks and future UI styling.")]
    [SerializeField] AssignmentCategory category = AssignmentCategory.General;
    [Tooltip("Free-form tags used by dialog, boards and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Priority used by future UI sorting. Higher priority appears first.")]
    [SerializeField] int priority;

    [Header("Repeat Rules")]
    [Tooltip("If enabled, this assignment can be accepted without being unlocked first.")]
    [SerializeField] bool unlockedByDefault = true;
    [Tooltip("How often this assignment can be completed.")]
    [SerializeField] AssignmentRepeatMode repeatMode = AssignmentRepeatMode.Once;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("If enabled, the same assignment cannot be accepted while already active.")]
    [SerializeField] bool blockDuplicateActiveAssignment = true;
    [Tooltip("Message shown when access rules block this assignment.")]
    [SerializeField] string lockedMessage = "This assignment is not available yet.";

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this assignment can be accepted.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional organization membership required before this assignment can be accepted.")]
    [SerializeField] OrganizationDefinition requiredOrganization;
    [Tooltip("Minimum organization rank index required for Required Organization.")]
    [Min(0)]
    [SerializeField] int requiredOrganizationRankIndex;
    [Tooltip("Optional career required before this assignment can be accepted.")]
    [SerializeField] CareerPathDefinition requiredCareer;
    [Tooltip("Minimum career rank index required for Required Career.")]
    [Min(0)]
    [SerializeField] int requiredCareerRankIndex;
    [Tooltip("Optional faction whose reputation gates this assignment.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum reputation required with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional milestone required before this assignment can be accepted.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional calendar event that must be active before this assignment can be accepted.")]
    [SerializeField] CalendarEventDefinition requiredActiveCalendarEvent;
    [Tooltip("Additional requirements checked before this assignment can be accepted.")]
    [SerializeField] List<ActivityRequirement> acceptanceRequirements = new List<ActivityRequirement>();

    [Header("Linked Jobs")]
    [Tooltip("Optional jobs that are accepted or required by this assignment.")]
    [SerializeField] List<AssignmentJobLink> linkedJobs = new List<AssignmentJobLink>();

    [Header("Completion")]
    [Tooltip("All requirements that must pass before this assignment can be completed.")]
    [SerializeField] List<ActivityRequirement> completionRequirements = new List<ActivityRequirement>();

    [Header("Acceptance Effects")]
    [Tooltip("Titles, badges, permits or ranks granted when this assignment is accepted.")]
    [SerializeField] List<TitleGrant> acceptanceTitleGrants = new List<TitleGrant>();
    [Tooltip("Organization memberships granted when this assignment is accepted.")]
    [SerializeField] List<OrganizationMembershipGrant> acceptanceOrganizationMemberships = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points awarded when this assignment is accepted.")]
    [SerializeField] List<OrganizationPointGrant> acceptanceOrganizationPoints = new List<OrganizationPointGrant>();
    [Tooltip("Career points awarded when this assignment is accepted.")]
    [SerializeField] List<CareerPointGrant> acceptanceCareerPoints = new List<CareerPointGrant>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this assignment is accepted.")]
    [SerializeField] List<LifePathReward> acceptanceLifePathRewards = new List<LifePathReward>();
    [Tooltip("Invitations, qualifier passes or wildcards granted when this assignment is accepted.")]
    [SerializeField] List<CompetitionInvitationDefinition> acceptanceCompetitionInvitations = new List<CompetitionInvitationDefinition>();
    [Tooltip("Sponsors or brand agreements granted when this assignment is accepted.")]
    [SerializeField] List<SponsorDefinition> acceptanceSponsors = new List<SponsorDefinition>();

    [Header("Completion Rewards")]
    [Tooltip("Money awarded when this assignment completes.")]
    [Min(0f)]
    [SerializeField] float moneyReward;
    [Tooltip("Items awarded when this assignment completes.")]
    [SerializeField] List<AssignmentItemReward> itemRewards = new List<AssignmentItemReward>();
    [Tooltip("Trainer XP awarded when this assignment completes.")]
    [Min(0)]
    [SerializeField] int experienceReward;
    [Tooltip("Progression source used for trainer XP.")]
    [SerializeField] PlayerExperienceSource experienceSource = PlayerExperienceSource.Quest;
    [Tooltip("Faction reputation changes awarded on completion.")]
    [SerializeField] List<ReputationChange> reputationRewards = new List<ReputationChange>();
    [Tooltip("Relationship changes awarded on completion.")]
    [SerializeField] List<RelationshipChange> relationshipRewards = new List<RelationshipChange>();
    [Tooltip("Milestones completed when this assignment completes.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, badges, permits or ranks granted when this assignment completes.")]
    [SerializeField] List<TitleGrant> titleRewards = new List<TitleGrant>();
    [Tooltip("Crafting recipes learned when this assignment completes.")]
    [SerializeField] List<RecipeGrant> recipeRewards = new List<RecipeGrant>();
    [Tooltip("Career points awarded when this assignment completes.")]
    [SerializeField] List<CareerPointGrant> careerPointRewards = new List<CareerPointGrant>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this assignment completes.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();
    [Tooltip("Organization memberships granted when this assignment completes.")]
    [SerializeField] List<OrganizationMembershipGrant> organizationMembershipRewards = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points awarded when this assignment completes.")]
    [SerializeField] List<OrganizationPointGrant> organizationPointRewards = new List<OrganizationPointGrant>();
    [Tooltip("Research progress awarded when this assignment completes.")]
    [SerializeField] List<ResearchProgressReward> researchRewards = new List<ResearchProgressReward>();
    [Tooltip("Calendar events completed when this assignment completes.")]
    [SerializeField] List<CalendarEventDefinition> calendarEventsToComplete = new List<CalendarEventDefinition>();
    [Tooltip("Invitations, qualifier passes or wildcards granted when this assignment completes.")]
    [SerializeField] List<CompetitionInvitationDefinition> competitionInvitationRewards = new List<CompetitionInvitationDefinition>();
    [Tooltip("Sponsors or brand agreements granted when this assignment completes.")]
    [SerializeField] List<SponsorDefinition> sponsorRewards = new List<SponsorDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this assignment is unlocked.")]
    [SerializeField] GameEventDefinition unlockedEvent;
    [Tooltip("Optional event published when this assignment is accepted.")]
    [SerializeField] GameEventDefinition acceptedEvent;
    [Tooltip("Optional event published when this assignment completes.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("If enabled, assignment events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, assignment events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public AssignmentCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public int Priority => priority;
    public bool UnlockedByDefault => unlockedByDefault;
    public AssignmentRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public bool BlockDuplicateActiveAssignment => blockDuplicateActiveAssignment;
    public IReadOnlyList<ActivityRequirement> AcceptanceRequirements => acceptanceRequirements;
    public IReadOnlyList<AssignmentJobLink> LinkedJobs => linkedJobs;
    public IReadOnlyList<ActivityRequirement> CompletionRequirements => completionRequirements;
    public IReadOnlyList<LifePathReward> AcceptanceLifePathRewards => acceptanceLifePathRewards;
    public IReadOnlyList<CareerPointGrant> CareerPointRewards => careerPointRewards;
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards;
    public IReadOnlyList<OrganizationMembershipGrant> OrganizationMembershipRewards => organizationMembershipRewards;
    public IReadOnlyList<OrganizationPointGrant> OrganizationPointRewards => organizationPointRewards;
    public IReadOnlyList<ResearchProgressReward> ResearchRewards => researchRewards;
    public IReadOnlyList<CompetitionInvitationDefinition> AcceptanceCompetitionInvitations => acceptanceCompetitionInvitations;
    public IReadOnlyList<CompetitionInvitationDefinition> CompetitionInvitationRewards => competitionInvitationRewards;
    public IReadOnlyList<SponsorDefinition> AcceptanceSponsors => acceptanceSponsors;
    public IReadOnlyList<SponsorDefinition> SponsorRewards => sponsorRewards;

    public bool CanAccept(PlayerController player, PlayerAssignmentLog log, string sourceId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to accept assignments.";
            return false;
        }

        if(!unlockedByDefault && !(log?.HasUnlockedAssignment(this) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not unlocked." : lockedMessage;
            return false;
        }

        if(blockDuplicateActiveAssignment && log != null && log.HasActiveAssignment(this, sourceId)) {
            failureMessage = $"{DisplayName} is already active.";
            return false;
        }

        if(!MeetsAccess(player, out failureMessage)) {
            return false;
        }

        if(log != null && !log.CanAccept(this, sourceId, repeatMode, CooldownHours, out failureMessage)) {
            return false;
        }

        foreach(var requirement in acceptanceRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool CanComplete(PlayerController player, PlayerAssignmentState state, out string failureMessage) {
        foreach(var link in linkedJobs) {
            if(link != null && link.requireCompletedForAssignment && !link.IsCompleted(player)) {
                failureMessage = link.GetProgressText();
                return false;
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

    public void ApplyAcceptanceEffects(PlayerController player, string sourceId) {
        if(player == null) {
            return;
        }

        player.GetComponent<PlayerTitles>()?.ApplyGrants(acceptanceTitleGrants, this);
        player.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(acceptanceCareerPoints, $"assignment-accepted:{Id}");
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(acceptanceLifePathRewards, $"assignment-accepted:{Id}", DisplayName, this);

        var organizationLog = player.GetComponent<PlayerOrganizationLog>();
        organizationLog?.ApplyMembershipGrants(acceptanceOrganizationMemberships, $"assignment-accepted:{Id}");
        organizationLog?.ApplyPointGrants(acceptanceOrganizationPoints, $"assignment-accepted:{Id}");

        foreach(var invitation in acceptanceCompetitionInvitations) {
            invitation?.TryGrant(player, $"assignment-accepted:{Id}", out _, out _);
        }

        foreach(var sponsor in acceptanceSponsors) {
            sponsor?.TryGrant(player, $"assignment-accepted:{Id}", out _, out _);
        }

        var jobLog = player.GetComponent<PlayerJobLog>();
        foreach(var link in linkedJobs) {
            if(link != null && link.acceptJobOnAssignmentAccepted && link.job != null && jobLog != null && !jobLog.HasActiveJob(link.job, link.BoardId)) {
                jobLog.Accept(link.job, link.BoardId, out _);
            }
        }
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
        player.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(careerPointRewards, $"assignment:{Id}");
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewards, $"assignment:{Id}", DisplayName, this);

        var organizationLog = player.GetComponent<PlayerOrganizationLog>();
        organizationLog?.ApplyMembershipGrants(organizationMembershipRewards, $"assignment:{Id}");
        organizationLog?.ApplyPointGrants(organizationPointRewards, $"assignment:{Id}");

        var researchLog = player.GetComponent<PlayerResearchLog>();
        foreach(var reward in researchRewards) {
            reward?.Apply(researchLog);
        }

        var calendarLog = player.GetComponent<PlayerCalendarLog>();
        foreach(var calendarEvent in calendarEventsToComplete) {
            calendarLog?.CompleteEvent(calendarEvent, Id);
        }

        foreach(var invitation in competitionInvitationRewards) {
            invitation?.TryGrant(player, $"assignment:{Id}", out _, out _);
        }

        foreach(var sponsor in sponsorRewards) {
            sponsor?.TryGrant(player, $"assignment:{Id}", out _, out _);
        }
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishUnlocked(PlayerController player, string sourceId = null) {
        PublishAssignmentEvent(unlockedEvent, "unlocked", $"{DisplayName} unlocked.", GameEventImportance.Success, player, sourceId);
    }

    public void PublishAccepted(PlayerController player, string sourceId = null) {
        PublishAssignmentEvent(acceptedEvent, "accepted", $"{DisplayName} accepted.", GameEventImportance.Info, player, sourceId);
    }

    public void PublishCompleted(PlayerController player, string sourceId = null) {
        PublishAssignmentEvent(completedEvent, "completed", $"{DisplayName} completed.", GameEventImportance.Success, player, sourceId);
    }

    bool MeetsAccess(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredOrganization != null && !(player.GetComponent<PlayerOrganizationLog>()?.HasReachedRank(requiredOrganization, requiredOrganizationRankIndex) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more progress with {requiredOrganization.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredCareer != null && !(player.GetComponent<PlayerCareerLog>()?.HasReachedRank(requiredCareer, requiredCareerRankIndex) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more progress in {requiredCareer.DisplayName}." : lockedMessage;
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

        if(requiredActiveCalendarEvent != null && !requiredActiveCalendarEvent.IsActiveNow()) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{requiredActiveCalendarEvent.Title} is not active right now." : lockedMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }

    void PublishAssignmentEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, string sourceId) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"assignment.{phase}.{Id}",
            message,
            GameEventCategory.Assignment,
            importance,
            player != null ? player : this,
            "AssignmentDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("assignmentId", Id),
            GameEventPublishing.Value("assignmentName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId));
    }
}

[Serializable]
public class AssignmentJobLink {
    [Tooltip("Job connected to this assignment.")]
    public JobDefinition job;
    [Tooltip("Optional board id used when accepting or checking this job. Empty accepts any board for completion checks.")]
    public string boardId;
    [Tooltip("If enabled, accepting the assignment also accepts this job.")]
    public bool acceptJobOnAssignmentAccepted = true;
    [Tooltip("If enabled, this job must be completed before the assignment can be turned in.")]
    public bool requireCompletedForAssignment = true;

    public string BoardId => boardId;

    public bool IsCompleted(PlayerController player) {
        return job != null && (player?.GetComponent<PlayerJobLog>()?.GetCompletedCount(job, boardId) ?? 0) > 0;
    }

    public string GetProgressText() {
        return job != null ? $"{job.DisplayName} must be completed." : "A linked job must be completed.";
    }
}

[Serializable]
public class AssignmentItemReward {
    [Tooltip("Item awarded by this assignment.")]
    public ItemBase item;
    [Tooltip("Amount of this item awarded.")]
    [Min(1)]
    public int count = 1;
}

[Serializable]
public class ResearchProgressReward {
    [Tooltip("Research subject that receives progress.")]
    public ResearchSubjectDefinition subject;
    [Tooltip("Research points added when this reward applies.")]
    [Min(1)]
    public int points = 1;

    public void Apply(PlayerResearchLog log) {
        if(log != null && subject != null && points > 0) {
            log.AddProgress(subject, points);
        }
    }
}
