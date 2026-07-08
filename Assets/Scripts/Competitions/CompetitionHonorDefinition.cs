using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompetitionHonorKind {
    Badge,
    Medal,
    FrontierSymbol,
    EliteFourSeat,
    RegionalChampion,
    WorldChampion,
    HallOfFame,
    Rank,
    Custom
}

[CreateAssetMenu(menuName = "Competitions/Honor Definition")]
public class CompetitionHonorDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this honor. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future honor, league card or Hall of Fame UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this honor.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad honor kind used by filters and access logic.")]
    [SerializeField] CompetitionHonorKind kind = CompetitionHonorKind.Medal;
    [Tooltip("Optional world region this honor belongs to.")]
    [SerializeField] WorldRegionDefinition worldRegion;
    [Tooltip("Optional icon used by future honor UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as kanto, champion, elite-four, frontier, world or hall-of-fame.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Award Rules")]
    [Tooltip("If enabled, this honor can only be recorded once.")]
    [SerializeField] bool unique = true;
    [Tooltip("If enabled, this honor records a party snapshot when awarded.")]
    [SerializeField] bool capturePartySnapshot = true;
    [Tooltip("Optional competition that must be completed before this honor can be awarded.")]
    [SerializeField] CompetitionDefinition requiredCompletedCompetition;
    [Tooltip("Optional ranking track required before this honor can be awarded.")]
    [SerializeField] CompetitionRankingDefinition requiredRanking;
    [Tooltip("Optional ranking tier id required before this honor can be awarded.")]
    [SerializeField] string requiredRankingTierId = string.Empty;
    [Tooltip("Optional title, badge, permit or rank required before this honor can be awarded.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this honor can be awarded.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Minimum best streak required in Required Completed Competition. 0 disables the streak check.")]
    [Min(0)]
    [SerializeField] int requiredBestStreak;
    [Tooltip("How additional requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Additional activity-style requirements checked before this honor can be awarded.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this honor cannot be awarded.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This honor is not available yet.";

    [Header("Rewards")]
    [Tooltip("Titles, medals, permits or ranks granted when this honor is awarded.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Milestones completed when this honor is awarded.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this honor is awarded.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();
    [Tooltip("Competitions unlocked when this honor is awarded.")]
    [SerializeField] List<CompetitionDefinition> competitionsToUnlock = new List<CompetitionDefinition>();
    [Tooltip("Ranking tracks unlocked when this honor is awarded.")]
    [SerializeField] List<CompetitionRankingDefinition> rankingsToUnlock = new List<CompetitionRankingDefinition>();
    [Tooltip("Battle rule sets unlocked when this honor is awarded.")]
    [SerializeField] List<BattleRuleSetDefinition> battleRulesToUnlock = new List<BattleRuleSetDefinition>();
    [Tooltip("Power mechanics unlocked when this honor is awarded.")]
    [SerializeField] List<PowerMechanicDefinition> powerMechanicsToUnlock = new List<PowerMechanicDefinition>();
    [Tooltip("Calendar events unlocked when this honor is awarded.")]
    [SerializeField] List<CalendarEventDefinition> calendarEventsToUnlock = new List<CalendarEventDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this honor is awarded.")]
    [SerializeField] GameEventDefinition awardedEvent;
    [Tooltip("If enabled, generated honor events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, honor events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public CompetitionHonorKind Kind => kind;
    public WorldRegionDefinition WorldRegion => worldRegion;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public bool Unique => unique;
    public bool CapturePartySnapshot => capturePartySnapshot;
    public CompetitionDefinition RequiredCompletedCompetition => requiredCompletedCompetition;
    public CompetitionRankingDefinition RequiredRanking => requiredRanking;
    public string RequiredRankingTierId => requiredRankingTierId;
    public int RequiredBestStreak => Mathf.Max(0, requiredBestStreak);
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants != null ? (IReadOnlyList<TitleGrant>)titleGrants : Array.Empty<TitleGrant>();
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete != null ? (IReadOnlyList<MilestoneDefinition>)milestonesToComplete : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards != null ? (IReadOnlyList<LifePathReward>)lifePathRewards : Array.Empty<LifePathReward>();
    public IReadOnlyList<CompetitionDefinition> CompetitionsToUnlock => competitionsToUnlock != null ? (IReadOnlyList<CompetitionDefinition>)competitionsToUnlock : Array.Empty<CompetitionDefinition>();
    public IReadOnlyList<CompetitionRankingDefinition> RankingsToUnlock => rankingsToUnlock != null ? (IReadOnlyList<CompetitionRankingDefinition>)rankingsToUnlock : Array.Empty<CompetitionRankingDefinition>();
    public IReadOnlyList<BattleRuleSetDefinition> BattleRulesToUnlock => battleRulesToUnlock != null ? (IReadOnlyList<BattleRuleSetDefinition>)battleRulesToUnlock : Array.Empty<BattleRuleSetDefinition>();
    public IReadOnlyList<PowerMechanicDefinition> PowerMechanicsToUnlock => powerMechanicsToUnlock != null ? (IReadOnlyList<PowerMechanicDefinition>)powerMechanicsToUnlock : Array.Empty<PowerMechanicDefinition>();
    public IReadOnlyList<CalendarEventDefinition> CalendarEventsToUnlock => calendarEventsToUnlock != null ? (IReadOnlyList<CalendarEventDefinition>)calendarEventsToUnlock : Array.Empty<CalendarEventDefinition>();

    public bool CanAward(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to award this honor.";
            return false;
        }

        var honorLog = player.GetComponent<PlayerCompetitionHonorLog>();
        if(unique && (honorLog?.HasHonor(this) ?? false)) {
            failureMessage = $"{DisplayName} is already recorded.";
            return false;
        }

        var competitionLog = player.GetComponent<PlayerCompetitionLog>();
        if(requiredCompletedCompetition != null && !(competitionLog?.HasCompleted(requiredCompletedCompetition) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need to complete {requiredCompletedCompetition.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredBestStreak > 0 && requiredCompletedCompetition != null && (competitionLog?.GetBestStreak(requiredCompletedCompetition) ?? 0) < requiredBestStreak) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need a better streak in {requiredCompletedCompetition.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredRanking != null) {
            var rankingLog = player.GetComponent<PlayerCompetitionRankingLog>();
            if(!string.IsNullOrWhiteSpace(requiredRankingTierId)) {
                if(!(rankingLog?.HasReachedTier(requiredRanking, requiredRankingTierId) ?? false)) {
                    failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need a higher rank in {requiredRanking.DisplayName}." : lockedMessage;
                    return false;
                }
            } else if((rankingLog?.GetCurrentPoints(requiredRanking) ?? 0) <= 0) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need progress in {requiredRanking.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredTitle != null && !(player.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredMilestone != null && !(player.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(!RequirementsMet(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryAward(PlayerController player, string sourceId, CompetitionDefinition sourceCompetition, CompetitionRankingDefinition sourceRanking, out string failureMessage) {
        if(!CanAward(player, out failureMessage)) {
            return false;
        }

        var log = player.GetComponent<PlayerCompetitionHonorLog>();
        if(log == null) {
            failureMessage = "PlayerCompetitionHonorLog is missing.";
            return false;
        }

        bool recorded = log.RecordHonor(this, sourceId, sourceCompetition, sourceRanking);
        if(!recorded) {
            failureMessage = $"{DisplayName} could not be recorded.";
            return false;
        }

        ApplyRewards(player, sourceId);
        PublishAwarded(player, sourceId, sourceCompetition, sourceRanking);
        failureMessage = null;
        return true;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    void ApplyRewards(PlayerController player, string sourceId) {
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, this);
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewards, $"honor:{Id}", DisplayName, this);

        var competitionLog = player.GetComponent<PlayerCompetitionLog>();
        foreach(var competition in CompetitionsToUnlock) {
            competitionLog?.Unlock(competition, $"honor:{Id}");
        }

        var rankingLog = player.GetComponent<PlayerCompetitionRankingLog>();
        foreach(var ranking in RankingsToUnlock) {
            rankingLog?.Unlock(ranking, $"honor:{Id}");
        }

        var battleRuleLog = player.GetComponent<PlayerBattleRuleLog>();
        foreach(var ruleSet in BattleRulesToUnlock) {
            battleRuleLog?.UnlockRuleSet(ruleSet, $"honor:{Id}");
        }

        var powerMechanicLog = player.GetComponent<PlayerPowerMechanicLog>();
        foreach(var mechanic in PowerMechanicsToUnlock) {
            powerMechanicLog?.Unlock(mechanic, $"honor:{Id}");
        }

        var calendarLog = player.GetComponent<PlayerCalendarLog>();
        foreach(var calendarEvent in CalendarEventsToUnlock) {
            calendarLog?.UnlockEvent(calendarEvent, string.IsNullOrWhiteSpace(sourceId) ? Id : sourceId);
        }
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

    void PublishAwarded(PlayerController player, string sourceId, CompetitionDefinition sourceCompetition, CompetitionRankingDefinition sourceRanking) {
        GameEventPublishing.PublishOptional(
            awardedEvent,
            $"competition-honor.awarded.{Id}",
            $"{DisplayName} awarded.",
            GameEventCategory.BattleRule,
            GameEventImportance.Success,
            player != null ? player : this,
            "CompetitionHonorDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("honorId", Id),
            GameEventPublishing.Value("honorName", DisplayName),
            GameEventPublishing.Value("kind", kind),
            GameEventPublishing.Value("worldRegion", worldRegion != null ? worldRegion.Id : string.Empty),
            GameEventPublishing.Value("sourceCompetition", sourceCompetition != null ? sourceCompetition.Id : string.Empty),
            GameEventPublishing.Value("sourceRanking", sourceRanking != null ? sourceRanking.Id : string.Empty),
            GameEventPublishing.Value("sourceId", sourceId));
    }
}
