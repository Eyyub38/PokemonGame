using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerLifestyleLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum lifestyle history records kept in save data. 0 keeps all records.")]
    [Min(0)]
    [SerializeField] int maxRecords = 120;
    [Tooltip("Runtime/save lifestyle point states.")]
    [SerializeField] List<PlayerLifestyleState> lifestyles = new List<PlayerLifestyleState>();
    [Tooltip("Runtime/save history of lifestyle point changes.")]
    [SerializeField] List<PlayerLifestyleRecord> records = new List<PlayerLifestyleRecord>();

    public IReadOnlyList<PlayerLifestyleState> Lifestyles => lifestyles;
    public IReadOnlyList<PlayerLifestyleRecord> Records => records;
    public event Action<PlayerLifestyleState, int> OnLifestyleChanged;

    public PlayerLifestyleState GetState(PlayerLifestyleDefinition lifestyle) {
        return lifestyle != null ? GetState(lifestyle.Id) : null;
    }

    public PlayerLifestyleState GetState(string lifestyleId) {
        if(string.IsNullOrWhiteSpace(lifestyleId)) {
            return null;
        }

        return lifestyles.FirstOrDefault(state => state != null && state.lifestyleId == lifestyleId);
    }

    public int GetPoints(PlayerLifestyleDefinition lifestyle) {
        return GetState(lifestyle)?.points ?? 0;
    }

    public int GetRankIndex(PlayerLifestyleDefinition lifestyle) {
        var state = GetState(lifestyle);
        return state != null ? state.rankIndex : -1;
    }

    public bool HasLifestyle(PlayerLifestyleDefinition lifestyle, int minimumPoints = 1) {
        return lifestyle != null && GetPoints(lifestyle) >= Mathf.Max(0, minimumPoints);
    }

    public bool HasLifestyleTag(string tag, int minimumPoints = 1) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        return GetStatesWithDefinitions()
            .Any(pair => pair.definition != null
                && pair.definition.HasTag(tag)
                && pair.state.points >= Mathf.Max(0, minimumPoints));
    }

    public bool HasLifestyleCategory(PlayerLifestyleCategory category, int minimumPoints = 1) {
        return GetStatesWithDefinitions()
            .Any(pair => pair.definition != null
                && pair.definition.Category == category
                && pair.state.points >= Mathf.Max(0, minimumPoints));
    }

    public PlayerLifestyleState GetDominantLifestyle() {
        return lifestyles
            .Where(state => state != null && state.points > 0)
            .OrderByDescending(state => state.points)
            .ThenBy(state => state.lifestyleName)
            .FirstOrDefault();
    }

    public bool DominantLifestyleIs(PlayerLifestyleDefinition lifestyle) {
        var dominant = GetDominantLifestyle();
        return lifestyle != null && dominant != null && dominant.lifestyleId == lifestyle.Id;
    }

    public void RecordActivityCompletion(ActivityDefinition activity) {
        if(activity == null) {
            return;
        }

        foreach(var lifestyle in Resources.LoadAll<PlayerLifestyleDefinition>("")) {
            if(lifestyle == null) {
                continue;
            }

            int points = lifestyle.GetActivityPoints(activity);
            if(points != 0) {
                AddPoints(lifestyle, points, $"activity:{activity.Id}", activity.DisplayName, activity);
            }
        }
    }

    public void ApplyGrants(IEnumerable<LifestylePointGrant> grants, string fallbackSourceId = null, string fallbackSourceName = null, UnityEngine.Object context = null) {
        if(grants == null) {
            return;
        }

        foreach(var grant in grants) {
            if(grant == null || grant.lifestyle == null || grant.points == 0) {
                continue;
            }

            AddPoints(
                grant.lifestyle,
                grant.points,
                string.IsNullOrWhiteSpace(grant.sourceId) ? fallbackSourceId : grant.sourceId,
                string.IsNullOrWhiteSpace(grant.sourceName) ? fallbackSourceName : grant.sourceName,
                context);
        }
    }

    public PlayerLifestyleState AddPoints(PlayerLifestyleDefinition lifestyle, int amount, string sourceId = null, string sourceName = null, UnityEngine.Object context = null) {
        if(lifestyle == null || amount == 0) {
            return null;
        }

        var state = GetOrCreateState(lifestyle);
        int before = state.points;
        state.points = lifestyle.ClampPoints(state.points + amount);
        int delta = state.points - before;
        if(delta == 0) {
            return state;
        }

        var rank = lifestyle.GetRankForPoints(state.points);
        state.rankId = rank != null ? rank.RankId : string.Empty;
        state.rankName = rank != null ? rank.DisplayName : string.Empty;
        state.rankIndex = lifestyle.GetRankIndexForPoints(state.points);
        state.lastDelta = delta;
        state.lastSourceId = sourceId;
        state.lastSourceName = sourceName;
        state.lastChangedHour = GetCurrentTotalHour();

        records.Add(new PlayerLifestyleRecord(state, delta, sourceId, sourceName, GetCurrentTotalHour(), Time.frameCount));
        TrimRecords();
        OnLifestyleChanged?.Invoke(state, delta);
        lifestyle.PublishChanged(GetComponent<PlayerController>(), state, delta, sourceId, sourceName, context != null ? context : this);
        return state;
    }

    PlayerLifestyleState GetOrCreateState(PlayerLifestyleDefinition lifestyle) {
        var state = GetState(lifestyle.Id);
        if(state != null) {
            return state;
        }

        state = new PlayerLifestyleState {
            lifestyleId = lifestyle.Id,
            lifestyleName = lifestyle.DisplayName,
            category = lifestyle.Category,
            points = 0,
            rankIndex = -1,
            lastChangedHour = -1
        };
        lifestyles.Add(state);
        return state;
    }

    IEnumerable<(PlayerLifestyleState state, PlayerLifestyleDefinition definition)> GetStatesWithDefinitions() {
        foreach(var state in lifestyles) {
            if(state == null) {
                continue;
            }

            var definition = ResolveDefinition(state.lifestyleId);
            yield return (state, definition);
        }
    }

    PlayerLifestyleDefinition ResolveDefinition(string lifestyleId) {
        if(string.IsNullOrWhiteSpace(lifestyleId)) {
            return null;
        }

        return Resources.LoadAll<PlayerLifestyleDefinition>("").FirstOrDefault(lifestyle => lifestyle != null && lifestyle.Id == lifestyleId);
    }

    void TrimRecords() {
        if(maxRecords <= 0) {
            return;
        }

        while(records.Count > maxRecords) {
            records.RemoveAt(0);
        }
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerLifestyleLogSaveData {
            lifestyles = lifestyles.Where(state => state != null).Select(state => new PlayerLifestyleState(state)).ToList(),
            records = records.Where(record => record != null).Select(record => new PlayerLifestyleRecord(record)).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerLifestyleLogSaveData;
        lifestyles = saveData?.lifestyles?.Where(entry => entry != null).Select(entry => new PlayerLifestyleState(entry)).ToList()
            ?? new List<PlayerLifestyleState>();
        records = saveData?.records?.Where(record => record != null).Select(record => new PlayerLifestyleRecord(record)).ToList()
            ?? new List<PlayerLifestyleRecord>();
    }
}

