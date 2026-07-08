using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CompanionPokemonTeam : MonoBehaviour, ISavable {
    [Header("Roster")]
    [Tooltip("Starting roster used when this companion has no saved Pokemon yet.")]
    [SerializeField] CompanionPokemonRosterDefinition roster;
    [Tooltip("If enabled, runtime Pokemon are created from the roster during Awake when the team is empty.")]
    [SerializeField] bool initializeFromRosterOnAwake = true;
    [Tooltip("If enabled, RestoreState creates roster Pokemon when no saved data exists.")]
    [SerializeField] bool fallbackToRosterWhenSaveMissing = true;
    [Tooltip("Maximum active Pokemon kept in this team. 0 means no limit.")]
    [Min(0)]
    [SerializeField] int maxActivePokemon = 6;

    [Header("Runtime")]
    [Tooltip("Runtime/save list of this companion's Pokemon.")]
    [SerializeField] List<Pokemon> pokemon = new List<Pokemon>();

    public CompanionPokemonRosterDefinition Roster => roster;
    public IReadOnlyList<Pokemon> Pokemon => pokemon;
    public event Action OnTeamChanged;

    void Awake() {
        if(initializeFromRosterOnAwake) {
            EnsureInitializedFromRoster();
        }
    }

    public void EnsureInitializedFromRoster() {
        if(pokemon != null && pokemon.Count > 0) {
            return;
        }

        ResetFromRoster();
    }

    public void ResetFromRoster() {
        pokemon = roster != null ? roster.CreatePokemon() : new List<Pokemon>();
        EnforceMaxActivePokemon();
        OnTeamChanged?.Invoke();
    }

    public void SetRoster(CompanionPokemonRosterDefinition newRoster, bool resetTeam) {
        roster = newRoster;
        if(resetTeam) {
            ResetFromRoster();
        }
    }

    public bool AddPokemon(Pokemon newPokemon) {
        if(newPokemon == null) {
            return false;
        }

        if(pokemon == null) {
            pokemon = new List<Pokemon>();
        }

        if(maxActivePokemon > 0 && pokemon.Count >= maxActivePokemon) {
            return false;
        }

        pokemon.Add(newPokemon);
        OnTeamChanged?.Invoke();
        return true;
    }

    public bool RemovePokemon(Pokemon target) {
        if(target == null || pokemon == null) {
            return false;
        }

        bool removed = pokemon.Remove(target);
        if(removed) {
            OnTeamChanged?.Invoke();
        }
        return removed;
    }

    public Pokemon GetFirstHealthyPokemon() {
        EnsureInitializedFromRoster();
        return pokemon.FirstOrDefault(entry => entry != null && entry.HP > 0);
    }

    public List<Pokemon> GetHealthyPokemon(int count = 0) {
        EnsureInitializedFromRoster();
        var query = pokemon.Where(entry => entry != null && entry.HP > 0);
        if(count > 0) {
            query = query.Take(count);
        }
        return query.ToList();
    }

    public Pokemon FindUsableForRide(RidePokemonDefinition ride, out string failureMessage) {
        EnsureInitializedFromRoster();
        if(ride == null) {
            failureMessage = "Ride definition is missing.";
            return null;
        }

        foreach(var candidate in pokemon) {
            if(candidate != null && ride.CanUsePokemon(candidate, out _)) {
                failureMessage = null;
                return candidate;
            }
        }

        failureMessage = "No companion Pokemon can support this ride.";
        return null;
    }

    public Pokemon FindMatchingPokemon(
        IEnumerable<PokemonBase> allowedPokemon,
        IEnumerable<PokemonType> allowedTypes,
        MoveBase requiredMove,
        int minimumLevel,
        bool requireHealthy,
        out string failureMessage
    ) {
        EnsureInitializedFromRoster();

        var species = allowedPokemon != null ? allowedPokemon.Where(entry => entry != null).ToList() : new List<PokemonBase>();
        var types = allowedTypes != null ? allowedTypes.ToList() : new List<PokemonType>();
        int minLevel = Mathf.Max(1, minimumLevel);

        foreach(var candidate in pokemon) {
            if(candidate == null || candidate.Base == null) {
                continue;
            }

            if(requireHealthy && candidate.HP <= 0) {
                continue;
            }

            if(candidate.Level < minLevel) {
                continue;
            }

            if(species.Count > 0 && !species.Contains(candidate.OriginalBase)) {
                continue;
            }

            if(types.Count > 0 && !types.Any(type => candidate.HasType(type))) {
                continue;
            }

            if(requiredMove != null && !candidate.HasMove(requiredMove)) {
                continue;
            }

            failureMessage = null;
            return candidate;
        }

        failureMessage = "No companion Pokemon matches the requested filter.";
        return null;
    }

    public Pokemon ResolveByInstanceId(string instanceId) {
        if(string.IsNullOrWhiteSpace(instanceId) || pokemon == null) {
            return null;
        }

        return pokemon.FirstOrDefault(entry => entry != null && entry.InstanceId == instanceId);
    }

    public void HealAll() {
        EnsureInitializedFromRoster();
        foreach(var entry in pokemon) {
            entry?.Heal();
        }
        OnTeamChanged?.Invoke();
    }

    void EnforceMaxActivePokemon() {
        if(maxActivePokemon <= 0 || pokemon == null) {
            return;
        }

        while(pokemon.Count > maxActivePokemon) {
            pokemon.RemoveAt(pokemon.Count - 1);
        }
    }

    public object CaptureState() {
        EnsureInitializedFromRoster();
        return new CompanionPokemonTeamSaveData {
            rosterId = roster != null ? roster.Id : string.Empty,
            pokemon = pokemon.Where(entry => entry != null).Select(entry => entry.GetSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as CompanionPokemonTeamSaveData;
        if(saveData == null || saveData.pokemon == null) {
            pokemon = new List<Pokemon>();
            if(fallbackToRosterWhenSaveMissing) {
                ResetFromRoster();
            }
            return;
        }

        pokemon = saveData.pokemon.Where(entry => entry != null).Select(entry => new Pokemon(entry)).ToList();
        EnforceMaxActivePokemon();
        if(pokemon.Count == 0 && fallbackToRosterWhenSaveMissing) {
            ResetFromRoster();
        } else {
            OnTeamChanged?.Invoke();
        }
    }
}

[Serializable]
public class CompanionPokemonTeamSaveData {
    public string rosterId;
    public List<PokemonSaveData> pokemon;
}
