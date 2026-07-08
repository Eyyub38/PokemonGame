using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Companion/Pokemon Roster Definition")]
public class CompanionPokemonRosterDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this companion Pokemon roster. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining who this roster belongs to or how it should be used.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as rival, researcher, rider, police, contest or early-game.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Pokemon")]
    [Tooltip("Pokemon created for this companion when their team initializes from this roster.")]
    [SerializeField] List<CompanionPokemonRosterEntry> pokemon = new List<CompanionPokemonRosterEntry>();
    [Tooltip("Maximum Pokemon copied from this roster. 0 means all valid entries.")]
    [Min(0)]
    [SerializeField] int maxPokemon;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public IReadOnlyList<CompanionPokemonRosterEntry> Pokemon => pokemon != null ? (IReadOnlyList<CompanionPokemonRosterEntry>)pokemon : Array.Empty<CompanionPokemonRosterEntry>();
    public int MaxPokemon => Mathf.Max(0, maxPokemon);

    public List<Pokemon> CreatePokemon() {
        var created = new List<Pokemon>();
        if(pokemon == null) {
            return created;
        }

        foreach(var entry in pokemon) {
            if(entry == null || !entry.CanCreate) {
                continue;
            }

            created.Add(entry.CreatePokemon());
            if(MaxPokemon > 0 && created.Count >= MaxPokemon) {
                break;
            }
        }

        return created;
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        return tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class CompanionPokemonRosterEntry {
    [Header("Identity")]
    [Tooltip("Pokemon species/base data used to create this companion Pokemon.")]
    public PokemonBase pokemon;
    [Tooltip("Optional nickname assigned after the Pokemon is created. Empty uses species name.")]
    public string nickname;
    [Tooltip("Starting level for this companion Pokemon.")]
    [Min(1)]
    public int level = 5;
    [Tooltip("Gender override. None lets Pokemon initialization roll or choose the species default.")]
    public Gender gender = Gender.None;
    [Tooltip("Pokeball assigned to this Pokemon.")]
    public PokeballItem pokeball;
    [Tooltip("Held item assigned after creation.")]
    public ItemBase heldItem;

    [Header("Moves")]
    [Tooltip("If enabled, generated level-up moves are replaced by the Custom Moves list.")]
    public bool overrideMoves;
    [Tooltip("Moves added when Override Moves is enabled. Empty leaves the Pokemon with no custom moves.")]
    public List<MoveBase> customMoves = new List<MoveBase>();

    [Header("State")]
    [Tooltip("If enabled, the Pokemon is healed after creation.")]
    public bool startHealed = true;
    [Tooltip("Initial friendship override. -1 keeps the Pokemon default.")]
    public int friendshipOverride = -1;
    [Tooltip("Free-form tags for future UI/filtering. These are roster metadata, not saved on Pokemon.")]
    public List<string> tags = new List<string>();

    public bool CanCreate => pokemon != null && level > 0;

    public Pokemon CreatePokemon() {
        var created = new Pokemon(pokemon, Mathf.Max(1, level), pokeball);
        if(!string.IsNullOrWhiteSpace(nickname)) {
            created.Nickname = nickname;
        }

        if(gender != Gender.None) {
            created.Gender = gender;
        }

        if(heldItem != null) {
            created.HeldItem = heldItem;
        }

        if(overrideMoves) {
            created.Moves.Clear();
            if(customMoves != null) {
                foreach(var move in customMoves) {
                    if(move != null && !created.HasMove(move)) {
                        created.LearnMove(move);
                    }
                }
            }
        }

        if(startHealed) {
            created.Heal();
        }

        if(friendshipOverride >= 0) {
            created.Friendship = Mathf.Clamp(friendshipOverride, 0, 255);
        }

        return created;
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        return tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}
