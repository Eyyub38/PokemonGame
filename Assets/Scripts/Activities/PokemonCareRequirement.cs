using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokemonCareRequirementMode {
    CareActionCount,
    CareCategoryCount,
    CareNeedAtLeast,
    CareNeedAtMost,
    HoursSinceLastCareActionAtMost,
    HoursSinceLastCareCategoryAtMost
}

public enum PokemonCareRequirementTarget {
    FirstHealthyPokemon,
    AnyPartyPokemon,
    AllPartyPokemon
}

[CreateAssetMenu(menuName = "Activities/Requirements/Pokemon Care Requirement")]
public class PokemonCareRequirement : ActivityRequirement {
    [Tooltip("Which Pokemon care value this requirement checks.")]
    [SerializeField] PokemonCareRequirementMode mode = PokemonCareRequirementMode.CareActionCount;
    [Tooltip("Which Pokemon or Pokemon group this requirement checks.")]
    [SerializeField] PokemonCareRequirementTarget target = PokemonCareRequirementTarget.FirstHealthyPokemon;
    [Tooltip("Care action checked by action-specific modes. Empty accepts any action.")]
    [SerializeField] PokemonCareActionDefinition careAction;
    [Tooltip("Care category checked by category-specific modes.")]
    [SerializeField] PokemonCareCategory category = PokemonCareCategory.General;
    [Tooltip("Care need checked by need-specific modes.")]
    [SerializeField] PokemonCareNeedDefinition careNeed;
    [Tooltip("Required count, value, or max hours depending on mode.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("If enabled, the selected Pokemon care condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        var candidates = GetCandidates(party);
        if(candidates.Count == 0) {
            return !mustBeMet;
        }

        bool result = target == PokemonCareRequirementTarget.AllPartyPokemon
            ? candidates.All(EvaluatePokemon)
            : candidates.Any(EvaluatePokemon);

        return mustBeMet ? result : !result;
    }

    List<Pokemon> GetCandidates(PokemonParty party) {
        if(party == null || party.Pokemons == null) {
            return new List<Pokemon>();
        }

        if(target == PokemonCareRequirementTarget.FirstHealthyPokemon) {
            var pokemon = party.GetHealthyPokemon();
            return pokemon != null ? new List<Pokemon> { pokemon } : new List<Pokemon>();
        }

        return party.Pokemons.Where(pokemon => pokemon != null && pokemon.HP > 0).ToList();
    }

    bool EvaluatePokemon(Pokemon pokemon) {
        int value = mode switch {
            PokemonCareRequirementMode.CareCategoryCount => pokemon.GetCareCategoryCount(category),
            PokemonCareRequirementMode.CareNeedAtLeast => pokemon.GetCareNeedValue(careNeed),
            PokemonCareRequirementMode.CareNeedAtMost => pokemon.GetCareNeedValue(careNeed),
            PokemonCareRequirementMode.HoursSinceLastCareActionAtMost => pokemon.GetHoursSinceLastCare(careAction),
            PokemonCareRequirementMode.HoursSinceLastCareCategoryAtMost => pokemon.GetHoursSinceLastCare(null, category),
            _ => pokemon.GetCareActionCount(careAction)
        };

        if(mode == PokemonCareRequirementMode.CareNeedAtMost) {
            return value <= requiredValue;
        }

        if(mode == PokemonCareRequirementMode.HoursSinceLastCareActionAtMost || mode == PokemonCareRequirementMode.HoursSinceLastCareCategoryAtMost) {
            return value >= 0 && value <= Mathf.Max(0, requiredValue);
        }

        return value >= Mathf.Max(0, requiredValue);
    }
}
