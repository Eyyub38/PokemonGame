using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RadialPartyActionKind {
    Summary,
    Switch,
    Item,
    HeldItem,
    Ability,
    Moves,
    Follow,
    Cancel,
    Custom
}

[Serializable]
public class RadialPartyActionDefinition {
    [Tooltip("Action kind represented by this radial option.")]
    public RadialPartyActionKind actionKind = RadialPartyActionKind.Summary;
    [Tooltip("Stable option id. Empty uses the action kind.")]
    public string optionId = string.Empty;
    [Tooltip("Label shown by the radial option tag/frame.")]
    public string label = string.Empty;
    [Tooltip("Description shown by the radial option tag/frame.")]
    [TextArea]
    public string description = string.Empty;
    [Tooltip("Icon shown inside the radial segment.")]
    public Sprite icon;
    [Tooltip("Lower priority appears earlier around the ring.")]
    public int priority;
    [Tooltip("If enabled, this option is always shown even when it is not currently usable.")]
    public bool showWhenDisabled = true;
}

public class RadialPartyMenuProvider : MonoBehaviour, IRadialMenuProvider {
    [Header("Party")]
    [Tooltip("Party screen used to resolve the currently selected Pokemon. Empty uses the first PartyScreen in the scene.")]
    [SerializeField] PartyScreen partyScreen;
    [Tooltip("Explicit party override. Empty uses PokemonParty.GetPlayerParty when available.")]
    [SerializeField] PokemonParty partyOverride;
    [Tooltip("If enabled, the context index is used as the selected party slot when available.")]
    [SerializeField] bool preferContextIndex = true;

    [Header("Actions")]
    [Tooltip("Actions exposed for normal party menu context.")]
    [SerializeField] List<RadialPartyActionDefinition> actions = new List<RadialPartyActionDefinition> {
        new RadialPartyActionDefinition { actionKind = RadialPartyActionKind.Summary, label = "Summary", priority = 0 },
        new RadialPartyActionDefinition { actionKind = RadialPartyActionKind.Switch, label = "Switch", priority = 10 },
        new RadialPartyActionDefinition { actionKind = RadialPartyActionKind.Item, label = "Item", priority = 20 },
        new RadialPartyActionDefinition { actionKind = RadialPartyActionKind.Ability, label = "Ability", priority = 30 },
        new RadialPartyActionDefinition { actionKind = RadialPartyActionKind.Cancel, label = "Back", priority = 100 }
    };
    [Tooltip("If enabled, fainted Pokemon can still open non-battle options such as Summary.")]
    [SerializeField] bool allowSummaryForFainted = true;
    [Tooltip("If enabled, the Switch option is hidden/disabled when there is only one Pokemon.")]
    [SerializeField] bool requireTwoPokemonForSwitch = true;
    [Tooltip("If enabled, Held Item action is only enabled when the selected Pokemon has a held item.")]
    [SerializeField] bool requireHeldItemForHeldItemAction = true;
    [Tooltip("If enabled, Ability action is disabled when the selected Pokemon has no ability.")]
    [SerializeField] bool requireAbilityForAbilityAction = true;
    [Tooltip("If enabled, Moves action is disabled when the selected Pokemon has no active moves.")]
    [SerializeField] bool requireMovesForMovesAction = true;

    [Header("Debug")]
    [Tooltip("If enabled, selected radial party actions are written to GameDebug.")]
    [SerializeField] bool logSelectedActions = true;

    public PartyScreen PartyScreen => partyScreen;
    public PokemonParty PartyOverride => partyOverride;
    public IReadOnlyList<RadialPartyActionDefinition> Actions => actions;
    public event Action<RadialPartyActionKind, Pokemon, int, RadialMenuOption> OnPartyActionSelected;

    public IReadOnlyList<RadialMenuOption> BuildRadialOptions(RadialMenuContext context) {
        var pokemon = ResolvePokemon(context, out int slotIndex);
        if(pokemon == null) {
            return new List<RadialMenuOption> {
                BuildOption(new RadialPartyActionDefinition { actionKind = RadialPartyActionKind.Cancel, label = "Back", priority = 100 }, null, slotIndex, disabled: false, disabledReason: null)
            };
        }

        var result = new List<RadialMenuOption>();
        foreach(var action in actions.OrderBy(action => action != null ? action.priority : int.MaxValue)) {
            if(action == null) {
                continue;
            }

            bool disabled = IsDisabled(action.actionKind, pokemon, slotIndex, out var reason);
            if(disabled && !action.showWhenDisabled) {
                continue;
            }

            result.Add(BuildOption(action, pokemon, slotIndex, disabled, reason));
        }

        return result;
    }

