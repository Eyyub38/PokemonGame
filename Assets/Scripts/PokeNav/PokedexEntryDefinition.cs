using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokemonKnowledgeLevel {
    Unknown,
    Seen,
    Battled,
    Caught,
    Researched
}

[CreateAssetMenu(menuName = "PokeNav/Pokedex Entry Definition")]
public class PokedexEntryDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this Pokedex entry. Empty uses the related Pokemon asset name or this asset name.")]
    [SerializeField] string id;
    [Tooltip("Pokemon species represented by this entry.")]
    [SerializeField] PokemonBase pokemon;
    [Tooltip("Name shown in Pokedex UI. Empty uses the Pokemon name or asset name.")]
    [SerializeField] string displayNameOverride;
    [Tooltip("Free-form category/species text shown by future Pokedex UI, such as Seed Pokemon or River Pokemon.")]
    [SerializeField] string classification;
    [Tooltip("Free-form tags used by filters, social posts and future map UI.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Notes")]
    [Tooltip("Short public note visible even before the Pokemon is fully researched.")]
    [TextArea]
    [SerializeField] string publicNote;
    [Tooltip("Note shown after the Pokemon has been seen.")]
    [TextArea]
    [SerializeField] string seenNote;
    [Tooltip("Note shown after the Pokemon has been battled.")]
    [TextArea]
    [SerializeField] string battledNote;
    [Tooltip("Note shown after the Pokemon has been caught.")]
    [TextArea]
    [SerializeField] string caughtNote;
    [Tooltip("Note shown after the Pokemon has been researched.")]
    [TextArea]
    [SerializeField] string researchedNote;

    [Header("Habitat")]
    [Tooltip("Known or discoverable habitat records for this Pokemon.")]
    [SerializeField] List<PokedexHabitatInfo> habitats = new List<PokedexHabitatInfo>();

    [Header("Research")]
    [Tooltip("Research subjects that can reveal this Pokemon's researched-level information.")]
    [SerializeField] List<ResearchSubjectDefinition> relatedResearch = new List<ResearchSubjectDefinition>();
    [Tooltip("Pokemon care actions or notes that future UI may show after research.")]
    [SerializeField] List<PokedexCareHint> careHints = new List<PokedexCareHint>();

    public string Id => !string.IsNullOrWhiteSpace(id) ? id : pokemon != null ? pokemon.name : name;
    public PokemonBase Pokemon => pokemon;
    public string DisplayName => !string.IsNullOrWhiteSpace(displayNameOverride) ? displayNameOverride : pokemon != null ? pokemon.Name : name;
    public string Classification => classification;
    public IReadOnlyList<string> Tags => tags;
    public string PublicNote => publicNote;
    public IReadOnlyList<PokedexHabitatInfo> Habitats => habitats;
    public IReadOnlyList<ResearchSubjectDefinition> RelatedResearch => relatedResearch;
    public IReadOnlyList<PokedexCareHint> CareHints => careHints;

    public string GetBestNote(PokemonKnowledgeLevel level) {
        if(level >= PokemonKnowledgeLevel.Researched && !string.IsNullOrWhiteSpace(researchedNote)) return researchedNote;
        if(level >= PokemonKnowledgeLevel.Caught && !string.IsNullOrWhiteSpace(caughtNote)) return caughtNote;
        if(level >= PokemonKnowledgeLevel.Battled && !string.IsNullOrWhiteSpace(battledNote)) return battledNote;
        if(level >= PokemonKnowledgeLevel.Seen && !string.IsNullOrWhiteSpace(seenNote)) return seenNote;
        return publicNote;
    }

    public List<PokedexHabitatInfo> GetVisibleHabitats(PokemonKnowledgeLevel level) {
        return (habitats ?? new List<PokedexHabitatInfo>())
            .Where(habitat => habitat != null && level >= habitat.minimumKnowledgeToReveal)
            .ToList();
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class PokedexHabitatInfo {
    [Tooltip("Region where this Pokemon can be found.")]
    public RegionInfoDefinition region;
    [Tooltip("Encounter table that can produce this Pokemon.")]
    public EncounterTableDefinition encounterTable;
    [Tooltip("Encounter source type, such as grass, water, tree or roaming.")]
    public EncounterSourceType sourceType = EncounterSourceType.Any;
    [Tooltip("Minimum Pokemon knowledge level required before this habitat appears in UI.")]
    public PokemonKnowledgeLevel minimumKnowledgeToReveal = PokemonKnowledgeLevel.Seen;
    [Tooltip("Optional designer/player-facing habitat note.")]
    [TextArea]
    public string note;
}

[Serializable]
public class PokedexCareHint {
    [Tooltip("Minimum Pokemon knowledge level required before this care hint appears in UI.")]
    public PokemonKnowledgeLevel minimumKnowledgeToReveal = PokemonKnowledgeLevel.Researched;
    [Tooltip("Optional care action connected to this hint.")]
    public PokemonCareActionDefinition careAction;
    [Tooltip("Care, mood, bait or handling note shown by future Pokedex UI.")]
    [TextArea]
    public string note;
}
