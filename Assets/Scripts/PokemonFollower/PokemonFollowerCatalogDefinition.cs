using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Pokemon Follower/Follower Catalog")]
public class PokemonFollowerCatalogDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this follower catalog. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note for this catalog.")]
    [TextArea]
    [SerializeField] string description = string.Empty;

    [Header("Definitions")]
    [Tooltip("Follower definitions checked in order. The first definition that matches the selected Pokemon is used.")]
    [SerializeField] List<PokemonFollowerVisualDefinition> definitions = new List<PokemonFollowerVisualDefinition>();
    [Tooltip("Optional fallback definition used when no ordered definition matches. Usually this should allow any Pokemon and use fallback sprites.")]
    [SerializeField] PokemonFollowerVisualDefinition fallbackDefinition;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<PokemonFollowerVisualDefinition> Definitions => definitions != null ? (IReadOnlyList<PokemonFollowerVisualDefinition>)definitions : Array.Empty<PokemonFollowerVisualDefinition>();
    public PokemonFollowerVisualDefinition FallbackDefinition => fallbackDefinition;

    public PokemonFollowerVisualDefinition FindDefinition(Pokemon pokemon) {
        if(pokemon == null) {
            return null;
        }

        var definition = definitions != null
            ? definitions.FirstOrDefault(entry => entry != null && entry.Matches(pokemon))
            : null;

        if(definition != null) {
            return definition;
        }

        return fallbackDefinition != null && fallbackDefinition.Matches(pokemon) ? fallbackDefinition : null;
    }
}
