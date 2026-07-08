using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokemonHeldItemAction {
    Give,
    Take,
    Swap,
    Clear
}

public enum PokemonHeldItemTargetMode {
    PartySlot,
    FirstPartyPokemon,
    FirstHealthyPokemon
}

[Serializable]
public class PokemonHeldItemRecord {
    [Tooltip("Action that changed held item state.")]
    public PokemonHeldItemAction action;
    [Tooltip("Source id that caused this held item change.")]
    public string sourceId;
    [Tooltip("Human-readable source name saved for debug/history UI.")]
    public string sourceName;
    [Tooltip("Party slot affected when the action was applied.")]
    public int partySlot;
    [Tooltip("Pokemon instance id affected by this action.")]
    public string pokemonInstanceId;
    [Tooltip("Pokemon display name affected by this action.")]
    public string pokemonName;
    [Tooltip("Held item before the action.")]
    public string previousItem;
    [Tooltip("Held item after the action.")]
    public string nextItem;
    [Tooltip("If enabled, the action was completed successfully.")]
    public bool success;
    [Tooltip("Short result or failure message for debug/UI display.")]
    public string message;
    [Tooltip("UTC timestamp recorded when the action occurred.")]
    public string utcTimestamp;
}

public class PlayerPokemonHeldItemLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum held item history records kept in memory and save data.")]
    [Min(1)]
    [SerializeField] int maxHistory = 100;

    readonly List<PokemonHeldItemRecord> history = new List<PokemonHeldItemRecord>();

    public IReadOnlyList<PokemonHeldItemRecord> History => history;

    public void Record(PokemonHeldItemRecord record) {
        if(record == null) {
            return;
        }

        record.utcTimestamp = DateTime.UtcNow.ToString("O");
        history.Add(record);
        if(history.Count > maxHistory) {
            history.RemoveRange(0, history.Count - maxHistory);
        }
    }

    public object CaptureState() {
        return new PlayerPokemonHeldItemLogSaveData {
            history = history.ToList()
        };
    }

    public void RestoreState(object state) {
        history.Clear();
        if(state is PlayerPokemonHeldItemLogSaveData saveData && saveData.history != null) {
            history.AddRange(saveData.history.Where(record => record != null));
        }
    }
}

[Serializable]
public class PlayerPokemonHeldItemLogSaveData {
    public List<PokemonHeldItemRecord> history;
}

public static class PokemonHeldItemService {
    public static bool TryGiveFromInventory(Inventory inventory, Pokemon pokemon, ItemBase item, out string message, bool replaceExisting = false, bool returnPreviousToInventory = true) {
        message = null;
        if(pokemon == null) {
            message = "No Pokemon selected.";
            return false;
        }

        if(item == null) {
            message = "No item selected.";
            return false;
        }

        if(inventory != null && !inventory.HasItemEnough(item)) {
            message = $"Inventory does not contain {item.Name}.";
            return false;
        }

        if(pokemon.HeldItem != null && !replaceExisting) {
            message = $"{pokemon.NickName} is already holding {pokemon.HeldItem.Name}.";
            return false;
        }

        var previous = pokemon.HeldItem;
        if(inventory != null) {
            inventory.RemoveItem(item);
        }

        pokemon.HeldItem = item;

        if(previous != null && returnPreviousToInventory && inventory != null) {
            inventory.AddItem(previous);
        }

        message = previous != null
            ? $"{pokemon.NickName} switched {previous.Name} for {item.Name}."
            : $"{pokemon.NickName} is now holding {item.Name}.";
        return true;
    }

    public static bool TryTakeToInventory(Inventory inventory, Pokemon pokemon, out ItemBase takenItem, out string message) {
        takenItem = null;
        message = null;
        if(pokemon == null) {
            message = "No Pokemon selected.";
            return false;
        }

        if(pokemon.HeldItem == null) {
            message = $"{pokemon.NickName} is not holding anything.";
            return false;
        }

        takenItem = pokemon.HeldItem;
        pokemon.HeldItem = null;
        inventory?.AddItem(takenItem);
        message = $"{pokemon.NickName} gave back {takenItem.Name}.";
        return true;
    }

    public static bool TrySwap(Pokemon first, Pokemon second, out string message) {
        message = null;
        if(first == null || second == null) {
            message = "Two Pokemon are required to swap held items.";
            return false;
        }

        if(first == second) {
            message = "A Pokemon cannot swap held items with itself.";
            return false;
        }

        (first.HeldItem, second.HeldItem) = (second.HeldItem, first.HeldItem);
        message = $"{first.NickName} and {second.NickName} swapped held items.";
        return true;
    }

