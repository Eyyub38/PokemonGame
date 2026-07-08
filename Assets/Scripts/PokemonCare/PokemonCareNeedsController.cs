using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PokemonCareNeedsController : MonoBehaviour, ISavable {
    [Header("Definitions")]
    [Tooltip("Care needs tracked passively for the player's party Pokemon.")]
    [SerializeField] List<PokemonCareNeedDefinition> needDefinitions = new List<PokemonCareNeedDefinition>();

    [Header("Rules")]
    [Tooltip("If enabled, active party care needs update from TimeSystem hour changes.")]
    [SerializeField] bool updateWithWorldTime = true;
    [Tooltip("If enabled, passive changes apply to every party Pokemon. If disabled, only the first healthy Pokemon is affected.")]
    [SerializeField] bool applyToWholeParty = true;
    [Tooltip("If enabled, fainted Pokemon are skipped unless the care need allows fainted Pokemon passive changes.")]
    [SerializeField] bool skipFaintedPokemon = true;
    [Tooltip("If enabled, Pokemon currently admitted to a care facility are skipped by active party decay.")]
    [SerializeField] bool skipPokemonInCareFacilities = true;
    [Tooltip("If enabled, missing care need values are written to Pokemon save data at their default value when this controller starts.")]
    [SerializeField] bool initializeMissingNeedValues = true;
    [Tooltip("If enabled, low, critical and recovered threshold transitions publish game events.")]
    [SerializeField] bool publishThresholdEvents = true;
    [Tooltip("If enabled, passive care need changes are written to the debug log.")]
    [SerializeField] bool writeDebugLogs;
    [Tooltip("Maximum number of recent care need changes kept in this controller for UI/debug views.")]
    [Min(1)]
    [SerializeField] int maxRecentChangeRecords = 40;

    [Header("Runtime")]
    [Tooltip("Minute counter used to convert TimeSystem minute events into hourly care need changes.")]
    [SerializeField] int minuteBuffer;
    [Tooltip("Recent passive care need changes. Saved for debugging and lightweight UI history.")]
    [SerializeField] List<PokemonCareNeedChangeRecord> recentChanges = new List<PokemonCareNeedChangeRecord>();

    PokemonParty party;
    PlayerPokemonCareFacilityLog facilityLog;

    public event Action<Pokemon, PokemonCareNeedDefinition, PokemonCareNeedChangeRecord> OnCareNeedChanged;
    public IReadOnlyList<PokemonCareNeedDefinition> NeedDefinitions => needDefinitions;
    public IReadOnlyList<PokemonCareNeedChangeRecord> RecentChanges => recentChanges;

    void Awake() {
        party = GetComponent<PokemonParty>();
        facilityLog = GetComponent<PlayerPokemonCareFacilityLog>();
        EnsureKnownNeedValues();
    }

    void OnEnable() {
        if(updateWithWorldTime && TimeSystem.i != null) {
            TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        }
    }

    void OnDisable() {
        if(TimeSystem.i != null) {
            TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        }
    }

    public PokemonCareNeedDefinition GetNeedDefinition(string id) {
        if(string.IsNullOrWhiteSpace(id)) {
            return null;
        }

        return needDefinitions.FirstOrDefault(need => need != null && need.Id == id);
    }

    public int ApplyActiveHours(int hours, string sourceId = "world-time") {
        return ApplyHourlyChanges(Mathf.Max(1, hours), PokemonCareNeedHourlyContext.Active, sourceId);
    }

    public int ApplyRest(int hours, string sourceId = "player-rest") {
        return ApplyHourlyChanges(Mathf.Max(1, hours), PokemonCareNeedHourlyContext.Resting, sourceId);
    }

    public int ApplySleep(int hours, string sourceId = "player-sleep") {
        return ApplyHourlyChanges(Mathf.Max(1, hours), PokemonCareNeedHourlyContext.Sleeping, sourceId);
    }

    public bool TryChangeNeed(Pokemon pokemon, PokemonCareNeedDefinition need, int amount, string sourceId, PokemonCareNeedHourlyContext context, out PokemonCareNeedChangeRecord record) {
        record = null;
        if(pokemon == null || need == null || amount == 0) {
            return false;
        }

        int before = pokemon.GetCareNeedValue(need);
        var beforeState = need.GetState(before);
        pokemon.ChangeCareNeed(need, amount);
        int after = pokemon.GetCareNeedValue(need);
        int applied = after - before;
        if(applied == 0) {
            return false;
        }

        var afterState = need.GetState(after);
        record = new PokemonCareNeedChangeRecord {
            pokemonId = pokemon.InstanceId,
            pokemonName = pokemon.NickName,
            speciesName = pokemon.Base != null ? pokemon.Base.Name : null,
            needId = need.Id,
            needName = need.DisplayName,
            context = context,
            sourceId = sourceId,
            amountRequested = amount,
            amountApplied = applied,
            beforeValue = before,
            afterValue = after,
            beforeState = beforeState,
            afterState = afterState,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour()
        };

        RecordChange(pokemon, need, record);
        return true;
    }

    void HandleTimeChanged() {
        minuteBuffer++;
        if(minuteBuffer < 60) {
            return;
        }

        int hours = minuteBuffer / 60;
        minuteBuffer %= 60;
        ApplyActiveHours(hours);
    }

    int ApplyHourlyChanges(int hours, PokemonCareNeedHourlyContext context, string sourceId) {
        if(hours <= 0 || needDefinitions.Count == 0) {
            return 0;
        }

        int appliedCount = 0;
        foreach(var pokemon in GetTargetPokemon(context)) {
            foreach(var need in needDefinitions) {
                if(!CanApplyNeedToPokemon(pokemon, need, context)) {
                    continue;
                }

                int hourlyChange = need.GetHourlyChange(context);
                if(hourlyChange == 0) {
                    continue;
                }

                if(TryChangeNeed(pokemon, need, hourlyChange * hours, sourceId, context, out _)) {
                    appliedCount++;
                }
            }
        }

        return appliedCount;
    }

    IEnumerable<Pokemon> GetTargetPokemon(PokemonCareNeedHourlyContext context) {
        party ??= GetComponent<PokemonParty>();
        if(party == null || party.Pokemons == null) {
            return Enumerable.Empty<Pokemon>();
        }

        if(!applyToWholeParty) {
            var pokemon = party.GetHealthyPokemon();
            return pokemon != null ? new[] { pokemon } : Enumerable.Empty<Pokemon>();
        }

        return party.Pokemons.Where(pokemon => pokemon != null && (context != PokemonCareNeedHourlyContext.Active || !IsInCareFacility(pokemon)));
    }

    bool CanApplyNeedToPokemon(Pokemon pokemon, PokemonCareNeedDefinition need, PokemonCareNeedHourlyContext context) {
        if(pokemon == null || need == null) {
            return false;
        }

        if(skipFaintedPokemon && !need.PassiveChangesAffectFaintedPokemon && pokemon.HP <= 0) {
            return false;
        }

        return context != PokemonCareNeedHourlyContext.Active || !skipPokemonInCareFacilities || !IsInCareFacility(pokemon);
    }

    bool IsInCareFacility(Pokemon pokemon) {
        if(!skipPokemonInCareFacilities || pokemon == null) {
            return false;
        }

        facilityLog ??= GetComponent<PlayerPokemonCareFacilityLog>();
        return facilityLog != null && facilityLog.HasActiveStay(pokemon);
    }

    void EnsureKnownNeedValues() {
        if(!initializeMissingNeedValues || needDefinitions.Count == 0) {
            return;
        }

        party ??= GetComponent<PokemonParty>();
        if(party?.Pokemons == null) {
            return;
        }

        foreach(var pokemon in party.Pokemons) {
            if(pokemon == null) {
                continue;
            }

            foreach(var need in needDefinitions) {
                if(need != null && !pokemon.HasCareNeedValue(need)) {
                    pokemon.SetCareNeed(need, need.DefaultValue);
                }
            }
        }
    }

    void RecordChange(Pokemon pokemon, PokemonCareNeedDefinition need, PokemonCareNeedChangeRecord record) {
        recentChanges.Add(record);
        while(recentChanges.Count > Mathf.Max(1, maxRecentChangeRecords)) {
            recentChanges.RemoveAt(0);
        }

        PublishThresholdEvent(pokemon, need, record);
        if(writeDebugLogs) {
            GameDebug.Step(
                $"{record.pokemonName} {record.needName}: {record.beforeValue} -> {record.afterValue} ({record.amountApplied:+#;-#;0})",
                GameDebugCategory.PokemonCare,
                this,
                "PokemonCareNeedsController");
        }

        OnCareNeedChanged?.Invoke(pokemon, need, record);
    }

    void PublishThresholdEvent(Pokemon pokemon, PokemonCareNeedDefinition need, PokemonCareNeedChangeRecord record) {
        if(!publishThresholdEvents || pokemon == null || need == null || record == null) {
            return;
        }

        if(record.afterState == PokemonCareNeedState.Critical && record.beforeState != PokemonCareNeedState.Critical) {
            PublishNeedEvent(need.CriticalEvent, "critical", pokemon, need, record, GameEventImportance.Warning);
        } else if(record.afterState == PokemonCareNeedState.Low && record.beforeState != PokemonCareNeedState.Low && record.beforeState != PokemonCareNeedState.Critical) {
            PublishNeedEvent(need.LowEvent, "low", pokemon, need, record, GameEventImportance.Warning);
        } else if((record.beforeState == PokemonCareNeedState.Low || record.beforeState == PokemonCareNeedState.Critical)
            && (record.afterState == PokemonCareNeedState.Normal || record.afterState == PokemonCareNeedState.High)) {
            PublishNeedEvent(need.RecoveredEvent, "recovered", pokemon, need, record, GameEventImportance.Info);
        }
    }

    void PublishNeedEvent(GameEventDefinition eventDefinition, string phase, Pokemon pokemon, PokemonCareNeedDefinition need, PokemonCareNeedChangeRecord record, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"pokemon-care.need.{phase}.{need.Id}.{pokemon.InstanceId}",
            $"{pokemon.NickName} {need.DisplayName} is {phase}.",
            GameEventCategory.PokemonCare,
            importance,
            this,
            "PokemonCareNeedsController",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("pokemonId", pokemon.InstanceId),
            GameEventPublishing.Value("pokemonName", pokemon.NickName),
            GameEventPublishing.Value("speciesName", pokemon.Base != null ? pokemon.Base.Name : null),
            GameEventPublishing.Value("needId", need.Id),
            GameEventPublishing.Value("needName", need.DisplayName),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("beforeValue", record.beforeValue),
            GameEventPublishing.Value("afterValue", record.afterValue),
            GameEventPublishing.Value("sourceId", record.sourceId));
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PokemonCareNeedsControllerSaveData {
            minuteBuffer = minuteBuffer,
            recentChanges = recentChanges != null ? new List<PokemonCareNeedChangeRecord>(recentChanges) : new List<PokemonCareNeedChangeRecord>()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PokemonCareNeedsControllerSaveData;
        minuteBuffer = Mathf.Max(0, saveData?.minuteBuffer ?? 0);
        recentChanges = saveData?.recentChanges?.Where(record => record != null).ToList()
            ?? new List<PokemonCareNeedChangeRecord>();
        EnsureKnownNeedValues();
    }
}

[Serializable]
public class PokemonCareNeedChangeRecord {
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
    [Tooltip("Source id that caused this change, such as world-time, player-rest or player-sleep.")]
    public string sourceId;
    [Tooltip("Raw requested amount before clamping.")]
    public int amountRequested;
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
}

[Serializable]
public class PokemonCareNeedsControllerSaveData {
    public int minuteBuffer;
    public List<PokemonCareNeedChangeRecord> recentChanges;
}
