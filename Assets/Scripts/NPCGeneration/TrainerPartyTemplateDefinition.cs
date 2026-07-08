using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC Generation/Trainer Party Template")]
public class TrainerPartyTemplateDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this trainer party template. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in editor/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note for this trainer party template.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Free-form tags used by NPC pools and future filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Party")]
    [Tooltip("Pokemon slots generated for this trainer.")]
    [SerializeField] List<TrainerPartySlot> partySlots = new List<TrainerPartySlot>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags;
    public IReadOnlyList<TrainerPartySlot> PartySlots => partySlots;

    public List<Pokemon> CreateParty(int seed) {
        var random = new System.Random(seed);
        var party = new List<Pokemon>();
        foreach(var slot in partySlots) {
            var pokemon = slot?.CreatePokemon(random);
            if(pokemon != null) {
                party.Add(pokemon);
            }
        }

        return party;
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        foreach(var entry in tags) {
            if(string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }
}

[System.Serializable]
public class TrainerPartySlot {
    [Tooltip("Pokemon species pool for this slot. One is picked randomly.")]
    [SerializeField] List<PokemonBase> pokemonPool = new List<PokemonBase>();
    [Tooltip("Minimum level for this slot.")]
    [Min(1)]
    [SerializeField] int minLevel = 3;
    [Tooltip("Maximum level for this slot.")]
    [Min(1)]
    [SerializeField] int maxLevel = 5;
    [Tooltip("Chance for this slot to be included. 100 means always.")]
    [Range(0f, 100f)]
    [SerializeField] float includeChancePercent = 100f;

    public IReadOnlyList<PokemonBase> PokemonPool => pokemonPool;
    public int MinLevel => Mathf.Max(1, minLevel);
    public int MaxLevel => Mathf.Max(MinLevel, maxLevel);
    public float IncludeChancePercent => Mathf.Clamp(includeChancePercent, 0f, 100f);

    public Pokemon CreatePokemon(System.Random random) {
        if(random == null || pokemonPool == null || pokemonPool.Count == 0) {
            return null;
        }

        if(random.NextDouble() * 100d > IncludeChancePercent) {
            return null;
        }

        var validPool = pokemonPool.FindAll(p => p != null);
        if(validPool.Count == 0) {
            return null;
        }

        var pokemon = validPool[random.Next(0, validPool.Count)];
        int level = random.Next(MinLevel, MaxLevel + 1);
        return new Pokemon(pokemon, level);
    }
}
