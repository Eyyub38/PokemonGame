using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerBattleRuleLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for battle rule sets unlocked for the player.")]
    [SerializeField] List<string> unlockedRuleSetIds = new List<string>();
    [Tooltip("Runtime/save history for battle challenges the player has started or completed.")]
    [SerializeField] List<PlayerBattleChallengeState> challengeHistory = new List<PlayerBattleChallengeState>();

    public IReadOnlyList<string> UnlockedRuleSetIds => unlockedRuleSetIds;
    public IReadOnlyList<PlayerBattleChallengeState> ChallengeHistory => challengeHistory;
    public event Action<BattleRuleSetDefinition> OnRuleSetUnlocked;
    public event Action<BattleChallengeDefinition, BattleRuleSetDefinition> OnChallengeStarted;
    public event Action<BattleChallengeDefinition, BattleRuleSetDefinition, bool> OnChallengeCompleted;
    public event Action OnBattleRuleLogChanged;

    public bool HasUnlockedRuleSet(BattleRuleSetDefinition ruleSet) {
        return ruleSet != null && HasUnlockedRuleSet(ruleSet.Id);
    }

    public bool HasUnlockedRuleSet(string ruleSetId) {
        return !string.IsNullOrWhiteSpace(ruleSetId) && unlockedRuleSetIds.Contains(ruleSetId);
    }

    public bool UnlockRuleSet(BattleRuleSetDefinition ruleSet, string source = null) {
        if(ruleSet == null || HasUnlockedRuleSet(ruleSet)) {
            return false;
        }

        unlockedRuleSetIds.Add(ruleSet.Id);
        OnRuleSetUnlocked?.Invoke(ruleSet);
        OnBattleRuleLogChanged?.Invoke();
        PublishLogEvent("unlocked", ruleSet.Id, null, false, source, GameEventImportance.Success);
        return true;
    }

    public void RecordChallengeStarted(BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet, string source = null) {
        RecordChallengeStarted(challenge, ruleSet, null, source);
    }

    public void RecordChallengeStarted(BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet, BattleModeDefinition battleMode, string source = null) {
        if(challenge == null) {
            return;
        }

        var state = GetOrCreateState(challenge, ruleSet);
        state.startedCount++;
        state.lastStartedHour = GetCurrentTotalHour();
        state.lastSource = source;
        state.lastBattleModeId = battleMode != null ? battleMode.Id : string.Empty;
        state.lastBattleModeName = battleMode != null ? battleMode.DisplayName : string.Empty;
        OnChallengeStarted?.Invoke(challenge, ruleSet);
        OnBattleRuleLogChanged?.Invoke();
        PublishLogEvent("started", ruleSet != null ? ruleSet.Id : null, challenge.Id, false, source, GameEventImportance.Info);
    }

    public void RecordChallengeCompleted(BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet, bool won, string source = null) {
        RecordChallengeCompleted(challenge, ruleSet, null, won, source);
    }

    public void RecordChallengeCompleted(BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet, BattleModeDefinition battleMode, bool won, string source = null) {
        if(challenge == null) {
            return;
        }

        var state = GetOrCreateState(challenge, ruleSet);
        state.completedCount++;
        if(won) {
            state.winCount++;
        } else {
            state.lossCount++;
        }

        state.lastCompletedHour = GetCurrentTotalHour();
        state.lastWon = won;
        state.lastSource = source;
        state.lastBattleModeId = battleMode != null ? battleMode.Id : string.Empty;
        state.lastBattleModeName = battleMode != null ? battleMode.DisplayName : string.Empty;
        OnChallengeCompleted?.Invoke(challenge, ruleSet, won);
        OnBattleRuleLogChanged?.Invoke();
        PublishLogEvent("completed", ruleSet != null ? ruleSet.Id : null, challenge.Id, won, source, won ? GameEventImportance.Success : GameEventImportance.Info);
    }

    public bool HasStartedChallenge(BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet = null) {
        return GetStartedCount(challenge, ruleSet) > 0;
    }

    public bool HasCompletedChallenge(BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet = null) {
        return GetCompletedCount(challenge, ruleSet) > 0;
    }

    public int GetStartedCount(BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet = null) {
        return GetStates(challenge, ruleSet).Sum(state => Mathf.Max(0, state.startedCount));
    }

    public int GetCompletedCount(BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet = null) {
        return GetStates(challenge, ruleSet).Sum(state => Mathf.Max(0, state.completedCount));
    }

    public int GetWinCount(BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet = null) {
        return GetStates(challenge, ruleSet).Sum(state => Mathf.Max(0, state.winCount));
    }

    public int GetLossCount(BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet = null) {
        return GetStates(challenge, ruleSet).Sum(state => Mathf.Max(0, state.lossCount));
    }

    PlayerBattleChallengeState GetOrCreateState(BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet) {
        string challengeId = challenge.Id;
        string ruleId = ruleSet != null ? ruleSet.Id : string.Empty;
        var state = challengeHistory.FirstOrDefault(s => s != null && s.challengeId == challengeId && s.ruleSetId == ruleId);
        if(state != null) {
            return state;
        }

        state = new PlayerBattleChallengeState {
            challengeId = challengeId,
            challengeName = challenge.DisplayName,
            ruleSetId = ruleId,
            ruleSetName = ruleSet != null ? ruleSet.DisplayName : string.Empty
        };
        challengeHistory.Add(state);
        return state;
    }

    IEnumerable<PlayerBattleChallengeState> GetStates(BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet) {
        if(challenge == null) {
            return Enumerable.Empty<PlayerBattleChallengeState>();
        }

        string challengeId = challenge.Id;
        string ruleId = ruleSet != null ? ruleSet.Id : null;
        return challengeHistory.Where(state => state != null
            && state.challengeId == challengeId
            && (ruleId == null || state.ruleSetId == ruleId));
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishLogEvent(string phase, string ruleSetId, string challengeId, bool won, string source, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            null,
            $"battle-rule-log.{phase}.{challengeId}.{ruleSetId}",
            $"Battle rule log {phase}.",
            GameEventCategory.BattleRule,
            importance,
            this,
            "PlayerBattleRuleLog",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("ruleSetId", ruleSetId),
            GameEventPublishing.Value("challengeId", challengeId),
            GameEventPublishing.Value("won", won),
            GameEventPublishing.Value("source", source));
    }

    public object CaptureState() {
        return new PlayerBattleRuleLogSaveData {
            unlockedRuleSetIds = unlockedRuleSetIds.Distinct().ToList(),
            challengeHistory = challengeHistory.Where(state => state != null).Select(state => state.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerBattleRuleLogSaveData;
        unlockedRuleSetIds = saveData?.unlockedRuleSetIds?.Distinct().ToList() ?? new List<string>();
        challengeHistory = saveData?.challengeHistory?.Where(s => s != null).Select(s => new PlayerBattleChallengeState(s)).ToList() ?? new List<PlayerBattleChallengeState>();
        OnBattleRuleLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerBattleChallengeState {
    [Tooltip("Saved challenge id.")]
    public string challengeId;
    [Tooltip("Saved challenge display name for fallback/debug output.")]
    public string challengeName;
    [Tooltip("Saved battle rule set id used for this challenge.")]
    public string ruleSetId;
    [Tooltip("Saved battle rule set display name for fallback/debug output.")]
    public string ruleSetName;
    [Tooltip("How many times this challenge/rule combination has started.")]
    [Min(0)]
    public int startedCount;
    [Tooltip("How many times this challenge/rule combination has completed.")]
    [Min(0)]
    public int completedCount;
    [Tooltip("How many wins were recorded for this challenge/rule combination.")]
    [Min(0)]
    public int winCount;
    [Tooltip("How many losses were recorded for this challenge/rule combination.")]
    [Min(0)]
    public int lossCount;
    [Tooltip("Last in-game total hour this challenge started.")]
    public int lastStartedHour = -1;
    [Tooltip("Last in-game total hour this challenge completed.")]
    public int lastCompletedHour = -1;
    [Tooltip("Whether the last completed attempt was won.")]
    public bool lastWon;
    [Tooltip("Short source id that last started or completed this challenge.")]
    public string lastSource;
    [Tooltip("Battle mode id used by the latest start/completion record.")]
    public string lastBattleModeId;
    [Tooltip("Battle mode display name used by the latest start/completion record.")]
    public string lastBattleModeName;

    public PlayerBattleChallengeState() {
    }

    public PlayerBattleChallengeState(PlayerBattleChallengeStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        challengeId = saveData.challengeId;
        challengeName = saveData.challengeName;
        ruleSetId = saveData.ruleSetId;
        ruleSetName = saveData.ruleSetName;
        startedCount = Mathf.Max(0, saveData.startedCount);
        completedCount = Mathf.Max(0, saveData.completedCount);
        winCount = Mathf.Max(0, saveData.winCount);
        lossCount = Mathf.Max(0, saveData.lossCount);
        lastStartedHour = saveData.lastStartedHour;
        lastCompletedHour = saveData.lastCompletedHour;
        lastWon = saveData.lastWon;
        lastSource = saveData.lastSource;
        lastBattleModeId = saveData.lastBattleModeId;
        lastBattleModeName = saveData.lastBattleModeName;
    }

    public PlayerBattleChallengeStateSaveData ToSaveData() {
        return new PlayerBattleChallengeStateSaveData {
            challengeId = challengeId,
            challengeName = challengeName,
            ruleSetId = ruleSetId,
            ruleSetName = ruleSetName,
            startedCount = startedCount,
            completedCount = completedCount,
            winCount = winCount,
            lossCount = lossCount,
            lastStartedHour = lastStartedHour,
            lastCompletedHour = lastCompletedHour,
            lastWon = lastWon,
            lastSource = lastSource,
            lastBattleModeId = lastBattleModeId,
            lastBattleModeName = lastBattleModeName
        };
    }
}

[Serializable]
public class PlayerBattleRuleLogSaveData {
    [Tooltip("Saved ids for battle rule sets unlocked by the player.")]
    public List<string> unlockedRuleSetIds;
    [Tooltip("Saved challenge history entries keyed by challenge and rule set.")]
    public List<PlayerBattleChallengeStateSaveData> challengeHistory;
}

[Serializable]
public class PlayerBattleChallengeStateSaveData {
    [Tooltip("Saved challenge id.")]
    public string challengeId;
    [Tooltip("Saved challenge display name for fallback/debug output.")]
    public string challengeName;
    [Tooltip("Saved battle rule set id used for this challenge.")]
    public string ruleSetId;
    [Tooltip("Saved battle rule set display name for fallback/debug output.")]
    public string ruleSetName;
    [Tooltip("How many times this challenge/rule combination has started.")]
    public int startedCount;
    [Tooltip("How many times this challenge/rule combination has completed.")]
    public int completedCount;
    [Tooltip("How many wins were recorded for this challenge/rule combination.")]
    public int winCount;
    [Tooltip("How many losses were recorded for this challenge/rule combination.")]
    public int lossCount;
    [Tooltip("Last in-game total hour this challenge started.")]
    public int lastStartedHour;
    [Tooltip("Last in-game total hour this challenge completed.")]
    public int lastCompletedHour;
    [Tooltip("Whether the last completed attempt was won.")]
    public bool lastWon;
    [Tooltip("Short source id that last started or completed this challenge.")]
    public string lastSource;
    [Tooltip("Battle mode id used by the latest start/completion record.")]
    public string lastBattleModeId;
    [Tooltip("Battle mode display name used by the latest start/completion record.")]
    public string lastBattleModeName;
}
