using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PokemonPartyCareStatusUIManager : MonoBehaviour {
    [Header("Source")]
    [Tooltip("Pokemon party read by this UI manager. Empty uses PokemonParty.GetPlayerParty at runtime.")]
    [SerializeField] PokemonParty party;
    [Tooltip("Care needs controller used for known care need definitions and recent care history. Empty uses the party/player object.")]
    [SerializeField] PokemonCareNeedsController careNeedsController;
    [Tooltip("Vital profile used for max values and low/critical estimates. Empty uses default Pokemon vital formulas.")]
    [SerializeField] PokemonVitalProfileDefinition vitalProfile;
    [Tooltip("If enabled, Refresh is called automatically when this object is enabled.")]
    [SerializeField] bool refreshOnEnable = true;
    [Tooltip("If enabled, snapshots update when party or care need changes are reported.")]
    [SerializeField] bool listenForChanges = true;

    [Header("Rows")]
    [Tooltip("If enabled, fainted Pokemon are included in party rows.")]
    [SerializeField] bool includeFaintedPokemon = true;
    [Tooltip("If enabled, Pokemon with healthy/high care and vital state are included in party rows.")]
    [SerializeField] bool includeHealthyPokemon = true;
    [Tooltip("If enabled, party rows are sorted by care/vital urgency before slot index.")]
    [SerializeField] bool sortByUrgency = true;
    [Tooltip("Maximum recent care change rows returned to UI. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRecentRows = 12;

    [Header("Debug")]
    [Tooltip("If enabled, refresh and action calls write debug messages.")]
    [SerializeField] bool logDebugMessages;

    PokemonPartyCareStatusSnapshot currentSnapshot = new PokemonPartyCareStatusSnapshot();

    public PokemonParty Party => party;
    public PokemonCareNeedsController CareNeedsController => careNeedsController;
    public PokemonVitalProfileDefinition VitalProfile => vitalProfile;
    public int MaxRecentRows => maxRecentRows;
    public PokemonPartyCareStatusSnapshot CurrentSnapshot => currentSnapshot;
    public event Action<PokemonPartyCareStatusSnapshot> OnSnapshotChanged;
    public event Action<PokemonCareStatusActionResult> OnActionRan;

    void OnEnable() {
        var resolvedParty = ResolveParty();
        var resolvedCare = ResolveCareNeedsController();
        if(listenForChanges) {
            if(resolvedParty != null) {
                resolvedParty.OnUpdated += RefreshSilently;
            }

            if(resolvedCare != null) {
                resolvedCare.OnCareNeedChanged += HandleCareNeedChanged;
            }
        }

        if(refreshOnEnable) {
            Refresh();
        }
    }

    void OnDisable() {
        if(party != null) {
            party.OnUpdated -= RefreshSilently;
        }

        if(careNeedsController != null) {
            careNeedsController.OnCareNeedChanged -= HandleCareNeedChanged;
        }
    }

    [ContextMenu("Refresh Pokemon Party Care Snapshot")]
    public PokemonPartyCareStatusSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public PokemonPartyCareStatusSnapshot Refresh() {
        var resolvedParty = ResolveParty();
        if(resolvedParty == null || resolvedParty.Pokemons == null) {
            currentSnapshot = new PokemonPartyCareStatusSnapshot {
                isAvailable = false,
                unavailableReason = "No PokemonParty was found."
            };
            OnSnapshotChanged?.Invoke(currentSnapshot);
            return currentSnapshot;
        }

        var pokemonRows = BuildPokemonRows(resolvedParty).ToList();
        var recentRows = BuildRecentRows().ToList();

        currentSnapshot = new PokemonPartyCareStatusSnapshot {
            isAvailable = true,
            unavailableReason = string.Empty,
            pokemonCount = pokemonRows.Count,
            criticalPokemonCount = pokemonRows.Count(row => row.hasCriticalNeed || row.hasCriticalVital || row.needsLongTermTreatment),
            lowPokemonCount = pokemonRows.Count(row => row.hasLowNeed || row.hasLowVital || row.needsRestOrFeeding),
            healthyPokemonCount = pokemonRows.Count(row => !row.hasLowNeed && !row.hasLowVital && !row.needsLongTermTreatment && !row.needsRestOrFeeding),
            recentChangeCount = recentRows.Count,
            rows = pokemonRows,
            recentChanges = recentRows
        };

        if(logDebugMessages) {
            GameDebug.Step($"Pokemon party care snapshot refreshed: {currentSnapshot.pokemonCount} rows.", GameDebugCategory.PokemonCare, this, "PokemonPartyCareStatusUIManager");
        }

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public PokemonCareStatusActionResult ChangeCareNeed(int partyIndex, PokemonCareNeedDefinition need, int amount, string sourceId = "ui") {
        var pokemon = ResolvePokemon(partyIndex);
        if(pokemon == null) {
            return FinishAction(PokemonCareStatusActionResult.Blocked("change-care-need", "No Pokemon was found for the selected party slot."));
        }

        if(need == null) {
            return FinishAction(PokemonCareStatusActionResult.Blocked("change-care-need", "No care need definition was provided."));
        }

        var controller = ResolveCareNeedsController();
        bool changed = controller != null
            ? controller.TryChangeNeed(pokemon, need, amount, sourceId, PokemonCareNeedHourlyContext.Active, out _)
            : ChangeCareNeedDirectly(pokemon, need, amount);

        return FinishAction(changed
            ? PokemonCareStatusActionResult.Success("change-care-need", $"{pokemon.NickName} {need.DisplayName} changed by {amount}.")
            : PokemonCareStatusActionResult.Blocked("change-care-need", $"{pokemon.NickName} {need.DisplayName} did not change."));
    }

    public PokemonCareStatusActionResult RestorePokemonVitals(int partyIndex, bool restoreCore = true, bool restoreBattle = true) {
        var pokemon = ResolvePokemon(partyIndex);
        if(pokemon == null) {
            return FinishAction(PokemonCareStatusActionResult.Blocked("restore-vitals", "No Pokemon was found for the selected party slot."));
        }

        if(restoreCore && restoreBattle) {
            pokemon.RestoreVitalsToFull(vitalProfile);
        } else if(restoreCore) {
            pokemon.RestoreCoreVitalsToFull(vitalProfile);
        } else if(restoreBattle) {
            pokemon.RestoreBattleVitalsToFull(vitalProfile);
        } else {
            return FinishAction(PokemonCareStatusActionResult.Blocked("restore-vitals", "No vital resource group was selected."));
        }

        return FinishAction(PokemonCareStatusActionResult.Success("restore-vitals", $"{pokemon.NickName} vitals restored."));
    }

    IEnumerable<PokemonCareStatusPokemonRow> BuildPokemonRows(PokemonParty source) {
        var rows = source.Pokemons
            .Select((pokemon, index) => BuildPokemonRow(pokemon, index))
            .Where(row => row != null)
            .Where(ShouldIncludePokemon);

        if(sortByUrgency) {
            rows = rows
                .OrderBy(row => row.urgencyRank)
                .ThenBy(row => row.slotIndex);
        } else {
            rows = rows.OrderBy(row => row.slotIndex);
        }

        return rows;
    }

    PokemonCareStatusPokemonRow BuildPokemonRow(Pokemon pokemon, int slotIndex) {
        if(pokemon == null) {
            return null;
        }

        var careRows = BuildCareNeedRows(pokemon).ToList();
        var vitalRows = BuildVitalRows(pokemon).ToList();
        bool hasCriticalNeed = careRows.Any(row => row.state == PokemonCareNeedState.Critical);
        bool hasLowNeed = careRows.Any(row => row.state == PokemonCareNeedState.Low);
        bool hasCriticalVital = vitalRows.Any(row => row.isCritical);
        bool hasLowVital = vitalRows.Any(row => row.isLow);
        bool needsTreatment = pokemon.NeedsLongTermTreatment(vitalProfile);
        bool needsRest = pokemon.NeedsRestOrFeeding(vitalProfile);

        return new PokemonCareStatusPokemonRow {
            slotIndex = slotIndex,
            pokemonId = pokemon.InstanceId,
            pokemonName = pokemon.NickName,
            speciesName = pokemon.Base != null ? pokemon.Base.Name : string.Empty,
            level = pokemon.Level,
            hp = pokemon.HP,
            maxHp = pokemon.MaxHp,
            hpNormalized = pokemon.MaxHp <= 0 ? 0f : Mathf.Clamp01(pokemon.HP / (float)pokemon.MaxHp),
            isFainted = pokemon.HP <= 0,
            isVitallyUsable = pokemon.IsVitallyUsable(vitalProfile, out var blockReason),
            vitalBlockReason = blockReason,
            needsLongTermTreatment = needsTreatment,
            needsRestOrFeeding = needsRest,
            hasCriticalNeed = hasCriticalNeed,
            hasLowNeed = hasLowNeed,
            hasCriticalVital = hasCriticalVital,
            hasLowVital = hasLowVital,
            urgencyRank = CalculateUrgency(pokemon.HP <= 0, needsTreatment, needsRest, hasCriticalNeed, hasLowNeed, hasCriticalVital, hasLowVital),
            careNeeds = careRows,
            vitals = vitalRows
        };
    }

    IEnumerable<PokemonCareNeedStatusRow> BuildCareNeedRows(Pokemon pokemon) {
        var controller = ResolveCareNeedsController();
        var definitions = controller != null ? controller.NeedDefinitions : Array.Empty<PokemonCareNeedDefinition>();
        return definitions
            .Where(need => need != null)
            .Select(need => PokemonCareNeedStatusRow.FromPokemon(pokemon, need));
    }

    IEnumerable<PokemonVitalStatusRow> BuildVitalRows(Pokemon pokemon) {
        yield return PokemonVitalStatusRow.FromPokemon(pokemon, PokemonVitalResourceKind.CoreHealth, "Core Health", vitalProfile);
        yield return PokemonVitalStatusRow.FromPokemon(pokemon, PokemonVitalResourceKind.CorePhysicalStamina, "Core Physical Stamina", vitalProfile);
        yield return PokemonVitalStatusRow.FromPokemon(pokemon, PokemonVitalResourceKind.CoreElementalStamina, "Core Elemental Stamina", vitalProfile);
        yield return PokemonVitalStatusRow.FromPokemon(pokemon, PokemonVitalResourceKind.BattlePhysicalStamina, "Battle Physical Stamina", vitalProfile);
        yield return PokemonVitalStatusRow.FromPokemon(pokemon, PokemonVitalResourceKind.BattleElementalStamina, "Battle Elemental Stamina", vitalProfile);
    }

    IEnumerable<PokemonCareNeedChangeUIRow> BuildRecentRows() {
        var controller = ResolveCareNeedsController();
        var rows = controller != null
            ? controller.RecentChanges
                .Where(record => record != null)
                .Reverse()
                .Select(PokemonCareNeedChangeUIRow.FromRecord)
            : Enumerable.Empty<PokemonCareNeedChangeUIRow>();

        return maxRecentRows > 0 ? rows.Take(maxRecentRows) : rows;
    }

    bool ShouldIncludePokemon(PokemonCareStatusPokemonRow row) {
        if(row == null) {
            return false;
        }

        if(row.isFainted && !includeFaintedPokemon) {
            return false;
        }

        if(includeHealthyPokemon) {
            return true;
        }

        return row.hasCriticalNeed || row.hasLowNeed || row.hasCriticalVital || row.hasLowVital || row.needsLongTermTreatment || row.needsRestOrFeeding;
    }

    int CalculateUrgency(bool fainted, bool needsTreatment, bool needsRest, bool criticalNeed, bool lowNeed, bool criticalVital, bool lowVital) {
        if(fainted || needsTreatment || criticalNeed || criticalVital) return 0;
        if(needsRest || lowNeed || lowVital) return 1;
        return 2;
    }

    Pokemon ResolvePokemon(int partyIndex) {
        var resolvedParty = ResolveParty();
        return resolvedParty != null
            && resolvedParty.Pokemons != null
            && partyIndex >= 0
            && partyIndex < resolvedParty.Pokemons.Count
            ? resolvedParty.Pokemons[partyIndex]
            : null;
    }

    bool ChangeCareNeedDirectly(Pokemon pokemon, PokemonCareNeedDefinition need, int amount) {
        int before = pokemon.GetCareNeedValue(need);
        pokemon.ChangeCareNeed(need, amount);
        return pokemon.GetCareNeedValue(need) != before;
    }

    void RefreshSilently() {
        Refresh();
    }

    void HandleCareNeedChanged(Pokemon pokemon, PokemonCareNeedDefinition need, PokemonCareNeedChangeRecord record) {
        Refresh();
    }

    PokemonCareStatusActionResult FinishAction(PokemonCareStatusActionResult result) {
        Refresh();
        if(logDebugMessages && result != null) {
            var severity = result.success ? GameDebugSeverity.Success : GameDebugSeverity.Warning;
            GameDebugLogger.Ensure().Record(severity, GameDebugCategory.PokemonCare, result.message, this, "PokemonPartyCareStatusUIManager");
        }

        OnActionRan?.Invoke(result);
        return result;
    }

    PokemonParty ResolveParty() {
        if(party != null) {
            return party;
        }

        try {
            party = PokemonParty.GetPlayerParty();
        } catch {
            party = FindAnyObjectByType<PokemonParty>();
        }

        return party;
    }

    PokemonCareNeedsController ResolveCareNeedsController() {
        if(careNeedsController != null) {
            return careNeedsController;
        }

        var resolvedParty = ResolveParty();
        careNeedsController = resolvedParty != null
            ? resolvedParty.GetComponent<PokemonCareNeedsController>()
            : FindAnyObjectByType<PokemonCareNeedsController>();
        return careNeedsController;
    }
}

public class PokemonPartyCareStatusSnapshot {
    [Tooltip("If false, no party was available.")]
    public bool isAvailable;
    [Tooltip("Reason shown when unavailable.")]
    public string unavailableReason;
    [Tooltip("Visible Pokemon row count.")]
    public int pokemonCount;
    [Tooltip("Pokemon count with critical care or vital state.")]
    public int criticalPokemonCount;
    [Tooltip("Pokemon count with low care or vital state.")]
    public int lowPokemonCount;
    [Tooltip("Pokemon count without low/critical care or vital warnings.")]
    public int healthyPokemonCount;
    [Tooltip("Visible recent care change count.")]
    public int recentChangeCount;
    [Tooltip("Party care rows available to UI.")]
    public List<PokemonCareStatusPokemonRow> rows = new List<PokemonCareStatusPokemonRow>();
    [Tooltip("Recent care need changes available to UI.")]
    public List<PokemonCareNeedChangeUIRow> recentChanges = new List<PokemonCareNeedChangeUIRow>();
}

public class PokemonCareStatusPokemonRow {
    [Tooltip("Party slot index.")]
    public int slotIndex;
    [Tooltip("Pokemon instance id.")]
    public string pokemonId;
    [Tooltip("Pokemon display/nickname.")]
    public string pokemonName;
    [Tooltip("Pokemon species name.")]
    public string speciesName;
    [Tooltip("Pokemon level.")]
    public int level;
    [Tooltip("Current battle HP.")]
    public int hp;
    [Tooltip("Maximum battle HP.")]
    public int maxHp;
    [Tooltip("Normalized battle HP from 0 to 1.")]
    public float hpNormalized;
    [Tooltip("If enabled, Pokemon battle HP is 0.")]
    public bool isFainted;
    [Tooltip("If enabled, Pokemon can be used according to vital rules.")]
    public bool isVitallyUsable;
    [Tooltip("Vital block reason if not vitally usable.")]
    public PokemonVitalBlockReason vitalBlockReason;
    [Tooltip("If enabled, core health is depleted and longer treatment is needed.")]
    public bool needsLongTermTreatment;
    [Tooltip("If enabled, core stamina is depleted and rest/feeding is needed.")]
    public bool needsRestOrFeeding;
    [Tooltip("If enabled, at least one care need is critical.")]
    public bool hasCriticalNeed;
    [Tooltip("If enabled, at least one care need is low.")]
    public bool hasLowNeed;
    [Tooltip("If enabled, at least one vital resource is critical.")]
    public bool hasCriticalVital;
    [Tooltip("If enabled, at least one vital resource is low.")]
    public bool hasLowVital;
    [Tooltip("Lower rank means more urgent in UI sorting.")]
    public int urgencyRank;
    [Tooltip("Care need rows for this Pokemon.")]
    public List<PokemonCareNeedStatusRow> careNeeds = new List<PokemonCareNeedStatusRow>();
    [Tooltip("Vital resource rows for this Pokemon.")]
    public List<PokemonVitalStatusRow> vitals = new List<PokemonVitalStatusRow>();
}

public class PokemonCareNeedStatusRow {
    [Tooltip("Care need id.")]
    public string needId;
    [Tooltip("Care need display name.")]
    public string displayName;
    [Tooltip("Care need description.")]
    public string description;
    [Tooltip("Current care need value.")]
    public int currentValue;
    [Tooltip("Minimum care need value.")]
    public int minValue;
    [Tooltip("Maximum care need value.")]
    public int maxValue;
    [Tooltip("Normalized care need value from 0 to 1.")]
    public float normalized;
    [Tooltip("Current care need state.")]
    public PokemonCareNeedState state;

    public static PokemonCareNeedStatusRow FromPokemon(Pokemon pokemon, PokemonCareNeedDefinition need) {
        int value = pokemon.GetCareNeedValue(need);
        int range = Mathf.Max(1, need.MaxValue - need.MinValue);
        return new PokemonCareNeedStatusRow {
            needId = need.Id,
            displayName = need.DisplayName,
            description = need.Description,
            currentValue = value,
            minValue = need.MinValue,
            maxValue = need.MaxValue,
            normalized = Mathf.Clamp01((value - need.MinValue) / (float)range),
            state = need.GetState(value)
        };
    }
}

public class PokemonVitalStatusRow {
    [Tooltip("Vital resource kind.")]
    public PokemonVitalResourceKind resource;
    [Tooltip("Display name for this vital resource.")]
    public string displayName;
    [Tooltip("Current vital value.")]
    public int currentValue;
    [Tooltip("Maximum vital value.")]
    public int maxValue;
    [Tooltip("Normalized vital value from 0 to 1.")]
    public float normalized;
    [Tooltip("If enabled, this vital resource is at or below 25%.")]
    public bool isLow;
    [Tooltip("If enabled, this vital resource is depleted.")]
    public bool isCritical;

    public static PokemonVitalStatusRow FromPokemon(Pokemon pokemon, PokemonVitalResourceKind resource, string displayName, PokemonVitalProfileDefinition profile) {
        int max = pokemon.GetVitalMax(resource, profile);
        int current = pokemon.GetVitalValue(resource, profile);
        float normalized = max <= 0 ? 0f : Mathf.Clamp01(current / (float)max);
        return new PokemonVitalStatusRow {
            resource = resource,
            displayName = displayName,
            currentValue = current,
            maxValue = max,
            normalized = normalized,
            isLow = normalized <= 0.25f,
            isCritical = current <= 0
        };
    }
}

public class PokemonCareNeedChangeUIRow {
    [Tooltip("Saved Pokemon instance id affected by this change.")]
    public string pokemonId;
    [Tooltip("Saved Pokemon nickname/display name affected by this change.")]
    public string pokemonName;
    [Tooltip("Saved Pokemon species name affected by this change.")]
    public string speciesName;
    [Tooltip("Care need id affected by this change.")]
    public string needId;
    [Tooltip("Care need display name affected by this change.")]
    public string needName;
    [Tooltip("Hourly context that caused this change.")]
    public PokemonCareNeedHourlyContext context;
    [Tooltip("Source id that caused this change.")]
    public string sourceId;
    [Tooltip("Actual applied amount after clamping.")]
    public int amountApplied;
    [Tooltip("Care need value before the change.")]
    public int beforeValue;
    [Tooltip("Care need value after the change.")]
    public int afterValue;
    [Tooltip("Care need state before the change.")]
    public PokemonCareNeedState beforeState;
    [Tooltip("Care need state after the change.")]
    public PokemonCareNeedState afterState;
    [Tooltip("In-game day when this change happened.")]
    public int day;
    [Tooltip("Absolute in-game hour when this change happened.")]
    public int absoluteHour;

    public static PokemonCareNeedChangeUIRow FromRecord(PokemonCareNeedChangeRecord record) {
        return new PokemonCareNeedChangeUIRow {
            pokemonId = record.pokemonId,
            pokemonName = record.pokemonName,
            speciesName = record.speciesName,
            needId = record.needId,
            needName = record.needName,
            context = record.context,
            sourceId = record.sourceId,
            amountApplied = record.amountApplied,
            beforeValue = record.beforeValue,
            afterValue = record.afterValue,
            beforeState = record.beforeState,
            afterState = record.afterState,
            day = record.day,
            absoluteHour = record.absoluteHour
        };
    }
}

public class PokemonCareStatusActionResult {
    [Tooltip("Action id.")]
    public string actionId;
    [Tooltip("If enabled, the action ran successfully.")]
    public bool success;
    [Tooltip("If enabled, the action was blocked.")]
    public bool blocked;
    [Tooltip("Human-readable result message.")]
    public string message;

    public static PokemonCareStatusActionResult Success(string actionId, string message) {
        return new PokemonCareStatusActionResult {
            actionId = actionId,
            success = true,
            blocked = false,
            message = message
        };
    }

    public static PokemonCareStatusActionResult Blocked(string actionId, string message) {
        return new PokemonCareStatusActionResult {
            actionId = actionId,
            success = false,
            blocked = true,
            message = message
        };
    }
}
