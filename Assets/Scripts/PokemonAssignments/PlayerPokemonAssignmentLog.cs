using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPokemonAssignmentLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save list of active Pokemon assignments.")]
    [SerializeField] List<PlayerPokemonAssignmentState> activeAssignments = new List<PlayerPokemonAssignmentState>();
    [Tooltip("Runtime/save history of claimed Pokemon assignments.")]
    [SerializeField] List<PlayerPokemonAssignmentHistory> assignmentHistory = new List<PlayerPokemonAssignmentHistory>();

    public IReadOnlyList<PlayerPokemonAssignmentState> ActiveAssignments => activeAssignments;
    public IReadOnlyList<PlayerPokemonAssignmentHistory> AssignmentHistory => assignmentHistory;
    public event Action OnPokemonAssignmentsChanged;
    public event Action<PlayerPokemonAssignmentHistory> OnPokemonAssignmentClaimed;

    public bool HasActiveAssignment(PokemonAssignmentDefinition assignment, string sourceId = null) {
        return assignment != null && activeAssignments.Any(state => state != null
            && state.assignmentId == assignment.Id
            && (string.IsNullOrWhiteSpace(sourceId) || state.sourceId == sourceId));
    }

    public bool HasActiveAssignmentForPokemon(string pokemonKey) {
        return !string.IsNullOrWhiteSpace(pokemonKey)
            && activeAssignments.Any(state => state != null && state.pokemonKey == pokemonKey);
    }

    public List<PlayerPokemonAssignmentState> GetReadyAssignments(PokemonAssignmentDefinition assignment = null, string sourceId = null) {
        return activeAssignments
            .Where(state => state != null
                && state.IsReady()
                && (assignment == null || state.assignmentId == assignment.Id)
                && (string.IsNullOrWhiteSpace(sourceId) || state.sourceId == sourceId))
            .ToList();
    }

    public int GetCompletedCount(PokemonAssignmentDefinition assignment, string sourceId = null, bool? success = null) {
        if(assignment == null) {
            return 0;
        }

        return assignmentHistory.Count(history => history != null
            && history.assignmentId == assignment.Id
            && (string.IsNullOrWhiteSpace(sourceId) || history.sourceId == sourceId)
            && (!success.HasValue || history.success == success.Value));
    }

    public bool CanStart(PokemonAssignmentDefinition assignment, string sourceId, PokemonAssignmentRepeatMode repeatMode, int cooldownHours, int maxClaimCount, out string failureMessage) {
        if(assignment == null) {
            failureMessage = "No Pokemon assignment selected.";
            return false;
        }

        if(maxClaimCount > 0 && GetCompletedCount(assignment, sourceId) >= maxClaimCount) {
            failureMessage = $"{assignment.DisplayName} has reached its completion limit.";
            return false;
        }

        var history = GetLatestHistory(assignment.Id, sourceId);
        if(repeatMode == PokemonAssignmentRepeatMode.Once && history != null) {
            failureMessage = $"{assignment.DisplayName} has already been completed.";
            return false;
        }

        if(repeatMode == PokemonAssignmentRepeatMode.Daily && history != null && history.claimedDay == GetCurrentDay()) {
            failureMessage = $"{assignment.DisplayName} can only be completed once per day.";
            return false;
        }

        if(repeatMode == PokemonAssignmentRepeatMode.CooldownHours && history != null && history.claimedAbsoluteHour >= 0) {
            int elapsed = GetCurrentAbsoluteHour() - history.claimedAbsoluteHour;
            if(elapsed < Mathf.Max(0, cooldownHours)) {
                failureMessage = $"{assignment.DisplayName} will be available again in {cooldownHours - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool TryStart(PlayerController player, PokemonAssignmentDefinition assignment, Pokemon pokemon, ActivityZoneDefinition zone, string sourceId, string sourceName, out string failureMessage) {
        if(assignment == null) {
            failureMessage = "No Pokemon assignment selected.";
            return false;
        }

        if(!assignment.CanStart(player, pokemon, this, zone, sourceId, out failureMessage)) {
            return false;
        }

        if(assignment.StartActivity != null && !assignment.StartActivity.TryPayCosts(player, out failureMessage)) {
            return false;
        }

        int currentHour = GetCurrentAbsoluteHour();
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        var state = new PlayerPokemonAssignmentState {
            assignmentId = assignment.Id,
            assignmentName = assignment.DisplayName,
            category = assignment.Category,
            sourceId = sourceId,
            sourceName = string.IsNullOrWhiteSpace(sourceName) ? assignment.DisplayName : sourceName,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            pokemonKey = BuildPokemonKey(pokemon, party),
            pokemonName = pokemon != null ? pokemon.NickName : string.Empty,
            pokemonBaseName = pokemon != null && pokemon.Base != null ? pokemon.Base.Name : string.Empty,
            pokemonLevel = pokemon != null ? pokemon.Level : 0,
            partyIndex = GetPartyIndex(pokemon, party),
            startedDay = GetCurrentDay(),
            startedAbsoluteHour = currentHour,
            readyAbsoluteHour = currentHour + assignment.DurationHours,
            successChance = assignment.GetSuccessChance(pokemon),
            allowAutoClaim = assignment.AllowAutoClaim
        };

        activeAssignments.Add(state);
        assignment.ApplyStarted(player, pokemon, sourceId, this);
        OnPokemonAssignmentsChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public bool TryClaim(PlayerController player, PokemonAssignmentDefinition assignment, PlayerPokemonAssignmentState state, out string failureMessage) {
        return TryClaim(player, assignment, state, out _, out failureMessage);
    }

    public bool TryClaim(PlayerController player, PokemonAssignmentDefinition assignment, PlayerPokemonAssignmentState state, out PlayerPokemonAssignmentHistory claimedHistory, out string failureMessage) {
        claimedHistory = null;
        if(assignment == null) {
            failureMessage = "No Pokemon assignment selected.";
            return false;
        }

        if(state == null) {
            state = GetReadyAssignments(assignment).FirstOrDefault();
        }

        if(!assignment.CanClaim(player, state, out failureMessage)) {
            return false;
        }

        if(assignment.ClaimActivity != null && !assignment.ClaimActivity.TryPayCosts(player, out failureMessage)) {
            return false;
        }

        var pokemon = ResolvePokemon(player, state);
        bool success = UnityEngine.Random.value <= Mathf.Clamp01(state.successChance);
        assignment.ApplyClaimed(player, pokemon, state.sourceId, success, state.successChance, state.ResolveZone(), this);

        activeAssignments.Remove(state);
        claimedHistory = PlayerPokemonAssignmentHistory.FromState(state, GetCurrentDay(), GetCurrentAbsoluteHour(), success);
        assignmentHistory.Add(claimedHistory);
        OnPokemonAssignmentClaimed?.Invoke(claimedHistory);
        OnPokemonAssignmentsChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public bool TryClaimFirstReady(PlayerController player, PokemonAssignmentDefinition assignment, string sourceId, out string failureMessage) {
        var state = GetReadyAssignments(assignment, sourceId).FirstOrDefault();
        return TryClaim(player, assignment, state, out failureMessage);
    }

    public int ClaimAllReady(PlayerController player, PokemonAssignmentDefinition assignment = null, string sourceId = null) {
        int claimed = 0;
        foreach(var state in GetReadyAssignments(assignment, sourceId).ToList()) {
            if(assignment == null) {
                continue;
            }

            if(TryClaim(player, assignment, state, out _)) {
                claimed++;
            }
        }
        return claimed;
    }

    public bool TryCancel(PlayerPokemonAssignmentState state) {
        if(state == null || !activeAssignments.Remove(state)) {
            return false;
        }

        OnPokemonAssignmentsChanged?.Invoke();
        return true;
    }

    public string BuildPokemonKey(Pokemon pokemon, PokemonParty party = null) {
        if(pokemon == null) {
            return string.Empty;
        }

        int index = GetPartyIndex(pokemon, party);
        string baseName = pokemon.Base != null ? pokemon.Base.Name : "unknown";
        return index >= 0
            ? $"party:{index}:{baseName}:{pokemon.Level}:{pokemon.NickName}"
            : $"pokemon:{baseName}:{pokemon.Level}:{pokemon.NickName}";
    }

    public Pokemon ResolvePokemon(PlayerController player, PlayerPokemonAssignmentState state) {
        if(player == null || state == null) {
            return null;
        }

        var party = player.GetComponent<PokemonParty>();
        if(party?.Pokemons == null) {
            return null;
        }

        if(state.partyIndex >= 0 && state.partyIndex < party.Pokemons.Count) {
            var indexedPokemon = party.Pokemons[state.partyIndex];
            if(indexedPokemon != null && BuildPokemonKey(indexedPokemon, party) == state.pokemonKey) {
                return indexedPokemon;
            }
        }

        return party.Pokemons.FirstOrDefault(pokemon => pokemon != null && BuildPokemonKey(pokemon, party) == state.pokemonKey)
            ?? party.Pokemons.FirstOrDefault(pokemon => pokemon != null
                && pokemon.Base != null
                && pokemon.Base.Name == state.pokemonBaseName
                && pokemon.NickName == state.pokemonName
                && pokemon.Level == state.pokemonLevel);
    }

    PlayerPokemonAssignmentHistory GetLatestHistory(string assignmentId, string sourceId) {
        if(string.IsNullOrWhiteSpace(assignmentId)) {
            return null;
        }

        return assignmentHistory
            .Where(history => history != null
                && history.assignmentId == assignmentId
                && (string.IsNullOrWhiteSpace(sourceId) || history.sourceId == sourceId))
            .OrderByDescending(history => history.claimedAbsoluteHour)
            .FirstOrDefault();
    }

    int GetPartyIndex(Pokemon pokemon, PokemonParty party) {
        if(pokemon == null || party?.Pokemons == null) {
            return -1;
        }

        return party.Pokemons.IndexOf(pokemon);
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerPokemonAssignmentLogSaveData {
            activeAssignments = activeAssignments.Where(state => state != null).Select(state => state.ToSaveData()).ToList(),
            assignmentHistory = assignmentHistory.Where(history => history != null).Select(history => history.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerPokemonAssignmentLogSaveData;
        activeAssignments = saveData?.activeAssignments?.Where(entry => entry != null).Select(entry => new PlayerPokemonAssignmentState(entry)).ToList()
            ?? new List<PlayerPokemonAssignmentState>();
        assignmentHistory = saveData?.assignmentHistory?.Where(entry => entry != null).Select(entry => new PlayerPokemonAssignmentHistory(entry)).ToList()
            ?? new List<PlayerPokemonAssignmentHistory>();
        OnPokemonAssignmentsChanged?.Invoke();
    }
}

[Serializable]
public class PlayerPokemonAssignmentState {
    [Tooltip("Saved assignment definition id.")]
    public string assignmentId;
    [Tooltip("Saved assignment display name.")]
    public string assignmentName;
    [Tooltip("Saved assignment category.")]
    public PokemonAssignmentCategory category;
    [Tooltip("Board/source id where this assignment started.")]
    public string sourceId;
    [Tooltip("Board/source name where this assignment started.")]
    public string sourceName;
    [Tooltip("Activity zone id where this assignment started.")]
    public string zoneId;
    [Tooltip("Activity zone name where this assignment started.")]
    public string zoneName;
    [Tooltip("Runtime save key used to find the assigned party Pokemon again.")]
    public string pokemonKey;
    [Tooltip("Saved Pokemon nickname/display name.")]
    public string pokemonName;
    [Tooltip("Saved Pokemon base/species name.")]
    public string pokemonBaseName;
    [Tooltip("Saved Pokemon level at assignment start.")]
    public int pokemonLevel;
    [Tooltip("Party slot index captured when the assignment started.")]
    public int partyIndex = -1;
    [Tooltip("In-game day when this assignment started.")]
    public int startedDay;
    [Tooltip("Absolute in-game hour when this assignment started.")]
    public int startedAbsoluteHour;
    [Tooltip("Absolute in-game hour when this assignment can be claimed.")]
    public int readyAbsoluteHour;
    [Tooltip("Captured success chance used when this assignment is claimed.")]
    [Range(0f, 1f)]
    public float successChance;
    [Tooltip("If enabled, future controllers may auto-claim this state when it becomes ready.")]
    public bool allowAutoClaim;

    public PlayerPokemonAssignmentState() {
    }

    public PlayerPokemonAssignmentState(PlayerPokemonAssignmentStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        assignmentId = saveData.assignmentId;
        assignmentName = saveData.assignmentName;
        category = saveData.category;
        sourceId = saveData.sourceId;
        sourceName = saveData.sourceName;
        zoneId = saveData.zoneId;
        zoneName = saveData.zoneName;
        pokemonKey = saveData.pokemonKey;
        pokemonName = saveData.pokemonName;
        pokemonBaseName = saveData.pokemonBaseName;
        pokemonLevel = saveData.pokemonLevel;
        partyIndex = saveData.partyIndex;
        startedDay = saveData.startedDay;
        startedAbsoluteHour = saveData.startedAbsoluteHour;
        readyAbsoluteHour = saveData.readyAbsoluteHour;
        successChance = Mathf.Clamp01(saveData.successChance);
        allowAutoClaim = saveData.allowAutoClaim;
    }

    public bool IsReady() {
        int currentHour = TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
        return currentHour >= readyAbsoluteHour;
    }

    public ActivityZoneDefinition ResolveZone() {
        if(string.IsNullOrWhiteSpace(zoneId)) {
            return null;
        }

        return Resources.FindObjectsOfTypeAll<ActivityZoneDefinition>()
            .FirstOrDefault(zone => zone != null && zone.Id == zoneId);
    }

    public PlayerPokemonAssignmentStateSaveData ToSaveData() {
        return new PlayerPokemonAssignmentStateSaveData {
            assignmentId = assignmentId,
            assignmentName = assignmentName,
            category = category,
            sourceId = sourceId,
            sourceName = sourceName,
            zoneId = zoneId,
            zoneName = zoneName,
            pokemonKey = pokemonKey,
            pokemonName = pokemonName,
            pokemonBaseName = pokemonBaseName,
            pokemonLevel = pokemonLevel,
            partyIndex = partyIndex,
            startedDay = startedDay,
            startedAbsoluteHour = startedAbsoluteHour,
            readyAbsoluteHour = readyAbsoluteHour,
            successChance = successChance,
            allowAutoClaim = allowAutoClaim
        };
    }
}

[Serializable]
public class PlayerPokemonAssignmentHistory {
    [Tooltip("Saved assignment definition id.")]
    public string assignmentId;
    [Tooltip("Saved assignment display name.")]
    public string assignmentName;
    [Tooltip("Board/source id where this assignment started.")]
    public string sourceId;
    [Tooltip("Board/source name where this assignment started.")]
    public string sourceName;
    [Tooltip("Activity zone id where this assignment started.")]
    public string zoneId;
    [Tooltip("Saved Pokemon assignment key.")]
    public string pokemonKey;
    [Tooltip("Saved Pokemon nickname/display name.")]
    public string pokemonName;
    [Tooltip("Saved Pokemon base/species name.")]
    public string pokemonBaseName;
    [Tooltip("In-game day when this assignment started.")]
    public int startedDay;
    [Tooltip("Absolute in-game hour when this assignment started.")]
    public int startedAbsoluteHour;
    [Tooltip("In-game day when this assignment was claimed.")]
    public int claimedDay;
    [Tooltip("Absolute in-game hour when this assignment was claimed.")]
    public int claimedAbsoluteHour;
    [Tooltip("If enabled, the assignment succeeded.")]
    public bool success;
    [Tooltip("Captured success chance used for this result.")]
    [Range(0f, 1f)]
    public float successChance;

    public PlayerPokemonAssignmentHistory() {
    }

    public PlayerPokemonAssignmentHistory(PlayerPokemonAssignmentHistorySaveData saveData) {
        if(saveData == null) {
            return;
        }

        assignmentId = saveData.assignmentId;
        assignmentName = saveData.assignmentName;
        sourceId = saveData.sourceId;
        sourceName = saveData.sourceName;
        zoneId = saveData.zoneId;
        pokemonKey = saveData.pokemonKey;
        pokemonName = saveData.pokemonName;
        pokemonBaseName = saveData.pokemonBaseName;
        startedDay = saveData.startedDay;
        startedAbsoluteHour = saveData.startedAbsoluteHour;
        claimedDay = saveData.claimedDay;
        claimedAbsoluteHour = saveData.claimedAbsoluteHour;
        success = saveData.success;
        successChance = Mathf.Clamp01(saveData.successChance);
    }

    public static PlayerPokemonAssignmentHistory FromState(PlayerPokemonAssignmentState state, int claimedDay, int claimedAbsoluteHour, bool success) {
        return new PlayerPokemonAssignmentHistory {
            assignmentId = state.assignmentId,
            assignmentName = state.assignmentName,
            sourceId = state.sourceId,
            sourceName = state.sourceName,
            zoneId = state.zoneId,
            pokemonKey = state.pokemonKey,
            pokemonName = state.pokemonName,
            pokemonBaseName = state.pokemonBaseName,
            startedDay = state.startedDay,
            startedAbsoluteHour = state.startedAbsoluteHour,
            claimedDay = claimedDay,
            claimedAbsoluteHour = claimedAbsoluteHour,
            success = success,
            successChance = state.successChance
        };
    }

    public PlayerPokemonAssignmentHistorySaveData ToSaveData() {
        return new PlayerPokemonAssignmentHistorySaveData {
            assignmentId = assignmentId,
            assignmentName = assignmentName,
            sourceId = sourceId,
            sourceName = sourceName,
            zoneId = zoneId,
            pokemonKey = pokemonKey,
            pokemonName = pokemonName,
            pokemonBaseName = pokemonBaseName,
            startedDay = startedDay,
            startedAbsoluteHour = startedAbsoluteHour,
            claimedDay = claimedDay,
            claimedAbsoluteHour = claimedAbsoluteHour,
            success = success,
            successChance = successChance
        };
    }
}

[Serializable]
public class PlayerPokemonAssignmentLogSaveData {
    public List<PlayerPokemonAssignmentStateSaveData> activeAssignments;
    public List<PlayerPokemonAssignmentHistorySaveData> assignmentHistory;
}

[Serializable]
public class PlayerPokemonAssignmentStateSaveData {
    public string assignmentId;
    public string assignmentName;
    public PokemonAssignmentCategory category;
    public string sourceId;
    public string sourceName;
    public string zoneId;
    public string zoneName;
    public string pokemonKey;
    public string pokemonName;
    public string pokemonBaseName;
    public int pokemonLevel;
    public int partyIndex;
    public int startedDay;
    public int startedAbsoluteHour;
    public int readyAbsoluteHour;
    public float successChance;
    public bool allowAutoClaim;
}

[Serializable]
public class PlayerPokemonAssignmentHistorySaveData {
    public string assignmentId;
    public string assignmentName;
    public string sourceId;
    public string sourceName;
    public string zoneId;
    public string pokemonKey;
    public string pokemonName;
    public string pokemonBaseName;
    public int startedDay;
    public int startedAbsoluteHour;
    public int claimedDay;
    public int claimedAbsoluteHour;
    public bool success;
    public float successChance;
}
