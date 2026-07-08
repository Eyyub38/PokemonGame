using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EncounterLogCountType {
    Seen,
    BattleStarted,
    Captured,
    StealthCaptured,
    ResolvedPeacefully,
    ResolutionFailed
}

public class PlayerEncounterLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save list of encounter history entries.")]
    [SerializeField] List<PlayerEncounterLogEntry> entries = new List<PlayerEncounterLogEntry>();
    [Tooltip("Runtime/save list of non-battle encounter resolution attempts.")]
    [SerializeField] List<PlayerEncounterResolutionRecord> resolutionHistory = new List<PlayerEncounterResolutionRecord>();
    [Tooltip("Maximum non-battle resolution records kept in save data.")]
    [Min(1)]
    [SerializeField] int maxResolutionHistory = 100;

    public IReadOnlyList<PlayerEncounterLogEntry> Entries => entries;
    public IReadOnlyList<PlayerEncounterResolutionRecord> ResolutionHistory => resolutionHistory;
    public event Action OnEncounterLogChanged;

    public void RecordSeen(Pokemon pokemon, EncounterSourceType sourceType, EncounterTableDefinition table = null) {
        AddCount(pokemon, sourceType, table, seen: 1);
    }

    public void RecordBattleStarted(Pokemon pokemon, EncounterSourceType sourceType, EncounterTableDefinition table = null) {
        AddCount(pokemon, sourceType, table, battleStarted: 1);
    }

    public void RecordCaptured(Pokemon pokemon, EncounterSourceType sourceType, EncounterTableDefinition table = null, bool stealth = false) {
        AddCount(pokemon, sourceType, table, captured: 1, stealthCaptured: stealth ? 1 : 0);
    }

    public void RecordResolutionAttempt(Pokemon pokemon, EncounterSourceType sourceType, EncounterTableDefinition table, string resolutionId, string resolutionName, bool success, float chancePercent, string message) {
        AddCount(pokemon, sourceType, table, resolvedPeacefully: success ? 1 : 0, resolutionFailed: success ? 0 : 1);

        if(pokemon == null || pokemon.Base == null) {
            return;
        }

        resolutionHistory.Add(new PlayerEncounterResolutionRecord {
            pokemonId = pokemon.Base.name,
            pokemonName = pokemon.Base.Name,
            tableId = table != null ? table.Id : string.Empty,
            tableName = table != null ? table.DisplayName : string.Empty,
            sourceType = sourceType,
            resolutionId = resolutionId,
            resolutionName = resolutionName,
            success = success,
            chancePercent = chancePercent,
            message = message,
            utcTimestamp = DateTime.UtcNow.ToString("O")
        });

        if(resolutionHistory.Count > maxResolutionHistory) {
            resolutionHistory.RemoveRange(0, resolutionHistory.Count - maxResolutionHistory);
        }

        OnEncounterLogChanged?.Invoke();
    }

    public int GetCount(PokemonBase pokemon, EncounterSourceType sourceType, EncounterLogCountType countType) {
        string pokemonId = pokemon != null ? pokemon.name : null;
        return GetCount(pokemonId, sourceType, countType);
    }

    public int GetCount(string pokemonId, EncounterSourceType sourceType, EncounterLogCountType countType) {
        if(string.IsNullOrWhiteSpace(pokemonId)) {
            return 0;
        }

        return entries
            .Where(e => e != null && e.pokemonId == pokemonId)
            .Where(e => sourceType == EncounterSourceType.Any || e.sourceType == sourceType)
            .Sum(e => e.GetCount(countType));
    }

    void AddCount(Pokemon pokemon, EncounterSourceType sourceType, EncounterTableDefinition table, int seen = 0, int battleStarted = 0, int captured = 0, int stealthCaptured = 0, int resolvedPeacefully = 0, int resolutionFailed = 0) {
        if(pokemon == null || pokemon.Base == null) {
            return;
        }

        string tableId = table != null ? table.Id : string.Empty;
        string pokemonId = pokemon.Base.name;
        var entry = entries.FirstOrDefault(e => e != null
            && e.pokemonId == pokemonId
            && e.sourceType == sourceType
            && e.tableId == tableId);

        if(entry == null) {
            entry = new PlayerEncounterLogEntry {
                pokemonId = pokemonId,
                pokemonName = pokemon.Base.Name,
                tableId = tableId,
                tableName = table != null ? table.DisplayName : string.Empty,
                sourceType = sourceType
            };
            entries.Add(entry);
        }

        entry.seenCount += Mathf.Max(0, seen);
        entry.battleStartedCount += Mathf.Max(0, battleStarted);
        entry.capturedCount += Mathf.Max(0, captured);
        entry.stealthCapturedCount += Mathf.Max(0, stealthCaptured);
        entry.resolvedPeacefullyCount += Mathf.Max(0, resolvedPeacefully);
        entry.resolutionFailedCount += Mathf.Max(0, resolutionFailed);
        RecordPokeNavKnowledge(pokemon.Base, sourceType, table, seen, battleStarted, captured, stealthCaptured);
        OnEncounterLogChanged?.Invoke();
    }

    void RecordPokeNavKnowledge(PokemonBase pokemon, EncounterSourceType sourceType, EncounterTableDefinition table, int seen, int battleStarted, int captured, int stealthCaptured) {
        var pokeNav = GetComponent<PlayerPokeNavLog>();
        if(pokeNav == null || pokemon == null) {
            return;
        }

        if(captured > 0 || stealthCaptured > 0) {
            pokeNav.RecordPokemonEncounter(pokemon, sourceType, table, PokemonKnowledgeLevel.Caught);
        } else if(battleStarted > 0) {
            pokeNav.RecordPokemonEncounter(pokemon, sourceType, table, PokemonKnowledgeLevel.Battled);
        } else if(seen > 0) {
            pokeNav.RecordPokemonEncounter(pokemon, sourceType, table, PokemonKnowledgeLevel.Seen);
        }
    }

    public object CaptureState() {
        return new PlayerEncounterLogSaveData {
            entries = entries.Where(e => e != null).Select(e => new PlayerEncounterLogEntrySaveData {
                pokemonId = e.pokemonId,
                pokemonName = e.pokemonName,
                tableId = e.tableId,
                tableName = e.tableName,
                sourceType = e.sourceType,
                seenCount = e.seenCount,
                battleStartedCount = e.battleStartedCount,
                capturedCount = e.capturedCount,
                stealthCapturedCount = e.stealthCapturedCount,
                resolvedPeacefullyCount = e.resolvedPeacefullyCount,
                resolutionFailedCount = e.resolutionFailedCount
            }).ToList(),
            resolutionHistory = resolutionHistory.Where(record => record != null).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerEncounterLogSaveData;
        entries = saveData?.entries?.Where(e => e != null).Select(e => new PlayerEncounterLogEntry {
            pokemonId = e.pokemonId,
            pokemonName = e.pokemonName,
            tableId = e.tableId,
            tableName = e.tableName,
            sourceType = e.sourceType,
            seenCount = Mathf.Max(0, e.seenCount),
            battleStartedCount = Mathf.Max(0, e.battleStartedCount),
            capturedCount = Mathf.Max(0, e.capturedCount),
            stealthCapturedCount = Mathf.Max(0, e.stealthCapturedCount),
            resolvedPeacefullyCount = Mathf.Max(0, e.resolvedPeacefullyCount),
            resolutionFailedCount = Mathf.Max(0, e.resolutionFailedCount)
        }).ToList() ?? new List<PlayerEncounterLogEntry>();
        resolutionHistory = saveData?.resolutionHistory?.Where(record => record != null).ToList() ?? new List<PlayerEncounterResolutionRecord>();
        OnEncounterLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerEncounterLogEntry {
    [Tooltip("Saved Pokemon asset id/name.")]
    public string pokemonId;
    [Tooltip("Saved Pokemon display name.")]
    public string pokemonName;
    [Tooltip("Encounter table id that produced this entry.")]
    public string tableId;
    [Tooltip("Encounter table display name.")]
    public string tableName;
    [Tooltip("Encounter source that produced this entry.")]
    public EncounterSourceType sourceType;
    [Tooltip("How many times this Pokemon was seen.")]
    [Min(0)]
    public int seenCount;
    [Tooltip("How many times this Pokemon started a battle.")]
    [Min(0)]
    public int battleStartedCount;
    [Tooltip("How many times this Pokemon was captured.")]
    [Min(0)]
    public int capturedCount;
    [Tooltip("How many times this Pokemon was captured without battle.")]
    [Min(0)]
    public int stealthCapturedCount;
    [Tooltip("How many times this Pokemon was resolved peacefully without battle.")]
    [Min(0)]
    public int resolvedPeacefullyCount;
    [Tooltip("How many non-battle resolution attempts failed for this Pokemon.")]
    [Min(0)]
    public int resolutionFailedCount;

    public int GetCount(EncounterLogCountType countType) {
        return countType switch {
            EncounterLogCountType.BattleStarted => battleStartedCount,
            EncounterLogCountType.Captured => capturedCount,
            EncounterLogCountType.StealthCaptured => stealthCapturedCount,
            EncounterLogCountType.ResolvedPeacefully => resolvedPeacefullyCount,
            EncounterLogCountType.ResolutionFailed => resolutionFailedCount,
            _ => seenCount
        };
    }
}

[Serializable]
public class PlayerEncounterResolutionRecord {
    [Tooltip("Saved Pokemon asset id/name.")]
    public string pokemonId;
    [Tooltip("Saved Pokemon display name.")]
    public string pokemonName;
    [Tooltip("Encounter table id that produced this record.")]
    public string tableId;
    [Tooltip("Encounter table display name.")]
    public string tableName;
    [Tooltip("Encounter source that produced this record.")]
    public EncounterSourceType sourceType;
    [Tooltip("Resolution definition id used by this attempt.")]
    public string resolutionId;
    [Tooltip("Resolution definition display name used by this attempt.")]
    public string resolutionName;
    [Tooltip("If enabled, the non-battle resolution succeeded.")]
    public bool success;
    [Tooltip("Final success chance used by this attempt.")]
    public float chancePercent;
    [Tooltip("Result message shown or logged for this attempt.")]
    public string message;
    [Tooltip("UTC timestamp recorded when the attempt happened.")]
    public string utcTimestamp;
}

[Serializable]
public class PlayerEncounterLogSaveData {
    public List<PlayerEncounterLogEntrySaveData> entries;
    public List<PlayerEncounterResolutionRecord> resolutionHistory;
}

[Serializable]
public class PlayerEncounterLogEntrySaveData {
    public string pokemonId;
    public string pokemonName;
    public string tableId;
    public string tableName;
    public EncounterSourceType sourceType;
    public int seenCount;
    public int battleStartedCount;
    public int capturedCount;
    public int stealthCapturedCount;
    public int resolvedPeacefullyCount;
    public int resolutionFailedCount;
}
