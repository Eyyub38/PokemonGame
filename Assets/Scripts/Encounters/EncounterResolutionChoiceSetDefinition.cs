using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Encounters/Encounter Resolution Choice Set")]
public class EncounterResolutionChoiceSetDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this resolution choice set. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining where this choice set is used.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as wild, ranger, bait, research, rare, camp or tutorial.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Choices")]
    [Tooltip("Available non-battle encounter choices shown to the player.")]
    [SerializeField] List<EncounterResolutionChoiceEntry> choices = new List<EncounterResolutionChoiceEntry>();
    [Tooltip("If enabled, blocked choices are included in snapshots with their failure reason.")]
    [SerializeField] bool includeBlockedChoices = true;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public IReadOnlyList<EncounterResolutionChoiceEntry> Choices => choices != null ? (IReadOnlyList<EncounterResolutionChoiceEntry>)choices : Array.Empty<EncounterResolutionChoiceEntry>();
    public bool IncludeBlockedChoices => includeBlockedChoices;

    public EncounterResolutionChoiceSnapshot BuildSnapshot(PlayerController player, Pokemon pokemon, EncounterSourceType sourceType, EncounterTableDefinition table, BattleTrigger battleTrigger, bool? includeBlockedOverride = null) {
        bool includeBlocked = includeBlockedOverride ?? includeBlockedChoices;
        var rows = Choices
            .Where(choice => choice != null && choice.Resolution != null)
            .Select(choice => EncounterResolutionChoiceRow.FromChoice(choice, player, pokemon, sourceType, table, battleTrigger))
            .Where(row => row != null && (includeBlocked || row.canRun))
            .OrderBy(row => row.priority)
            .ThenBy(row => row.displayName)
            .ToList();

        return new EncounterResolutionChoiceSnapshot {
            choiceSetId = Id,
            choiceSetName = DisplayName,
            description = description,
            pokemonName = pokemon != null ? pokemon.NickName : string.Empty,
            sourceType = sourceType,
            tableId = table != null ? table.Id : string.Empty,
            tableName = table != null ? table.DisplayName : string.Empty,
            rowCount = rows.Count,
            availableRowCount = rows.Count(row => row != null && row.canRun),
            blockedRowCount = rows.Count(row => row != null && !row.canRun),
            rows = rows
        };
    }

    public EncounterResolutionResult RunChoice(string choiceId, PlayerController player, Pokemon pokemon, EncounterSourceType sourceType, EncounterTableDefinition table, BattleTrigger battleTrigger, UnityEngine.Object context) {
        var choice = FindChoice(choiceId);
        if(choice == null || choice.Resolution == null) {
            return new EncounterResolutionResult {
                blocked = true,
                message = "Encounter resolution choice is missing."
            };
        }

        return choice.Resolution.TryResolve(player, pokemon, sourceType, table, battleTrigger, context);
    }

    public EncounterResolutionResult RunFirstAvailable(PlayerController player, Pokemon pokemon, EncounterSourceType sourceType, EncounterTableDefinition table, BattleTrigger battleTrigger, UnityEngine.Object context) {
        var snapshot = BuildSnapshot(player, pokemon, sourceType, table, battleTrigger, includeBlockedOverride: false);
        var first = snapshot.rows.FirstOrDefault(row => row != null && row.canRun);
        return first != null
            ? RunChoice(first.choiceId, player, pokemon, sourceType, table, battleTrigger, context)
            : new EncounterResolutionResult {
                blocked = true,
                message = "No encounter resolution choice is currently available."
            };
    }

    public EncounterResolutionChoiceEntry FindChoice(string choiceId) {
        if(string.IsNullOrWhiteSpace(choiceId)) {
            return null;
        }

        return Choices.FirstOrDefault(choice => choice != null && string.Equals(choice.ChoiceId, choiceId, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class EncounterResolutionChoiceEntry {
    [Tooltip("Stable id for this choice row. Empty uses the resolution id.")]
    [SerializeField] string choiceId = string.Empty;
    [Tooltip("Optional display name override. Empty uses the resolution display name.")]
    [SerializeField] string displayNameOverride = string.Empty;
    [Tooltip("Optional description override. Empty uses the resolution description.")]
    [TextArea]
    [SerializeField] string descriptionOverride = string.Empty;
    [Tooltip("Resolution run when this choice is selected.")]
    [SerializeField] EncounterResolutionDefinition resolution;
    [Tooltip("Lower priority rows appear first.")]
    [SerializeField] int priority;
    [Tooltip("If enabled, this row is shown even when blocked by requirements/costs.")]
    [SerializeField] bool showWhenBlocked = true;

    public string ChoiceId => !string.IsNullOrWhiteSpace(choiceId) ? choiceId : resolution != null ? resolution.Id : string.Empty;
    public string DisplayName => !string.IsNullOrWhiteSpace(displayNameOverride) ? displayNameOverride : resolution != null ? resolution.DisplayName : string.Empty;
    public string Description => !string.IsNullOrWhiteSpace(descriptionOverride) ? descriptionOverride : resolution != null ? resolution.Description : string.Empty;
    public EncounterResolutionDefinition Resolution => resolution;
    public int Priority => priority;
    public bool ShowWhenBlocked => showWhenBlocked;
}

public class EncounterResolutionChoiceSnapshot {
    [Tooltip("Choice set id used by this snapshot.")]
    public string choiceSetId;
    [Tooltip("Choice set display name.")]
    public string choiceSetName;
    [Tooltip("Choice set description.")]
    public string description;
    [Tooltip("Pokemon display name for this choice context.")]
    public string pokemonName;
    [Tooltip("Encounter source type for this choice context.")]
    public EncounterSourceType sourceType;
    [Tooltip("Encounter table id for this choice context.")]
    public string tableId;
    [Tooltip("Encounter table display name for this choice context.")]
    public string tableName;
    [Tooltip("Visible row count.")]
    public int rowCount;
    [Tooltip("Rows that can run now.")]
    public int availableRowCount;
    [Tooltip("Rows that are visible but blocked.")]
    public int blockedRowCount;
    [Tooltip("Rows available to UI.")]
    public List<EncounterResolutionChoiceRow> rows = new List<EncounterResolutionChoiceRow>();
}

public class EncounterResolutionChoiceRow {
    [Tooltip("Choice row id.")]
    public string choiceId;
    [Tooltip("Resolution definition id.")]
    public string resolutionId;
    [Tooltip("Display name shown in UI.")]
    public string displayName;
    [Tooltip("Description shown in UI.")]
    public string description;
    [Tooltip("Resolution kind shown in UI.")]
    public EncounterResolutionKind kind;
    [Tooltip("If enabled, the player can run this row now.")]
    public bool canRun;
    [Tooltip("Reason shown when the row is blocked.")]
    public string blockedReason;
    [Tooltip("Preview success chance.")]
    public float chancePercent;
    [Tooltip("Success outcome preview.")]
    public EncounterResolutionOutcome successOutcome;
    [Tooltip("Failure outcome preview.")]
    public EncounterResolutionOutcome failureOutcome;
    [Tooltip("Priority used for sorting rows.")]
    public int priority;
    [Tooltip("Human-readable item costs for this row.")]
    public List<string> itemCosts = new List<string>();

    public static EncounterResolutionChoiceRow FromChoice(EncounterResolutionChoiceEntry choice, PlayerController player, Pokemon pokemon, EncounterSourceType sourceType, EncounterTableDefinition table, BattleTrigger battleTrigger) {
        if(choice == null || choice.Resolution == null) {
            return null;
        }

        bool canRun = choice.Resolution.CanAttempt(player, out var blockedReason);
        if(!choice.ShowWhenBlocked && !canRun) {
            return null;
        }

        return new EncounterResolutionChoiceRow {
            choiceId = choice.ChoiceId,
            resolutionId = choice.Resolution.Id,
            displayName = choice.DisplayName,
            description = choice.Description,
            kind = choice.Resolution.Kind,
            canRun = canRun,
            blockedReason = canRun ? string.Empty : blockedReason,
            chancePercent = choice.Resolution.PreviewChance(pokemon, sourceType),
            successOutcome = choice.Resolution.SuccessOutcome,
            failureOutcome = choice.Resolution.FailureOutcome,
            priority = choice.Priority,
            itemCosts = choice.Resolution.ItemCosts
                .Where(cost => cost != null && cost.item != null && cost.count > 0)
                .Select(cost => $"{cost.count}x {cost.item.Name}")
                .ToList()
        };
    }
}

public class EncounterResolutionChoiceSource : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("Choice Set")]
    [Tooltip("Choice set shown/run by this source.")]
    [SerializeField] EncounterResolutionChoiceSetDefinition choiceSet;
    [Tooltip("Exact Pokemon species used by this source. If empty, Encounter Table is rolled.")]
    [SerializeField] PokemonBase pokemon;
    [Tooltip("Minimum level used when Exact Pokemon is assigned.")]
    [Min(1)]
    [SerializeField] int minLevel = 2;
    [Tooltip("Maximum level used when Exact Pokemon is assigned.")]
    [Min(1)]
    [SerializeField] int maxLevel = 4;
    [Tooltip("Encounter table rolled when Exact Pokemon is empty.")]
    [SerializeField] EncounterTableDefinition encounterTable;
    [Tooltip("Optional source override. Any uses the table source type.")]
    [SerializeField] EncounterSourceType sourceOverride = EncounterSourceType.Any;
    [Tooltip("Battle trigger used if a selected resolution starts battle.")]
    [SerializeField] BattleTrigger battleTrigger = BattleTrigger.LongGrass;

    [Header("Activation")]
    [Tooltip("If enabled, touching this object can run the first available choice.")]
    [SerializeField] bool triggerOnTouch;
    [Tooltip("If enabled, interacting with this object can run the first available choice.")]
    [SerializeField] bool interactRunsFirstAvailable;
    [Tooltip("If enabled, trigger activation can repeat while the player remains in the trigger.")]
    [SerializeField] bool triggerRepeatedly;
    [Tooltip("If enabled, result messages are shown through DialogManager when available.")]
    [SerializeField] bool showResultMessages = true;

    Pokemon cachedPokemon;

    public EncounterResolutionChoiceSetDefinition ChoiceSet => choiceSet;
    public PokemonBase Pokemon => pokemon;
    public EncounterTableDefinition EncounterTable => encounterTable;
    public EncounterSourceType SourceOverride => sourceOverride;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(triggerOnTouch) {
            RunFirstAvailable(player);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : PlayerController.i;
        if(!interactRunsFirstAvailable) {
            yield break;
        }

        var result = RunFirstAvailable(player);
        if(showResultMessages && result != null && !string.IsNullOrWhiteSpace(result.message) && DialogManager.i != null) {
            yield return DialogManager.i.ShowDialogText(result.message);
        }
    }

    public EncounterResolutionChoiceSnapshot GetSnapshot(PlayerController player, bool? includeBlockedOverride = null) {
        var encounterPokemon = GetOrCreatePokemon(player);
        return choiceSet != null
            ? choiceSet.BuildSnapshot(player, encounterPokemon, ResolveSourceType(), encounterTable, ResolveBattleTrigger(), includeBlockedOverride)
            : new EncounterResolutionChoiceSnapshot();
    }

    public EncounterResolutionResult RunChoice(string choiceId, PlayerController player) {
        var encounterPokemon = GetOrCreatePokemon(player);
        if(choiceSet == null || encounterPokemon == null) {
            return new EncounterResolutionResult {
                blocked = true,
                message = "Encounter resolution choice source is missing a choice set or Pokemon."
            };
        }

        var result = choiceSet.RunChoice(choiceId, player, encounterPokemon, ResolveSourceType(), encounterTable, ResolveBattleTrigger(), this);
        if(result.disableSource) {
            gameObject.SetActive(false);
        }
        return result;
    }

    public EncounterResolutionResult RunFirstAvailable(PlayerController player) {
        var encounterPokemon = GetOrCreatePokemon(player);
        if(choiceSet == null || encounterPokemon == null) {
            return new EncounterResolutionResult {
                blocked = true,
                message = "Encounter resolution choice source is missing a choice set or Pokemon."
            };
        }

        var result = choiceSet.RunFirstAvailable(player, encounterPokemon, ResolveSourceType(), encounterTable, ResolveBattleTrigger(), this);
        if(result.disableSource) {
            gameObject.SetActive(false);
        }
        return result;
    }

    Pokemon GetOrCreatePokemon(PlayerController player) {
        if(cachedPokemon != null) {
            return cachedPokemon;
        }

        if(pokemon != null) {
            cachedPokemon = new Pokemon(pokemon, UnityEngine.Random.Range(Mathf.Max(1, minLevel), Mathf.Max(minLevel, maxLevel) + 1));
            return cachedPokemon;
        }

        if(encounterTable != null && encounterTable.RollPokemon(player, out var rolledPokemon, out _)) {
            cachedPokemon = rolledPokemon;
            return cachedPokemon;
        }

        return null;
    }

    EncounterSourceType ResolveSourceType() {
        if(sourceOverride != EncounterSourceType.Any) {
            return sourceOverride;
        }

        return encounterTable != null ? encounterTable.SourceType : EncounterSourceType.Special;
    }

    BattleTrigger ResolveBattleTrigger() {
        return encounterTable != null ? encounterTable.BattleTrigger : battleTrigger;
    }
}