[Serializable]
public class PlayerLifestyleState {
    [Tooltip("Saved lifestyle definition id.")]
    public string lifestyleId;
    [Tooltip("Saved lifestyle display name.")]
    public string lifestyleName;
    [Tooltip("Saved lifestyle category.")]
    public PlayerLifestyleCategory category;
    [Tooltip("Current lifestyle points.")]
    public int points;
    [Tooltip("Current rank id reached by these points.")]
    public string rankId;
    [Tooltip("Current rank display name reached by these points.")]
    public string rankName;
    [Tooltip("Current rank index reached by these points.")]
    public int rankIndex = -1;
    [Tooltip("Most recent point change.")]
    public int lastDelta;
    [Tooltip("Source id of the most recent point change.")]
    public string lastSourceId;
    [Tooltip("Source display name of the most recent point change.")]
    public string lastSourceName;
    [Tooltip("Total in-game hour when this lifestyle last changed.")]
    public int lastChangedHour;

    public PlayerLifestyleState() {
    }

    public PlayerLifestyleState(PlayerLifestyleState other) {
        lifestyleId = other.lifestyleId;
        lifestyleName = other.lifestyleName;
        category = other.category;
        points = other.points;
        rankId = other.rankId;
        rankName = other.rankName;
        rankIndex = other.rankIndex;
        lastDelta = other.lastDelta;
        lastSourceId = other.lastSourceId;
        lastSourceName = other.lastSourceName;
        lastChangedHour = other.lastChangedHour;
    }
}

[Serializable]
public class PlayerLifestyleRecord {
    [Tooltip("Lifestyle id changed by this record.")]
    public string lifestyleId;
    [Tooltip("Lifestyle display name changed by this record.")]
    public string lifestyleName;
    [Tooltip("Lifestyle category changed by this record.")]
    public PlayerLifestyleCategory category;
    [Tooltip("Point delta applied by this record.")]
    public int delta;
    [Tooltip("Resulting points after this record.")]
    public int resultingPoints;
    [Tooltip("Resulting rank id after this record.")]
    public string rankId;
    [Tooltip("Resulting rank display name after this record.")]
    public string rankName;
    [Tooltip("Source id that caused this record.")]
    public string sourceId;
    [Tooltip("Source display name that caused this record.")]
    public string sourceName;
    [Tooltip("Total in-game hour when this record was created.")]
    public int recordedAtHour;
    [Tooltip("Unity frame when this record was created.")]
    public int frame;

    public PlayerLifestyleRecord() {
    }

    public PlayerLifestyleRecord(PlayerLifestyleState state, int delta, string sourceId, string sourceName, int recordedAtHour, int frame) {
        lifestyleId = state.lifestyleId;
        lifestyleName = state.lifestyleName;
        category = state.category;
        this.delta = delta;
        resultingPoints = state.points;
        rankId = state.rankId;
        rankName = state.rankName;
        this.sourceId = sourceId;
        this.sourceName = sourceName;
        this.recordedAtHour = recordedAtHour;
        this.frame = frame;
    }

    public PlayerLifestyleRecord(PlayerLifestyleRecord other) {
        lifestyleId = other.lifestyleId;
        lifestyleName = other.lifestyleName;
        category = other.category;
        delta = other.delta;
        resultingPoints = other.resultingPoints;
        rankId = other.rankId;
        rankName = other.rankName;
        sourceId = other.sourceId;
        sourceName = other.sourceName;
        recordedAtHour = other.recordedAtHour;
        frame = other.frame;
    }
}

[Serializable]
public class PlayerLifestyleLogSaveData {
    public List<PlayerLifestyleState> lifestyles;
    public List<PlayerLifestyleRecord> records;
}
