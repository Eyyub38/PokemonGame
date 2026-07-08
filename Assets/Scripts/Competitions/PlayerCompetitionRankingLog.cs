using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCompetitionRankingLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for ranking tracks unlocked for this player.")]
    [SerializeField] List<string> unlockedRankingIds = new List<string>();
    [Tooltip("Runtime/save point and rank state for each ranking track.")]
    [SerializeField] List<PlayerCompetitionRankingState> rankingStates = new List<PlayerCompetitionRankingState>();

    public IReadOnlyList<string> UnlockedRankingIds => unlockedRankingIds;
    public IReadOnlyList<PlayerCompetitionRankingState> RankingStates => rankingStates;
    public event Action<CompetitionRankingDefinition> OnRankingUnlocked;
    public event Action<CompetitionRankingDefinition, int> OnRankingPointsChanged;
    public event Action<CompetitionRankingDefinition, CompetitionRankTier> OnRankReached;
    public event Action OnCompetitionRankingChanged;

    public bool HasUnlocked(CompetitionRankingDefinition ranking) {
        return ranking != null && (ranking.UnlockedByDefault || HasUnlocked(ranking.Id));
    }

    public bool HasUnlocked(string rankingId) {
        return !string.IsNullOrWhiteSpace(rankingId) && unlockedRankingIds.Contains(rankingId);
    }

    public bool Unlock(CompetitionRankingDefinition ranking, string sourceId = null) {
        if(ranking == null || HasUnlocked(ranking.Id)) {
            return false;
        }

        unlockedRankingIds.Add(ranking.Id);
        OnRankingUnlocked?.Invoke(ranking);
        OnCompetitionRankingChanged?.Invoke();
        ranking.PublishUnlocked(GetComponent<PlayerController>(), sourceId);
        PublishLogEvent("unlocked", ranking, null, 0, 0, sourceId, GameEventImportance.Success);
        return true;
    }

    public void RecordCompetitionProgressEvent(CompetitionRankingEventType eventType, CompetitionDefinition competition, CompetitionStage stage, BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet, bool won, string sourceId = null) {
        var player = GetComponent<PlayerController>();
        foreach(var ranking in Resources.LoadAll<CompetitionRankingDefinition>("")) {
            if(ranking == null) {
                continue;
            }

            if(!ranking.CanScore(player, this, out _)) {
                continue;
            }

            if(!ranking.TryCalculatePoints(eventType, competition, stage, challenge, ruleSet, won, out int points, out _)) {
                continue;
            }

            AddPoints(ranking, points, $"{sourceId}:{eventType}", competition, stage, challenge);
        }
    }

    public int AddPoints(CompetitionRankingDefinition ranking, int points, string sourceId = null, CompetitionDefinition competition = null, CompetitionStage stage = null, BattleChallengeDefinition challenge = null) {
        if(ranking == null || points == 0) {
            return 0;
        }

        var player = GetComponent<PlayerController>();
        var state = GetOrCreateState(ranking);
        int previousPoints = state.currentPoints;
        string previousTierId = state.currentTierId;

        state.currentPoints += points;
        if(!ranking.AllowNegativePoints) {
            state.currentPoints = Mathf.Max(0, state.currentPoints);
        }

        state.lifetimePoints += points;
        if(!ranking.AllowNegativePoints) {
            state.lifetimePoints = Mathf.Max(0, state.lifetimePoints);
        }

        state.lastDelta = state.currentPoints - previousPoints;
        state.lastSourceId = sourceId;
        state.lastCompetitionId = competition != null ? competition.Id : string.Empty;
        state.lastStageId = stage != null ? stage.StageId : string.Empty;
        state.lastChallengeId = challenge != null ? challenge.Id : string.Empty;
        state.lastChangedHour = GetCurrentTotalHour();

        var tier = ranking.GetTierForPoints(state.currentPoints);
        if(tier != null) {
            state.currentTierId = tier.TierId;
            state.currentTierName = tier.DisplayName;

            bool reachedNewTier = !string.Equals(previousTierId, tier.TierId, StringComparison.OrdinalIgnoreCase)
                && !state.reachedTierIds.Contains(tier.TierId);
            if(reachedNewTier || !ranking.GrantTierRewardsOnce) {
                if(!state.reachedTierIds.Contains(tier.TierId)) {
                    state.reachedTierIds.Add(tier.TierId);
                }

                ranking.ApplyTierRewards(player, tier);
                ranking.PublishRankReached(player, tier, state.currentPoints, sourceId);
                OnRankReached?.Invoke(ranking, tier);
            }
        }

        state.bestPoints = Mathf.Max(state.bestPoints, state.currentPoints);
        state.pointHistory.Add(new PlayerCompetitionRankingPointRecord {
            rankingId = ranking.Id,
            rankingName = ranking.DisplayName,
            competitionId = competition != null ? competition.Id : string.Empty,
            competitionName = competition != null ? competition.DisplayName : string.Empty,
            stageId = stage != null ? stage.StageId : string.Empty,
            challengeId = challenge != null ? challenge.Id : string.Empty,
            delta = state.lastDelta,
            totalPoints = state.currentPoints,
            totalHour = state.lastChangedHour,
            sourceId = sourceId
        });

        OnRankingPointsChanged?.Invoke(ranking, state.lastDelta);
        OnCompetitionRankingChanged?.Invoke();
        ranking.PublishPointsChanged(player, state.lastDelta, state.currentPoints, sourceId);
        PublishLogEvent("points", ranking, tier, state.lastDelta, state.currentPoints, sourceId, GameEventImportance.Info);
        return state.lastDelta;
    }

    public void ResetCurrentPoints(CompetitionRankingDefinition ranking, bool keepReachedTiers = true, string sourceId = null) {
        if(ranking == null) {
            return;
        }

        var state = GetOrCreateState(ranking);
        int previousPoints = state.currentPoints;
        if(previousPoints == 0 && keepReachedTiers) {
            return;
        }

        state.currentPoints = 0;
        state.currentTierId = string.Empty;
        state.currentTierName = string.Empty;
        state.lastDelta = -previousPoints;
        state.lastCompetitionId = string.Empty;
        state.lastStageId = string.Empty;
        state.lastChallengeId = string.Empty;
        state.lastSourceId = sourceId;
        state.lastChangedHour = GetCurrentTotalHour();

        if(!keepReachedTiers) {
            state.reachedTierIds.Clear();
        }

        state.pointHistory.Add(new PlayerCompetitionRankingPointRecord {
            rankingId = ranking.Id,
            rankingName = ranking.DisplayName,
            delta = state.lastDelta,
            totalPoints = state.currentPoints,
            totalHour = state.lastChangedHour,
            sourceId = sourceId
        });

        OnRankingPointsChanged?.Invoke(ranking, state.lastDelta);
        OnCompetitionRankingChanged?.Invoke();
        ranking.PublishPointsChanged(GetComponent<PlayerController>(), state.lastDelta, state.currentPoints, sourceId);
        PublishLogEvent("reset", ranking, null, state.lastDelta, state.currentPoints, sourceId, GameEventImportance.Warning);
    }

    public int GetCurrentPoints(CompetitionRankingDefinition ranking) {
        return ranking != null ? Mathf.Max(0, GetState(ranking)?.currentPoints ?? 0) : 0;
    }

    public int GetLifetimePoints(CompetitionRankingDefinition ranking) {
        return ranking != null ? Mathf.Max(0, GetState(ranking)?.lifetimePoints ?? 0) : 0;
    }

    public int GetBestPoints(CompetitionRankingDefinition ranking) {
        return ranking != null ? Mathf.Max(0, GetState(ranking)?.bestPoints ?? 0) : 0;
    }

    public bool HasReachedTier(CompetitionRankingDefinition ranking, string tierId) {
        var state = ranking != null ? GetState(ranking) : null;
        return state != null && state.HasReachedTier(tierId);
    }

    public bool HasReachedTier(CompetitionRankingDefinition ranking, CompetitionRankTier tier) {
        return tier != null && HasReachedTier(ranking, tier.TierId);
    }

    public PlayerCompetitionRankingState GetState(CompetitionRankingDefinition ranking) {
        return ranking != null ? GetState(ranking.Id) : null;
    }

    public PlayerCompetitionRankingState GetState(string rankingId) {
        if(string.IsNullOrWhiteSpace(rankingId)) {
            return null;
        }

        return rankingStates.FirstOrDefault(state => state != null && state.rankingId == rankingId);
    }

    public PlayerCompetitionRankingState GetOrCreateState(CompetitionRankingDefinition ranking) {
        string rankingId = ranking != null ? ranking.Id : string.Empty;
        var state = GetState(rankingId);
        if(state != null) {
            return state;
        }

        state = new PlayerCompetitionRankingState {
            rankingId = rankingId,
            rankingName = ranking != null ? ranking.DisplayName : string.Empty
        };
        rankingStates.Add(state);
        return state;
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishLogEvent(string phase, CompetitionRankingDefinition ranking, CompetitionRankTier tier, int delta, int totalPoints, string sourceId, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            null,
            $"competition-ranking-log.{phase}.{ranking?.Id}.{tier?.TierId}",
            $"Competition ranking log {phase}.",
            GameEventCategory.BattleRule,
            importance,
            this,
            "PlayerCompetitionRankingLog",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("rankingId", ranking != null ? ranking.Id : string.Empty),
            GameEventPublishing.Value("rankingName", ranking != null ? ranking.DisplayName : string.Empty),
            GameEventPublishing.Value("tierId", tier != null ? tier.TierId : string.Empty),
            GameEventPublishing.Value("delta", delta),
            GameEventPublishing.Value("totalPoints", totalPoints),
            GameEventPublishing.Value("sourceId", sourceId));
    }

    public object CaptureState() {
        return new PlayerCompetitionRankingLogSaveData {
            unlockedRankingIds = unlockedRankingIds.Distinct().ToList(),
            rankingStates = rankingStates.Where(state => state != null).Select(state => state.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCompetitionRankingLogSaveData;
        unlockedRankingIds = saveData?.unlockedRankingIds?.Distinct().ToList() ?? new List<string>();
        rankingStates = saveData?.rankingStates?.Where(entry => entry != null).Select(entry => new PlayerCompetitionRankingState(entry)).ToList() ?? new List<PlayerCompetitionRankingState>();
        OnCompetitionRankingChanged?.Invoke();
    }
}

[Serializable]
public class PlayerCompetitionRankingState {
    [Tooltip("Saved ranking track id.")]
    public string rankingId;
    [Tooltip("Saved ranking display name for fallback/debug output.")]
    public string rankingName;
    [Tooltip("Current point total for the active season/progression window.")]
    public int currentPoints;
    [Tooltip("Best current-points value ever reached.")]
    public int bestPoints;
    [Tooltip("Lifetime point total for long-term records.")]
    public int lifetimePoints;
    [Tooltip("Current rank tier id.")]
    public string currentTierId;
    [Tooltip("Current rank tier display name.")]
    public string currentTierName;
    [Tooltip("Tier ids reached at least once.")]
    public List<string> reachedTierIds = new List<string>();
    [Tooltip("Recent point changes for debugging, UI and save history.")]
    public List<PlayerCompetitionRankingPointRecord> pointHistory = new List<PlayerCompetitionRankingPointRecord>();
    [Tooltip("Last point delta applied.")]
    public int lastDelta;
    [Tooltip("Last related competition id.")]
    public string lastCompetitionId;
    [Tooltip("Last related stage id.")]
    public string lastStageId;
    [Tooltip("Last related challenge id.")]
    public string lastChallengeId;
    [Tooltip("Last source id that changed this ranking.")]
    public string lastSourceId;
    [Tooltip("Last in-game total hour this ranking changed.")]
    public int lastChangedHour = -1;

    public PlayerCompetitionRankingState() {
    }

    public PlayerCompetitionRankingState(PlayerCompetitionRankingStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        rankingId = saveData.rankingId;
        rankingName = saveData.rankingName;
        currentPoints = saveData.currentPoints;
        bestPoints = saveData.bestPoints;
        lifetimePoints = saveData.lifetimePoints;
        currentTierId = saveData.currentTierId;
        currentTierName = saveData.currentTierName;
        reachedTierIds = saveData.reachedTierIds?.Distinct().ToList() ?? new List<string>();
        pointHistory = saveData.pointHistory?.Where(record => record != null).Select(record => record.Clone()).ToList() ?? new List<PlayerCompetitionRankingPointRecord>();
        lastDelta = saveData.lastDelta;
        lastCompetitionId = saveData.lastCompetitionId;
        lastStageId = saveData.lastStageId;
        lastChallengeId = saveData.lastChallengeId;
        lastSourceId = saveData.lastSourceId;
        lastChangedHour = saveData.lastChangedHour;
    }

    public bool HasReachedTier(string tierId) {
        return !string.IsNullOrWhiteSpace(tierId) && reachedTierIds.Contains(tierId);
    }

    public PlayerCompetitionRankingStateSaveData ToSaveData() {
        return new PlayerCompetitionRankingStateSaveData {
            rankingId = rankingId,
            rankingName = rankingName,
            currentPoints = currentPoints,
            bestPoints = bestPoints,
            lifetimePoints = lifetimePoints,
            currentTierId = currentTierId,
            currentTierName = currentTierName,
            reachedTierIds = reachedTierIds.Distinct().ToList(),
            pointHistory = pointHistory.Where(record => record != null).Select(record => record.Clone()).ToList(),
            lastDelta = lastDelta,
            lastCompetitionId = lastCompetitionId,
            lastStageId = lastStageId,
            lastChallengeId = lastChallengeId,
            lastSourceId = lastSourceId,
            lastChangedHour = lastChangedHour
        };
    }
}

[Serializable]
public class PlayerCompetitionRankingPointRecord {
    [Tooltip("Ranking track id.")]
    public string rankingId;
    [Tooltip("Ranking track display name.")]
    public string rankingName;
    [Tooltip("Related competition id.")]
    public string competitionId;
    [Tooltip("Related competition display name.")]
    public string competitionName;
    [Tooltip("Related stage id.")]
    public string stageId;
    [Tooltip("Related challenge id.")]
    public string challengeId;
    [Tooltip("Point delta applied.")]
    public int delta;
    [Tooltip("Point total after the delta.")]
    public int totalPoints;
    [Tooltip("In-game total hour when this point change happened.")]
    public int totalHour;
    [Tooltip("Short source id that caused this point change.")]
    public string sourceId;

    public PlayerCompetitionRankingPointRecord Clone() {
        return new PlayerCompetitionRankingPointRecord {
            rankingId = rankingId,
            rankingName = rankingName,
            competitionId = competitionId,
            competitionName = competitionName,
            stageId = stageId,
            challengeId = challengeId,
            delta = delta,
            totalPoints = totalPoints,
            totalHour = totalHour,
            sourceId = sourceId
        };
    }
}

[Serializable]
public class PlayerCompetitionRankingLogSaveData {
    public List<string> unlockedRankingIds = new List<string>();
    public List<PlayerCompetitionRankingStateSaveData> rankingStates = new List<PlayerCompetitionRankingStateSaveData>();
}

[Serializable]
public class PlayerCompetitionRankingStateSaveData {
    public string rankingId;
    public string rankingName;
    public int currentPoints;
    public int bestPoints;
    public int lifetimePoints;
    public string currentTierId;
    public string currentTierName;
    public List<string> reachedTierIds = new List<string>();
    public List<PlayerCompetitionRankingPointRecord> pointHistory = new List<PlayerCompetitionRankingPointRecord>();
    public int lastDelta;
    public string lastCompetitionId;
    public string lastStageId;
    public string lastChallengeId;
    public string lastSourceId;
    public int lastChangedHour;
}
