using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCompetitionLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for competitions unlocked for this player.")]
    [SerializeField] List<string> unlockedCompetitionIds = new List<string>();
    [Tooltip("Runtime/save progress for entered competitions, stages and challenge results.")]
    [SerializeField] List<PlayerCompetitionState> competitionStates = new List<PlayerCompetitionState>();

    public IReadOnlyList<string> UnlockedCompetitionIds => unlockedCompetitionIds;
    public IReadOnlyList<PlayerCompetitionState> CompetitionStates => competitionStates;
    public event Action<CompetitionDefinition> OnCompetitionUnlocked;
    public event Action<CompetitionDefinition> OnCompetitionStarted;
    public event Action<CompetitionDefinition, CompetitionStage> OnCompetitionStageCompleted;
    public event Action<CompetitionDefinition, bool> OnCompetitionChallengeRecorded;
    public event Action<CompetitionDefinition> OnCompetitionCompleted;
    public event Action OnCompetitionLogChanged;

    public bool HasUnlocked(CompetitionDefinition competition) {
        return competition != null && (competition.UnlockedByDefault || HasUnlocked(competition.Id));
    }

    public bool HasUnlocked(string competitionId) {
        return !string.IsNullOrWhiteSpace(competitionId) && unlockedCompetitionIds.Contains(competitionId);
    }

    public bool Unlock(CompetitionDefinition competition, string sourceId = null) {
        if(competition == null || HasUnlocked(competition.Id)) {
            return false;
        }

        unlockedCompetitionIds.Add(competition.Id);
        OnCompetitionUnlocked?.Invoke(competition);
        OnCompetitionLogChanged?.Invoke();
        competition.PublishUnlocked(GetComponent<PlayerController>(), sourceId);
        PublishLogEvent("unlocked", competition, null, null, true, sourceId, GameEventImportance.Success);
        return true;
    }

    public void RecordStarted(CompetitionDefinition competition, string sourceId = null) {
        if(competition == null) {
            return;
        }

        var state = GetOrCreateState(competition);
        state.enteredCount++;
        state.active = true;
        state.lastEnteredHour = GetCurrentTotalHour();
        state.lastSourceId = sourceId;
        OnCompetitionStarted?.Invoke(competition);
        OnCompetitionLogChanged?.Invoke();
        PublishLogEvent("started", competition, null, null, true, sourceId, GameEventImportance.Info);
    }

    public void RecordChallengeResult(CompetitionDefinition competition, CompetitionStage stage, BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet, bool won, bool resetStreakOnLoss, string sourceId = null) {
        if(competition == null || stage == null || challenge == null) {
            return;
        }

        var state = GetOrCreateState(competition);
        state.active = true;
        state.attemptCount++;
        state.lastAttemptHour = GetCurrentTotalHour();
        state.lastSourceId = sourceId;
        state.lastChallengeId = challenge.Id;
        state.lastRuleSetId = ruleSet != null ? ruleSet.Id : string.Empty;
        state.lastWon = won;

        if(won) {
            state.winCount++;
            state.currentWinStreak++;
            state.bestWinStreak = Mathf.Max(state.bestWinStreak, state.currentWinStreak);
        } else {
            state.lossCount++;
            if(resetStreakOnLoss) {
                state.currentWinStreak = 0;
            }
        }

        var stageState = state.GetOrCreateStageState(stage.StageId, stage.DisplayName);
        stageState.RecordChallenge(challenge, ruleSet, won, GetCurrentTotalHour(), sourceId);

        OnCompetitionChallengeRecorded?.Invoke(competition, won);
        OnCompetitionLogChanged?.Invoke();
        PublishLogEvent("challenge", competition, stage, challenge, won, sourceId, won ? GameEventImportance.Success : GameEventImportance.Info);
    }

    public void MarkStageCompleted(CompetitionDefinition competition, CompetitionStage stage) {
        if(competition == null || stage == null) {
            return;
        }

        var state = GetOrCreateState(competition);
        if(!state.completedStageIds.Contains(stage.StageId)) {
            state.completedStageIds.Add(stage.StageId);
        }

        state.lastCompletedStageId = stage.StageId;
        state.lastStageCompletedHour = GetCurrentTotalHour();
        OnCompetitionStageCompleted?.Invoke(competition, stage);
        OnCompetitionLogChanged?.Invoke();
        PublishLogEvent("stage-completed", competition, stage, null, true, null, GameEventImportance.Success);
    }

    public void MarkCompleted(CompetitionDefinition competition, string sourceId = null) {
        if(competition == null) {
            return;
        }

        var state = GetOrCreateState(competition);
        state.completedCount++;
        state.active = false;
        state.lastCompletedHour = GetCurrentTotalHour();
        state.lastSourceId = sourceId;
        OnCompetitionCompleted?.Invoke(competition);
        OnCompetitionLogChanged?.Invoke();
        PublishLogEvent("completed", competition, null, null, true, sourceId, GameEventImportance.Success);
    }

    public void ResetProgress(CompetitionDefinition competition, bool keepBestStreak = true, string sourceId = null) {
        if(competition == null) {
            return;
        }

        var state = GetOrCreateState(competition);
        int bestStreak = state.bestWinStreak;
        int completedCount = state.completedCount;
        int enteredCount = state.enteredCount;
        int lastCompletedHour = state.lastCompletedHour;

        state.ResetRuntimeProgress();
        if(keepBestStreak) {
            state.bestWinStreak = bestStreak;
        }

        state.completedCount = completedCount;
        state.enteredCount = enteredCount;
        state.lastCompletedHour = lastCompletedHour;
        state.lastResetHour = GetCurrentTotalHour();
        state.lastSourceId = sourceId;
        OnCompetitionLogChanged?.Invoke();
        PublishLogEvent("reset", competition, null, null, true, sourceId, GameEventImportance.Warning);
    }

    public void SetCurrentStageIndex(CompetitionDefinition competition, int stageIndex) {
        if(competition == null) {
            return;
        }

        var state = GetOrCreateState(competition);
        state.currentStageIndex = Mathf.Max(0, stageIndex);
        OnCompetitionLogChanged?.Invoke();
    }

    public bool HasCompleted(CompetitionDefinition competition) {
        return GetCompletedCount(competition) > 0;
    }

    public int GetCompletedCount(CompetitionDefinition competition) {
        return competition != null ? Mathf.Max(0, GetState(competition)?.completedCount ?? 0) : 0;
    }

    public int GetWinCount(CompetitionDefinition competition) {
        return competition != null ? Mathf.Max(0, GetState(competition)?.winCount ?? 0) : 0;
    }

    public int GetLossCount(CompetitionDefinition competition) {
        return competition != null ? Mathf.Max(0, GetState(competition)?.lossCount ?? 0) : 0;
    }

    public int GetBestStreak(CompetitionDefinition competition) {
        return competition != null ? Mathf.Max(0, GetState(competition)?.bestWinStreak ?? 0) : 0;
    }

    public int GetCurrentStageIndex(CompetitionDefinition competition) {
        return competition != null ? Mathf.Max(0, GetState(competition)?.currentStageIndex ?? 0) : 0;
    }

    public bool HasCompletedStage(CompetitionDefinition competition, string stageId) {
        var state = competition != null ? GetState(competition) : null;
        return state != null && state.HasCompletedStage(stageId);
    }

    public int GetChallengeWinCount(CompetitionDefinition competition, BattleChallengeDefinition challenge) {
        if(competition == null || challenge == null) {
            return 0;
        }

        var state = GetState(competition);
        return state != null ? state.GetChallengeWinCount(challenge.Id) : 0;
    }

    public int GetRemainingEntryCooldownHours(CompetitionDefinition competition) {
        if(competition == null || competition.EntryCooldownHours <= 0) {
            return 0;
        }

        var state = GetState(competition);
        if(state == null || state.lastEnteredHour < 0) {
            return 0;
        }

        int elapsed = Mathf.Max(0, GetCurrentTotalHour() - state.lastEnteredHour);
        return Mathf.Max(0, competition.EntryCooldownHours - elapsed);
    }

    public PlayerCompetitionState GetState(CompetitionDefinition competition) {
        return competition != null ? GetState(competition.Id) : null;
    }

    public PlayerCompetitionState GetState(string competitionId) {
        if(string.IsNullOrWhiteSpace(competitionId)) {
            return null;
        }

        return competitionStates.FirstOrDefault(state => state != null && state.competitionId == competitionId);
    }

    public PlayerCompetitionState GetOrCreateState(CompetitionDefinition competition) {
        string competitionId = competition != null ? competition.Id : string.Empty;
        var state = GetState(competitionId);
        if(state != null) {
            return state;
        }

        state = new PlayerCompetitionState {
            competitionId = competitionId,
            competitionName = competition != null ? competition.DisplayName : string.Empty
        };
        competitionStates.Add(state);
        return state;
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishLogEvent(string phase, CompetitionDefinition competition, CompetitionStage stage, BattleChallengeDefinition challenge, bool won, string sourceId, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            null,
            $"competition-log.{phase}.{competition?.Id}.{stage?.StageId}.{challenge?.Id}",
            $"Competition log {phase}.",
            GameEventCategory.BattleRule,
            importance,
            this,
            "PlayerCompetitionLog",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("competitionId", competition != null ? competition.Id : string.Empty),
            GameEventPublishing.Value("competitionName", competition != null ? competition.DisplayName : string.Empty),
            GameEventPublishing.Value("stageId", stage != null ? stage.StageId : string.Empty),
            GameEventPublishing.Value("challengeId", challenge != null ? challenge.Id : string.Empty),
            GameEventPublishing.Value("won", won),
            GameEventPublishing.Value("sourceId", sourceId));
    }

    public object CaptureState() {
        return new PlayerCompetitionLogSaveData {
            unlockedCompetitionIds = unlockedCompetitionIds.Distinct().ToList(),
            competitionStates = competitionStates.Where(state => state != null).Select(state => state.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCompetitionLogSaveData;
        unlockedCompetitionIds = saveData?.unlockedCompetitionIds?.Distinct().ToList() ?? new List<string>();
        competitionStates = saveData?.competitionStates?.Where(entry => entry != null).Select(entry => new PlayerCompetitionState(entry)).ToList() ?? new List<PlayerCompetitionState>();
        OnCompetitionLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerCompetitionState {
    [Tooltip("Saved competition id.")]
    public string competitionId;
    [Tooltip("Saved competition display name for fallback/debug output.")]
    public string competitionName;
    [Tooltip("Current active stage index.")]
    [Min(0)]
    public int currentStageIndex;
    [Tooltip("Whether the player is currently inside or progressing this competition.")]
    public bool active;
    [Tooltip("How many times this competition was entered.")]
    [Min(0)]
    public int enteredCount;
    [Tooltip("How many challenge attempts were recorded for this competition.")]
    [Min(0)]
    public int attemptCount;
    [Tooltip("How many challenge wins were recorded for this competition.")]
    [Min(0)]
    public int winCount;
    [Tooltip("How many challenge losses were recorded for this competition.")]
    [Min(0)]
    public int lossCount;
    [Tooltip("Current consecutive win streak.")]
    [Min(0)]
    public int currentWinStreak;
    [Tooltip("Best consecutive win streak ever recorded.")]
    [Min(0)]
    public int bestWinStreak;
    [Tooltip("How many times this competition was completed.")]
    [Min(0)]
    public int completedCount;
    [Tooltip("Completed stage ids.")]
    public List<string> completedStageIds = new List<string>();
    [Tooltip("Per-stage progress snapshots.")]
    public List<PlayerCompetitionStageState> stages = new List<PlayerCompetitionStageState>();
    [Tooltip("Last entered in-game total hour.")]
    public int lastEnteredHour = -1;
    [Tooltip("Last challenge attempt in-game total hour.")]
    public int lastAttemptHour = -1;
    [Tooltip("Last completed in-game total hour.")]
    public int lastCompletedHour = -1;
    [Tooltip("Last stage completed in-game total hour.")]
    public int lastStageCompletedHour = -1;
    [Tooltip("Last reset in-game total hour.")]
    public int lastResetHour = -1;
    [Tooltip("Last completed stage id.")]
    public string lastCompletedStageId;
    [Tooltip("Last attempted challenge id.")]
    public string lastChallengeId;
    [Tooltip("Last used battle rule set id.")]
    public string lastRuleSetId;
    [Tooltip("Whether the last recorded challenge was won.")]
    public bool lastWon;
    [Tooltip("Short source id that last changed this state.")]
    public string lastSourceId;

    public PlayerCompetitionState() {
    }

    public PlayerCompetitionState(PlayerCompetitionStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        competitionId = saveData.competitionId;
        competitionName = saveData.competitionName;
        currentStageIndex = Mathf.Max(0, saveData.currentStageIndex);
        active = saveData.active;
        enteredCount = Mathf.Max(0, saveData.enteredCount);
        attemptCount = Mathf.Max(0, saveData.attemptCount);
        winCount = Mathf.Max(0, saveData.winCount);
        lossCount = Mathf.Max(0, saveData.lossCount);
        currentWinStreak = Mathf.Max(0, saveData.currentWinStreak);
        bestWinStreak = Mathf.Max(0, saveData.bestWinStreak);
        completedCount = Mathf.Max(0, saveData.completedCount);
        completedStageIds = saveData.completedStageIds?.Distinct().ToList() ?? new List<string>();
        stages = saveData.stages?.Where(stage => stage != null).Select(stage => new PlayerCompetitionStageState(stage)).ToList() ?? new List<PlayerCompetitionStageState>();
        lastEnteredHour = saveData.lastEnteredHour;
        lastAttemptHour = saveData.lastAttemptHour;
        lastCompletedHour = saveData.lastCompletedHour;
        lastStageCompletedHour = saveData.lastStageCompletedHour;
        lastResetHour = saveData.lastResetHour;
        lastCompletedStageId = saveData.lastCompletedStageId;
        lastChallengeId = saveData.lastChallengeId;
        lastRuleSetId = saveData.lastRuleSetId;
        lastWon = saveData.lastWon;
        lastSourceId = saveData.lastSourceId;
    }

    public bool HasCompletedStage(string stageId) {
        return !string.IsNullOrWhiteSpace(stageId) && completedStageIds.Contains(stageId);
    }

    public PlayerCompetitionStageState GetOrCreateStageState(string stageId, string stageName) {
        stageId = string.IsNullOrWhiteSpace(stageId) ? "stage" : stageId;
        var state = stages.FirstOrDefault(entry => entry != null && entry.stageId == stageId);
        if(state != null) {
            return state;
        }

        state = new PlayerCompetitionStageState {
            stageId = stageId,
            stageName = stageName
        };
        stages.Add(state);
        return state;
    }

    public int GetChallengeWinCount(string challengeId) {
        if(string.IsNullOrWhiteSpace(challengeId)) {
            return 0;
        }

        return stages
            .Where(stage => stage != null)
            .Sum(stage => stage.GetChallengeWinCount(challengeId));
    }

    public void ResetRuntimeProgress() {
        currentStageIndex = 0;
        active = false;
        attemptCount = 0;
        winCount = 0;
        lossCount = 0;
        currentWinStreak = 0;
        completedStageIds.Clear();
        stages.Clear();
        lastAttemptHour = -1;
        lastStageCompletedHour = -1;
        lastCompletedStageId = null;
        lastChallengeId = null;
        lastRuleSetId = null;
        lastWon = false;
    }

    public PlayerCompetitionStateSaveData ToSaveData() {
        return new PlayerCompetitionStateSaveData {
            competitionId = competitionId,
            competitionName = competitionName,
            currentStageIndex = currentStageIndex,
            active = active,
            enteredCount = enteredCount,
            attemptCount = attemptCount,
            winCount = winCount,
            lossCount = lossCount,
            currentWinStreak = currentWinStreak,
            bestWinStreak = bestWinStreak,
            completedCount = completedCount,
            completedStageIds = completedStageIds.Distinct().ToList(),
            stages = stages.Where(stage => stage != null).Select(stage => stage.ToSaveData()).ToList(),
            lastEnteredHour = lastEnteredHour,
            lastAttemptHour = lastAttemptHour,
            lastCompletedHour = lastCompletedHour,
            lastStageCompletedHour = lastStageCompletedHour,
            lastResetHour = lastResetHour,
            lastCompletedStageId = lastCompletedStageId,
            lastChallengeId = lastChallengeId,
            lastRuleSetId = lastRuleSetId,
            lastWon = lastWon,
            lastSourceId = lastSourceId
        };
    }
}

[Serializable]
public class PlayerCompetitionStageState {
    [Tooltip("Saved stage id.")]
    public string stageId;
    [Tooltip("Saved stage display name for fallback/debug output.")]
    public string stageName;
    [Tooltip("How many challenge attempts were recorded in this stage.")]
    [Min(0)]
    public int attemptCount;
    [Tooltip("How many challenge wins were recorded in this stage.")]
    [Min(0)]
    public int winCount;
    [Tooltip("How many challenge losses were recorded in this stage.")]
    [Min(0)]
    public int lossCount;
    [Tooltip("Per-challenge progress snapshots for this stage.")]
    public List<PlayerCompetitionChallengeState> challenges = new List<PlayerCompetitionChallengeState>();
    [Tooltip("Last attempted challenge id in this stage.")]
    public string lastChallengeId;
    [Tooltip("Last used battle rule set id in this stage.")]
    public string lastRuleSetId;
    [Tooltip("Last attempt in-game total hour for this stage.")]
    public int lastAttemptHour = -1;
    [Tooltip("Whether the last challenge attempt in this stage was won.")]
    public bool lastWon;
    [Tooltip("Short source id that last changed this stage.")]
    public string lastSourceId;

    public PlayerCompetitionStageState() {
    }

    public PlayerCompetitionStageState(PlayerCompetitionStageStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        stageId = saveData.stageId;
        stageName = saveData.stageName;
        attemptCount = Mathf.Max(0, saveData.attemptCount);
        winCount = Mathf.Max(0, saveData.winCount);
        lossCount = Mathf.Max(0, saveData.lossCount);
        challenges = saveData.challenges?.Where(challenge => challenge != null).Select(challenge => new PlayerCompetitionChallengeState(challenge)).ToList() ?? new List<PlayerCompetitionChallengeState>();
        lastChallengeId = saveData.lastChallengeId;
        lastRuleSetId = saveData.lastRuleSetId;
        lastAttemptHour = saveData.lastAttemptHour;
        lastWon = saveData.lastWon;
        lastSourceId = saveData.lastSourceId;
    }

    public void RecordChallenge(BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet, bool won, int totalHour, string sourceId) {
        if(challenge == null) {
            return;
        }

        attemptCount++;
        if(won) {
            winCount++;
        } else {
            lossCount++;
        }

        lastChallengeId = challenge.Id;
        lastRuleSetId = ruleSet != null ? ruleSet.Id : string.Empty;
        lastAttemptHour = totalHour;
        lastWon = won;
        lastSourceId = sourceId;

        var challengeState = GetOrCreateChallengeState(challenge, ruleSet);
        challengeState.Record(won, totalHour, sourceId);
    }

    public int GetChallengeWinCount(string challengeId) {
        if(string.IsNullOrWhiteSpace(challengeId)) {
            return 0;
        }

        return challenges
            .Where(challenge => challenge != null && challenge.challengeId == challengeId)
            .Sum(challenge => Mathf.Max(0, challenge.winCount));
    }

    PlayerCompetitionChallengeState GetOrCreateChallengeState(BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet) {
        string challengeId = challenge.Id;
        string ruleSetId = ruleSet != null ? ruleSet.Id : string.Empty;
        var state = challenges.FirstOrDefault(entry => entry != null && entry.challengeId == challengeId && entry.ruleSetId == ruleSetId);
        if(state != null) {
            return state;
        }

        state = new PlayerCompetitionChallengeState {
            challengeId = challengeId,
            challengeName = challenge.DisplayName,
            ruleSetId = ruleSetId,
            ruleSetName = ruleSet != null ? ruleSet.DisplayName : string.Empty
        };
        challenges.Add(state);
        return state;
    }

    public PlayerCompetitionStageStateSaveData ToSaveData() {
        return new PlayerCompetitionStageStateSaveData {
            stageId = stageId,
            stageName = stageName,
            attemptCount = attemptCount,
            winCount = winCount,
            lossCount = lossCount,
            challenges = challenges.Where(challenge => challenge != null).Select(challenge => challenge.ToSaveData()).ToList(),
            lastChallengeId = lastChallengeId,
            lastRuleSetId = lastRuleSetId,
            lastAttemptHour = lastAttemptHour,
            lastWon = lastWon,
            lastSourceId = lastSourceId
        };
    }
}

[Serializable]
public class PlayerCompetitionChallengeState {
    [Tooltip("Saved battle challenge id.")]
    public string challengeId;
    [Tooltip("Saved battle challenge display name for fallback/debug output.")]
    public string challengeName;
    [Tooltip("Saved battle rule set id used for this challenge.")]
    public string ruleSetId;
    [Tooltip("Saved battle rule set display name for fallback/debug output.")]
    public string ruleSetName;
    [Tooltip("How many times this challenge/rule combination was attempted.")]
    [Min(0)]
    public int attemptCount;
    [Tooltip("How many wins were recorded for this challenge/rule combination.")]
    [Min(0)]
    public int winCount;
    [Tooltip("How many losses were recorded for this challenge/rule combination.")]
    [Min(0)]
    public int lossCount;
    [Tooltip("Last attempt in-game total hour.")]
    public int lastAttemptHour = -1;
    [Tooltip("Whether the last attempt was won.")]
    public bool lastWon;
    [Tooltip("Short source id that last changed this challenge state.")]
    public string lastSourceId;

    public PlayerCompetitionChallengeState() {
    }

    public PlayerCompetitionChallengeState(PlayerCompetitionChallengeStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        challengeId = saveData.challengeId;
        challengeName = saveData.challengeName;
        ruleSetId = saveData.ruleSetId;
        ruleSetName = saveData.ruleSetName;
        attemptCount = Mathf.Max(0, saveData.attemptCount);
        winCount = Mathf.Max(0, saveData.winCount);
        lossCount = Mathf.Max(0, saveData.lossCount);
        lastAttemptHour = saveData.lastAttemptHour;
        lastWon = saveData.lastWon;
        lastSourceId = saveData.lastSourceId;
    }

    public void Record(bool won, int totalHour, string sourceId) {
        attemptCount++;
        if(won) {
            winCount++;
        } else {
            lossCount++;
        }

        lastAttemptHour = totalHour;
        lastWon = won;
        lastSourceId = sourceId;
    }

    public PlayerCompetitionChallengeStateSaveData ToSaveData() {
        return new PlayerCompetitionChallengeStateSaveData {
            challengeId = challengeId,
            challengeName = challengeName,
            ruleSetId = ruleSetId,
            ruleSetName = ruleSetName,
            attemptCount = attemptCount,
            winCount = winCount,
            lossCount = lossCount,
            lastAttemptHour = lastAttemptHour,
            lastWon = lastWon,
            lastSourceId = lastSourceId
        };
    }
}

[Serializable]
public class PlayerCompetitionLogSaveData {
    public List<string> unlockedCompetitionIds = new List<string>();
    public List<PlayerCompetitionStateSaveData> competitionStates = new List<PlayerCompetitionStateSaveData>();
}

[Serializable]
public class PlayerCompetitionStateSaveData {
    public string competitionId;
    public string competitionName;
    public int currentStageIndex;
    public bool active;
    public int enteredCount;
    public int attemptCount;
    public int winCount;
    public int lossCount;
    public int currentWinStreak;
    public int bestWinStreak;
    public int completedCount;
    public List<string> completedStageIds = new List<string>();
    public List<PlayerCompetitionStageStateSaveData> stages = new List<PlayerCompetitionStageStateSaveData>();
    public int lastEnteredHour;
    public int lastAttemptHour;
    public int lastCompletedHour;
    public int lastStageCompletedHour;
    public int lastResetHour;
    public string lastCompletedStageId;
    public string lastChallengeId;
    public string lastRuleSetId;
    public bool lastWon;
    public string lastSourceId;
}

[Serializable]
public class PlayerCompetitionStageStateSaveData {
    public string stageId;
    public string stageName;
    public int attemptCount;
    public int winCount;
    public int lossCount;
    public List<PlayerCompetitionChallengeStateSaveData> challenges = new List<PlayerCompetitionChallengeStateSaveData>();
    public string lastChallengeId;
    public string lastRuleSetId;
    public int lastAttemptHour;
    public bool lastWon;
    public string lastSourceId;
}

[Serializable]
public class PlayerCompetitionChallengeStateSaveData {
    public string challengeId;
    public string challengeName;
    public string ruleSetId;
    public string ruleSetName;
    public int attemptCount;
    public int winCount;
    public int lossCount;
    public int lastAttemptHour;
    public bool lastWon;
    public string lastSourceId;
}