    public void OnRadialOptionSelected(RadialMenuOption option, RadialMenuContext context) {
        var pokemon = ResolvePokemon(context, out int slotIndex);
        var actionKind = ResolveActionKind(option);
        OnPartyActionSelected?.Invoke(actionKind, pokemon, slotIndex, option);

        if(logSelectedActions) {
            string pokemonName = pokemon != null ? pokemon.Base.Name : "No Pokemon";
            GameDebug.Step($"Party radial action selected: {actionKind} for {pokemonName}.", GameDebugCategory.UI, this, "RadialPartyMenuProvider");
        }
    }

    public void OnRadialMenuClosed(RadialMenuContext context) {
    }

    public Pokemon ResolvePokemon(RadialMenuContext context, out int slotIndex) {
        slotIndex = -1;
        if(preferContextIndex && context != null && context.index >= 0) {
            slotIndex = context.index;
            var party = ResolveParty();
            if(party != null && party.Pokemons != null && slotIndex < party.Pokemons.Count) {
                return party.Pokemons[slotIndex];
            }
        }

        var screen = ResolvePartyScreen();
        if(screen != null && screen.SelectedMember != null) {
            var party = ResolveParty();
            slotIndex = party != null && party.Pokemons != null ? party.Pokemons.IndexOf(screen.SelectedMember) : -1;
            return screen.SelectedMember;
        }

        return null;
    }

    RadialMenuOption BuildOption(RadialPartyActionDefinition action, Pokemon pokemon, int slotIndex, bool disabled, string disabledReason) {
        string id = !string.IsNullOrWhiteSpace(action.optionId) ? action.optionId : action.actionKind.ToString();
        string label = !string.IsNullOrWhiteSpace(action.label) ? action.label : action.actionKind.ToString();
        return new RadialMenuOption {
            id = id,
            label = label,
            description = action.description,
            icon = action.icon,
            disabled = disabled,
            disabledReason = disabledReason,
            priority = action.priority,
            payload = pokemon != null ? pokemon.Base : null
        };
    }

    bool IsDisabled(RadialPartyActionKind actionKind, Pokemon pokemon, int slotIndex, out string reason) {
        reason = null;
        if(actionKind == RadialPartyActionKind.Cancel) {
            return false;
        }

        if(pokemon == null) {
            reason = "No Pokemon selected.";
            return true;
        }

        switch(actionKind) {
            case RadialPartyActionKind.Summary:
                if(!allowSummaryForFainted && pokemon.HP <= 0) {
                    reason = $"{pokemon.Base.Name} is fainted.";
                    return true;
                }
                return false;
            case RadialPartyActionKind.Switch:
                if(requireTwoPokemonForSwitch && (ResolveParty()?.Pokemons?.Count ?? 0) < 2) {
                    reason = "There is no other Pokemon to switch with.";
                    return true;
                }
                return false;
            case RadialPartyActionKind.HeldItem:
                if(requireHeldItemForHeldItemAction && pokemon.HeldItem == null) {
                    reason = $"{pokemon.Base.Name} is not holding an item.";
                    return true;
                }
                return false;
            case RadialPartyActionKind.Ability:
                if(requireAbilityForAbilityAction && pokemon.Ability == null) {
                    reason = $"{pokemon.Base.Name} has no ability.";
                    return true;
                }
                return false;
            case RadialPartyActionKind.Moves:
                if(requireMovesForMovesAction && (pokemon.Moves == null || pokemon.Moves.Count == 0)) {
                    reason = $"{pokemon.Base.Name} has no moves.";
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    RadialPartyActionKind ResolveActionKind(RadialMenuOption option) {
        if(option == null || string.IsNullOrWhiteSpace(option.id)) {
            return RadialPartyActionKind.Custom;
        }

        return Enum.TryParse(option.id, true, out RadialPartyActionKind kind) ? kind : RadialPartyActionKind.Custom;
    }

    PartyScreen ResolvePartyScreen() {
        if(partyScreen != null) {
            return partyScreen;
        }

        partyScreen = FindAnyObjectByType<PartyScreen>();
        return partyScreen;
    }

    PokemonParty ResolveParty() {
        if(partyOverride != null) {
            return partyOverride;
        }

        try {
            partyOverride = PokemonParty.GetPlayerParty();
        } catch {
            partyOverride = FindAnyObjectByType<PokemonParty>();
        }

        return partyOverride;
    }
}
