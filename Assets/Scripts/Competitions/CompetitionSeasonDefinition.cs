using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompetitionSeasonKind {
    LeagueSeason,
    ChampionshipQualifier,
    WorldChampionship,
    FrontierRotation,
    FestivalCup,
    Custom
}

[CreateAssetMenu(menuName = "Competitions/Season Definition")]
public class CompetitionSeasonDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this season. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future season, league or calendar UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this season.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad season kind used by filters and future UI.")]
    [SerializeField] CompetitionSeasonKind kind = CompetitionSeasonKind.LeagueSeason;
    [Tooltip("Optional world region this season belongs to.")]
    [SerializeField] WorldRegionDefinition worldRegion;
    [Tooltip("Calendar event that controls when this season is active. Empty means the season can be managed manually.")]
    [SerializeField] CalendarEventDefinition calendarEvent;
    [Tooltip("Optional icon used by future season UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as kanto, qualifier, world, frontier, seasonal or postgame.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Access")]
    [Tooltip("If enabled, this season can start without PlayerCompetitionSeasonLog unlock state.")]
    [SerializeField] bool unlockedByDefault = true;
    [Tooltip("If enabled, starting this season requires it to be unlocked in PlayerCompetitionSeasonLog.")]
    [SerializeField] bool requirePlayerUnlock;
    [Tooltip("If enabled, Calendar Event must be active before this season can start.")]
    [SerializeField] bool requireActiveCalendarEvent = true;
    [Tooltip("If disabled, this season can only complete once.")]
    [SerializeField] bool allowRepeatCompletion = true;
    [Tooltip("Optional title, badge, permit or rank required before this season can start.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this season can start.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional competition that must be completed before this season can start.")]
    [SerializeField] CompetitionDefinition requiredCompletedCompetition;
    [Tooltip("Optional ranking track required before this season can start.")]
    [SerializeField] CompetitionRankingDefinition requiredRanking;
    [Tooltip("Optional ranking tier id required before this season can start.")]
    [SerializeField] string requiredRankingTierId = string.Empty;
    [Tooltip("Optional honor required before this season can start.")]
    [SerializeField] CompetitionHonorDefinition requiredHonor;
    [Tooltip("How additional start requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Additional activity-style requirements checked before this season can start.")]
    [SerializeField] List<ActivityRequirement> startRequirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this season cannot start.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This season is not available yet.";

    [Header("Start Effects")]
    [Tooltip("Competitions unlocked when this season starts.")]
    [SerializeField] List<CompetitionDefinition> competitionsToUnlockOnStart = new List<CompetitionDefinition>();
    [Tooltip("Ranking tracks unlocked when this season starts.")]
    [SerializeField] List<CompetitionRankingDefinition> rankingsToUnlockOnStart = new List<CompetitionRankingDefinition>();
    [Tooltip("Battle rule sets unlocked when this season starts.")]
    [SerializeField] List<BattleRuleSetDefinition> battleRulesToUnlockOnStart = new List<BattleRuleSetDefinition>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this season starts.")]
    [SerializeField] List<LifePathReward> lifePathRewardsOnStart = new List<LifePathReward>();
    [Tooltip("Calendar events unlocked when this season starts.")]
    [SerializeField] List<CalendarEventDefinition> calendarEventsToUnlockOnStart = new List<CalendarEventDefinition>();
    [Tooltip("Competition progress reset when this season starts.")]
    [SerializeField] List<CompetitionDefinition> competitionsToResetOnStart = new List<CompetitionDefinition>();
    [Tooltip("Ranking tracks whose current points reset when this season starts.")]
    [SerializeField] List<CompetitionRankingDefinition> rankingsToResetOnStart = new List<CompetitionRankingDefinition>();
    [Tooltip("If enabled, ranking tier reach history is kept when rankings reset. Disable for seasons whose tier rewards can be earned again.")]
    [SerializeField] bool keepReachedRankingTiersOnReset = true;

    [Header("Completion")]
    [Tooltip("How completion requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode completionRequirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements checked before this season can be completed manually.")]
    [SerializeField] List<ActivityRequirement> completionRequirements = new List<ActivityRequirement>();
    [Tooltip("If enabled, Calendar Event is completed in PlayerCalendarLog when the season completes.")]
    [SerializeField] bool completeCalendarEventOnCompletion = true;

    [Header("Completion Rewards")]
    [Tooltip("Milestones completed when this season completes.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, medals, permits or ranks granted when this season completes.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this season completes.")]
    [SerializeField] List<LifePathReward> lifePathRewardsOnCompletion = new List<LifePathReward>();
    [Tooltip("Honors, medals or Hall of Fame records awarded when this season completes.")]
    [SerializeField] List<CompetitionHonorDefinition> honorsToAward = new List<CompetitionHonorDefinition>();
    [Tooltip("Competitions unlocked when this season completes.")]
    [SerializeField] List<CompetitionDefinition> competitionsToUnlockOnCompletion = new List<CompetitionDefinition>();
    [Tooltip("Ranking tracks unlocked when this season completes.")]
    [SerializeField] List<CompetitionRankingDefinition> rankingsToUnlockOnCompletion = new List<CompetitionRankingDefinition>();
    [Tooltip("Power mechanics unlocked when this season completes.")]
    [SerializeField] List<PowerMechanicDefinition> powerMechanicsToUnlockOnCompletion = new List<PowerMechanicDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this season is unlocked.")]
    [SerializeField] GameEventDefinition unlockedEvent;
    [Tooltip("Optional event published when this season starts.")]
    [SerializeField] GameEventDefinition startedEvent;
    [Tooltip("Optional event published when this season completes.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("If enabled, generated season events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, season events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public CompetitionSeasonKind Kind => kind;
    public WorldRegionDefinition WorldRegion => worldRegion;
    public CalendarEventDefinition CalendarEvent => calendarEvent;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public bool UnlockedByDefault => unlockedByDefault;
    public bool RequirePlayerUnlock => requirePlayerUnlock;
    public bool RequireActiveCalendarEvent => requireActiveCalendarEvent;
    public bool AllowRepeatCompletion => allowRepeatCompletion;
    public IReadOnlyList<ActivityRequirement> StartRequirements => startRequirements != null ? (IReadOnlyList<ActivityRequirement>)startRequirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<ActivityRequirement> CompletionRequirements => completionRequirements != null ? (IReadOnlyList<ActivityRequirement>)completionRequirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<CompetitionDefinition> CompetitionsToUnlockOnStart => competitionsToUnlockOnStart != null ? (IReadOnlyList<CompetitionDefinition>)competitionsToUnlockOnStart : Array.Empty<CompetitionDefinition>();
    public IReadOnlyList<CompetitionRankingDefinition> RankingsToUnlockOnStart => rankingsToUnlockOnStart != null ? (IReadOnlyList<CompetitionRankingDefinition>)rankingsToUnlockOnStart : Array.Empty<CompetitionRankingDefinition>();
    public IReadOnlyList<BattleRuleSetDefinition> BattleRulesToUnlockOnStart => battleRulesToUnlockOnStart != null ? (IReadOnlyList<BattleRuleSetDefinition>)battleRulesToUnlockOnStart : Array.Empty<BattleRuleSetDefinition>();
    public IReadOnlyList<LifePathReward> LifePathRewardsOnStart => lifePathRewardsOnStart != null ? (IReadOnlyList<LifePathReward>)lifePathRewardsOnStart : Array.Empty<LifePathReward>();
    public IReadOnlyList<CalendarEventDefinition> CalendarEventsToUnlockOnStart => calendarEventsToUnlockOnStart != null ? (IReadOnlyList<CalendarEventDefinition>)calendarEventsToUnlockOnStart : Array.Empty<CalendarEventDefinition>();
    public IReadOnlyList<CompetitionDefinition> CompetitionsToResetOnStart => competitionsToResetOnStart != null ? (IReadOnlyList<CompetitionDefinition>)competitionsToResetOnStart : Array.Empty<CompetitionDefinition>();
    public IReadOnlyList<CompetitionRankingDefinition> RankingsToResetOnStart => rankingsToResetOnStart != null ? (IReadOnlyList<CompetitionRankingDefinition>)rankingsToResetOnStart : Array.Empty<CompetitionRankingDefinition>();
    public bool KeepReachedRankingTiersOnReset => keepReachedRankingTiersOnReset;
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete != null ? (IReadOnlyList<MilestoneDefinition>)milestonesToComplete : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants != null ? (IReadOnlyList<TitleGrant>)titleGrants : Array.Empty<TitleGrant>();
    public IReadOnlyList<LifePathReward> LifePathRewardsOnCompletion => lifePathRewardsOnCompletion != null ? (IReadOnlyList<LifePathReward>)lifePathRewardsOnCompletion : Array.Empty<LifePathReward>();
    public IReadOnlyList<CompetitionHonorDefinition> HonorsToAward => honorsToAward != null ? (IReadOnlyList<CompetitionHonorDefinition>)honorsToAward : Array.Empty<CompetitionHonorDefinition>();
    public IReadOnlyList<CompetitionDefinition> CompetitionsToUnlockOnCompletion => competitionsToUnlockOnCompletion != null ? (IReadOnlyList<CompetitionDefinition>)competitionsToUnlockOnCompletion : Array.Empty<CompetitionDefinition>();
    public IReadOnlyList<CompetitionRankingDefinition> RankingsToUnlockOnCompletion => rankingsToUnlockOnCompletion != null ? (IReadOnlyList<CompetitionRankingDefinition>)rankingsToUnlockOnCompletion : Array.Empty<CompetitionRankingDefinition>();
    public IReadOnlyList<PowerMechanicDefinition> PowerMechanicsToUnlockOnCompletion => powerMechanicsToUnlockOnCompletion != null ? (IReadOnlyList<PowerMechanicDefinition>)powerMechanicsToUnlockOnCompletion : Array.Empty<PowerMechanicDefinition>();

    public bool IsActiveNow() {
        return calendarEvent == null || calendarEvent.IsActiveNow();
    }

    public bool CanStart(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to start this season.";
            return false;
        }

        var seasonLog = player.GetComponent<PlayerCompetitionSeasonLog>();
        if(requirePlayerUnlock && !unlockedByDefault && !(seasonLog?.HasUnlocked(this) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not unlocked." : lockedMessage;
            return false;
        }

        if(requireActiveCalendarEvent && calendarEvent != null && !calendarEvent.IsActiveNow()) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{calendarEvent.Title} is not active right now." : lockedMessage;
            return false;
        }

        if(!allowRepeatCompletion && (seasonLog?.HasCompleted(this) ?? false)) {
            failureMessage = $"{DisplayName} is already completed.";
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

        if(requiredHonor != null && !(player.GetComponent<PlayerCompetitionHonorLog>()?.HasHonor(requiredHonor) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredHonor.DisplayName}." : lockedMessage;
            return false;
        }

        return RequirementsMet(player, startRequirements, requirementMatchMode, lockedMessage, out failureMessage);
    }

    public bool CanComplete(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to complete this season.";
            return false;
        }

        if(!RequirementsMet(player, completionRequirements, completionRequirementMatchMode, "This season is not complete yet.", out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryStart(PlayerController player, string sourceId, out string failureMessage) {
        if(!CanStart(player, out failureMessage)) {
            return false;
        }

        var log = player.GetComponent<PlayerCompetitionSeasonLog>();
        if(log == null) {
            failureMessage = "PlayerCompetitionSeasonLog is missing.";
            return false;
        }

        log.RecordStarted(this, sourceId);
        ApplyStartEffects(player, sourceId);
        PublishSeasonEvent(startedEvent, "started", $"{DisplayName} started.", GameEventImportance.Info, player, sourceId);
        failureMessage = null;
        return true;
    }

    public bool TryComplete(PlayerController player, string sourceId, out string failureMessage) {
        if(!CanComplete(player, out failureMessage)) {
            return false;
        }

        var log = player.GetComponent<PlayerCompetitionSeasonLog>();
        if(log == null) {
            failureMessage = "PlayerCompetitionSeasonLog is missing.";
            return false;
        }

        log.RecordCompleted(this, sourceId);
        ApplyCompletionRewards(player, sourceId);
        PublishSeasonEvent(completedEvent, "completed", $"{DisplayName} completed.", GameEventImportance.Success, player, sourceId);
        failureMessage = null;
        return true;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishUnlocked(PlayerController player, string sourceId = null) {
        PublishSeasonEvent(unlockedEvent, "unlocked", $"{DisplayName} unlocked.", GameEventImportance.Success, player, sourceId);
    }

    void ApplyStartEffects(PlayerController player, string sourceId) {
        var competitionLog = player.GetComponent<PlayerCompetitionLog>();
        foreach(var competition in CompetitionsToUnlockOnStart) {
            competitionLog?.Unlock(competition, $"season:{Id}");
        }

        var rankingLog = player.GetComponent<PlayerCompetitionRankingLog>();
        foreach(var ranking in RankingsToUnlockOnStart) {
            rankingLog?.Unlock(ranking, $"season:{Id}");
        }

        var battleRuleLog = player.GetComponent<PlayerBattleRuleLog>();
        foreach(var ruleSet in BattleRulesToUnlockOnStart) {
            battleRuleLog?.UnlockRuleSet(ruleSet, $"season:{Id}");
        }

        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewardsOnStart, $"season-start:{Id}", DisplayName, this);

        var calendarLog = player.GetComponent<PlayerCalendarLog>();
        foreach(var seasonCalendarEvent in CalendarEventsToUnlockOnStart) {
            calendarLog?.UnlockEvent(seasonCalendarEvent, string.IsNullOrWhiteSpace(sourceId) ? Id : sourceId);
        }

        foreach(var competition in CompetitionsToResetOnStart) {
            competitionLog?.ResetProgress(competition, keepBestStreak: true, sourceId: $"season:{Id}");
        }

        foreach(var ranking in RankingsToResetOnStart) {
            rankingLog?.ResetCurrentPoints(ranking, keepReachedRankingTiersOnReset, $"season:{Id}");
        }
    }

    void ApplyCompletionRewards(PlayerController player, string sourceId) {
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, this);
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewardsOnCompletion, $"season-complete:{Id}", DisplayName, this);

        var competitionLog = player.GetComponent<PlayerCompetitionLog>();
        foreach(var competition in CompetitionsToUnlockOnCompletion) {
            competitionLog?.Unlock(competition, $"season-complete:{Id}");
        }

        var rankingLog = player.GetComponent<PlayerCompetitionRankingLog>();
        foreach(var ranking in RankingsToUnlockOnCompletion) {
            rankingLog?.Unlock(ranking, $"season-complete:{Id}");
        }

        var powerMechanicLog = player.GetComponent<PlayerPowerMechanicLog>();
        foreach(var mechanic in PowerMechanicsToUnlockOnCompletion) {
            powerMechanicLog?.Unlock(mechanic, $"season-complete:{Id}");
        }

        foreach(var honor in HonorsToAward) {
            honor?.TryAward(player, $"season-complete:{Id}", null, null, out _);
        }

        if(completeCalendarEventOnCompletion && calendarEvent != null) {
            player.GetComponent<PlayerCalendarLog>()?.CompleteEvent(calendarEvent, Id);
        }
    }

    bool RequirementsMet(PlayerController player, List<ActivityRequirement> requirements, ConsequenceRequirementMatchMode matchMode, string fallbackMessage, out string failureMessage) {
        var activeRequirements = requirements?.Where(requirement => requirement != null).ToList() ?? new List<ActivityRequirement>();
        if(activeRequirements.Count == 0) {
            failureMessage = null;
            return true;
        }

        if(matchMode == ConsequenceRequirementMatchMode.Any) {
            foreach(var requirement in activeRequirements) {
                if(requirement.IsMet(player)) {
                    failureMessage = null;
                    return true;
                }
            }

            failureMessage = activeRequirements.FirstOrDefault()?.FailureMessage ?? fallbackMessage;
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

    void PublishSeasonEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, string sourceId) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"competition-season.{phase}.{Id}",
            message,
            GameEventCategory.BattleRule,
            importance,
            player != null ? player : this,
            "CompetitionSeasonDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("seasonId", Id),
            GameEventPublishing.Value("seasonName", DisplayName),
            GameEventPublishing.Value("kind", kind),
            GameEventPublishing.Value("worldRegion", worldRegion != null ? worldRegion.Id : string.Empty),
            GameEventPublishing.Value("calendarEvent", calendarEvent != null ? calendarEvent.Id : string.Empty),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId));
    }
}
