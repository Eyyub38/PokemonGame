using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPowerMechanicLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for unlocked power mechanics.")]
    [SerializeField] List<string> unlockedMechanicIds = new List<string>();
    [Tooltip("Runtime/save trainer charge states by charge group.")]
    [SerializeField] List<PlayerPowerMechanicChargeState> chargeStates = new List<PlayerPowerMechanicChargeState>();
    [Tooltip("Runtime/save power mechanic usage history.")]
    [SerializeField] List<PlayerPowerMechanicUseRecord> usageHistory = new List<PlayerPowerMechanicUseRecord>();

    public IReadOnlyList<string> UnlockedMechanicIds => unlockedMechanicIds;
    public IReadOnlyList<PlayerPowerMechanicChargeState> ChargeStates => chargeStates;
    public IReadOnlyList<PlayerPowerMechanicUseRecord> UsageHistory => usageHistory;
    public event Action<PowerMechanicDefinition> OnMechanicUnlocked;
    public event Action<PowerMechanicDefinition, PlayerPowerMechanicUseRecord> OnMechanicUsed;
    public event Action OnPowerMechanicLogChanged;

    public bool HasUnlocked(PowerMechanicDefinition mechanic) {
        return mechanic != null && (mechanic.UnlockedByDefault || HasUnlocked(mechanic.Id));
    }

    public bool HasUnlocked(string mechanicId) {
        return !string.IsNullOrWhiteSpace(mechanicId) && unlockedMechanicIds.Contains(mechanicId);
    }

    public bool Unlock(PowerMechanicDefinition mechanic, string source = null) {
        if(mechanic == null || HasUnlocked(mechanic.Id)) {
            return false;
        }

        unlockedMechanicIds.Add(mechanic.Id);
        OnMechanicUnlocked?.Invoke(mechanic);
        OnPowerMechanicLogChanged?.Invoke();
        PublishLogEvent("unlocked", mechanic, null, source, GameEventImportance.Success);
        return true;
    }

    public bool CanSpendCharge(PowerMechanicDefinition mechanic, out string failureMessage) {
        if(mechanic == null || !mechanic.ConsumesTrainerCharge) {
            failureMessage = null;
            return true;
        }

        var state = GetOrCreateChargeState(mechanic);
        int remainingCooldown = GetRemainingCooldownHours(mechanic);
        if(remainingCooldown > 0) {
            failureMessage = $"{mechanic.DisplayName} charge is recovering for {remainingCooldown} more hour(s).";
            return false;
        }

        if(mechanic.CooldownHours > 0 && state.availableCharges < mechanic.TrainerChargeCost) {
            state.availableCharges = Mathf.Max(state.availableCharges, mechanic.TrainerChargeCost);
        }

        if(state.availableCharges < mechanic.TrainerChargeCost) {
            failureMessage = $"You need {mechanic.TrainerChargeCost} charge for {mechanic.DisplayName}.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public void RecordUse(PowerMechanicDefinition mechanic, Pokemon pokemon, string battleRuleId, string source, bool blocked, string failureMessage = null) {
        if(mechanic == null) {
            return;
        }

        var record = new PlayerPowerMechanicUseRecord {
            mechanicId = mechanic.Id,
            mechanicName = mechanic.DisplayName,
            kind = mechanic.Kind,
            chargeGroupId = mechanic.ChargeGroupKey,
            pokemonInstanceId = pokemon != null ? pokemon.InstanceId : string.Empty,
            pokemonName = pokemon != null ? pokemon.NickName : string.Empty,
            battleRuleId = battleRuleId,
            source = source,
            absoluteHour = GetCurrentTotalHour(),
            blocked = blocked,
            failureMessage = failureMessage
        };
        usageHistory.Add(record);

        if(!blocked && mechanic.ConsumesTrainerCharge) {
            SpendCharge(mechanic);
        }

        OnMechanicUsed?.Invoke(mechanic, record);
        OnPowerMechanicLogChanged?.Invoke();
        PublishLogEvent(blocked ? "blocked" : "used", mechanic, record, source, blocked ? GameEventImportance.Warning : GameEventImportance.Success);
    }

    public int GetUseCount(PowerMechanicDefinition mechanic = null, bool includeBlocked = false) {
        return usageHistory.Count(record => record != null
            && (includeBlocked || !record.blocked)
            && (mechanic == null || record.mechanicId == mechanic.Id));
    }

    public int GetKindUseCount(PowerMechanicKind kind, bool includeBlocked = false) {
        return usageHistory.Count(record => record != null
            && (includeBlocked || !record.blocked)
            && record.kind == kind);
    }

    public int GetRemainingCooldownHours(PowerMechanicDefinition mechanic) {
        if(mechanic == null || mechanic.CooldownHours <= 0) {
            return 0;
        }

        var state = GetOrCreateChargeState(mechanic);
        if(state.lastUsedHour < 0) {
            return 0;
        }

        int readyAt = state.lastUsedHour + mechanic.CooldownHours;
        return Mathf.Max(0, readyAt - GetCurrentTotalHour());
    }

    public PlayerPowerMechanicChargeState GetOrCreateChargeState(PowerMechanicDefinition mechanic) {
        string key = mechanic != null ? mechanic.ChargeGroupKey : string.Empty;
        var state = chargeStates.FirstOrDefault(entry => entry != null && entry.chargeGroupId == key);
        if(state != null) {
            state.availableCharges = Mathf.Max(state.availableCharges, mechanic != null ? mechanic.TrainerChargeCost : 1);
            return state;
        }

        state = new PlayerPowerMechanicChargeState {
            chargeGroupId = key,
            displayName = mechanic != null ? mechanic.Kind.ToString() : key,
            availableCharges = mechanic != null ? mechanic.TrainerChargeCost : 1,
            maxCharges = mechanic != null ? mechanic.TrainerChargeCost : 1,
            lastUsedHour = -1
        };
        chargeStates.Add(state);
        return state;
    }

    public void RestoreCharge(PowerMechanicDefinition mechanic, int amount = 1) {
        if(mechanic == null || amount <= 0) {
            return;
        }

        var state = GetOrCreateChargeState(mechanic);
        state.availableCharges = Mathf.Clamp(state.availableCharges + amount, 0, Mathf.Max(state.maxCharges, mechanic.TrainerChargeCost));
        OnPowerMechanicLogChanged?.Invoke();
    }

    void SpendCharge(PowerMechanicDefinition mechanic) {
        var state = GetOrCreateChargeState(mechanic);
        state.maxCharges = Mathf.Max(state.maxCharges, mechanic.TrainerChargeCost);
        state.availableCharges = Mathf.Max(0, state.availableCharges - mechanic.TrainerChargeCost);
        state.lastUsedHour = GetCurrentTotalHour();
        if(mechanic.CooldownHours <= 0) {
            state.availableCharges = state.maxCharges;
        }
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishLogEvent(string phase, PowerMechanicDefinition mechanic, PlayerPowerMechanicUseRecord record, string source, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            null,
            $"power-mechanic-log.{phase}.{mechanic.Id}",
            $"Power mechanic {phase}: {mechanic.DisplayName}.",
            GameEventCategory.Battle,
            importance,
            this,
            "PlayerPowerMechanicLog",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("mechanicId", mechanic.Id),
            GameEventPublishing.Value("mechanicName", mechanic.DisplayName),
            GameEventPublishing.Value("kind", mechanic.Kind),
            GameEventPublishing.Value("pokemonName", record != null ? record.pokemonName : string.Empty),
            GameEventPublishing.Value("source", source));
    }

    public object CaptureState() {
        return new PlayerPowerMechanicLogSaveData {
            unlockedMechanicIds = unlockedMechanicIds.Distinct().ToList(),
            chargeStates = chargeStates.Where(state => state != null).Select(state => state.Clone()).ToList(),
            usageHistory = usageHistory.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerPowerMechanicLogSaveData;
        unlockedMechanicIds = saveData?.unlockedMechanicIds?.Distinct().ToList() ?? new List<string>();
        chargeStates = saveData?.chargeStates?.Where(entry => entry != null).Select(entry => entry.Clone()).ToList() ?? new List<PlayerPowerMechanicChargeState>();
        usageHistory = saveData?.usageHistory?.Where(entry => entry != null).Select(entry => entry.Clone()).ToList() ?? new List<PlayerPowerMechanicUseRecord>();
        OnPowerMechanicLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerPowerMechanicChargeState {
    [Tooltip("Saved charge group id. Mechanics can share a charge group by kind or custom id.")]
    public string chargeGroupId;
    [Tooltip("Readable charge group name for fallback/debug output.")]
    public string displayName;
    [Tooltip("Current available charges.")]
    [Min(0)]
    public int availableCharges = 1;
    [Tooltip("Maximum charges for this group.")]
    [Min(1)]
    public int maxCharges = 1;
    [Tooltip("Last in-game total hour this charge group was used. -1 means never.")]
    public int lastUsedHour = -1;

    public PlayerPowerMechanicChargeState Clone() {
        return new PlayerPowerMechanicChargeState {
            chargeGroupId = chargeGroupId,
            displayName = displayName,
            availableCharges = availableCharges,
            maxCharges = maxCharges,
            lastUsedHour = lastUsedHour
        };
    }
}

[Serializable]
public class PlayerPowerMechanicUseRecord {
    [Tooltip("Saved mechanic id.")]
    public string mechanicId;
    [Tooltip("Saved mechanic display name for fallback/debug output.")]
    public string mechanicName;
    [Tooltip("Saved mechanic kind.")]
    public PowerMechanicKind kind;
    [Tooltip("Charge group used by this mechanic.")]
    public string chargeGroupId;
    [Tooltip("Pokemon instance id that used the mechanic.")]
    public string pokemonInstanceId;
    [Tooltip("Pokemon display name saved for fallback/debug output.")]
    public string pokemonName;
    [Tooltip("Battle rule id active when this mechanic was used.")]
    public string battleRuleId;
    [Tooltip("Short source id that triggered this use.")]
    public string source;
    [Tooltip("In-game total hour of this record.")]
    public int absoluteHour;
    [Tooltip("If enabled, this record is a blocked attempt rather than a successful use.")]
    public bool blocked;
    [Tooltip("Failure message saved when blocked.")]
    public string failureMessage;

    public PlayerPowerMechanicUseRecord Clone() {
        return new PlayerPowerMechanicUseRecord {
            mechanicId = mechanicId,
            mechanicName = mechanicName,
            kind = kind,
            chargeGroupId = chargeGroupId,
            pokemonInstanceId = pokemonInstanceId,
            pokemonName = pokemonName,
            battleRuleId = battleRuleId,
            source = source,
            absoluteHour = absoluteHour,
            blocked = blocked,
            failureMessage = failureMessage
        };
    }
}

[Serializable]
public class PlayerPowerMechanicLogSaveData {
    [Tooltip("Saved unlocked power mechanic ids.")]
    public List<string> unlockedMechanicIds = new List<string>();
    [Tooltip("Saved trainer charge states.")]
    public List<PlayerPowerMechanicChargeState> chargeStates = new List<PlayerPowerMechanicChargeState>();
    [Tooltip("Saved mechanic usage records.")]
    public List<PlayerPowerMechanicUseRecord> usageHistory = new List<PlayerPowerMechanicUseRecord>();
}
