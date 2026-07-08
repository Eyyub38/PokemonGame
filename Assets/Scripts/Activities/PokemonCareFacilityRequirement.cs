using System.Linq;
using UnityEngine;

public enum PokemonCareFacilityRequirementMode {
    ActiveStayCountAtLeast,
    ActiveStayCountAtMost,
    AnyPokemonStaying,
    SelectedPokemonStaying,
    CompletedStayCountAtLeast,
    TotalCareActionsAppliedAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/Pokemon Care Facility Requirement")]
public class PokemonCareFacilityRequirement : ActivityRequirement {
    [Tooltip("Care facility checked by this requirement.")]
    [SerializeField] PokemonCareFacilityDefinition facility;
    [Tooltip("Optional scene/local facility id. Empty checks every instance of the selected facility.")]
    [SerializeField] string facilityInstanceId;
    [Tooltip("Which care facility condition this requirement checks.")]
    [SerializeField] PokemonCareFacilityRequirementMode mode = PokemonCareFacilityRequirementMode.ActiveStayCountAtLeast;
    [Tooltip("Required count depending on mode.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("If enabled, the selected condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerPokemonCareFacilityLog>() : null;
        if(log == null || facility == null) {
            return !mustBeMet;
        }

        bool result = mode switch {
            PokemonCareFacilityRequirementMode.ActiveStayCountAtMost => log.GetActiveStayCount(facility, facilityInstanceId) <= requiredValue,
            PokemonCareFacilityRequirementMode.AnyPokemonStaying => log.GetActiveStayCount(facility, facilityInstanceId) > 0,
            PokemonCareFacilityRequirementMode.SelectedPokemonStaying => IsFirstHealthyPokemonStaying(player, log),
            PokemonCareFacilityRequirementMode.CompletedStayCountAtLeast => GetCompletedStayCount(log) >= Mathf.Max(0, requiredValue),
            PokemonCareFacilityRequirementMode.TotalCareActionsAppliedAtLeast => GetTotalCareActionsApplied(log) >= Mathf.Max(0, requiredValue),
            _ => log.GetActiveStayCount(facility, facilityInstanceId) >= Mathf.Max(0, requiredValue)
        };

        return mustBeMet ? result : !result;
    }

    bool IsFirstHealthyPokemonStaying(PlayerController player, PlayerPokemonCareFacilityLog log) {
        var pokemon = player?.GetComponent<PokemonParty>()?.GetHealthyPokemon();
        return pokemon != null && log.HasActiveStay(pokemon, facility);
    }

    int GetCompletedStayCount(PlayerPokemonCareFacilityLog log) {
        return log.StayHistory.Count(history => history != null
            && history.facilityId == facility.Id
            && (string.IsNullOrWhiteSpace(facilityInstanceId) || history.facilityInstanceId == facilityInstanceId));
    }

    int GetTotalCareActionsApplied(PlayerPokemonCareFacilityLog log) {
        int active = log.ActiveStays
            .Where(stay => stay != null
                && stay.facilityId == facility.Id
                && (string.IsNullOrWhiteSpace(facilityInstanceId) || stay.facilityInstanceId == facilityInstanceId))
            .Sum(stay => Mathf.Max(0, stay.totalCareActionsApplied));

        int completed = log.StayHistory
            .Where(history => history != null
                && history.facilityId == facility.Id
                && (string.IsNullOrWhiteSpace(facilityInstanceId) || history.facilityInstanceId == facilityInstanceId))
            .Sum(history => Mathf.Max(0, history.totalCareActionsApplied));

        return active + completed;
    }
}
