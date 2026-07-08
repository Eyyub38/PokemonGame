using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerContestLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for contests unlocked for the player.")]
    [SerializeField] List<string> unlockedContestIds = new List<string>();
    [Tooltip("Runtime/save history for contest attempts and best results.")]
    [SerializeField] List<PlayerContestState> contestHistory = new List<PlayerContestState>();

    public IReadOnlyList<string> UnlockedContestIds => unlockedContestIds;
    public IReadOnlyList<PlayerContestState> ContestHistory => contestHistory;
    public event Action<ContestDefinition> OnContestUnlocked;
    public event Action<ContestDefinition, ContestRunResult> OnContestAttemptRecorded;
    public event Action OnContestLogChanged;

    public bool HasUnlockedContest(ContestDefinition contest) {
        return contest != null && (contest.UnlockedByDefault || HasUnlockedContest(contest.Id));
    }

    public bool HasUnlockedContest(string contestId) {
        return !string.IsNullOrWhiteSpace(contestId) && unlockedContestIds.Contains(contestId);
    }

    public bool UnlockContest(ContestDefinition contest, string source = null) {
        if(contest == null || HasUnlockedContest(contest.Id)) {
            return false;
        }

        unlockedContestIds.Add(contest.Id);
        OnContestUnlocked?.Invoke(contest);
        OnContestLogChanged?.Invoke();
        PublishContestLogEvent("unlocked", contest.Id, contest.DisplayName, 0, string.Empty, false, source, GameEventImportance.Success);
        return true;
    }

    public void RecordAttempt(ContestDefinition contest, ContestRunResult result, string source = null) {
        if(contest == null || result == null) {
            return;
        }

        var state = GetOrCreateState(contest);
        state.attemptCount++;
        if(result.won) {
            state.winCount++;
        }

        if(result.score > state.bestScore) {
            state.bestScore = result.score;
            state.bestRankIndex = result.rankIndex;
            state.bestRankName = result.rankName;
            state.bestPokemonName = result.pokemonName;
        } else if(result.rankIndex > state.bestRankIndex) {
            state.bestRankIndex = result.rankIndex;
            state.bestRankName = result.rankName;
        }

        state.lastScore = result.score;
        state.lastRankIndex = result.rankIndex;
        state.lastRankName = result.rankName;
        state.lastWon = result.won;
        state.lastPokemonName = result.pokemonName;
        state.lastAttemptHour = GetCurrentTotalHour();
        state.lastSource = source;

        OnContestAttemptRecorded?.Invoke(contest, result);
        OnContestLogChanged?.Invoke();
        PublishContestLogEvent("attempted", contest.Id, contest.DisplayName, result.score, result.rankName, result.won, source, result.won ? GameEventImportance.Success : GameEventImportance.Info);
    }

    public int GetAttemptCount(ContestDefinition contest) {
        return GetState(contest)?.attemptCount ?? 0;
    }

    public int GetWinCount(ContestDefinition contest) {
        return GetState(contest)?.winCount ?? 0;
    }

    public bool HasWonContest(ContestDefinition contest) {
        return GetWinCount(contest) > 0;
    }

    public int GetBestScore(ContestDefinition contest) {
        return GetState(contest)?.bestScore ?? 0;
    }

    public int GetBestRankIndex(ContestDefinition contest) {
        return GetState(contest)?.bestRankIndex ?? -1;
    }

    public int GetWinCountWithTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        int count = 0;
        foreach(var state in contestHistory) {
            var contest = ResolveContest(state?.contestId);
            if(contest != null && contest.HasTag(tag)) {
                count += Mathf.Max(0, state.winCount);
            }
        }

        return count;
    }

    PlayerContestState GetOrCreateState(ContestDefinition contest) {
        var state = GetState(contest);
        if(state != null) {
            return state;
        }

        state = new PlayerContestState {
            contestId = contest.Id,
            contestName = contest.DisplayName,
            category = contest.Category,
            difficulty = contest.Difficulty,
            bestRankIndex = -1,
            lastRankIndex = -1
        };
        contestHistory.Add(state);
        return state;
    }

    PlayerContestState GetState(ContestDefinition contest) {
        return contest != null ? contestHistory.FirstOrDefault(state => state != null && state.contestId == contest.Id) : null;
    }

    ContestDefinition ResolveContest(string contestId) {
        if(string.IsNullOrWhiteSpace(contestId)) {
            return null;
        }

        return Resources.LoadAll<ContestDefinition>("").FirstOrDefault(contest => contest != null && contest.Id == contestId);
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishContestLogEvent(string phase, string contestId, string contestName, int score, string rankName, bool won, string source, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            null,
            $"contest-log.{phase}.{contestId}",
            $"{contestName} {phase}.",
            GameEventCategory.Contest,
            importance,
            this,
            "PlayerContestLog",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("contestId", contestId),
            GameEventPublishing.Value("contestName", contestName),
            GameEventPublishing.Value("score", score),
            GameEventPublishing.Value("rank", rankName),
            GameEventPublishing.Value("won", won),
            GameEventPublishing.Value("source", source));
    }

    public object CaptureState() {
        return new PlayerContestLogSaveData {
            unlockedContestIds = unlockedContestIds.Distinct().ToList(),
            contestHistory = contestHistory.Where(state => state != null).Select(state => state.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerContestLogSaveData;
        unlockedContestIds = saveData?.unlockedContestIds?.Distinct().ToList() ?? new List<string>();
        contestHistory = saveData?.contestHistory?.Where(s => s != null).Select(s => new PlayerContestState(s)).ToList() ?? new List<PlayerContestState>();
        OnContestLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerContestState {
    [Tooltip("Saved contest id.")]
    public string contestId;
    [Tooltip("Saved contest display name for fallback/debug output.")]
    public string contestName;
    [Tooltip("Saved contest category.")]
    public ContestCategory category;
    [Tooltip("Saved contest difficulty.")]
    public ContestDifficulty difficulty;
    [Tooltip("How many times this contest has been attempted.")]
    [Min(0)]
    public int attemptCount;
    [Tooltip("How many attempts counted as wins.")]
    [Min(0)]
    public int winCount;
    [Tooltip("Best score achieved in this contest.")]
    [Min(0)]
    public int bestScore;
    [Tooltip("Best rank index achieved in this contest.")]
    public int bestRankIndex = -1;
    [Tooltip("Best rank name achieved in this contest.")]
    public string bestRankName;
    [Tooltip("Pokemon name used for the best score.")]
    public string bestPokemonName;
    [Tooltip("Score from the last attempt.")]
    [Min(0)]
    public int lastScore;
    [Tooltip("Rank index from the last attempt.")]
    public int lastRankIndex = -1;
    [Tooltip("Rank name from the last attempt.")]
    public string lastRankName;
    [Tooltip("Whether the last attempt counted as a win.")]
    public bool lastWon;
    [Tooltip("Pokemon name used in the last attempt.")]
    public string lastPokemonName;
    [Tooltip("In-game total hour of the last attempt.")]
    public int lastAttemptHour = -1;
    [Tooltip("Short source id that last started this contest.")]
    public string lastSource;

    public PlayerContestState() {
    }

    public PlayerContestState(PlayerContestStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        contestId = saveData.contestId;
        contestName = saveData.contestName;
        category = saveData.category;
        difficulty = saveData.difficulty;
        attemptCount = Mathf.Max(0, saveData.attemptCount);
        winCount = Mathf.Max(0, saveData.winCount);
        bestScore = Mathf.Max(0, saveData.bestScore);
        bestRankIndex = saveData.bestRankIndex;
        bestRankName = saveData.bestRankName;
        bestPokemonName = saveData.bestPokemonName;
        lastScore = Mathf.Max(0, saveData.lastScore);
        lastRankIndex = saveData.lastRankIndex;
        lastRankName = saveData.lastRankName;
        lastWon = saveData.lastWon;
        lastPokemonName = saveData.lastPokemonName;
        lastAttemptHour = saveData.lastAttemptHour;
        lastSource = saveData.lastSource;
    }

    public PlayerContestStateSaveData ToSaveData() {
        return new PlayerContestStateSaveData {
            contestId = contestId,
            contestName = contestName,
            category = category,
            difficulty = difficulty,
            attemptCount = attemptCount,
            winCount = winCount,
            bestScore = bestScore,
            bestRankIndex = bestRankIndex,
            bestRankName = bestRankName,
            bestPokemonName = bestPokemonName,
            lastScore = lastScore,
            lastRankIndex = lastRankIndex,
            lastRankName = lastRankName,
            lastWon = lastWon,
            lastPokemonName = lastPokemonName,
            lastAttemptHour = lastAttemptHour,
            lastSource = lastSource
        };
    }
}

[Serializable]
public class PlayerContestLogSaveData {
    public List<string> unlockedContestIds;
    public List<PlayerContestStateSaveData> contestHistory;
}

[Serializable]
public class PlayerContestStateSaveData {
    public string contestId;
    public string contestName;
    public ContestCategory category;
    public ContestDifficulty difficulty;
    public int attemptCount;
    public int winCount;
    public int bestScore;
    public int bestRankIndex;
    public string bestRankName;
    public string bestPokemonName;
    public int lastScore;
    public int lastRankIndex;
    public string lastRankName;
    public bool lastWon;
    public string lastPokemonName;
    public int lastAttemptHour;
    public string lastSource;
}
