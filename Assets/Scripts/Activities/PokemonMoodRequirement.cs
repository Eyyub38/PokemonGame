using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Requirements/Pokemon Mood Requirement")]
public class PokemonMoodRequirement : ActivityRequirement {
    [Tooltip("Pokemon mood to check.")]
    [SerializeField] PokemonMoodDefinition mood;
    [Tooltip("Minimum required value for the selected mood.")]
    [SerializeField] int minimumValue = 50;
    [Tooltip("If enabled, one party member can satisfy the requirement. If disabled, all candidates must satisfy it.")]
    [SerializeField] bool anyPartyMember = true;
    [Tooltip("If enabled, fainted Pokemon are ignored.")]
    [SerializeField] bool healthyPokemonOnly = true;

    public override bool IsMet(PlayerController player) {
        if(player == null || mood == null) {
            return false;
        }

        var party = player.GetComponent<PokemonParty>();
        if(party == null || party.Pokemons == null || party.Pokemons.Count == 0) {
            return false;
        }

        var candidates = party.Pokemons.Where(p => p != null);
        if(healthyPokemonOnly) {
            candidates = candidates.Where(p => p.HP > 0);
        }

        return anyPartyMember
            ? candidates.Any(p => p.GetMoodValue(mood) >= minimumValue)
            : candidates.All(p => p.GetMoodValue(mood) >= minimumValue);
    }
}