    public static bool TryClear(Pokemon pokemon, out ItemBase clearedItem, out string message) {
        clearedItem = null;
        message = null;
        if(pokemon == null) {
            message = "No Pokemon selected.";
            return false;
        }

        if(pokemon.HeldItem == null) {
            message = $"{pokemon.NickName} is not holding anything.";
            return false;
        }

        clearedItem = pokemon.HeldItem;
        pokemon.HeldItem = null;
        message = $"{pokemon.NickName}'s held item was cleared.";
        return true;
    }

    public static Pokemon ResolveTarget(PokemonParty party, PokemonHeldItemTargetMode targetMode, int partySlot) {
        if(party == null || party.Pokemons == null || party.Pokemons.Count == 0) {
            return null;
        }

        return targetMode switch {
            PokemonHeldItemTargetMode.FirstPartyPokemon => party.Pokemons.FirstOrDefault(pokemon => pokemon != null),
            PokemonHeldItemTargetMode.FirstHealthyPokemon => party.Pokemons.FirstOrDefault(pokemon => pokemon != null && pokemon.HP > 0),
            _ => partySlot >= 0 && partySlot < party.Pokemons.Count ? party.Pokemons[partySlot] : null
        };
    }
}

public class PokemonHeldItemSource : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("Action")]
    [Tooltip("Held item action performed by this source.")]
    [SerializeField] PokemonHeldItemAction action = PokemonHeldItemAction.Give;
    [Tooltip("Inventory item given to the selected Pokemon when action is Give or Swap.")]
    [SerializeField] ItemBase item;
    [Tooltip("If enabled, giving an item replaces the Pokemon's current held item.")]
    [SerializeField] bool replaceExisting;
    [Tooltip("If enabled, previous held items are returned to the player's inventory when replaced.")]
    [SerializeField] bool returnPreviousToInventory = true;

    [Header("Target")]
    [Tooltip("How this source chooses the target Pokemon.")]
    [SerializeField] PokemonHeldItemTargetMode targetMode = PokemonHeldItemTargetMode.PartySlot;
    [Tooltip("Party slot used when Target Mode is Party Slot.")]
    [Range(0, 5)]
    [SerializeField] int partySlot;

    [Header("Source")]
    [Tooltip("Optional source id saved to held item history records.")]
    [SerializeField] string sourceId = "held-item-source";
    [Tooltip("Optional source name shown in held item history/debug records.")]
    [SerializeField] string sourceName = "Held Item Source";
    [Tooltip("If enabled, this source can be activated by trigger as well as interaction.")]
    [SerializeField] bool triggerEnabled;
    [Tooltip("If enabled, trigger activation can repeat while the player remains in the trigger.")]
    [SerializeField] bool triggerRepeatedly;
    [Tooltip("If enabled, result messages are sent to the dialog manager when available.")]
    [SerializeField] bool showDialogMessage = true;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public PokemonHeldItemAction Action => action;
    public ItemBase Item => item;
    public PokemonHeldItemTargetMode TargetMode => targetMode;
    public int PartySlot => partySlot;
    public string SourceId => sourceId;

    public IEnumerator Interact(Transform initiator) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : PlayerController.i;
        string message = Run(player);
        if(showDialogMessage && DialogManager.i != null && !string.IsNullOrWhiteSpace(message)) {
            yield return DialogManager.i.ShowDialogText(message);
        }
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(triggerEnabled) {
            Run(player);
        }
    }

    public string Run(PlayerController player) {
        player ??= PlayerController.i;
        var party = player != null ? player.GetComponent<PokemonParty>() : PokemonParty.GetPlayerParty();
        var inventory = player != null ? player.GetComponent<Inventory>() : Inventory.GetInventory();
        var log = player != null ? player.GetComponent<PlayerPokemonHeldItemLog>() : null;
        var pokemon = PokemonHeldItemService.ResolveTarget(party, targetMode, partySlot);
        string previousItem = pokemon?.HeldItem?.Name;
        bool success = false;
        string message;

        switch(action) {
            case PokemonHeldItemAction.Take:
                success = PokemonHeldItemService.TryTakeToInventory(inventory, pokemon, out _, out message);
                break;
            case PokemonHeldItemAction.Clear:
                success = PokemonHeldItemService.TryClear(pokemon, out _, out message);
                break;
            case PokemonHeldItemAction.Swap:
            case PokemonHeldItemAction.Give:
            default:
                success = PokemonHeldItemService.TryGiveFromInventory(inventory, pokemon, item, out message, replaceExisting || action == PokemonHeldItemAction.Swap, returnPreviousToInventory);
                break;
        }

        log?.Record(new PokemonHeldItemRecord {
            action = action,
            sourceId = sourceId,
            sourceName = sourceName,
            partySlot = partySlot,
            pokemonInstanceId = pokemon?.InstanceId,
            pokemonName = pokemon?.NickName,
            previousItem = previousItem,
            nextItem = pokemon?.HeldItem?.Name,
            success = success,
            message = message
        });

        return message;
    }
}
