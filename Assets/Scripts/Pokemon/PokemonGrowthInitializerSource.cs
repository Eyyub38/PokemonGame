using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokemonGrowthInitializerTarget {
    AllPartyPokemon,
    FirstPartyPokemon,
    FirstHealthyPokemon,
    PartySlot
}

public class PokemonGrowthInitializerSource : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("References")]
    [Tooltip("Growth profile applied to selected Pokemon.")]
    [SerializeField] PokemonGrowthProfileDefinition growthProfile;
    [Tooltip("Player affected by Start initialization when no initiator is available. Empty uses PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;

    [Header("Targeting")]
    [Tooltip("Which Pokemon receive the growth profile.")]
    [SerializeField] PokemonGrowthInitializerTarget target = PokemonGrowthInitializerTarget.AllPartyPokemon;
    [Tooltip("Party slot used when Target is Party Slot.")]
    [Min(0)]
    [SerializeField] int partySlotIndex;
    [Tooltip("If enabled, Pokemon with existing initialized growth keep their current growth state.")]
    [SerializeField] bool preserveExistingGrowth = true;

    [Header("Triggering")]
    [Tooltip("If enabled, growth initialization runs during Start.")]
    [SerializeField] bool initializeOnStart;
    [Tooltip("If enabled, trigger volumes may run this source repeatedly.")]
    [SerializeField] bool triggerRepeatedly;
    [Tooltip("If enabled, initialization results are written to GameDebug.")]
    [SerializeField] bool writeDebugLog;

    public PokemonGrowthProfileDefinition GrowthProfile => growthProfile;
    public PokemonGrowthInitializerTarget Target => target;
    public int PartySlotIndex => Mathf.Max(0, partySlotIndex);
    public bool PreserveExistingGrowth => preserveExistingGrowth;
    public bool TriggerRepeatedly => triggerRepeatedly;

    void Start() {
        if(initializeOnStart) {
            ApplyToPlayer(ResolvePlayer());
        }
    }

    public IEnumerator Interact(Transform initiator) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : ResolvePlayer();
        ApplyToPlayer(player);
        yield break;
    }

    public void OnPlayerTriggered(PlayerController player) {
        ApplyToPlayer(player != null ? player : ResolvePlayer());
    }

    [ContextMenu("Apply Growth To Player Party")]
    public void ApplyFromContextMenu() {
        ApplyToPlayer(ResolvePlayer());
    }

    public int ApplyToPlayer(PlayerController player) {
        if(growthProfile == null || player == null) {
            WriteDebug("Growth initializer could not run because profile or player is missing.", warning: true);
            return 0;
        }

        var party = player.GetComponent<PokemonParty>();
        if(party == null || party.Pokemons == null) {
            WriteDebug("Growth initializer could not find a Pokemon party.", warning: true);
            return 0;
        }

        int applied = 0;
        foreach(var pokemon in ResolveTargets(party)) {
            if(pokemon == null) {
                continue;
            }

            if(preserveExistingGrowth && pokemon.GrowthState != null && pokemon.GrowthState.initialized) {
                continue;
            }

            pokemon.InitializeGrowth(growthProfile, preserveExisting: preserveExistingGrowth);
            applied++;
        }

        if(applied > 0) {
            party.PartyUpdated();
        }

        WriteDebug($"Growth initializer applied {growthProfile.DisplayName} to {applied} Pokemon.", warning: false);
        return applied;
    }

    IEnumerable<Pokemon> ResolveTargets(PokemonParty party) {
        if(party == null || party.Pokemons == null) {
            return Enumerable.Empty<Pokemon>();
        }

        switch(target) {
            case PokemonGrowthInitializerTarget.FirstPartyPokemon:
                return party.Pokemons.Take(1);
            case PokemonGrowthInitializerTarget.FirstHealthyPokemon:
                var healthy = party.GetHealthyPokemon();
                return healthy != null ? new[] { healthy } : Enumerable.Empty<Pokemon>();
            case PokemonGrowthInitializerTarget.PartySlot:
                return partySlotIndex >= 0 && partySlotIndex < party.Pokemons.Count && party.Pokemons[partySlotIndex] != null
                    ? new[] { party.Pokemons[partySlotIndex] }
                    : Enumerable.Empty<Pokemon>();
            default:
                return party.Pokemons.Where(pokemon => pokemon != null);
        }
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        return PlayerController.i != null ? PlayerController.i : FindAnyObjectByType<PlayerController>();
    }

    void WriteDebug(string message, bool warning) {
        if(!writeDebugLog || string.IsNullOrWhiteSpace(message)) {
            return;
        }

        if(warning) {
            GameDebug.Warning(message, GameDebugCategory.General, this, "PokemonGrowthInitializerSource");
        } else {
            GameDebug.Success(message, GameDebugCategory.General, this, "PokemonGrowthInitializerSource");
        }
    }
}
