using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompetitionRankingScope {
    Local,
    Regional,
    World,
    Special
}

public enum CompetitionRankingEventType {
    ChallengeResult,
    StageCompleted,
    CompetitionCompleted
}

public enum CompetitionRankingResultFilter {
    AnyResult,
    WinOnly,
    LossOnly
}

[CreateAssetMenu(menuName = "Competitions/Ranking Definition")]
public class CompetitionRankingDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this ranking track. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future ranking, league or PokeNav UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this ranking track.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Scope used by future UI filters and world championship qualification logic.")]
    [SerializeField] CompetitionRankingScope scope = CompetitionRankingScope.Regional;
    [Tooltip("Optional world region this ranking belongs to.")]
    [SerializeField] WorldRegionDefinition worldRegion;
    [Tooltip("Optional calendar event used as the active season for this ranking.")]
    [SerializeField] CalendarEventDefinition seasonCalendarEvent;
    [Tooltip("Optional icon used by future ranking UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as kanto, frontier, championship, qualifier or seasonal.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Access")]
    [Tooltip("If enabled, the player can gain points on this track without an explicit unlock record.")]
    [SerializeField] bool unlockedByDefault = true;
    [Tooltip("If enabled, scoring requires this ranking to be unlocked in PlayerCompetitionRankingLog.")]
    [SerializeField] bool requirePlayerUnlock;
    [Tooltip("Optional title, badge, permit or rank required before this ranking can gain points.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this ranking can gain points.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional competition that must be completed before this ranking can gain points.")]
    [SerializeField] CompetitionDefinition requiredCompletedCompetition;
    [Tooltip("How additional requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Additional activity-style requirements checked before scoring.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this ranking cannot score.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This ranking is not available yet.";

    [Header("Scoring")]
    [Tooltip("If disabled, point totals are clamped to 0 after every score event.")]
    [SerializeField] bool allowNegativePoints;
    [Tooltip("If enabled, points only apply while Season Calendar Event is active.")]
    [SerializeField] bool requireActiveSeason;
    [Tooltip("Editable rules that convert competition events into rank points.")]
    [SerializeField] List<CompetitionRankingPointRule> pointRules = new List<CompetitionRankingPointRule>();

    [Header("Rank Tiers")]
    [Tooltip("Editable point tiers. The highest tier whose minimum points is reached becomes current rank.")]
    [SerializeField] List<CompetitionRankTier> rankTiers = new List<CompetitionRankTier>();
    [Tooltip("If enabled, rank tier rewards are granted only the first time each tier is reached.")]
    [SerializeField] bool grantTierRewardsOnce = true;

    [Header("Events")]
    [Tooltip("Optional event published when this ranking is unlocked.")]
    [SerializeField] GameEventDefinition unlockedEvent;
    [Tooltip("Optional event published when points are gained or lost.")]
    [SerializeField] GameEventDefinition pointsChangedEvent;
    [Tooltip("Optional event published when a new rank tier is reached.")]
    [SerializeField] GameEventDefinition rankReachedEvent;
    [Tooltip("If enabled, generated ranking events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, ranking events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public CompetitionRankingScope Scope => scope;
    public WorldRegionDefinition WorldRegion => worldRegion;
    public CalendarEventDefinition SeasonCalendarEvent => seasonCalendarEvent;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public bool UnlockedByDefault => unlockedByDefault;
    public bool RequirePlayerUnlock => requirePlayerUnlock;
    public bool AllowNegativePoints => allowNegativePoints;
    public bool RequireActiveSeason => requireActiveSeason;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<CompetitionRankingPointRule> PointRules => pointRules != null ? (IReadOnlyList<CompetitionRankingPointRule>)pointRules : Array.Empty<CompetitionRankingPointRule>();
    public IReadOnlyList<CompetitionRankTier> RankTiers => rankTiers != null ? (IReadOnlyList<CompetitionRankTier>)rankTiers : Array.Empty<CompetitionRankTier>();
    public bool GrantTierRewardsOnce => grantTierRewardsOnce;

    public bool CanScore(PlayerController player, PlayerCompetitionRankingLog log, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to score ranking points.";
            return false;
        }

        if(requirePlayerUnlock && !unlockedByDefault && !(log?.HasUnlocked(this) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not unlocked." : lockedMessage;
            return false;
        }

        if(requireActiveSeason && seasonCalendarEvent != null && !seasonCalendarEvent.IsActiveNow()) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{seasonCalendarEvent.Title} is not active right now." : lockedMessage;
            return false;
        }

        if(requiredTitle != null && !(player.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredMilestone != null && !(player.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredCompletedCompetition != null && !(player.GetComponent<PlayerCompetitionLog>()?.HasCompleted(requiredCompletedCompetition) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need to complete {requiredCompletedCompetition.DisplayName} first." : lockedMessage;
            return false;
        }

        if(!RequirementsMet(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryCalculatePoints(
        CompetitionRankingEventType eventType,
        CompetitionDefinition competition,
        CompetitionStage stage,
        BattleChallengeDefinition challenge,
        BattleRuleSetDefinition ruleSet,
        bool won,
        out int points,
        out CompetitionRankingPointRule matchedRule
    ) {
        points = 0;
        matchedRule = null;

        foreach(var rule in PointRules) {
            if(rule == null || !rule.Matches(eventType, competition, stage, challenge, ruleSet, won)) {
                continue;
            }

            points += rule.CalculatePoints(won);
            matchedRule = rule;
        }

        return matchedRule != null && points != 0;
    }

    public CompetitionRankTier GetTierForPoints(int points) {
        return RankTiers
            .Where(tier => tier != null && points >= tier.MinimumPoints)
            .OrderByDescending(tier => tier.MinimumPoints)
            .FirstOrDefault();
    }

    public void ApplyTierRewards(PlayerController player, CompetitionRankTier tier) {
        if(player == null || tier == null) {
            return;
        }

        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(tier.MilestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(tier.TitleGrants, this);
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(tier.LifePathRewards, $"ranking:{Id}:{tier.TierId}", tier.DisplayName, this);

        var competitionLog = player.GetComponent<PlayerCompetitionLog>();
        foreach(var competition in tier.CompetitionsToUnlock) {
            competitionLog?.Unlock(competition, $"ranking:{Id}:{tier.TierId}");
        }

        var battleRuleLog = player.GetComponent<PlayerBattleRuleLog>();
        foreach(var ruleSet in tier.BattleRulesToUnlock) {
            battleRuleLog?.UnlockRuleSet(ruleSet, $"ranking:{Id}:{tier.TierId}");
        }

        var powerMechanicLog = player.GetComponent<PlayerPowerMechanicLog>();
        foreach(var mechanic in tier.PowerMechanicsToUnlock) {
            powerMechanicLog?.Unlock(mechanic, $"ranking:{Id}:{tier.TierId}");
        }

        foreach(var honor in tier.HonorsToAward) {
            honor?.TryAward(player, $"ranking:{Id}:{tier.TierId}", null, this, out _);
        }
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishUnlocked(PlayerController player, string sourceId = null) {
        PublishRankingEvent(unlockedEvent, "unlocked", $"{DisplayName} unlocked.", GameEventImportance.Success, player, null, 0, sourceId);
    }

    public void PublishPointsChanged(PlayerController player, int delta, int totalPoints, string sourceId = null) {
        string sign = delta >= 0 ? "+" : string.Empty;
        PublishRankingEvent(pointsChangedEvent, "points", $"{DisplayName}: {sign}{delta} point(s).", GameEventImportance.Info, player, null, totalPoints, sourceId);
    }

    public void PublishRankReached(PlayerController player, CompetitionRankTier tier, int totalPoints, string sourceId = null) {
        PublishRankingEvent(rankReachedEvent, "rank-reached", $"{tier.DisplayName} reached.", GameEventImportance.Success, player, tier, totalPoints, sourceId);
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

    void PublishRankingEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, CompetitionRankTier tier, int totalPoints, string sourceId) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"competition-ranking.{phase}.{Id}.{tier?.TierId}",
            message,
            GameEventCategory.BattleRule,
            importance,
            player != null ? player : this,
            "CompetitionRankingDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("rankingId", Id),
            GameEventPublishing.Value("rankingName", DisplayName),
            GameEventPublishing.Value("scope", scope),
            GameEventPublishing.Value("worldRegion", worldRegion != null ? worldRegion.Id : string.Empty),
            GameEventPublishing.Value("tierId", tier != null ? tier.TierId : string.Empty),
            GameEventPublishing.Value("tierName", tier != null ? tier.DisplayName : string.Empty),
            GameEventPublishing.Value("totalPoints", totalPoints),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId));
    }
}

[Serializable]
public class CompetitionRankingPointRule {
    [Tooltip("If disabled, this scoring rule is ignored.")]
    [SerializeField] bool enabled = true;
    [Tooltip("Competition event type this rule scores.")]
    [SerializeField] CompetitionRankingEventType eventType = CompetitionRankingEventType.ChallengeResult;
    [Tooltip("Result filter for challenge result events.")]
    [SerializeField] CompetitionRankingResultFilter resultFilter = CompetitionRankingResultFilter.AnyResult;
    [Tooltip("Optional competition that must match. Empty means any competition.")]
    [SerializeField] CompetitionDefinition competition;
    [Tooltip("Optional competition tag that must match. Empty means no tag filter.")]
    [SerializeField] string competitionTag = string.Empty;
    [Tooltip("Optional stage id that must match. Empty means any stage.")]
    [SerializeField] string stageId = string.Empty;
    [Tooltip("Optional challenge that must match. Empty means any challenge.")]
    [SerializeField] BattleChallengeDefinition challenge;
    [Tooltip("Optional battle rule set that must match. Empty means any rule set.")]
    [SerializeField] BattleRuleSetDefinition ruleSet;
    [Tooltip("Flat points added whenever this rule matches.")]
    [SerializeField] int flatPoints;
    [Tooltip("Extra points added when the result is a win.")]
    [SerializeField] int winPoints = 1;
    [Tooltip("Extra points added when the result is a loss.")]
    [SerializeField] int lossPoints;

    public bool Enabled => enabled;
    public CompetitionRankingEventType EventType => eventType;
    public CompetitionDefinition Competition => competition;
    public string CompetitionTag => competitionTag;
    public string StageId => stageId;
    public BattleChallengeDefinition Challenge => challenge;
    public BattleRuleSetDefinition RuleSet => ruleSet;
    public int FlatPoints => flatPoints;
    public int WinPoints => winPoints;
    public int LossPoints => lossPoints;

    public bool Matches(CompetitionRankingEventType candidateEventType, CompetitionDefinition candidateCompetition, CompetitionStage candidateStage, BattleChallengeDefinition candidateChallenge, BattleRuleSetDefinition candidateRuleSet, bool won) {
        if(!enabled || eventType != candidateEventType) {
            return false;
        }

        if(resultFilter == CompetitionRankingResultFilter.WinOnly && !won) {
            return false;
        }

        if(resultFilter == CompetitionRankingResultFilter.LossOnly && won) {
            return false;
        }

        if(competition != null && competition != candidateCompetition) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(competitionTag) && !(candidateCompetition?.HasTag(competitionTag) ?? false)) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(stageId) && !string.Equals(candidateStage?.StageId, stageId, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        if(challenge != null && challenge != candidateChallenge) {
            return false;
        }

        if(ruleSet != null && ruleSet != candidateRuleSet) {
            return false;
        }

        return true;
    }

    public int CalculatePoints(bool won) {
        return flatPoints + (won ? winPoints : lossPoints);
    }
}

[Serializable]
public class CompetitionRankTier {
    [Tooltip("Stable id for this rank tier inside the ranking track. Empty uses Display Name.")]
    [SerializeField] string tierId = string.Empty;
    [Tooltip("Name shown in future ranking UI. Empty uses Tier Id.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Minimum current points required to reach this tier.")]
    [Min(0)]
    [SerializeField] int minimumPoints;
    [Tooltip("Designer note or player-facing explanation for this rank tier.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Titles, medals, permits or ranks granted when this tier is reached.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Milestones completed when this tier is reached.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this tier is reached.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();
    [Tooltip("Competitions unlocked when this tier is reached.")]
    [SerializeField] List<CompetitionDefinition> competitionsToUnlock = new List<CompetitionDefinition>();
    [Tooltip("Battle rule sets unlocked when this tier is reached.")]
    [SerializeField] List<BattleRuleSetDefinition> battleRulesToUnlock = new List<BattleRuleSetDefinition>();
    [Tooltip("Power mechanics unlocked when this tier is reached.")]
    [SerializeField] List<PowerMechanicDefinition> powerMechanicsToUnlock = new List<PowerMechanicDefinition>();
    [Tooltip("Honors, medals or Hall of Fame records awarded when this tier is reached.")]
    [SerializeField] List<CompetitionHonorDefinition> honorsToAward = new List<CompetitionHonorDefinition>();

    public string TierId => string.IsNullOrWhiteSpace(tierId) ? DisplayName : tierId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? (!string.IsNullOrWhiteSpace(tierId) ? tierId : "Rank Tier") : displayName;
    public int MinimumPoints => Mathf.Max(0, minimumPoints);
    public string Description => description;
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants != null ? (IReadOnlyList<TitleGrant>)titleGrants : Array.Empty<TitleGrant>();
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete != null ? (IReadOnlyList<MilestoneDefinition>)milestonesToComplete : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards != null ? (IReadOnlyList<LifePathReward>)lifePathRewards : Array.Empty<LifePathReward>();
    public IReadOnlyList<CompetitionDefinition> CompetitionsToUnlock => competitionsToUnlock != null ? (IReadOnlyList<CompetitionDefinition>)competitionsToUnlock : Array.Empty<CompetitionDefinition>();
    public IReadOnlyList<BattleRuleSetDefinition> BattleRulesToUnlock => battleRulesToUnlock != null ? (IReadOnlyList<BattleRuleSetDefinition>)battleRulesToUnlock : Array.Empty<BattleRuleSetDefinition>();
    public IReadOnlyList<PowerMechanicDefinition> PowerMechanicsToUnlock => powerMechanicsToUnlock != null ? (IReadOnlyList<PowerMechanicDefinition>)powerMechanicsToUnlock : Array.Empty<PowerMechanicDefinition>();
    public IReadOnlyList<CompetitionHonorDefinition> HonorsToAward => honorsToAward != null ? (IReadOnlyList<CompetitionHonorDefinition>)honorsToAward : Array.Empty<CompetitionHonorDefinition>();
}
