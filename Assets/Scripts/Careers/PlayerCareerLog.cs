using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCareerLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for career paths unlocked for the player.")]
    [SerializeField] List<string> unlockedCareerIds = new List<string>();
    [Tooltip("Runtime/save progress for joined career paths.")]
    [SerializeField] List<PlayerCareerState> careers = new List<PlayerCareerState>();

    public IReadOnlyList<string> UnlockedCareerIds => unlockedCareerIds;
    public IReadOnlyList<PlayerCareerState> Careers => careers;
    public event Action<CareerPathDefinition> OnCareerUnlocked;
    public event Action<CareerPathDefinition> OnCareerJoined;
    public event Action<CareerPathDefinition, int> OnCareerPointsChanged;
    public event Action<CareerPathDefinition, CareerRankDefinition> OnCareerRankReached;
    public event Action OnCareerLogChanged;

    public bool HasUnlockedCareer(CareerPathDefinition career) {
        return career != null && (career.UnlockedByDefault || HasUnlockedCareer(career.Id));
    }

    public bool HasUnlockedCareer(string careerId) {
        return !string.IsNullOrWhiteSpace(careerId) && unlockedCareerIds.Contains(careerId);
    }

    public bool UnlockCareer(CareerPathDefinition career, string source = null) {
        if(career == null || HasUnlockedCareer(career.Id)) {
            return false;
        }

        unlockedCareerIds.Add(career.Id);
        OnCareerUnlocked?.Invoke(career);
        OnCareerLogChanged?.Invoke();
        career.PublishUnlocked(GetComponent<PlayerController>(), source);
        return true;
    }

    public bool JoinCareer(CareerPathDefinition career, bool viaMentor, string source, out string failureMessage) {
        if(career == null) {
            failureMessage = "No career path assigned.";
            return false;
        }

        var player = GetComponent<PlayerController>();
        if(!career.CanJoin(player, viaMentor, out failureMessage)) {
            return false;
        }

        var state = GetOrCreateState(career);
        if(state.joined) {
            failureMessage = null;
            return true;
        }

        state.joined = true;
        state.joinedAtHour = GetCurrentTotalHour();
        state.lastSource = source;
        ApplyReachedRanks(career, state, source);
        OnCareerJoined?.Invoke(career);
        OnCareerLogChanged?.Invoke();
        career.PublishJoined(player, source);
        failureMessage = null;
        return true;
    }

    public void ApplyPointGrants(IEnumerable<CareerPointGrant> grants, string fallbackSource = null, bool viaMentor = false) {
        if(grants == null) {
            return;
        }

        foreach(var grant in grants) {
            if(grant != null) {
                AddPoints(grant.career, grant.points, string.IsNullOrWhiteSpace(grant.source) ? fallbackSource : grant.source, viaMentor);
            }
        }
    }

    public bool AddPoints(CareerPathDefinition career, int points, string source = null, bool viaMentor = false) {
        if(career == null || points <= 0) {
            return false;
        }

        var state = GetOrCreateState(career);
        if(!state.joined && career.AutoJoinOnPointGain) {
            JoinCareer(career, viaMentor, source, out _);
        }

        if(!state.joined) {
            return false;
        }

        state.points += points;
        state.totalPointsEarned += points;
        state.lastPointGain = points;
        state.lastPointGainHour = GetCurrentTotalHour();
        state.lastSource = source;
        ApplyReachedRanks(career, state, source);
        OnCareerPointsChanged?.Invoke(career, state.points);
        OnCareerLogChanged?.Invoke();
        PublishPointEvent(career, state, points, source);
        return true;
    }

    public bool HasJoinedCareer(CareerPathDefinition career) {
        return GetState(career)?.joined ?? false;
    }

    public bool HasAnyJoinedCareerExcept(CareerPathDefinition career) {
        string careerId = career != null ? career.Id : null;
        return careers.Any(state => state != null && state.joined && state.careerId != careerId);
    }

    public int GetPoints(CareerPathDefinition career) {
        return GetState(career)?.points ?? 0;
    }

    public int GetRankIndex(CareerPathDefinition career) {
        var state = GetState(career);
        if(state == null) {
            return -1;
        }

        var rank = career.GetRankForPoints(state.points);
        return career.GetRankIndex(rank);
    }

    public bool HasReachedRank(CareerPathDefinition career, int rankIndex) {
        return GetRankIndex(career) >= Mathf.Max(0, rankIndex);
    }

    public bool HasJoinedCareerWithTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        foreach(var state in careers) {
            if(state == null || !state.joined) {
                continue;
            }

            var career = ResolveCareer(state.careerId);
            if(career != null && career.HasTag(tag)) {
                return true;
            }
        }

        return false;
    }

    PlayerCareerState GetOrCreateState(CareerPathDefinition career) {
        var state = GetState(career);
        if(state != null) {
            return state;
        }

        state = new PlayerCareerState {
            careerId = career.Id,
            careerName = career.DisplayName,
            category = career.Category,
            currentRankIndex = -1
        };
        careers.Add(state);
        return state;
    }

    PlayerCareerState GetState(CareerPathDefinition career) {
        return career != null ? careers.FirstOrDefault(state => state != null && state.careerId == career.Id) : null;
    }

    void ApplyReachedRanks(CareerPathDefinition career, PlayerCareerState state, string source) {
        var player = GetComponent<PlayerController>();
        foreach(var rank in career.GetRanksReached(state.points)) {
            if(rank == null || state.claimedRankIds.Contains(rank.Id)) {
                continue;
            }

            state.claimedRankIds.Add(rank.Id);
            state.currentRankIndex = Mathf.Max(state.currentRankIndex, career.GetRankIndex(rank));
            state.currentRankId = rank.Id;
            state.currentRankName = rank.DisplayName;
            career.ApplyRankRewards(player, rank, source);
            career.PublishRankUp(player, rank, state.points, source);
            OnCareerRankReached?.Invoke(career, rank);
        }

        var currentRank = career.GetRankForPoints(state.points);
        if(currentRank != null) {
            state.currentRankIndex = career.GetRankIndex(currentRank);
            state.currentRankId = currentRank.Id;
            state.currentRankName = currentRank.DisplayName;
        }
    }

    CareerPathDefinition ResolveCareer(string careerId) {
        if(string.IsNullOrWhiteSpace(careerId)) {
            return null;
        }

        return Resources.LoadAll<CareerPathDefinition>("").FirstOrDefault(career => career != null && career.Id == careerId);
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishPointEvent(CareerPathDefinition career, PlayerCareerState state, int points, string source) {
        GameEventPublishing.PublishOptional(
            null,
            $"career.points.{career.Id}",
            $"{career.DisplayName} gained {points} point(s).",
            GameEventCategory.Career,
            GameEventImportance.Info,
            this,
            "PlayerCareerLog",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("careerId", career.Id),
            GameEventPublishing.Value("careerName", career.DisplayName),
            GameEventPublishing.Value("pointsAdded", points),
            GameEventPublishing.Value("totalPoints", state.points),
            GameEventPublishing.Value("rank", state.currentRankName),
            GameEventPublishing.Value("source", source));
    }

    public object CaptureState() {
        return new PlayerCareerLogSaveData {
            unlockedCareerIds = unlockedCareerIds.Distinct().ToList(),
            careers = careers.Where(state => state != null).Select(state => state.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCareerLogSaveData;
        unlockedCareerIds = saveData?.unlockedCareerIds?.Distinct().ToList() ?? new List<string>();
        careers = saveData?.careers?.Where(s => s != null).Select(s => new PlayerCareerState(s)).ToList() ?? new List<PlayerCareerState>();
        OnCareerLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerCareerState {
    [Tooltip("Saved career id.")]
    public string careerId;
    [Tooltip("Saved career display name for fallback/debug output.")]
    public string careerName;
    [Tooltip("Saved career category.")]
    public CareerCategory category;
    [Tooltip("If enabled, the player has joined this career.")]
    public bool joined;
    [Tooltip("Current career points.")]
    [Min(0)]
    public int points;
    [Tooltip("Total career points ever earned.")]
    [Min(0)]
    public int totalPointsEarned;
    [Tooltip("Current career rank index.")]
    public int currentRankIndex = -1;
    [Tooltip("Current career rank id.")]
    public string currentRankId;
    [Tooltip("Current career rank display name.")]
    public string currentRankName;
    [Tooltip("Rank ids whose rewards have already been granted.")]
    public List<string> claimedRankIds = new List<string>();
    [Tooltip("In-game total hour this career was joined.")]
    public int joinedAtHour = -1;
    [Tooltip("Last career points gained in one action.")]
    [Min(0)]
    public int lastPointGain;
    [Tooltip("In-game total hour of the last point gain.")]
    public int lastPointGainHour = -1;
    [Tooltip("Short source id that last changed this career.")]
    public string lastSource;

    public PlayerCareerState() {
    }

    public PlayerCareerState(PlayerCareerStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        careerId = saveData.careerId;
        careerName = saveData.careerName;
        category = saveData.category;
        joined = saveData.joined;
        points = Mathf.Max(0, saveData.points);
        totalPointsEarned = Mathf.Max(0, saveData.totalPointsEarned);
        currentRankIndex = saveData.currentRankIndex;
        currentRankId = saveData.currentRankId;
        currentRankName = saveData.currentRankName;
        claimedRankIds = saveData.claimedRankIds?.Distinct().ToList() ?? new List<string>();
        joinedAtHour = saveData.joinedAtHour;
        lastPointGain = Mathf.Max(0, saveData.lastPointGain);
        lastPointGainHour = saveData.lastPointGainHour;
        lastSource = saveData.lastSource;
    }

    public PlayerCareerStateSaveData ToSaveData() {
        return new PlayerCareerStateSaveData {
            careerId = careerId,
            careerName = careerName,
            category = category,
            joined = joined,
            points = points,
            totalPointsEarned = totalPointsEarned,
            currentRankIndex = currentRankIndex,
            currentRankId = currentRankId,
            currentRankName = currentRankName,
            claimedRankIds = claimedRankIds?.Distinct().ToList() ?? new List<string>(),
            joinedAtHour = joinedAtHour,
            lastPointGain = lastPointGain,
            lastPointGainHour = lastPointGainHour,
            lastSource = lastSource
        };
    }
}

[Serializable]
public class PlayerCareerLogSaveData {
    public List<string> unlockedCareerIds;
    public List<PlayerCareerStateSaveData> careers;
}

[Serializable]
public class PlayerCareerStateSaveData {
    public string careerId;
    public string careerName;
    public CareerCategory category;
    public bool joined;
    public int points;
    public int totalPointsEarned;
    public int currentRankIndex;
    public string currentRankId;
    public string currentRankName;
    public List<string> claimedRankIds;
    public int joinedAtHour;
    public int lastPointGain;
    public int lastPointGainHour;
    public string lastSource;
}
