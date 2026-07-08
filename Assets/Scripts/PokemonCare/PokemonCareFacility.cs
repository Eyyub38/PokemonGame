using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokemonCareFacilityInteractionMode {
    AdmitFirstHealthyPokemon,
    AdmitPartySlot,
    ProcessDueCare,
    ReleaseFirstActivePokemon,
    ReleasePartySlot
}

public class PokemonCareFacility : MonoBehaviour, Interactable {
    [Header("Facility")]
    [Tooltip("Facility definition that controls capacity, access and care rules.")]
    [SerializeField] PokemonCareFacilityDefinition definition;
    [Tooltip("Optional stable scene id for this specific facility. Empty uses the GameObject name.")]
    [SerializeField] string facilityInstanceId;

    [Header("Interaction")]
    [Tooltip("Simple interaction behavior used until a dedicated UI chooses Pokemon and action.")]
    [SerializeField] PokemonCareFacilityInteractionMode interactionMode = PokemonCareFacilityInteractionMode.AdmitFirstHealthyPokemon;
    [Tooltip("Party slot used by Admit Party Slot and Release Party Slot modes.")]
    [Min(0)]
    [SerializeField] int partySlotIndex;
    [Tooltip("If enabled, release interactions also apply release care rules.")]
    [SerializeField] bool applyReleaseCare = true;
    [Tooltip("If enabled, DialogManager shows a result message after the interaction.")]
    [SerializeField] bool showDialogResult = true;

    public PokemonCareFacilityDefinition Definition => definition;
    public string FacilityInstanceId => string.IsNullOrWhiteSpace(facilityInstanceId) ? name : facilityInstanceId;

    public IEnumerator Interact(Transform initiator) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : null;
        string message = RunInteraction(player);

        if(showDialogResult && DialogManager.i != null) {
            yield return DialogManager.i.ShowDialogText(message);
        }
    }

    public bool TryAdmit(PlayerController player, Pokemon pokemon, out string failureMessage) {
        var log = GetOrCreateLog(player);
        if(log == null) {
            failureMessage = "Pokemon care facility log is missing.";
            return false;
        }

        return log.TryAdmit(player, definition, pokemon, FacilityInstanceId, out failureMessage);
    }

    public bool TryRelease(PlayerController player, Pokemon pokemon, out string failureMessage) {
        var log = GetOrCreateLog(player);
        if(log == null) {
            failureMessage = "Pokemon care facility log is missing.";
            return false;
        }

        return log.TryRelease(player, definition, pokemon, FacilityInstanceId, applyReleaseCare, out failureMessage);
    }

    public int ProcessDueCare(PlayerController player) {
        var log = GetOrCreateLog(player);
        return log != null ? log.ProcessDueCare(player, definition, FacilityInstanceId) : 0;
    }

    string RunInteraction(PlayerController player) {
        if(definition == null) {
            return "This care facility is not ready.";
        }

        if(player == null) {
            return "No player found for this care facility.";
        }

        var party = player.GetComponent<PokemonParty>();
        if(party == null) {
            return "No Pokemon party found.";
        }

        switch(interactionMode) {
            case PokemonCareFacilityInteractionMode.ProcessDueCare:
                int applied = ProcessDueCare(player);
                return applied > 0
                    ? $"{definition.DisplayName} applied {applied} care action(s)."
                    : $"{definition.DisplayName} has no care ready right now.";

            case PokemonCareFacilityInteractionMode.ReleaseFirstActivePokemon:
                return ReleaseFirstActive(player);

            case PokemonCareFacilityInteractionMode.ReleasePartySlot:
                return ReleasePokemon(player, GetPokemonFromSlot(party));

            case PokemonCareFacilityInteractionMode.AdmitPartySlot:
                return AdmitPokemon(player, GetPokemonFromSlot(party));

            default:
                return AdmitPokemon(player, party.GetHealthyPokemon());
        }
    }

    string AdmitPokemon(PlayerController player, Pokemon pokemon) {
        if(TryAdmit(player, pokemon, out var failureMessage)) {
            return $"{pokemon.NickName} entered {definition.DisplayName}.";
        }

        return failureMessage;
    }

    string ReleasePokemon(PlayerController player, Pokemon pokemon) {
        if(pokemon == null) {
            return "No Pokemon selected.";
        }

        if(TryRelease(player, pokemon, out var failureMessage)) {
            return $"{pokemon.NickName} left {definition.DisplayName}.";
        }

        return failureMessage;
    }

    string ReleaseFirstActive(PlayerController player) {
        var log = GetOrCreateLog(player);
        var stay = log?.GetActiveStays(definition, FacilityInstanceId).FirstOrDefault();
        if(stay == null) {
            return "No Pokemon is staying in this facility.";
        }

        var pokemon = log.ResolvePokemon(player, stay.pokemonId);
        if(log.TryRelease(player, definition, pokemon, FacilityInstanceId, applyReleaseCare, out var failureMessage)) {
            return $"{stay.pokemonName} left {definition.DisplayName}.";
        }

        return failureMessage;
    }

    Pokemon GetPokemonFromSlot(PokemonParty party) {
        if(party == null || party.Pokemons == null || party.Pokemons.Count == 0) {
            return null;
        }

        int index = Mathf.Clamp(partySlotIndex, 0, party.Pokemons.Count - 1);
        return party.Pokemons[index];
    }

    PlayerPokemonCareFacilityLog GetOrCreateLog(PlayerController player) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerPokemonCareFacilityLog>();
        return log != null ? log : player.gameObject.AddComponent<PlayerPokemonCareFacilityLog>();
    }
}
