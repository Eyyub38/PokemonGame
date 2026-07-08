using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerRideLog : MonoBehaviour, ISavable {
    [Tooltip("Current active ride state. Empty when the player is not mounted.")]
    [SerializeField] PlayerRideState activeRide;
    [Tooltip("Saved ride mount/dismount history.")]
    [SerializeField] List<PlayerRideRecord> rideHistory = new List<PlayerRideRecord>();

    public PlayerRideState ActiveRide => activeRide;
    public IReadOnlyList<PlayerRideRecord> RideHistory => rideHistory;
    public bool HasActiveRide => activeRide != null && activeRide.isActive;
    public event Action<PlayerRideState> OnRideMounted;
    public event Action<PlayerRideRecord> OnRideDismounted;

    public void RecordMount(RidePokemonDefinition ride, Pokemon pokemon, string sourceId) {
        if(ride == null) {
            return;
        }

        int now = GetCurrentAbsoluteHour();
        activeRide = new PlayerRideState {
            isActive = true,
            rideId = ride.Id,
            rideName = ride.DisplayName,
            rideMode = ride.RideMode,
            pokemonInstanceId = pokemon != null ? pokemon.InstanceId : string.Empty,
            pokemonName = pokemon != null ? pokemon.NickName : string.Empty,
            sourceId = sourceId,
            mountedAtHour = now
        };

        rideHistory.Add(new PlayerRideRecord {
            rideId = activeRide.rideId,
            rideName = activeRide.rideName,
            rideMode = activeRide.rideMode,
            pokemonInstanceId = activeRide.pokemonInstanceId,
            pokemonName = activeRide.pokemonName,
            sourceId = sourceId,
            mountedAtHour = now,
            dismountedAtHour = -1,
            completed = false
        });

        OnRideMounted?.Invoke(activeRide);
    }

    public void RecordBlocked(RidePokemonDefinition ride, Pokemon pokemon, string sourceId, string failureMessage) {
        rideHistory.Add(new PlayerRideRecord {
            rideId = ride != null ? ride.Id : string.Empty,
            rideName = ride != null ? ride.DisplayName : string.Empty,
            rideMode = ride != null ? ride.RideMode : PokemonRideMode.Custom,
            pokemonInstanceId = pokemon != null ? pokemon.InstanceId : string.Empty,
            pokemonName = pokemon != null ? pokemon.NickName : string.Empty,
            sourceId = sourceId,
            mountedAtHour = GetCurrentAbsoluteHour(),
            dismountedAtHour = GetCurrentAbsoluteHour(),
            completed = false,
            blocked = true,
            failureMessage = failureMessage
        });
    }

    public PlayerRideRecord RecordDismount(string sourceId, string reason) {
        if(activeRide == null || !activeRide.isActive) {
            return null;
        }

        int now = GetCurrentAbsoluteHour();
        var record = rideHistory.LastOrDefault(entry => entry != null
            && !entry.completed
            && !entry.blocked
            && entry.rideId == activeRide.rideId
            && entry.pokemonInstanceId == activeRide.pokemonInstanceId);

        if(record == null) {
            record = new PlayerRideRecord {
                rideId = activeRide.rideId,
                rideName = activeRide.rideName,
                rideMode = activeRide.rideMode,
                pokemonInstanceId = activeRide.pokemonInstanceId,
                pokemonName = activeRide.pokemonName,
                sourceId = sourceId,
                mountedAtHour = activeRide.mountedAtHour
            };
            rideHistory.Add(record);
        }

        record.completed = true;
        record.dismountedAtHour = now;
        record.durationHours = Mathf.Max(0, now - activeRide.mountedAtHour);
        record.reason = reason;

        activeRide.isActive = false;
        activeRide = null;
        OnRideDismounted?.Invoke(record);
        return record;
    }

    public bool HasMountedRide(RidePokemonDefinition ride = null) {
        return rideHistory.Any(record => record != null
            && !record.blocked
            && (ride == null || record.rideId == ride.Id));
    }

    public int GetMountCount(RidePokemonDefinition ride = null, bool includeBlocked = false) {
        return rideHistory.Count(record => record != null
            && (includeBlocked || !record.blocked)
            && (ride == null || record.rideId == ride.Id));
    }

    public bool IsRideActive(RidePokemonDefinition ride) {
        return ride != null && activeRide != null && activeRide.isActive && activeRide.rideId == ride.Id;
    }

    public object CaptureState() {
        return new PlayerRideSaveData {
            activeRide = activeRide,
            rideHistory = rideHistory != null ? rideHistory.ToList() : new List<PlayerRideRecord>()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerRideSaveData;
        activeRide = saveData?.activeRide;
        rideHistory = saveData?.rideHistory ?? new List<PlayerRideRecord>();
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }
}

[Serializable]
public class PlayerRideState {
    [Tooltip("If enabled, the player is currently mounted.")]
    public bool isActive;
    [Tooltip("Saved ride definition id.")]
    public string rideId;
    [Tooltip("Saved ride display name for fallback/debug output.")]
    public string rideName;
    [Tooltip("Saved ride mode for filtering and requirements.")]
    public PokemonRideMode rideMode;
    [Tooltip("Saved Pokemon instance id used for this ride.")]
    public string pokemonInstanceId;
    [Tooltip("Saved Pokemon name used for fallback/debug output.")]
    public string pokemonName;
    [Tooltip("Source point/system that started this ride.")]
    public string sourceId;
    [Tooltip("In-game absolute hour when this ride started.")]
    public int mountedAtHour;
}

[Serializable]
public class PlayerRideRecord {
    [Tooltip("Saved ride definition id.")]
    public string rideId;
    [Tooltip("Saved ride display name for fallback/debug output.")]
    public string rideName;
    [Tooltip("Saved ride mode for filtering and requirements.")]
    public PokemonRideMode rideMode;
    [Tooltip("Saved Pokemon instance id used for this ride.")]
    public string pokemonInstanceId;
    [Tooltip("Saved Pokemon name used for fallback/debug output.")]
    public string pokemonName;
    [Tooltip("Source point/system that started this ride.")]
    public string sourceId;
    [Tooltip("In-game absolute hour when this ride started.")]
    public int mountedAtHour;
    [Tooltip("In-game absolute hour when this ride ended. -1 means still active or unknown.")]
    public int dismountedAtHour = -1;
    [Tooltip("Duration in in-game hours, calculated on dismount.")]
    public int durationHours;
    [Tooltip("If enabled, this record completed with a dismount.")]
    public bool completed;
    [Tooltip("If enabled, this record is a blocked mount attempt.")]
    public bool blocked;
    [Tooltip("Optional reason recorded on dismount.")]
    public string reason;
    [Tooltip("Optional failure message recorded when a mount attempt was blocked.")]
    public string failureMessage;
}

[Serializable]
public class PlayerRideSaveData {
    [Tooltip("Saved active ride state.")]
    public PlayerRideState activeRide;
    [Tooltip("Saved ride history records.")]
    public List<PlayerRideRecord> rideHistory = new List<PlayerRideRecord>();
}
