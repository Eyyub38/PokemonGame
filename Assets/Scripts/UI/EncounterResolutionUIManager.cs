using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EncounterResolutionUIManager : MonoBehaviour {
    [Header("Context")]
    [Tooltip("Optional source that provides the Pokemon, encounter table and choice set context.")]
    [SerializeField] EncounterResolutionChoiceSource choiceSource;
    [Tooltip("Fallback choice set used when Choice Source is not assigned.")]
    [SerializeField] EncounterResolutionChoiceSetDefinition fallbackChoiceSet;
    [Tooltip("Fallback exact Pokemon used when Choice Source is not assigned.")]
    [SerializeField] PokemonBase fallbackPokemon;
    [Tooltip("Fallback Pokemon level used when Choice Source is not assigned.")]
    [Min(1)]
    [SerializeField] int fallbackPokemonLevel = 5;
    [Tooltip("Fallback encounter table used when Choice Source is not assigned.")]
    [SerializeField] EncounterTableDefinition fallbackEncounterTable;
    [Tooltip("Fallback source type used when Choice Source is not assigned and no table source is available.")]
    [SerializeField] EncounterSourceType fallbackSourceType = EncounterSourceType.Special;
    [Tooltip("Fallback battle trigger used when a selected resolution starts battle.")]
    [SerializeField] BattleTrigger fallbackBattleTrigger = BattleTrigger.LongGrass;

    [Header("Rows")]
    [Tooltip("If enabled, locked/blocked choices remain visible in the snapshot.")]
    [SerializeField] bool includeBlockedRows = true;
    [Tooltip("Maximum choice rows returned to UI. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxChoiceRows = 12;
    [Tooltip("Maximum history rows returned to UI. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRows = 20;

    [Header("Debug")]
    [Tooltip("If enabled, refresh/run actions write debug messages.")]
    [SerializeField] bool logDebugMessages;

    EncounterResolutionUIScreenSnapshot currentSnapshot = new EncounterResolutionUIScreenSnapshot();
    Pokemon fallbackRuntimePokemon;

    public EncounterResolutionUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public EncounterResolutionChoiceSource ChoiceSource => choiceSource;
    public EncounterResolutionChoiceSetDefinition FallbackChoiceSet => fallbackChoiceSet;
    public PokemonBase FallbackPokemon => fallbackPokemon;
    public EncounterTableDefinition FallbackEncounterTable => fallbackEncounterTable;
    public event Action<EncounterResolutionUIScreenSnapshot> OnSnapshotChanged;
    public event Action<EncounterResolutionResult> OnChoiceResolved;

    void OnEnable() {
        Refresh();
    }

    [ContextMenu("Refresh Encounter Resolution Snapshot")]
    public EncounterResolutionUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public EncounterResolutionUIScreenSnapshot Refresh() {
        var player = PlayerController.i;
        var choiceSnapshot = BuildChoiceSnapshot(player);
        var historyRows = BuildHistoryRows(player).ToList();

        if(choiceSnapshot.rows != null && maxChoiceRows > 0 && choiceSnapshot.rows.Count > maxChoiceRows) {
            choiceSnapshot.rows = choiceSnapshot.rows.Take(maxChoiceRows).ToList();
            choiceSnapshot.rowCount = choiceSnapshot.rows.Count;
            choiceSnapshot.availableRowCount = choiceSnapshot.rows.Count(row => row != null && row.canRun);
            choiceSnapshot.blockedRowCount = choiceSnapshot.rows.Count(row => row != null && !row.canRun);
        }

        currentSnapshot = new EncounterResolutionUIScreenSnapshot {
            choiceSetId = choiceSnapshot.choiceSetId,
            choiceSetName = choiceSnapshot.choiceSetName,
            description = choiceSnapshot.description,
            pokemonName = choiceSnapshot.pokemonName,
            sourceType = choiceSnapshot.sourceType,
            tableId = choiceSnapshot.tableId,
            tableName = choiceSnapshot.tableName,
            rowCount = choiceSnapshot.rowCount,
            availableRowCount = choiceSnapshot.availableRowCount,
            blockedRowCount = choiceSnapshot.blockedRowCount,
            historyCount = historyRows.Count,
            rows = choiceSnapshot.rows ?? new List<EncounterResolutionChoiceRow>(),
            historyRows = historyRows
        };

        if(logDebugMessages) {
            GameDebug.Step($"Encounter resolution snapshot refreshed: {currentSnapshot.rowCount} rows.", GameDebugCategory.Encounter, this, "EncounterResolutionUIManager");
        }

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public EncounterResolutionChoiceRow FindRow(string choiceId) {
        return currentSnapshot?.rows?.FirstOrDefault(row => row != null && string.Equals(row.choiceId, choiceId, StringComparison.OrdinalIgnoreCase));
    }

    public EncounterResolutionResult RunChoice(string choiceId) {
        var player = PlayerController.i;
        EncounterResolutionResult result;
        if(choiceSource != null) {
            result = choiceSource.RunChoice(choiceId, player);
        } else {
            var choiceSet = fallbackChoiceSet;
            var pokemon = ResolveFallbackPokemon();
            result = choiceSet != null && pokemon != null
                ? choiceSet.RunChoice(choiceId, player, pokemon, ResolveFallbackSourceType(), fallbackEncounterTable, ResolveFallbackBattleTrigger(), this)
                : new EncounterResolutionResult {
                    blocked = true,
                    message = "Encounter resolution UI has no source, choice set or Pokemon."
                };
        }

        HandleResult(result);
        return result;
    }

    public EncounterResolutionResult RunFirstAvailable() {
        var first = currentSnapshot?.rows?.FirstOrDefault(row => row != null && row.canRun);
        if(first == null) {
            var result = new EncounterResolutionResult {
                blocked = true,
                message = "No encounter resolution choice is available."
            };
            HandleResult(result);
            return result;
        }

        return RunChoice(first.choiceId);
    }

    EncounterResolutionChoiceSnapshot BuildChoiceSnapshot(PlayerController player) {
        if(choiceSource != null) {
            return choiceSource.GetSnapshot(player, includeBlockedRows);
        }

        var choiceSet = fallbackChoiceSet;
        if(choiceSet == null) {
            return new EncounterResolutionChoiceSnapshot();
        }

        return choiceSet.BuildSnapshot(
            player,
            ResolveFallbackPokemon(),
            ResolveFallbackSourceType(),
            fallbackEncounterTable,
            ResolveFallbackBattleTrigger(),
            includeBlockedRows);
    }

    IEnumerable<EncounterResolutionHistoryRow> BuildHistoryRows(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerEncounterLog>() : null;
        var rows = log != null
            ? log.ResolutionHistory
                .Where(record => record != null)
                .Reverse()
                .Select(EncounterResolutionHistoryRow.FromRecord)
            : Enumerable.Empty<EncounterResolutionHistoryRow>();

        return maxHistoryRows > 0 ? rows.Take(maxHistoryRows) : rows;
    }

    Pokemon ResolveFallbackPokemon() {
        if(fallbackRuntimePokemon != null) {
            return fallbackRuntimePokemon;
        }

        if(fallbackPokemon == null) {
            return null;
        }

        fallbackRuntimePokemon = new Pokemon(fallbackPokemon, fallbackPokemonLevel);
        return fallbackRuntimePokemon;
    }

    EncounterSourceType ResolveFallbackSourceType() {
        if(fallbackSourceType != EncounterSourceType.Any) {
            return fallbackSourceType;
        }

        return fallbackEncounterTable != null ? fallbackEncounterTable.SourceType : EncounterSourceType.Special;
    }

    BattleTrigger ResolveFallbackBattleTrigger() {
        return fallbackEncounterTable != null ? fallbackEncounterTable.BattleTrigger : fallbackBattleTrigger;
    }

    void HandleResult(EncounterResolutionResult result) {
        if(logDebugMessages && result != null) {
            var category = result.blocked ? GameDebugSeverity.Warning : result.success ? GameDebugSeverity.Success : GameDebugSeverity.Info;
            GameDebugLogger.Ensure().Record(category, GameDebugCategory.Encounter, result.message, this, "EncounterResolutionUIManager");
        }

        OnChoiceResolved?.Invoke(result);
        Refresh();
    }
}

public class EncounterResolutionUIScreenSnapshot {
    [Tooltip("Choice set id used by the current screen.")]
    public string choiceSetId;
    [Tooltip("Choice set display name.")]
    public string choiceSetName;
    [Tooltip("Choice set description.")]
    public string description;
    [Tooltip("Pokemon display name for this screen.")]
    public string pokemonName;
    [Tooltip("Encounter source type for this screen.")]
    public EncounterSourceType sourceType;
    [Tooltip("Encounter table id for this screen.")]
    public string tableId;
    [Tooltip("Encounter table display name for this screen.")]
    public string tableName;
    [Tooltip("Visible choice row count.")]
    public int rowCount;
    [Tooltip("Rows that can run now.")]
    public int availableRowCount;
    [Tooltip("Rows that are visible but blocked.")]
    public int blockedRowCount;
    [Tooltip("Visible history row count.")]
    public int historyCount;
    [Tooltip("Choice rows available to UI.")]
    public List<EncounterResolutionChoiceRow> rows = new List<EncounterResolutionChoiceRow>();
    [Tooltip("Recent resolution history rows.")]
    public List<EncounterResolutionHistoryRow> historyRows = new List<EncounterResolutionHistoryRow>();
}

public class EncounterResolutionHistoryRow {
    [Tooltip("Pokemon id involved in the attempt.")]
    public string pokemonId;
    [Tooltip("Pokemon display name involved in the attempt.")]
    public string pokemonName;
    [Tooltip("Resolution id used by the attempt.")]
    public string resolutionId;
    [Tooltip("Resolution display name used by the attempt.")]
    public string resolutionName;
    [Tooltip("If enabled, the attempt succeeded.")]
    public bool success;
    [Tooltip("Chance rolled for this attempt.")]
    public float chancePercent;
    [Tooltip("Result message recorded by the attempt.")]
    public string message;
    [Tooltip("UTC timestamp of the attempt.")]
    public string utcTimestamp;

    public static EncounterResolutionHistoryRow FromRecord(PlayerEncounterResolutionRecord record) {
        if(record == null) {
            return null;
        }

        return new EncounterResolutionHistoryRow {
            pokemonId = record.pokemonId,
            pokemonName = record.pokemonName,
            resolutionId = record.resolutionId,
            resolutionName = record.resolutionName,
            success = record.success,
            chancePercent = record.chancePercent,
            message = record.message,
            utcTimestamp = record.utcTimestamp
        };
    }
}
