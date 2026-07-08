using UnityEngine;

public enum RideRequirementMode {
    IsMounted,
    IsNotMounted,
    ActiveRideIs,
    ActiveRideModeIs,
    CanUseRide,
    HasUsableRidePokemon,
    HasMountedRide,
    MountCountAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/Ride Requirement")]
public class RideRequirement : ActivityRequirement {
    [Header("Target")]
    [Tooltip("How the player's ride state should be checked.")]
    [SerializeField] RideRequirementMode mode = RideRequirementMode.IsMounted;
    [Tooltip("Ride definition used by exact ride and mount-count checks.")]
    [SerializeField] RidePokemonDefinition ride;
    [Tooltip("Ride mode used by Active Ride Mode Is checks.")]
    [SerializeField] PokemonRideMode rideMode = PokemonRideMode.Ground;

    [Header("Threshold")]
    [Tooltip("Required count for Mount Count At Least.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, blocked mount attempts also count toward Mount Count At Least.")]
    [SerializeField] bool includeBlockedAttempts;
    [Tooltip("If enabled, the final result is inverted.")]
    [SerializeField] bool invertResult;

    public RideRequirementMode Mode => mode;
    public RidePokemonDefinition Ride => ride;
    public PokemonRideMode RideMode => rideMode;
    public int RequiredCount => Mathf.Max(0, requiredCount);
    public bool IncludeBlockedAttempts => includeBlockedAttempts;
    public bool InvertResult => invertResult;

    public override bool IsMet(PlayerController player) {
        bool met = Evaluate(player);
        return invertResult ? !met : met;
    }

    bool Evaluate(PlayerController player) {
        if(player == null) {
            return false;
        }

        var log = player.GetComponent<PlayerRideLog>();
        var controller = player.GetComponent<PlayerRideController>();
        bool mounted = controller != null && controller.IsMounted || log != null && log.HasActiveRide;

        switch(mode) {
            case RideRequirementMode.IsMounted:
                return mounted;
            case RideRequirementMode.IsNotMounted:
                return !mounted;
            case RideRequirementMode.ActiveRideIs:
                if(ride == null) return false;
                if(controller != null && controller.ActiveRide != null) return controller.ActiveRide.Id == ride.Id;
                return log != null && log.ActiveRide != null && log.ActiveRide.rideId == ride.Id;
            case RideRequirementMode.ActiveRideModeIs:
                if(controller != null && controller.ActiveRide != null) return controller.ActiveRide.RideMode == rideMode;
                return log != null && log.ActiveRide != null && log.ActiveRide.rideMode == rideMode;
            case RideRequirementMode.CanUseRide:
                return ride != null && ride.CanUse(player, null, out _);
            case RideRequirementMode.HasUsableRidePokemon:
                return ride != null && ride.FindUsablePokemon(player, out _) != null;
            case RideRequirementMode.HasMountedRide:
                return log != null && log.HasMountedRide(ride);
            case RideRequirementMode.MountCountAtLeast:
                return log != null && log.GetMountCount(ride, includeBlockedAttempts) >= RequiredCount;
            default:
                return false;
        }
    }
}
