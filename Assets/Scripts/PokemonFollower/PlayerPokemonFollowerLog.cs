using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokemonFollowerSelectionMode {
    Disabled,
    PartySlot,
    FirstPartyPokemon,
    FirstHealthyPokemon
}

public class PlayerPokemonFollowerLog : MonoBehaviour, ISavable {
    [Tooltip("Current saved follower selection mode.")]
    [SerializeField] PokemonFollowerSelectionMode selectionMode = PokemonFollowerSelectionMode.PartySlot;
    [Tooltip("Current saved party slot used when Selection Mode is Party Slot.")]
    [SerializeField] int partySlotIndex;
    [Tooltip("Pokemon instance id that most recently followed the player.")]
    [SerializeField] string activePokemonId;
    [Tooltip("Runtime/save history of follower changes and blocked attempts.")]
    [SerializeField] List<PokemonFollowerRecord> records = new List<PokemonFollowerRecord>();

    public PokemonFollowerSelectionMode SelectionMode => selectionMode;
    public int PartySlotIndex => partySlotIndex;
    public string ActivePokemonId => activePokemonId;
    public IReadOnlyList<PokemonFollowerRecord> Records => records;
    public event Action OnFollowerLogChanged;

    public void SaveSelection(PokemonFollowerSelectionMode mode, int slotIndex, Pokemon pokemon) {
        selectionMode = mode;
        partySlotIndex = Mathf.Max(0, slotIndex);
        activePokemonId = pokemon != null ? pokemon.InstanceId : null;
        OnFollowerLogChanged?.Invoke();
    }

    public void RecordStarted(Pokemon pokemon, PokemonFollowerVisualDefinition definition, string sourceId) {
        Record("started", pokemon, definition, sourceId, null);
    }

    public void RecordStopped(Pokemon pokemon, PokemonFollowerVisualDefinition definition, string sourceId) {
        Record("stopped", pokemon, definition, sourceId, null);
    }

    public void RecordBlocked(Pokemon pokemon, PokemonFollowerVisualDefinition definition, string sourceId, string reason) {
        Record("blocked", pokemon, definition, sourceId, reason);
    }

    public IEnumerable<PokemonFollowerRecord> GetRecent(int count = 20) {
        return records
            .Where(record => record != null)
            .OrderByDescending(record => record.absoluteHour)
            .Take(Mathf.Max(1, count));
    }

    void Record(string phase, Pokemon pokemon, PokemonFollowerVisualDefinition definition, string sourceId, string reason) {
        records.Add(new PokemonFollowerRecord {
            phase = phase,
            pokemonId = pokemon != null ? pokemon.InstanceId : null,
            pokemonName = pokemon != null ? pokemon.NickName : string.Empty,
            followerDefinitionId = definition != null ? definition.Id : null,
            followerDefinitionName = definition != null ? definition.DisplayName : string.Empty,
            sourceId = sourceId,
            reason = reason,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0
        });
        OnFollowerLogChanged?.Invoke();
    }

    public object CaptureState() {
        return new PlayerPokemonFollowerSaveData {
            selectionMode = selectionMode,
            partySlotIndex = partySlotIndex,
            activePokemonId = activePokemonId,
            records = records.Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerPokemonFollowerSaveData;
        if(saveData == null) {
            return;
        }

        selectionMode = saveData.selectionMode;
        partySlotIndex = saveData.partySlotIndex;
        activePokemonId = saveData.activePokemonId;
        records = saveData.records ?? new List<PokemonFollowerRecord>();
        OnFollowerLogChanged?.Invoke();
    }
}

[Serializable]
public class PokemonFollowerRecord {
    [Tooltip("Follower lifecycle phase, such as started, stopped or blocked.")]
    public string phase;
    [Tooltip("Pokemon instance id involved in this follower record.")]
    public string pokemonId;
    [Tooltip("Pokemon display name saved for fallback/debug output.")]
    public string pokemonName;
    [Tooltip("Follower visual definition id used by this record.")]
    public string followerDefinitionId;
    [Tooltip("Follower visual definition name saved for fallback/debug output.")]
    public string followerDefinitionName;
    [Tooltip("Source id that caused this follower change.")]
    public string sourceId;
    [Tooltip("Reason used for blocked/stop records.")]
    public string reason;
    [Tooltip("In-game day when this record was created.")]
    public int day;
    [Tooltip("In-game hour when this record was created.")]
    public int hour;
    [Tooltip("Absolute in-game hour when this record was created.")]
    public int absoluteHour;

    public PokemonFollowerRecord Clone() {
        return new PokemonFollowerRecord {
            phase = phase,
            pokemonId = pokemonId,
            pokemonName = pokemonName,
            followerDefinitionId = followerDefinitionId,
            followerDefinitionName = followerDefinitionName,
            sourceId = sourceId,
            reason = reason,
            day = day,
            hour = hour,
            absoluteHour = absoluteHour
        };
    }
}

[Serializable]
public class PlayerPokemonFollowerSaveData {
    public PokemonFollowerSelectionMode selectionMode;
    public int partySlotIndex;
    public string activePokemonId;
    public List<PokemonFollowerRecord> records;
}
