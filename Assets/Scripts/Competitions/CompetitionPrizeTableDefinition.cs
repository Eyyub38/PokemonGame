using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompetitionPrizeTrigger {
    MatchCompleted,
    MatchWon,
    MatchLost,
    BracketCompleted,
    BracketWon,
    BracketLost
}

public enum CompetitionPrizeRepeatMode {
    Always,
    OnceEver,
    OncePerRoster,
    OncePerCompetition,
    OncePerSeason,
    OncePerBracketRun,
    CooldownHours
}

[CreateAssetMenu(menuName = "Competitions/Prize Table Definition")]
public class CompetitionPrizeTableDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this prize table. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug logs or future reward UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation of what this prize table represents.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as kanto, frontier, championship, weekly, sponsor or elite.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("When")]
    [Tooltip("Bracket/match moments that can award this prize. Empty means every trigger is accepted.")]
    [SerializeField] List<CompetitionPrizeTrigger> triggers = new List<CompetitionPrizeTrigger> { CompetitionPrizeTrigger.BracketWon };
    [Tooltip("Roster that must match for this prize. Empty means any roster.")]
    [SerializeField] CompetitionRosterDefinition requiredRoster;
    [Tooltip("Roster tag that must match for this prize. Empty means no roster tag filter.")]
    [SerializeField] string requiredRosterTag = string.Empty;
    [Tooltip("Competition that must match for this prize. Empty means any competition.")]
    [SerializeField] CompetitionDefinition requiredCompetition;
    [Tooltip("Competition tag that must match for this prize. Empty means no competition tag filter.")]
    [SerializeField] string requiredCompetitionTag = string.Empty;
    [Tooltip("Season that must match for this prize. Empty means any season.")]
    [SerializeField] CompetitionSeasonDefinition requiredSeason;
    [Tooltip("Ranking track that must match for this prize. Empty means any ranking.")]
    [SerializeField] CompetitionRankingDefinition requiredRanking;
    [Tooltip("Battle challenge that must match for match-based prizes. Empty means any challenge.")]
    [SerializeField] BattleChallengeDefinition requiredChallenge;
    [Tooltip("Battle rule set that must match for match-based prizes. Empty means any rule set.")]
    [SerializeField] BattleRuleSetDefinition requiredRuleSet;

    [Header("Requirements")]
    [Tooltip("How extra requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Additional activity-style requirements checked before this prize is awarded.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this prize is blocked and no specific requirement message exists.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This prize is not available yet.";

    [Header("Repeat")]
    [Tooltip("How often this prize table can be awarded.")]
    [SerializeField] CompetitionPrizeRepeatMode repeatMode = CompetitionPrizeRepeatMode.OncePerBracketRun;
    [Tooltip("In-game hours before this prize can be awarded again when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;

    [Header("Basic Rewards")]
    [Tooltip("Money granted when this prize is awarded.")]
    [Min(0)]
    [SerializeField] int moneyReward;
    [Tooltip("Items granted when this prize is awarded.")]
    [SerializeField] List<CompetitionPrizeItemReward> itemRewards = new List<CompetitionPrizeItemReward>();
    [Tooltip("Trainer experience granted when this prize is awarded.")]
    [Min(0)]
    [SerializeField] int trainerExperience;
    [Tooltip("Progression source used by trainer XP multipliers.")]
    [SerializeField] PlayerExperienceSource experienceSource = PlayerExperienceSource.Battle;

    [Header("Social and RPG Rewards")]
    [Tooltip("Faction reputation changes applied when this prize is awarded.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Relationship changes applied when this prize is awarded.")]
    [SerializeField] List<RelationshipChange> relationshipChanges = new List<RelationshipChange>();
    [Tooltip("Career points awarded when this prize is awarded.")]
    [SerializeField] List<CareerPointGrant> careerPointRewards = new List<CareerPointGrant>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this prize is awarded.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();
    [Tooltip("Organization memberships granted when this prize is awarded.")]
    [SerializeField] List<OrganizationMembershipGrant> organizationMembershipRewards = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points awarded when this prize is awarded.")]
    [SerializeField] List<OrganizationPointGrant> organizationPointRewards = new List<OrganizationPointGrant>();

    [Header("Unlocks")]
    [Tooltip("Milestones completed when this prize is awarded.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, medals, permits or ranks granted when this prize is awarded.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Crafting recipes learned when this prize is awarded.")]
    [SerializeField] List<RecipeGrant> recipeRewards = new List<RecipeGrant>();
    [Tooltip("Competitions unlocked when this prize is awarded.")]
    [SerializeField] List<CompetitionDefinition> competitionsToUnlock = new List<CompetitionDefinition>();
    [Tooltip("Ranking tracks unlocked when this prize is awarded.")]
    [SerializeField] List<CompetitionRankingDefinition> rankingsToUnlock = new List<CompetitionRankingDefinition>();
    [Tooltip("Battle rule sets unlocked when this prize is awarded.")]
    [SerializeField] List<BattleRuleSetDefinition> battleRulesToUnlock = new List<BattleRuleSetDefinition>();
    [Tooltip("Power mechanics unlocked when this prize is awarded.")]
    [SerializeField] List<PowerMechanicDefinition> powerMechanicsToUnlock = new List<PowerMechanicDefinition>();
    [Tooltip("Honors, medals or Hall of Fame records awarded when this prize is awarded.")]
    [SerializeField] List<CompetitionHonorDefinition> honorsToAward = new List<CompetitionHonorDefinition>();
    [Tooltip("Calendar events unlocked when this prize is awarded.")]
    [SerializeField] List<CalendarEventDefinition> calendarEventsToUnlock = new List<CalendarEventDefinition>();
    [Tooltip("Invitations, qualifier passes or wildcards granted when this prize is awarded.")]
    [SerializeField] List<CompetitionInvitationDefinition> invitationsToGrant = new List<CompetitionInvitationDefinition>();
    [Tooltip("Sponsors or brand agreements granted when this prize is awarded.")]
    [SerializeField] List<SponsorDefinition> sponsorsToGrant = new List<SponsorDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this prize is awarded.")]
    [SerializeField] GameEventDefinition awardedEvent;
    [Tooltip("If enabled, generated prize events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, prize events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public IReadOnlyList<CompetitionPrizeTrigger> Triggers => triggers != null ? (IReadOnlyList<CompetitionPrizeTrigger>)triggers : Array.Empty<CompetitionPrizeTrigger>();
    public CompetitionRosterDefinition RequiredRoster => requiredRoster;
    public string RequiredRosterTag => requiredRosterTag;
    public CompetitionDefinition RequiredCompetition => requiredCompetition;
    public string RequiredCompetitionTag => requiredCompetitionTag;
    public CompetitionSeasonDefinition RequiredSeason => requiredSeason;
    public CompetitionRankingDefinition RequiredRanking => requiredRanking;
    public BattleChallengeDefinition RequiredChallenge => requiredChallenge;
    public BattleRuleSetDefinition RequiredRuleSet => requiredRuleSet;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public CompetitionPrizeRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MoneyReward => Mathf.Max(0, moneyReward);
    public IReadOnlyList<CompetitionPrizeItemReward> ItemRewards => itemRewards != null ? (IReadOnlyList<CompetitionPrizeItemReward>)itemRewards : Array.Empty<CompetitionPrizeItemReward>();
    public int TrainerExperience => Mathf.Max(0, trainerExperience);
    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges != null ? (IReadOnlyList<ReputationChange>)reputationChanges : Array.Empty<ReputationChange>();
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges != null ? (IReadOnlyList<RelationshipChange>)relationshipChanges : Array.Empty<RelationshipChange>();
    public IReadOnlyList<CareerPointGrant> CareerPointRewards => careerPointRewards != null ? (IReadOnlyList<CareerPointGrant>)careerPointRewards : Array.Empty<CareerPointGrant>();
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards != null ? (IReadOnlyList<LifePathReward>)lifePathRewards : Array.Empty<LifePathReward>();
    public IReadOnlyList<OrganizationMembershipGrant> OrganizationMembershipRewards => organizationMembershipRewards != null ? (IReadOnlyList<OrganizationMembershipGrant>)organizationMembershipRewards : Array.Empty<OrganizationMembershipGrant>();
    public IReadOnlyList<OrganizationPointGrant> OrganizationPointRewards => organizationPointRewards != null ? (IReadOnlyList<OrganizationPointGrant>)organizationPointRewards : Array.Empty<OrganizationPointGrant>();
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete != null ? (IReadOnlyList<MilestoneDefinition>)milestonesToComplete : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants != null ? (IReadOnlyList<TitleGrant>)titleGrants : Array.Empty<TitleGrant>();
    public IReadOnlyList<RecipeGrant> RecipeRewards => recipeRewards != null ? (IReadOnlyList<RecipeGrant>)recipeRewards : Array.Empty<RecipeGrant>();
    public IReadOnlyList<CompetitionDefinition> CompetitionsToUnlock => competitionsToUnlock != null ? (IReadOnlyList<CompetitionDefinition>)competitionsToUnlock : Array.Empty<CompetitionDefinition>();
    public IReadOnlyList<CompetitionRankingDefinition> RankingsToUnlock => rankingsToUnlock != null ? (IReadOnlyList<CompetitionRankingDefinition>)rankingsToUnlock : Array.Empty<CompetitionRankingDefinition>();
    public IReadOnlyList<BattleRuleSetDefinition> BattleRulesToUnlock => battleRulesToUnlock != null ? (IReadOnlyList<BattleRuleSetDefinition>)battleRulesToUnlock : Array.Empty<BattleRuleSetDefinition>();
    public IReadOnlyList<PowerMechanicDefinition> PowerMechanicsToUnlock => powerMechanicsToUnlock != null ? (IReadOnlyList<PowerMechanicDefinition>)powerMechanicsToUnlock : Array.Empty<PowerMechanicDefinition>();
    public IReadOnlyList<CompetitionHonorDefinition> HonorsToAward => honorsToAward != null ? (IReadOnlyList<CompetitionHonorDefinition>)honorsToAward : Array.Empty<CompetitionHonorDefinition>();
    public IReadOnlyList<CalendarEventDefinition> CalendarEventsToUnlock => calendarEventsToUnlock != null ? (IReadOnlyList<CalendarEventDefinition>)calendarEventsToUnlock : Array.Empty<CalendarEventDefinition>();
    public IReadOnlyList<CompetitionInvitationDefinition> InvitationsToGrant => invitationsToGrant != null ? (IReadOnlyList<CompetitionInvitationDefinition>)invitationsToGrant : Array.Empty<CompetitionInvitationDefinition>();
    public IReadOnlyList<SponsorDefinition> SponsorsToGrant => sponsorsToGrant != null ? (IReadOnlyList<SponsorDefinition>)sponsorsToGrant : Array.Empty<SponsorDefinition>();

    public bool CanApply(PlayerController player, CompetitionPrizeContext context, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to award competition prizes.";
            return false;
        }

        if(context == null) {
            failureMessage = "A prize context is required.";
            return false;
        }

        if(!MatchesContext(context, out failureMessage)) {
            return false;
        }

        if(!RequirementsMet(player, out failureMessage)) {
            return false;
        }

        var log = player.GetComponent<PlayerCompetitionPrizeLog>();
        if(log != null && !log.CanAward(this, context, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryApply(PlayerController player, CompetitionPrizeContext context, out string failureMessage) {
        if(!CanApply(player, context, out failureMessage)) {
            return false;
        }

        string sourceId = context.SourceId;
        if(MoneyReward > 0) {
            Wallet.i?.AddMoney(MoneyReward);
        }

        ApplyItemRewards(player);

        if(TrainerExperience > 0) {
            player.GetComponent<PlayerProgression>()?.AddExperience(TrainerExperience, experienceSource);
        }

        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        player.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipChanges);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, this);
        player.GetComponent<PlayerRecipeBook>()?.ApplyGrants(recipeRewards, player);
        player.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(careerPointRewards, $"competition-prize:{Id}");
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewards, $"competition-prize:{Id}", DisplayName, this);

        var organizationLog = player.GetComponent<PlayerOrganizationLog>();
        organizationLog?.ApplyMembershipGrants(organizationMembershipRewards, $"competition-prize:{Id}");
        organizationLog?.ApplyPointGrants(organizationPointRewards, $"competition-prize:{Id}");

        var competitionLog = player.GetComponent<PlayerCompetitionLog>();
        foreach(var competition in competitionsToUnlock) {
            competitionLog?.Unlock(competition, $"competition-prize:{Id}");
        }

        var rankingLog = player.GetComponent<PlayerCompetitionRankingLog>();
        foreach(var ranking in rankingsToUnlock) {
            rankingLog?.Unlock(ranking, $"competition-prize:{Id}");
        }

        var battleRuleLog = player.GetComponent<PlayerBattleRuleLog>();
        foreach(var ruleSet in battleRulesToUnlock) {
            battleRuleLog?.UnlockRuleSet(ruleSet, $"competition-prize:{Id}");
        }

        var powerMechanicLog = player.GetComponent<PlayerPowerMechanicLog>();
        foreach(var mechanic in powerMechanicsToUnlock) {
            powerMechanicLog?.Unlock(mechanic, $"competition-prize:{Id}");
        }

        var calendarLog = player.GetComponent<PlayerCalendarLog>();
        foreach(var calendarEvent in calendarEventsToUnlock) {
            calendarLog?.UnlockEvent(calendarEvent, $"competition-prize:{Id}");
        }

        foreach(var invitation in invitationsToGrant) {
            invitation?.TryGrant(player, $"competition-prize:{Id}", out _, out _);
        }

        foreach(var sponsor in sponsorsToGrant) {
            sponsor?.TryGrant(player, $"competition-prize:{Id}", out _, out _);
        }

        foreach(var honor in honorsToAward) {
            honor?.TryAward(player, $"competition-prize:{Id}", context.Competition, context.Ranking, out _);
        }

        var prizeLog = player.GetComponent<PlayerCompetitionPrizeLog>();
        var awardRecord = prizeLog?.RecordAward(this, context, sourceId);
        PublishAwarded(player, context, awardRecord, sourceId);
        failureMessage = null;
        return true;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    bool MatchesContext(CompetitionPrizeContext context, out string failureMessage) {
        if(Triggers.Count > 0 && !Triggers.Contains(context.Trigger)) {
            failureMessage = "Prize trigger does not match.";
            return false;
        }

        if(requiredRoster != null && context.Roster != requiredRoster) {
            failureMessage = "Prize roster does not match.";
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredRosterTag) && !(context.Roster?.HasTag(requiredRosterTag) ?? false)) {
            failureMessage = "Prize roster tag does not match.";
            return false;
        }

        if(requiredCompetition != null && context.Competition != requiredCompetition) {
            failureMessage = "Prize competition does not match.";
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredCompetitionTag) && !(context.Competition?.HasTag(requiredCompetitionTag) ?? false)) {
            failureMessage = "Prize competition tag does not match.";
            return false;
        }

        if(requiredSeason != null && context.Season != requiredSeason) {
            failureMessage = "Prize season does not match.";
            return false;
        }

        if(requiredRanking != null && context.Ranking != requiredRanking) {
            failureMessage = "Prize ranking does not match.";
            return false;
        }

        if(requiredChallenge != null && context.Challenge != requiredChallenge) {
            failureMessage = "Prize challenge does not match.";
            return false;
        }

        if(requiredRuleSet != null && context.RuleSet != requiredRuleSet) {
            failureMessage = "Prize rule set does not match.";
            return false;
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

    void ApplyItemRewards(PlayerController player) {
        var inventory = player != null ? player.GetComponent<Inventory>() : Inventory.GetInventory();
        if(inventory == null) {
            return;
        }

        foreach(var reward in itemRewards) {
            if(reward == null || reward.Item == null) {
                continue;
            }

            int count = reward.RollCount();
            if(count > 0) {
                inventory.AddItem(reward.Item, count);
            }
        }
    }

    void PublishAwarded(PlayerController player, CompetitionPrizeContext context, PlayerCompetitionPrizeAwardRecord awardRecord, string sourceId) {
        GameEventPublishing.PublishOptional(
            awardedEvent,
            $"competition-prize.awarded.{Id}.{awardRecord?.contextKey}",
            $"{DisplayName} awarded.",
            GameEventCategory.BattleRule,
            GameEventImportance.Success,
            player != null ? player : this,
            "CompetitionPrizeTableDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("prizeId", Id),
            GameEventPublishing.Value("prizeName", DisplayName),
            GameEventPublishing.Value("trigger", context != null ? context.Trigger.ToString() : string.Empty),
            GameEventPublishing.Value("rosterId", context?.Roster != null ? context.Roster.Id : string.Empty),
            GameEventPublishing.Value("competitionId", context?.Competition != null ? context.Competition.Id : string.Empty),
            GameEventPublishing.Value("matchId", context?.Match != null ? context.Match.matchId : string.Empty),
            GameEventPublishing.Value("sourceId", sourceId));
    }
}

[Serializable]
public class CompetitionPrizeItemReward {
    [Tooltip("Item granted by this prize reward.")]
    [SerializeField] ItemBase item;
    [Tooltip("Minimum item count granted.")]
    [Min(0)]
    [SerializeField] int minCount = 1;
    [Tooltip("Maximum item count granted.")]
    [Min(0)]
    [SerializeField] int maxCount = 1;

    public ItemBase Item => item;
    public int MinCount => Mathf.Max(0, minCount);
    public int MaxCount => Mathf.Max(MinCount, maxCount);

    public int RollCount() {
        return UnityEngine.Random.Range(MinCount, MaxCount + 1);
    }
}

public class CompetitionPrizeContext {
    public CompetitionPrizeTrigger Trigger { get; }
    public CompetitionRosterDefinition Roster { get; }
    public CompetitionDefinition Competition { get; }
    public CompetitionSeasonDefinition Season { get; }
    public CompetitionRankingDefinition Ranking { get; }
    public PlayerCompetitionBracketState BracketState { get; }
    public PlayerCompetitionBracketMatchRecord Match { get; }
    public BattleChallengeDefinition Challenge { get; }
    public BattleRuleSetDefinition RuleSet { get; }
    public bool Won { get; }
    public string SourceId { get; }

    public CompetitionPrizeContext(
        CompetitionPrizeTrigger trigger,
        CompetitionRosterDefinition roster,
        PlayerCompetitionBracketState bracketState,
        PlayerCompetitionBracketMatchRecord match,
        BattleChallengeDefinition challenge,
        BattleRuleSetDefinition ruleSet,
        bool won,
        string sourceId
    ) {
        Trigger = trigger;
        Roster = roster;
        Competition = roster != null ? roster.Competition : null;
        Season = roster != null ? roster.Season : null;
        Ranking = roster != null ? roster.Ranking : null;
        BracketState = bracketState;
        Match = match;
        Challenge = challenge;
        RuleSet = ruleSet;
        Won = won;
        SourceId = sourceId;
    }

    public string BuildContextKey(CompetitionPrizeRepeatMode repeatMode) {
        string rosterId = Roster != null ? Roster.Id : BracketState != null ? BracketState.rosterId : string.Empty;
        string competitionId = Competition != null ? Competition.Id : BracketState != null ? BracketState.competitionId : string.Empty;
        string seasonId = Season != null ? Season.Id : BracketState != null ? BracketState.seasonId : string.Empty;
        string matchId = Match != null ? Match.matchId : string.Empty;
        string bracketKey = BracketState != null ? $"{BracketState.rosterId}:{BracketState.seed}:{BracketState.generatedTotalHour}" : rosterId;

        return repeatMode switch {
            CompetitionPrizeRepeatMode.OnceEver => "ever",
            CompetitionPrizeRepeatMode.OncePerRoster => $"roster:{rosterId}",
            CompetitionPrizeRepeatMode.OncePerCompetition => $"competition:{competitionId}",
            CompetitionPrizeRepeatMode.OncePerSeason => $"season:{seasonId}",
            CompetitionPrizeRepeatMode.OncePerBracketRun => $"bracket:{bracketKey}:{Trigger}:{matchId}",
            CompetitionPrizeRepeatMode.CooldownHours => $"cooldown:{rosterId}:{competitionId}",
            _ => $"{Trigger}:{rosterId}:{competitionId}:{matchId}:{Guid.NewGuid():N}"
        };
    }
}
