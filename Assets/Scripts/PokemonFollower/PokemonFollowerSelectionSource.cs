using System.Collections;
using UnityEngine;

public enum PokemonFollowerSelectionAction {
    FollowPartySlot,
    FollowFirstHealthy,
    CycleNextHealthy,
    Toggle,
    Disable,
    Refresh
}

public class PokemonFollowerSelectionSource : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("Selection")]
    [Tooltip("Follower controller affected by this source. Empty uses the interacting player.")]
    [SerializeField] PlayerPokemonFollowerController controllerOverride;
    [Tooltip("Selection action performed by this source.")]
    [SerializeField] PokemonFollowerSelectionAction action = PokemonFollowerSelectionAction.Toggle;
    [Tooltip("Party slot used by Follow Party Slot.")]
    [Min(0)]
    [SerializeField] int partySlotIndex;

    [Header("Trigger")]
    [Tooltip("If enabled, entering this trigger runs the selection action.")]
    [SerializeField] bool runOnPlayerTrigger;
    [Tooltip("Controls IPlayerTriggerable repeat behavior.")]
    [SerializeField] bool triggerRepeatedly;

    [Header("Feedback")]
    [Tooltip("If enabled, DialogManager shows success and failure messages.")]
    [SerializeField] bool showDialogResult = true;

    public bool TriggerRepeatedly => triggerRepeatedly;

    public IEnumerator Interact(Transform initiator) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : PlayerController.i;
        string message = Run(player);
        if(showDialogResult && DialogManager.i != null && !string.IsNullOrWhiteSpace(message)) {
            yield return DialogManager.i.ShowDialogText(message);
        }
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(runOnPlayerTrigger) {
            Run(player);
        }
    }

    string Run(PlayerController player) {
        var controller = ResolveController(player);
        if(controller == null) {
            return "Pokemon follower controller is missing.";
        }

        bool success;
        string failureMessage;
        switch(action) {
            case PokemonFollowerSelectionAction.FollowPartySlot:
                success = controller.SetFollowerPartySlot(partySlotIndex, out failureMessage);
                break;
            case PokemonFollowerSelectionAction.FollowFirstHealthy:
                success = controller.FollowFirstHealthy(out failureMessage);
                break;
            case PokemonFollowerSelectionAction.CycleNextHealthy:
                success = controller.CycleNextHealthy(out failureMessage);
                break;
            case PokemonFollowerSelectionAction.Disable:
                success = controller.DisableFollower(out failureMessage);
                break;
            case PokemonFollowerSelectionAction.Refresh:
                success = controller.RefreshFollower("selection-source", out failureMessage);
                break;
            default:
                success = controller.ToggleFollower(out failureMessage);
                break;
        }

        if(!success) {
            return failureMessage;
        }

        return controller.HasFollower
            ? $"{controller.ActivePokemon.NickName} is following you."
            : "Pokemon follower disabled.";
    }

    PlayerPokemonFollowerController ResolveController(PlayerController player) {
        if(controllerOverride != null) {
            return controllerOverride;
        }

        player = player != null ? player : PlayerController.i;
        return player != null ? player.GetComponent<PlayerPokemonFollowerController>() : null;
    }
}
