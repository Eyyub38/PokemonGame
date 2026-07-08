using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompetitionKind {
    RegionalLeague,
    BattleFrontier,
    WorldChampionship,
    EliteFour,
    GymCircuit,
    ContestCup,
    ClubTournament,
    Custom
}

public enum CompetitionFormat {
    SingleEvent,
    StageSequence,
    Bracket,
    RoundRobin,
    Gauntlet,
    FrontierStreak,
    SeasonalRanking
}

public enum CompetitionScope {
    Local,
    Regional,
    World
}

public enum CompetitionResetPolicy {
    Never,
    OnLoss,
    OnCompletion,
    OnCalendarOccurrence
}

public enum CompetitionStageAdvanceMode {
    CompleteAnyChallenge,
    CompleteRequiredWins,
    CompleteAllChallenges,
    ManualOnly
}

[CreateAssetMenu(menuName = "Competitions/Competition Definition")]
public class CompetitionDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this competition. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future competition, PokeNav or calendar UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this competition.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad kind used by filters and future UI styling.")]
    [SerializeField] CompetitionKind kind = CompetitionKind.RegionalLeague;
    [Tooltip("How this competition is structured at a high level.")]
    [SerializeField] CompetitionFormat format = CompetitionFormat.StageSequence;
    [Tooltip("Whether this is a local, regional or world-scale competition.")]
    [SerializeField] CompetitionScope scope = CompetitionScope.Regional;
    [Tooltip("Optional icon used by future competition UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as kanto, frontier, championship, elite-four, postgame or seasonal.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("World And Schedule")]
    [Tooltip("Optional world region that hosts or owns this competition.")]
    [SerializeField] WorldRegionDefinition worldRegion;
    [Tooltip("Optional calendar event that must be active before this competition can be entered.")]
    [SerializeField] CalendarEventDefinition requiredActiveCalendarEvent;
    [Tooltip("Calendar events unlocked when the competition is revealed or unlocked.")]
    [SerializeField] List<CalendarEventDefinition> calendarEventsToUnlock = new List<CalendarEventDefinition>();
    [Tooltip("Calendar event marked complete when this competition is completed.")]
    [SerializeField] CalendarEventDefinition calendarEventToCompleteOnCompletion;

    [Header("Access")]
    [Tooltip("If enabled, this competition can be entered without PlayerCompetitionLog unlock state.")]
    [SerializeField] bool unlockedByDefault = true;
    [Tooltip("If enabled, player-side entry requires this competition to be unlocked in PlayerCompetitionLog.")]
    [SerializeField] bool requirePlayerUnlock;
    [Tooltip("Optional title, badge, permit or rank required to enter.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required to enter.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates entry.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional competition that must be completed before entry.")]
    [SerializeField] CompetitionDefinition requiredCompletedCompetition;
    [Tooltip("Minimum best streak required in the selected prerequisite competition.")]
    [Min(0)]
    [SerializeField] int requiredBestStreakInPrerequisite;
    [Tooltip("How additional entry requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Additional activity-style requirements checked before entry.")]
    [SerializeField] List<ActivityRequirement> entryRequirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when entry is blocked and no more specific reason exists.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This competition is not available yet.";

    [Header("Progression")]
    [Tooltip("Ordered stages, brackets, facilities or rounds that make up this competition.")]
    [SerializeField] List<CompetitionStage> stages = new List<CompetitionStage>();
    [Tooltip("How progress resets after losses, completion or calendar rotations.")]
    [SerializeField] CompetitionResetPolicy resetPolicy = CompetitionResetPolicy.Never;
    [Tooltip("If enabled, a completed competition can be entered again.")]
    [SerializeField] bool allowReplayAfterCompletion = true;
    [Tooltip("If enabled, completion rewards are only granted the first time this competition is completed.")]
    [SerializeField] bool grantCompletionRewardsOnce = true;
    [Tooltip("In-game hours before the competition can be entered again. 0 disables cooldown.")]
    [Min(0)]
    [SerializeField] int entryCooldownHours;
    [Tooltip("If greater than zero, the player's active win streak must reach this value to complete the competition.")]
    [Min(0)]
    [SerializeField] int requiredWinStreakToComplete;
    [Tooltip("If enabled, losing a challenge resets the active streak to zero.")]
    [SerializeField] bool resetStreakOnLoss = true;
    [Tooltip("If enabled, losing can reset the current stage according to Reset Policy.")]
    [SerializeField] bool resetStageOnLoss;

    [Header("Completion Rewards")]
    [Tooltip("Milestones completed when this competition is completed.")]
    [SerializeField] List<MilestoneDefinition> completionMilestones = new List<MilestoneDefinition>();
    [Tooltip("Titles, medals, permits or ranks granted when this competition is completed.")]
    [SerializeField] List<TitleGrant> completionTitleGrants = new List<TitleGrant>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this competition is completed.")]
    [SerializeField] List<LifePathReward> completionLifePathRewards = new List<LifePathReward>();
    [Tooltip("Battle rule sets unlocked when this competition is completed.")]
    [SerializeField] List<BattleRuleSetDefinition> battleRulesToUnlock = new List<BattleRuleSetDefinition>();
    [Tooltip("Honors, medals or Hall of Fame records awarded when this competition is completed.")]
    [SerializeField] List<CompetitionHonorDefinition> honorsToAward = new List<CompetitionHonorDefinition>();
    [Tooltip("Optional world region discovered when this competition is completed.")]
    [SerializeField] WorldRegionDefinition regionToDiscoverOnCompletion;

    [Header("Events")]
    [Tooltip("Optional event published when this competition is unlocked.")]
    [SerializeField] GameEventDefinition unlockedEvent;
    [Tooltip("Optional event published when this competition starts or is entered.")]
    [SerializeField] GameEventDefinition startedEvent;
    [Tooltip("Optional event published when a stage is completed.")]
    [SerializeField] GameEventDefinition stageCompletedEvent;
    [Tooltip("Optional event published when the whole competition is completed.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("Optional event published when progress is reset.")]
    [SerializeField] GameEventDefinition resetEvent;
    [Tooltip("If enabled, generated competition events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, competition events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public CompetitionKind Kind => kind;
    public CompetitionFormat Format => format;
    public CompetitionScope Scope => scope;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public WorldRegionDefinition WorldRegion => worldRegion;
    public CalendarEventDefinition RequiredActiveCalendarEvent => requiredActiveCalendarEvent;
    public IReadOnlyList<CalendarEventDefinition> CalendarEventsToUnlock => calendarEventsToUnlock != null ? (IReadOnlyList<CalendarEventDefinition>)calendarEventsToUnlock : Array.Empty<CalendarEventDefinition>();
    public bool UnlockedByDefault => unlockedByDefault;
    public bool RequirePlayerUnlock => requirePlayerUnlock;
    public CompetitionDefinition RequiredCompletedCompetition => requiredCompletedCompetition;
    public IReadOnlyList<ActivityRequirement> EntryRequirements => entryRequirements != null ? (IReadOnlyList<ActivityRequirement>)entryRequirements : Array.Empty<ActivityRequirement>();
    public CompetitionResetPolicy ResetPolicy => resetPolicy;
    public bool AllowReplayAfterCompletion => allowReplayAfterCompletion;
    public bool GrantCompletionRewardsOnce => grantCompletionRewardsOnce;
    public int EntryCooldownHours => Mathf.Max(0, entryCooldownHours);
    public int RequiredWinStreakToComplete => Mathf.Max(0, requiredWinStreakToComplete);
    public bool ResetStreakOnLoss => resetStreakOnLoss;
    public bool ResetStageOnLoss => resetStageOnLoss;
    public IReadOnlyList<CompetitionStage> Stages => stages != null ? (IReadOnlyList<CompetitionStage>)stages : Array.Empty<CompetitionStage>();
    public IReadOnlyList<MilestoneDefinition> CompletionMilestones => completionMilestones != null ? (IReadOnlyList<MilestoneDefinition>)completionMilestones : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<TitleGrant> CompletionTitleGrants => completionTitleGrants != null ? (IReadOnlyList<TitleGrant>)completionTitleGrants : Array.Empty<TitleGrant>();
    public IReadOnlyList<LifePathReward> CompletionLifePathRewards => completionLifePathRewards != null ? (IReadOnlyList<LifePathReward>)completionLifePathRewards : Array.Empty<LifePathReward>();
    public IReadOnlyList<BattleRuleSetDefinition> BattleRulesToUnlock => battleRulesToUnlock != null ? (IReadOnlyList<BattleRuleSetDefinition>)battleRulesToUnlock : Array.Empty<BattleRuleSetDefinition>();
    public IReadOnlyList<CompetitionHonorDefinition> HonorsToAward => honorsToAward != null ? (IReadOnlyList<CompetitionHonorDefinition>)honorsToAward : Array.Empty<CompetitionHonorDefinition>();

    public bool CanEnter(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to enter this competition.";
            return false;
        }

        var log = player.GetComponent<PlayerCompetitionLog>();
        var currentState = log != null ? log.GetState(this) : null;
        bool isActiveProgress = currentState != null && currentState.active;
        if(requirePlayerUnlock && !unlockedByDefault && !(log?.HasUnlocked(this) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not unlocked." : lockedMessage;
            return false;
        }

        if(!isActiveProgress && !allowReplayAfterCompletion && (log?.HasCompleted(this) ?? false)) {
            failureMessage = $"{DisplayName} is already completed.";
            return false;
        }

        int remainingCooldown = log != null ? log.GetRemainingEntryCooldownHours(this) : 0;
        if(!isActiveProgress && remainingCooldown > 0) {
            failureMessage = $"{DisplayName} reopens in {remainingCooldown} hour(s).";
            return false;
        }

        if(!isActiveProgress && requiredActiveCalendarEvent != null && !requiredActiveCalendarEvent.IsActiveNow()) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{requiredActiveCalendarEvent.Title} is not active right now." : lockedMessage;
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

        if(requiredFaction != null) {
            int reputation = player.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredCompletedCompetition != null) {
            var prerequisiteLog = player.GetComponent<PlayerCompetitionLog>();
            if(!(prerequisiteLog?.HasCompleted(requiredCompletedCompetition) ?? false)) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need to complete {requiredCompletedCompetition.DisplayName} first." : lockedMessage;
                return false;
            }

            if(requiredBestStreakInPrerequisite > 0 && prerequisiteLog.GetBestStreak(requiredCompletedCompetition) < requiredBestStreakInPrerequisite) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need a better streak in {requiredCompletedCompetition.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(!RequirementsMet(player, out failureMessage)) {
            return false;
        }

        if(Stages.Count == 0) {
            failureMessage = "This competition has no stages.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryBegin(PlayerController player, string sourceId, out string failureMessage) {
        if(!CanEnter(player, out failureMessage)) {
            return false;
        }

        var log = player.GetComponent<PlayerCompetitionLog>();
        log?.RecordStarted(this, sourceId);
        UnlockLinkedCalendarEvents(player, sourceId);
        PublishCompetitionEvent(startedEvent, "started", $"{DisplayName} started.", GameEventImportance.Info, player, sourceId);
        failureMessage = null;
        return true;
    }

    public CompetitionStage GetCurrentStage(PlayerCompetitionLog log) {
        if(Stages.Count == 0) {
            return null;
        }

        var state = log != null ? log.GetState(this) : null;
        int index = state != null ? Mathf.Clamp(state.currentStageIndex, 0, Stages.Count - 1) : 0;
        return Stages[index];
    }

    public List<BattleChallengeDefinition> GetAvailableChallenges(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerCompetitionLog>() : null;
        var stage = GetCurrentStage(log);
        if(stage == null || !stage.CanEnter(player, out _)) {
            return new List<BattleChallengeDefinition>();
        }

        return stage.Challenges
            .Where(challenge => challenge != null && challenge.CanStart(player, null, out _))
            .ToList();
    }

    public bool CanStartChallenge(PlayerController player, BattleChallengeDefinition challenge, BattleRuleSetDefinition selectedRuleSet, out string failureMessage) {
        if(!CanEnter(player, out failureMessage)) {
            return false;
        }

        var log = player.GetComponent<PlayerCompetitionLog>();
        var stage = GetCurrentStage(log);
        if(stage == null) {
            failureMessage = "This competition has no active stage.";
            return false;
        }

        if(!stage.CanEnter(player, out failureMessage)) {
            return false;
        }

        if(!stage.ContainsChallenge(challenge)) {
            failureMessage = $"{challenge?.DisplayName ?? "Challenge"} is not part of the current stage.";
            return false;
        }

        return challenge.CanStart(player, selectedRuleSet, out failureMessage);
    }

    public bool TryRecordChallengeResult(PlayerController player, BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet, bool won, string sourceId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to record competition progress.";
            return false;
        }

        var log = player.GetComponent<PlayerCompetitionLog>();
        if(log == null) {
            failureMessage = "PlayerCompetitionLog is missing.";
            return false;
        }

        var state = log.GetOrCreateState(this);
        var stage = GetStageByIndex(state.currentStageIndex);
        if(stage == null) {
            failureMessage = "This competition has no active stage.";
            return false;
        }

        if(!stage.ContainsChallenge(challenge)) {
            failureMessage = $"{challenge?.DisplayName ?? "Challenge"} is not part of the current stage.";
            return false;
        }

        bool wasCompleted = state.completedCount > 0;
        bool wasStageCompleted = state.HasCompletedStage(stage.StageId);

        log.RecordChallengeResult(this, stage, challenge, ruleSet, won, resetStreakOnLoss, sourceId);
        var rankingLog = player.GetComponent<PlayerCompetitionRankingLog>();
        rankingLog?.RecordCompetitionProgressEvent(CompetitionRankingEventType.ChallengeResult, this, stage, challenge, ruleSet, won, sourceId);

        if(!won && resetStageOnLoss && resetPolicy == CompetitionResetPolicy.OnLoss) {
            log.ResetProgress(this, keepBestStreak: true, sourceId);
            PublishCompetitionEvent(resetEvent, "reset", $"{DisplayName} progress reset.", GameEventImportance.Warning, player, sourceId);
            failureMessage = null;
            return true;
        }

        state = log.GetOrCreateState(this);
        var stageState = state.GetOrCreateStageState(stage.StageId, stage.DisplayName);
        if(!wasStageCompleted && won && stage.IsComplete(stageState)) {
            log.MarkStageCompleted(this, stage);
            stage.ApplyCompletionRewards(player, this);
            PublishStageCompleted(player, stage, sourceId);
            rankingLog?.RecordCompetitionProgressEvent(CompetitionRankingEventType.StageCompleted, this, stage, challenge, ruleSet, won, sourceId);
            AdvanceAfterStageCompletion(log, state);
        }

        state = log.GetOrCreateState(this);
        bool completedNow = IsProgressComplete(state);
        if(completedNow && (!wasCompleted || !grantCompletionRewardsOnce)) {
            log.MarkCompleted(this, sourceId);
            ApplyCompletionRewards(player);
            PublishCompetitionEvent(completedEvent, "completed", $"{DisplayName} completed.", GameEventImportance.Success, player, sourceId);
            rankingLog?.RecordCompetitionProgressEvent(CompetitionRankingEventType.CompetitionCompleted, this, stage, challenge, ruleSet, won, sourceId);

            if(resetPolicy == CompetitionResetPolicy.OnCompletion) {
                log.ResetProgress(this, keepBestStreak: true, sourceId);
            }
        }

        failureMessage = null;
        return true;
    }

    public bool IsProgressComplete(PlayerCompetitionState state) {
        if(state == null) {
            return false;
        }

        if(requiredWinStreakToComplete > 0 && state.currentWinStreak >= requiredWinStreakToComplete) {
            return true;
        }

        if(Stages.Count == 0) {
            return false;
        }

        return Stages.All(stage => stage != null && state.HasCompletedStage(stage.StageId));
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishUnlocked(PlayerController player, string sourceId = null) {
        UnlockLinkedCalendarEvents(player, sourceId);
        PublishCompetitionEvent(unlockedEvent, "unlocked", $"{DisplayName} unlocked.", GameEventImportance.Success, player, sourceId);
    }

    CompetitionStage GetStageByIndex(int index) {
        if(Stages.Count == 0) {
            return null;
        }

        return Stages[Mathf.Clamp(index, 0, Stages.Count - 1)];
    }

    void AdvanceAfterStageCompletion(PlayerCompetitionLog log, PlayerCompetitionState state) {
        if(log == null || state == null || Stages.Count == 0) {
            return;
        }

        if(state.currentStageIndex < Stages.Count - 1) {
            log.SetCurrentStageIndex(this, state.currentStageIndex + 1);
        }
    }

    bool RequirementsMet(PlayerController player, out string failureMessage) {
        var activeRequirements = entryRequirements?.Where(requirement => requirement != null).ToList() ?? new List<ActivityRequirement>();
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

    void ApplyCompletionRewards(PlayerController player) {
        if(player == null) {
            return;
        }

        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(completionMilestones);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(completionTitleGrants, this);
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(completionLifePathRewards, $"competition:{Id}", DisplayName, this);

        var battleRuleLog = player.GetComponent<PlayerBattleRuleLog>();
        foreach(var ruleSet in BattleRulesToUnlock) {
            battleRuleLog?.UnlockRuleSet(ruleSet, $"competition:{Id}");
        }

        foreach(var honor in HonorsToAward) {
            honor?.TryAward(player, $"competition:{Id}", this, null, out _);
        }

        if(calendarEventToCompleteOnCompletion != null) {
            player.GetComponent<PlayerCalendarLog>()?.CompleteEvent(calendarEventToCompleteOnCompletion, Id);
        }

        if(regionToDiscoverOnCompletion != null) {
            regionToDiscoverOnCompletion.ApplyDiscovery(player, $"competition:{Id}");
        }
    }

    void UnlockLinkedCalendarEvents(PlayerController player, string sourceId) {
        if(player == null || calendarEventsToUnlock == null) {
            return;
        }

        var calendarLog = player.GetComponent<PlayerCalendarLog>();
        foreach(var calendarEvent in calendarEventsToUnlock) {
            calendarLog?.UnlockEvent(calendarEvent, string.IsNullOrWhiteSpace(sourceId) ? Id : sourceId);
        }
    }

    void PublishStageCompleted(PlayerController player, CompetitionStage stage, string sourceId) {
        GameEventPublishing.PublishOptional(
            stage.StageCompletedEvent != null ? stage.StageCompletedEvent : stageCompletedEvent,
            $"competition.stage.completed.{Id}.{stage.StageId}",
            $"{stage.DisplayName} completed.",
            GameEventCategory.BattleRule,
            GameEventImportance.Success,
            player != null ? player : this,
            "CompetitionDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("competitionId", Id),
            GameEventPublishing.Value("competitionName", DisplayName),
            GameEventPublishing.Value("stageId", stage.StageId),
            GameEventPublishing.Value("stageName", stage.DisplayName),
            GameEventPublishing.Value("sourceId", sourceId));
    }

    void PublishCompetitionEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, string sourceId) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"competition.{phase}.{Id}",
            message,
            GameEventCategory.BattleRule,
            importance,
            player != null ? player : this,
            "CompetitionDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("competitionId", Id),
            GameEventPublishing.Value("competitionName", DisplayName),
            GameEventPublishing.Value("kind", kind),
            GameEventPublishing.Value("format", format),
            GameEventPublishing.Value("scope", scope),
            GameEventPublishing.Value("worldRegion", worldRegion != null ? worldRegion.Id : string.Empty),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId));
    }
}

[Serializable]
public class CompetitionStage {
    [Tooltip("Stable id for this stage inside the competition. Empty uses the display name or list index fallback.")]
    [SerializeField] string stageId = string.Empty;
    [Tooltip("Name shown in future competition UI. Empty uses Stage Id.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this stage.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("How this stage decides that it is complete.")]
    [SerializeField] CompetitionStageAdvanceMode advanceMode = CompetitionStageAdvanceMode.CompleteRequiredWins;
    [Tooltip("Minimum wins required when Advance Mode is Complete Required Wins.")]
    [Min(1)]
    [SerializeField] int requiredWins = 1;
    [Tooltip("Battle challenges that can count toward this stage.")]
    [SerializeField] List<BattleChallengeDefinition> challenges = new List<BattleChallengeDefinition>();
    [Tooltip("Optional title, badge, permit or rank required to enter this stage.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required to enter this stage.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Milestones completed when this stage is completed.")]
    [SerializeField] List<MilestoneDefinition> completionMilestones = new List<MilestoneDefinition>();
    [Tooltip("Titles, medals, permits or ranks granted when this stage is completed.")]
    [SerializeField] List<TitleGrant> completionTitleGrants = new List<TitleGrant>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this stage is completed.")]
    [SerializeField] List<LifePathReward> completionLifePathRewards = new List<LifePathReward>();
    [Tooltip("Battle rule sets unlocked when this stage is completed.")]
    [SerializeField] List<BattleRuleSetDefinition> battleRulesToUnlock = new List<BattleRuleSetDefinition>();
    [Tooltip("Honors, medals or Hall of Fame records awarded when this stage is completed.")]
    [SerializeField] List<CompetitionHonorDefinition> honorsToAward = new List<CompetitionHonorDefinition>();
    [Tooltip("Optional event published when this stage is completed.")]
    [SerializeField] GameEventDefinition stageCompletedEvent;

    public string StageId => string.IsNullOrWhiteSpace(stageId) ? DisplayName : stageId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? (!string.IsNullOrWhiteSpace(stageId) ? stageId : "Competition Stage") : displayName;
    public string Description => description;
    public CompetitionStageAdvanceMode AdvanceMode => advanceMode;
    public int RequiredWins => Mathf.Max(1, requiredWins);
    public IReadOnlyList<BattleChallengeDefinition> Challenges => challenges != null ? (IReadOnlyList<BattleChallengeDefinition>)challenges : Array.Empty<BattleChallengeDefinition>();
    public IReadOnlyList<MilestoneDefinition> CompletionMilestones => completionMilestones != null ? (IReadOnlyList<MilestoneDefinition>)completionMilestones : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<TitleGrant> CompletionTitleGrants => completionTitleGrants != null ? (IReadOnlyList<TitleGrant>)completionTitleGrants : Array.Empty<TitleGrant>();
    public IReadOnlyList<LifePathReward> CompletionLifePathRewards => completionLifePathRewards != null ? (IReadOnlyList<LifePathReward>)completionLifePathRewards : Array.Empty<LifePathReward>();
    public IReadOnlyList<BattleRuleSetDefinition> BattleRulesToUnlock => battleRulesToUnlock != null ? (IReadOnlyList<BattleRuleSetDefinition>)battleRulesToUnlock : Array.Empty<BattleRuleSetDefinition>();
    public IReadOnlyList<CompetitionHonorDefinition> HonorsToAward => honorsToAward != null ? (IReadOnlyList<CompetitionHonorDefinition>)honorsToAward : Array.Empty<CompetitionHonorDefinition>();
    public GameEventDefinition StageCompletedEvent => stageCompletedEvent;

    public bool CanEnter(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = $"You need {requiredTitle.DisplayName}.";
            return false;
        }

        if(requiredMilestone != null && !(player?.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = $"You need {requiredMilestone.DisplayName} first.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool ContainsChallenge(BattleChallengeDefinition challenge) {
        return challenge != null && Challenges.Contains(challenge);
    }

    public bool IsComplete(PlayerCompetitionStageState state) {
        if(state == null) {
            return false;
        }

        return advanceMode switch {
            CompetitionStageAdvanceMode.CompleteAnyChallenge => state.winCount > 0,
            CompetitionStageAdvanceMode.CompleteAllChallenges => Challenges.Where(challenge => challenge != null).All(challenge => state.GetChallengeWinCount(challenge.Id) > 0),
            CompetitionStageAdvanceMode.ManualOnly => false,
            _ => state.winCount >= RequiredWins
        };
    }

    public void ApplyCompletionRewards(PlayerController player, CompetitionDefinition competition) {
        if(player == null) {
            return;
        }

        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(completionMilestones);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(completionTitleGrants, competition);
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(completionLifePathRewards, $"competition-stage:{competition?.Id}:{StageId}", $"{competition?.DisplayName} {DisplayName}", competition);

        var battleRuleLog = player.GetComponent<PlayerBattleRuleLog>();
        foreach(var ruleSet in BattleRulesToUnlock) {
            battleRuleLog?.UnlockRuleSet(ruleSet, $"competition-stage:{competition?.Id}:{StageId}");
        }

        foreach(var honor in HonorsToAward) {
            honor?.TryAward(player, $"competition-stage:{competition?.Id}:{StageId}", competition, null, out _);
        }
    }
}
