using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerRideCompanionLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save history of ride companion coordination.")]
    [SerializeField] List<RideCompanionCoordinationRecord> records = new List<RideCompanionCoordinationRecord>();
    [Tooltip("Runtime/save list of companions currently waiting to return.")]
    [SerializeField] List<RideCompanionPendingReturnRecord> pendingReturns = new List<RideCompanionPendingReturnRecord>();

    public IReadOnlyList<RideCompanionCoordinationRecord> Records => records;
    public IReadOnlyList<RideCompanionPendingReturnRecord> PendingReturns => pendingReturns;
    public event Action OnRideCompanionLogChanged;

    public void RecordCoordination(RidePokemonDefinition ride, Pokemon mountedPokemon, RideCompanionCoordinationSummary summary, string phase) {
        if(summary == null) {
            return;
        }

        records.Add(new RideCompanionCoordinationRecord {
            phase = phase,
            rideId = ride != null ? ride.Id : summary.rideId,
            rideName = ride != null ? ride.DisplayName : summary.rideName,
            pokemonId = mountedPokemon != null ? mountedPokemon.InstanceId : string.Empty,
            pokemonName = mountedPokemon != null ? mountedPokemon.NickName : summary.pokemonName,
            totalRiderCapacity = summary.totalRiderCapacity,
            companionCapacity = summary.companionCapacity,
            followingCompanionCount = summary.followingCompanionCount,
            overflowMode = summary.overflowMode,
            policyNote = summary.policyNote,
            keptCompanionIds = Clone(summary.keptCompanionIds),
            selfTravelCompanionIds = Clone(summary.selfTravelCompanionIds),
            detachedCompanionIds = Clone(summary.detachedCompanionIds),
            day = GetCurrentDay(),
            hour = GetCurrentHour(),
            absoluteHour = GetCurrentAbsoluteHour()
        });
        OnRideCompanionLogChanged?.Invoke();
    }

    public void AddPendingReturn(CompanionController companion, RidePokemonDefinition ride, int dueAbsoluteHour, string reason) {
        if(companion == null) {
            return;
        }

        pendingReturns.RemoveAll(record => record != null && record.companionId == companion.CompanionId);
        pendingReturns.Add(new RideCompanionPendingReturnRecord {
            companionId = companion.CompanionId,
            companionName = companion.CompanionName,
            rideId = ride != null ? ride.Id : string.Empty,
            rideName = ride != null ? ride.DisplayName : string.Empty,
            dueAbsoluteHour = Mathf.Max(0, dueAbsoluteHour),
            reason = reason
        });
        OnRideCompanionLogChanged?.Invoke();
    }

    public void RemovePendingReturn(string companionId) {
        if(string.IsNullOrWhiteSpace(companionId)) {
            return;
        }

        if(pendingReturns.RemoveAll(record => record != null && record.companionId == companionId) > 0) {
            OnRideCompanionLogChanged?.Invoke();
        }
    }

    public IEnumerable<RideCompanionPendingReturnRecord> GetDueReturns(int absoluteHour) {
        return pendingReturns.Where(record => record != null && record.dueAbsoluteHour <= absoluteHour);
    }

    public IEnumerable<RideCompanionCoordinationRecord> GetRecent(int count = 20) {
        return records
            .Where(record => record != null)
            .OrderByDescending(record => record.absoluteHour)
            .Take(Mathf.Max(1, count));
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentHour() {
        return TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    List<string> Clone(IEnumerable<string> values) {
        return values != null ? values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList() : new List<string>();
    }

    public object CaptureState() {
        return new PlayerRideCompanionSaveData {
            records = records.Select(record => record.Clone()).ToList(),
            pendingReturns = pendingReturns.Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerRideCompanionSaveData;
        if(saveData == null) {
            return;
        }

        records = saveData.records ?? new List<RideCompanionCoordinationRecord>();
        pendingReturns = saveData.pendingReturns ?? new List<RideCompanionPendingReturnRecord>();
        OnRideCompanionLogChanged?.Invoke();
    }
}

[Serializable]
public class RideCompanionCoordinationRecord {
    [Tooltip("Coordination phase, such as mounted, dismounted or restored.")]
    public string phase;
    [Tooltip("Ride id involved in this record.")]
    public string rideId;
    [Tooltip("Ride display name saved for fallback/debug output.")]
    public string rideName;
    [Tooltip("Mounted Pokemon instance id involved in this record.")]
    public string pokemonId;
    [Tooltip("Mounted Pokemon display name saved for fallback/debug output.")]
    public string pokemonName;
    [Tooltip("Total rider capacity including the player.")]
    public int totalRiderCapacity;
    [Tooltip("Available companion seats after reserving one seat for the player.")]
    public int companionCapacity;
    [Tooltip("Number of companions following before coordination.")]
    public int followingCompanionCount;
    [Tooltip("Overflow mode used by this record.")]
    public RideCompanionOverflowMode overflowMode;
    [Tooltip("Policy note or matched rule note.")]
    public string policyNote;
    [Tooltip("Companion ids kept within capacity.")]
    public List<string> keptCompanionIds = new List<string>();
    [Tooltip("Companion ids allowed to self-travel.")]
    public List<string> selfTravelCompanionIds = new List<string>();
    [Tooltip("Companion ids detached to catch up later.")]
    public List<string> detachedCompanionIds = new List<string>();
    [Tooltip("In-game day when this record was created.")]
    public int day;
    [Tooltip("In-game hour when this record was created.")]
    public int hour;
    [Tooltip("Absolute in-game hour when this record was created.")]
    public int absoluteHour;

    public RideCompanionCoordinationRecord Clone() {
        return new RideCompanionCoordinationRecord {
            phase = phase,
            rideId = rideId,
            rideName = rideName,
            pokemonId = pokemonId,
            pokemonName = pokemonName,
            totalRiderCapacity = totalRiderCapacity,
            companionCapacity = companionCapacity,
            followingCompanionCount = followingCompanionCount,
            overflowMode = overflowMode,
            policyNote = policyNote,
            keptCompanionIds = keptCompanionIds != null ? new List<string>(keptCompanionIds) : new List<string>(),
            selfTravelCompanionIds = selfTravelCompanionIds != null ? new List<string>(selfTravelCompanionIds) : new List<string>(),
            detachedCompanionIds = detachedCompanionIds != null ? new List<string>(detachedCompanionIds) : new List<string>(),
            day = day,
            hour = hour,
            absoluteHour = absoluteHour
        };
    }
}

[Serializable]
public class RideCompanionPendingReturnRecord {
    [Tooltip("Companion id waiting to return.")]
    public string companionId;
    [Tooltip("Companion display name saved for fallback/debug output.")]
    public string companionName;
    [Tooltip("Ride id that caused this pending return.")]
    public string rideId;
    [Tooltip("Ride display name saved for fallback/debug output.")]
    public string rideName;
    [Tooltip("Absolute in-game hour when this companion may return.")]
    public int dueAbsoluteHour;
    [Tooltip("Reason this companion was detached.")]
    public string reason;

    public RideCompanionPendingReturnRecord Clone() {
        return new RideCompanionPendingReturnRecord {
            companionId = companionId,
            companionName = companionName,
            rideId = rideId,
            rideName = rideName,
            dueAbsoluteHour = dueAbsoluteHour,
            reason = reason
        };
    }
}

[Serializable]
public class PlayerRideCompanionSaveData {
    public List<RideCompanionCoordinationRecord> records;
    public List<RideCompanionPendingReturnRecord> pendingReturns;
}
