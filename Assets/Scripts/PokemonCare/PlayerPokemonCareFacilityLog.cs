using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPokemonCareFacilityLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save list of Pokemon currently staying in care facilities.")]
    [SerializeField] List<PlayerPokemonCareFacilityStay> activeStays = new List<PlayerPokemonCareFacilityStay>();
    [Tooltip("Runtime/save history of completed care facility stays.")]
    [SerializeField] List<PlayerPokemonCareFacilityHistory> stayHistory = new List<PlayerPokemonCareFacilityHistory>();

    public IReadOnlyList<PlayerPokemonCareFacilityStay> ActiveStays => activeStays;
    public IReadOnlyList<PlayerPokemonCareFacilityHistory> StayHistory => stayHistory;
    public event Action OnFacilityLogChanged;

    public bool HasActiveStay(Pokemon pokemon, PokemonCareFacilityDefinition facility = null) {
        if(pokemon == null) {
            return false;
        }

        return activeStays.Any(stay => stay != null
            && stay.pokemonId == pokemon.InstanceId
            && (facility == null || stay.facilityId == facility.Id));
    }

    public int GetActiveStayCount(PokemonCareFacilityDefinition facility, string facilityInstanceId = null) {
        if(facility == null) {
            return 0;
        }

        return activeStays.Count(stay => stay != null
            && stay.facilityId == facility.Id
            && (string.IsNullOrWhiteSpace(facilityInstanceId) || stay.facilityInstanceId == facilityInstanceId));
    }

    public List<PlayerPokemonCareFacilityStay> GetActiveStays(PokemonCareFacilityDefinition facility, string facilityInstanceId = null) {
        if(facility == null) {
            return new List<PlayerPokemonCareFacilityStay>();
        }

        return activeStays
            .Where(stay => stay != null
                && stay.facilityId == facility.Id
                && (string.IsNullOrWhiteSpace(facilityInstanceId) || stay.facilityInstanceId == facilityInstanceId))
            .ToList();
    }

    public PlayerPokemonCareFacilityStay GetActiveStay(Pokemon pokemon, PokemonCareFacilityDefinition facility = null) {
        if(pokemon == null) {
            return null;
        }

        return activeStays.FirstOrDefault(stay => stay != null
            && stay.pokemonId == pokemon.InstanceId
            && (facility == null || stay.facilityId == facility.Id));
    }

    public bool TryAdmit(PlayerController player, PokemonCareFacilityDefinition facility, Pokemon pokemon, string facilityInstanceId, out string failureMessage) {
        if(facility == null) {
            failureMessage = "No care facility selected.";
            return false;
        }

        if(!facility.CanAdmit(player, pokemon, this, facilityInstanceId, out failureMessage)) {
            return false;
        }

        if(facility.AdmissionActivity != null && !facility.AdmissionActivity.TryPayCosts(player, out failureMessage)) {
            return false;
        }

        var stay = new PlayerPokemonCareFacilityStay {
            facilityId = facility.Id,
            facilityName = facility.DisplayName,
            facilityInstanceId = facilityInstanceId,
            pokemonId = pokemon.InstanceId,
            pokemonName = pokemon.NickName,
            speciesName = pokemon.Base != null ? pokemon.Base.Name : null,
            enteredDay = GetCurrentDay(),
            enteredAbsoluteHour = GetCurrentAbsoluteHour(),
            lastProcessedAbsoluteHour = GetCurrentAbsoluteHour()
        };

        activeStays.Add(stay);
        int appliedCount = facility.ApplyAdmissionCare(pokemon, stay, facilityInstanceId);
        ApplyActivityRewards(player, facility.AdmissionActivity);
        facility.PublishAdmitted(player, pokemon, facilityInstanceId);
        if(appliedCount > 0) {
            facility.PublishCareProcessed(player, pokemon, facilityInstanceId, appliedCount);
        }

        OnFacilityLogChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public bool TryRelease(PlayerController player, PokemonCareFacilityDefinition facility, Pokemon pokemon, string facilityInstanceId, bool applyReleaseCare, out string failureMessage) {
        if(facility == null) {
            failureMessage = "No care facility selected.";
            return false;
        }

        var stay = pokemon != null
            ? GetActiveStay(pokemon, facility)
            : GetActiveStays(facility, facilityInstanceId).FirstOrDefault();

        if(stay == null) {
            failureMessage = "No Pokemon is staying in this facility.";
            return false;
        }

        if(!facility.CanRelease(player, stay, out failureMessage)) {
            return false;
        }

        if(facility.ReleaseActivity != null && !facility.ReleaseActivity.TryPayCosts(player, out failureMessage)) {
            return false;
        }

        pokemon ??= ResolvePokemon(player, stay.pokemonId);
        int appliedCount = applyReleaseCare ? facility.ApplyReleaseCare(pokemon, stay, facilityInstanceId) : 0;
        activeStays.Remove(stay);
        stayHistory.Add(PlayerPokemonCareFacilityHistory.FromStay(stay, GetCurrentDay(), GetCurrentAbsoluteHour()));
        ApplyActivityRewards(player, facility.ReleaseActivity);
        facility.PublishReleased(player, pokemon, facilityInstanceId, appliedCount);
        OnFacilityLogChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public int ProcessDueCare(PlayerController player, PokemonCareFacilityDefinition facility, string facilityInstanceId = null) {
        if(player == null || facility == null) {
            return 0;
        }

        int appliedTotal = 0;
        foreach(var stay in GetActiveStays(facility, facilityInstanceId)) {
            var pokemon = ResolvePokemon(player, stay.pokemonId);
            if(pokemon == null) {
                continue;
            }

            int applied = facility.ProcessDueCare(pokemon, stay, stay.facilityInstanceId);
            if(applied > 0) {
                appliedTotal += applied;
                facility.PublishCareProcessed(player, pokemon, stay.facilityInstanceId, applied);
            }
        }

        if(appliedTotal > 0) {
            OnFacilityLogChanged?.Invoke();
        }

        return appliedTotal;
    }

    public Pokemon ResolvePokemon(PlayerController player, string pokemonId) {
        if(player == null || string.IsNullOrWhiteSpace(pokemonId)) {
            return null;
        }

        var party = player.GetComponent<PokemonParty>();
        return party?.Pokemons?.FirstOrDefault(pokemon => pokemon != null && pokemon.InstanceId == pokemonId);
    }

    void ApplyActivityRewards(PlayerController player, ActivityDefinition activity) {
        if(player == null || activity == null) {
            return;
        }

        int experienceReward = PlayerActivityContext.ModifyExperience(activity, activity.BaseExperience);
        if(WorldEventManager.i != null) {
            experienceReward = WorldEventManager.i.ModifyExperience(activity, experienceReward);
            WorldEventManager.i.ApplyActivityReputation(player, activity);
        } else {
            player.GetComponent<PlayerReputation>()?.ApplyChanges(activity.ReputationChanges);
        }

        player.GetComponent<PlayerProgression>()?.AddExperience(experienceReward, activity.ExperienceSource);
        activity.ApplyRelationshipRewards(player);
        activity.RecordCompletion(player);
        activity.CompleteMilestones(player);
        activity.ApplyCareerPointRewards(player);
        activity.ApplyLifePathRewards(player);
        activity.ApplyOrganizationRewards(player);
        activity.ApplyOutcomes(player);
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerPokemonCareFacilityLogSaveData {
            activeStays = activeStays.Where(stay => stay != null).Select(stay => stay.ToSaveData()).ToList(),
            stayHistory = stayHistory.Where(history => history != null).Select(history => history.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerPokemonCareFacilityLogSaveData;
        activeStays = saveData?.activeStays?.Where(stay => stay != null).Select(stay => new PlayerPokemonCareFacilityStay(stay)).ToList()
            ?? new List<PlayerPokemonCareFacilityStay>();
        stayHistory = saveData?.stayHistory?.Where(history => history != null).Select(history => new PlayerPokemonCareFacilityHistory(history)).ToList()
            ?? new List<PlayerPokemonCareFacilityHistory>();
        OnFacilityLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerPokemonCareFacilityStay {
    [Tooltip("Saved care facility definition id.")]
    public string facilityId;
    [Tooltip("Saved care facility display name.")]
    public string facilityName;
    [Tooltip("Scene/local facility instance id.")]
    public string facilityInstanceId;
    [Tooltip("Saved Pokemon instance id.")]
    public string pokemonId;
    [Tooltip("Saved Pokemon display/nickname.")]
    public string pokemonName;
    [Tooltip("Saved Pokemon species name.")]
    public string speciesName;
    [Tooltip("In-game day when the stay started.")]
    public int enteredDay;
    [Tooltip("Absolute in-game hour when the stay started.")]
    public int enteredAbsoluteHour;
    [Tooltip("Last absolute in-game hour when timed care was processed.")]
    public int lastProcessedAbsoluteHour = -1;
    [Tooltip("Total facility care actions applied during this stay.")]
    [Min(0)]
    public int totalCareActionsApplied;
    [Tooltip("Per-care-action usage counters for this stay.")]
    public List<PlayerPokemonCareFacilityActionUse> actionUses = new List<PlayerPokemonCareFacilityActionUse>();

    public PlayerPokemonCareFacilityStay() {
    }

    public PlayerPokemonCareFacilityStay(PlayerPokemonCareFacilityStaySaveData saveData) {
        if(saveData == null) {
            return;
        }

        facilityId = saveData.facilityId;
        facilityName = saveData.facilityName;
        facilityInstanceId = saveData.facilityInstanceId;
        pokemonId = saveData.pokemonId;
        pokemonName = saveData.pokemonName;
        speciesName = saveData.speciesName;
        enteredDay = saveData.enteredDay;
        enteredAbsoluteHour = saveData.enteredAbsoluteHour;
        lastProcessedAbsoluteHour = saveData.lastProcessedAbsoluteHour;
        totalCareActionsApplied = Mathf.Max(0, saveData.totalCareActionsApplied);
        actionUses = saveData.actionUses ?? new List<PlayerPokemonCareFacilityActionUse>();
    }

    public PlayerPokemonCareFacilityActionUse GetActionUse(PokemonCareActionDefinition careAction) {
        if(careAction == null) {
            return null;
        }

        return actionUses?.FirstOrDefault(use => use != null && use.actionId == careAction.Id);
    }

    public PlayerPokemonCareFacilityActionUse GetOrCreateActionUse(PokemonCareActionDefinition careAction) {
        if(careAction == null) {
            return null;
        }

        actionUses ??= new List<PlayerPokemonCareFacilityActionUse>();
        var useState = GetActionUse(careAction);
        if(useState != null) {
            return useState;
        }

        useState = new PlayerPokemonCareFacilityActionUse {
            actionId = careAction.Id,
            actionName = careAction.DisplayName,
            lastUsedAbsoluteHour = -1
        };
        actionUses.Add(useState);
        return useState;
    }

    public PlayerPokemonCareFacilityStaySaveData ToSaveData() {
        return new PlayerPokemonCareFacilityStaySaveData {
            facilityId = facilityId,
            facilityName = facilityName,
            facilityInstanceId = facilityInstanceId,
            pokemonId = pokemonId,
            pokemonName = pokemonName,
            speciesName = speciesName,
            enteredDay = enteredDay,
            enteredAbsoluteHour = enteredAbsoluteHour,
            lastProcessedAbsoluteHour = lastProcessedAbsoluteHour,
            totalCareActionsApplied = totalCareActionsApplied,
            actionUses = actionUses
        };
    }
}

[Serializable]
public class PlayerPokemonCareFacilityActionUse {
    [Tooltip("Saved care action id.")]
    public string actionId;
    [Tooltip("Saved care action display name.")]
    public string actionName;
    [Tooltip("Number of times this action was applied during the stay.")]
    [Min(0)]
    public int uses;
    [Tooltip("Absolute in-game hour when this action was last applied.")]
    public int lastUsedAbsoluteHour = -1;
}

[Serializable]
public class PlayerPokemonCareFacilityHistory {
    [Tooltip("Saved care facility definition id.")]
    public string facilityId;
    [Tooltip("Saved care facility display name.")]
    public string facilityName;
    [Tooltip("Scene/local facility instance id.")]
    public string facilityInstanceId;
    [Tooltip("Saved Pokemon instance id.")]
    public string pokemonId;
    [Tooltip("Saved Pokemon display/nickname.")]
    public string pokemonName;
    [Tooltip("In-game day when the stay started.")]
    public int enteredDay;
    [Tooltip("Absolute in-game hour when the stay started.")]
    public int enteredAbsoluteHour;
    [Tooltip("In-game day when the stay ended.")]
    public int releasedDay;
    [Tooltip("Absolute in-game hour when the stay ended.")]
    public int releasedAbsoluteHour;
    [Tooltip("Total facility care actions applied during the stay.")]
    [Min(0)]
    public int totalCareActionsApplied;

    public PlayerPokemonCareFacilityHistory() {
    }

    public PlayerPokemonCareFacilityHistory(PlayerPokemonCareFacilityHistorySaveData saveData) {
        if(saveData == null) {
            return;
        }

        facilityId = saveData.facilityId;
        facilityName = saveData.facilityName;
        facilityInstanceId = saveData.facilityInstanceId;
        pokemonId = saveData.pokemonId;
        pokemonName = saveData.pokemonName;
        enteredDay = saveData.enteredDay;
        enteredAbsoluteHour = saveData.enteredAbsoluteHour;
        releasedDay = saveData.releasedDay;
        releasedAbsoluteHour = saveData.releasedAbsoluteHour;
        totalCareActionsApplied = Mathf.Max(0, saveData.totalCareActionsApplied);
    }

    public static PlayerPokemonCareFacilityHistory FromStay(PlayerPokemonCareFacilityStay stay, int releasedDay, int releasedAbsoluteHour) {
        return new PlayerPokemonCareFacilityHistory {
            facilityId = stay.facilityId,
            facilityName = stay.facilityName,
            facilityInstanceId = stay.facilityInstanceId,
            pokemonId = stay.pokemonId,
            pokemonName = stay.pokemonName,
            enteredDay = stay.enteredDay,
            enteredAbsoluteHour = stay.enteredAbsoluteHour,
            releasedDay = releasedDay,
            releasedAbsoluteHour = releasedAbsoluteHour,
            totalCareActionsApplied = stay.totalCareActionsApplied
        };
    }

    public PlayerPokemonCareFacilityHistorySaveData ToSaveData() {
        return new PlayerPokemonCareFacilityHistorySaveData {
            facilityId = facilityId,
            facilityName = facilityName,
            facilityInstanceId = facilityInstanceId,
            pokemonId = pokemonId,
            pokemonName = pokemonName,
            enteredDay = enteredDay,
            enteredAbsoluteHour = enteredAbsoluteHour,
            releasedDay = releasedDay,
            releasedAbsoluteHour = releasedAbsoluteHour,
            totalCareActionsApplied = totalCareActionsApplied
        };
    }
}

[Serializable]
public class PlayerPokemonCareFacilityLogSaveData {
    public List<PlayerPokemonCareFacilityStaySaveData> activeStays;
    public List<PlayerPokemonCareFacilityHistorySaveData> stayHistory;
}

[Serializable]
public class PlayerPokemonCareFacilityStaySaveData {
    public string facilityId;
    public string facilityName;
    public string facilityInstanceId;
    public string pokemonId;
    public string pokemonName;
    public string speciesName;
    public int enteredDay;
    public int enteredAbsoluteHour;
    public int lastProcessedAbsoluteHour;
    public int totalCareActionsApplied;
    public List<PlayerPokemonCareFacilityActionUse> actionUses;
}

[Serializable]
public class PlayerPokemonCareFacilityHistorySaveData {
    public string facilityId;
    public string facilityName;
    public string facilityInstanceId;
    public string pokemonId;
    public string pokemonName;
    public int enteredDay;
    public int enteredAbsoluteHour;
    public int releasedDay;
    public int releasedAbsoluteHour;
    public int totalCareActionsApplied;
}
