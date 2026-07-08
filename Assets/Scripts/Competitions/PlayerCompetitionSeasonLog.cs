using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCompetitionSeasonLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for competition seasons unlocked for this player.")]
    [SerializeField] List<string> unlockedSeasonIds = new List<string>();
    [Tooltip("Runtime/save history for competition season starts and completions.")]
    [SerializeField] List<PlayerCompetitionSeasonState> seasonStates = new List<PlayerCompetitionSeasonState>();

    public IReadOnlyList<string> UnlockedSeasonIds => unlockedSeasonIds;
    public IReadOnlyList<PlayerCompetitionSeasonState> SeasonStates => seasonStates;
    public event Action<CompetitionSeasonDefinition> OnSeasonUnlocked;
    public event Action<CompetitionSeasonDefinition> OnSeasonStarted;
    public event Action<CompetitionSeasonDefinition> OnSeasonCompleted;
    public event Action OnCompetitionSeasonLogChanged;

    public bool HasUnlocked(CompetitionSeasonDefinition season) {
        return season != null && (season.UnlockedByDefault || HasUnlocked(season.Id));
    }

    public bool HasUnlocked(string seasonId) {
        return !string.IsNullOrWhiteSpace(seasonId) && unlockedSeasonIds.Contains(seasonId);
    }

    public bool Unlock(CompetitionSeasonDefinition season, string sourceId = null) {
        if(season == null || HasUnlocked(season.Id)) {
            return false;
        }

        unlockedSeasonIds.Add(season.Id);
        OnSeasonUnlocked?.Invoke(season);
        OnCompetitionSeasonLogChanged?.Invoke();
        season.PublishUnlocked(GetComponent<PlayerController>(), sourceId);
        PublishLogEvent("unlocked", season, sourceId, GameEventImportance.Success);
        return true;
    }

    public void RecordStarted(CompetitionSeasonDefinition season, string sourceId = null) {
        if(season == null) {
            return;
        }

        var state = GetOrCreateState(season);
        state.startedCount++;
        state.active = true;
        state.lastStartedHour = GetCurrentTotalHour();
        state.lastSourceId = sourceId;
        OnSeasonStarted?.Invoke(season);
        OnCompetitionSeasonLogChanged?.Invoke();
        PublishLogEvent("started", season, sourceId, GameEventImportance.Info);
    }

    public void RecordCompleted(CompetitionSeasonDefinition season, string sourceId = null) {
        if(season == null) {
            return;
        }

        var state = GetOrCreateState(season);
        state.completedCount++;
        state.active = false;
        state.lastCompletedHour = GetCurrentTotalHour();
        state.lastSourceId = sourceId;
        OnSeasonCompleted?.Invoke(season);
        OnCompetitionSeasonLogChanged?.Invoke();
        PublishLogEvent("completed", season, sourceId, GameEventImportance.Success);
    }

    public bool HasStarted(CompetitionSeasonDefinition season) {
        return GetStartedCount(season) > 0;
    }

    public bool HasCompleted(CompetitionSeasonDefinition season) {
        return GetCompletedCount(season) > 0;
    }

    public bool IsActive(CompetitionSeasonDefinition season) {
        var state = GetState(season);
        return state != null && state.active && season != null && season.IsActiveNow();
    }

    public int GetStartedCount(CompetitionSeasonDefinition season) {
        return season != null ? Mathf.Max(0, GetState(season)?.startedCount ?? 0) : 0;
    }

    public int GetCompletedCount(CompetitionSeasonDefinition season) {
        return season != null ? Mathf.Max(0, GetState(season)?.completedCount ?? 0) : 0;
    }

    public PlayerCompetitionSeasonState GetState(CompetitionSeasonDefinition season) {
        return season != null ? GetState(season.Id) : null;
    }

    public PlayerCompetitionSeasonState GetState(string seasonId) {
        if(string.IsNullOrWhiteSpace(seasonId)) {
            return null;
        }

        return seasonStates.FirstOrDefault(state => state != null && state.seasonId == seasonId);
    }

    public PlayerCompetitionSeasonState GetOrCreateState(CompetitionSeasonDefinition season) {
        string seasonId = season != null ? season.Id : string.Empty;
        var state = GetState(seasonId);
        if(state != null) {
            return state;
        }

        state = new PlayerCompetitionSeasonState {
            seasonId = seasonId,
            seasonName = season != null ? season.DisplayName : string.Empty
        };
        seasonStates.Add(state);
        return state;
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishLogEvent(string phase, CompetitionSeasonDefinition season, string sourceId, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            null,
            $"competition-season-log.{phase}.{season?.Id}",
            $"Competition season log {phase}.",
            GameEventCategory.BattleRule,
            importance,
            this,
            "PlayerCompetitionSeasonLog",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("seasonId", season != null ? season.Id : string.Empty),
            GameEventPublishing.Value("seasonName", season != null ? season.DisplayName : string.Empty),
            GameEventPublishing.Value("sourceId", sourceId));
    }

    public object CaptureState() {
        return new PlayerCompetitionSeasonLogSaveData {
            unlockedSeasonIds = unlockedSeasonIds.Distinct().ToList(),
            seasonStates = seasonStates.Where(state => state != null).Select(state => state.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCompetitionSeasonLogSaveData;
        unlockedSeasonIds = saveData?.unlockedSeasonIds?.Distinct().ToList() ?? new List<string>();
        seasonStates = saveData?.seasonStates?.Where(entry => entry != null).Select(entry => entry.Clone()).ToList() ?? new List<PlayerCompetitionSeasonState>();
        OnCompetitionSeasonLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerCompetitionSeasonState {
    [Tooltip("Saved season id.")]
    public string seasonId;
    [Tooltip("Saved season display name for fallback/debug output.")]
    public string seasonName;
    [Tooltip("Whether this season is currently active for this player.")]
    public bool active;
    [Tooltip("How many times this season was started.")]
    [Min(0)]
    public int startedCount;
    [Tooltip("How many times this season was completed.")]
    [Min(0)]
    public int completedCount;
    [Tooltip("Last in-game total hour this season started.")]
    public int lastStartedHour = -1;
    [Tooltip("Last in-game total hour this season completed.")]
    public int lastCompletedHour = -1;
    [Tooltip("Short source id that last changed this season.")]
    public string lastSourceId;

    public PlayerCompetitionSeasonState Clone() {
        return new PlayerCompetitionSeasonState {
            seasonId = seasonId,
            seasonName = seasonName,
            active = active,
            startedCount = startedCount,
            completedCount = completedCount,
            lastStartedHour = lastStartedHour,
            lastCompletedHour = lastCompletedHour,
            lastSourceId = lastSourceId
        };
    }
}

[Serializable]
public class PlayerCompetitionSeasonLogSaveData {
    public List<string> unlockedSeasonIds = new List<string>();
    public List<PlayerCompetitionSeasonState> seasonStates = new List<PlayerCompetitionSeasonState>();
}
