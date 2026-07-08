using System.Collections;
using UnityEngine;

public class RidePoint : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id used by ride logs. Empty uses this GameObject name.")]
    [SerializeField] string pointId = string.Empty;
    [Tooltip("Readable point name used by debug logs and future UI. Empty uses this GameObject name.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Ride")]
    [Tooltip("Ride definition offered by this point.")]
    [SerializeField] RidePokemonDefinition ride;
    [Tooltip("Optional player ride controller. Empty searches the player or installs one when needed.")]
    [SerializeField] PlayerRideController rideControllerOverride;
    [Tooltip("Optional party index to use as the selected Pokemon. -1 lets the ride auto-select.")]
    [Min(-1)]
    [SerializeField] int partyPokemonIndex = -1;

    [Header("Behavior")]
    [Tooltip("If enabled, interacting with this point attempts to mount the ride.")]
    [SerializeField] bool mountOnInteract = true;
    [Tooltip("If enabled, interacting while the same ride is active dismounts instead.")]
    [SerializeField] bool dismountIfAlreadyMounted = true;
    [Tooltip("If enabled, entering the trigger attempts to mount the ride.")]
    [SerializeField] bool mountOnTrigger;
    [Tooltip("Controls IPlayerTriggerable.TriggerRepeatedly.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("If enabled, result text is shown through DialogManager when available.")]
    [SerializeField] bool showDialogFeedback;

    public string PointId => string.IsNullOrWhiteSpace(pointId) ? gameObject.name : pointId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public RidePokemonDefinition Ride => ride;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(mountOnTrigger) {
            TryUseRide(player, out _);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(!mountOnInteract) {
            yield break;
        }

        var player = initiator != null ? initiator.GetComponent<PlayerController>() : null;
        bool success = TryUseRide(player, out string message);
        if(showDialogFeedback && DialogManager.i != null) {
            yield return DialogManager.i.ShowDialogText(success ? BuildSuccessMessage() : message);
        }
    }

    [ContextMenu("Use Ride")]
    public void UseRideFromContextMenu() {
        TryUseRide(null, out _);
    }

    public bool TryUseRide(PlayerController player, out string message) {
        player = player != null ? player : PlayerController.i != null ? PlayerController.i : FindAnyObjectByType<PlayerController>();
        var controller = ResolveController(player);
        if(controller == null) {
            message = "Ride controller is missing.";
            return false;
        }

        var selectedPokemon = ResolveSelectedPokemon(player);
        if(dismountIfAlreadyMounted && controller.ActiveRide != null && ride != null && controller.ActiveRide.Id == ride.Id) {
            bool dismounted = controller.Dismount(PointId, out message);
            if(dismounted) {
                message = BuildSuccessMessage();
            }
            return dismounted;
        }

        bool mounted = controller.TryMount(ride, selectedPokemon, PointId, out message);
        if(mounted) {
            message = BuildSuccessMessage();
        }
        return mounted;
    }

    PlayerRideController ResolveController(PlayerController player) {
        if(rideControllerOverride != null) {
            return rideControllerOverride;
        }

        if(player != null) {
            rideControllerOverride = player.GetComponent<PlayerRideController>();
            if(rideControllerOverride == null) {
                rideControllerOverride = player.gameObject.AddComponent<PlayerRideController>();
            }
        }

        return rideControllerOverride;
    }

    Pokemon ResolveSelectedPokemon(PlayerController player) {
        if(player == null || partyPokemonIndex < 0) {
            return null;
        }

        var party = player.GetComponent<PokemonParty>();
        if(party == null || party.Pokemons == null || partyPokemonIndex >= party.Pokemons.Count) {
            return null;
        }

        return party.Pokemons[partyPokemonIndex];
    }

    string BuildSuccessMessage() {
        return ride != null ? $"{ride.DisplayName} ready." : "Ride ready.";
    }
}
