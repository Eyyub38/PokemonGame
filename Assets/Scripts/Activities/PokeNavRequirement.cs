using UnityEngine;

public enum PokeNavRequirementMode {
    PokemonKnowledgeAtLeast,
    EntryDiscovered,
    RegionDiscovered,
    SocialPostUnlocked,
    SocialPostRead
}

[CreateAssetMenu(menuName = "Activities/Requirements/PokeNav Requirement")]
public class PokeNavRequirement : ActivityRequirement {
    [Tooltip("Which PokeNav value this requirement checks.")]
    [SerializeField] PokeNavRequirementMode mode = PokeNavRequirementMode.PokemonKnowledgeAtLeast;
    [Tooltip("Pokemon checked by Pokemon Knowledge At Least mode.")]
    [SerializeField] PokemonBase pokemon;
    [Tooltip("Minimum knowledge level required by Pokemon Knowledge At Least mode.")]
    [SerializeField] PokemonKnowledgeLevel minimumKnowledge = PokemonKnowledgeLevel.Seen;
    [Tooltip("Knowledge entry checked by Entry Discovered mode.")]
    [SerializeField] PokeNavEntryDefinition entry;
    [Tooltip("Region checked by Region Discovered mode.")]
    [SerializeField] RegionInfoDefinition region;
    [Tooltip("Social post checked by Social Post modes.")]
    [SerializeField] SocialPostDefinition socialPost;
    [Tooltip("If enabled, the selected PokeNav condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerPokeNavLog>() : null;
        bool result = mode switch {
            PokeNavRequirementMode.EntryDiscovered => log != null && log.HasDiscoveredEntry(entry),
            PokeNavRequirementMode.RegionDiscovered => log != null && log.HasDiscoveredRegion(region),
            PokeNavRequirementMode.SocialPostUnlocked => log != null && log.HasUnlockedPost(socialPost),
            PokeNavRequirementMode.SocialPostRead => log != null && log.IsPostRead(socialPost),
            _ => log != null && log.GetPokemonKnowledgeLevel(pokemon) >= minimumKnowledge
        };

        return mustBeMet ? result : !result;
    }
}
