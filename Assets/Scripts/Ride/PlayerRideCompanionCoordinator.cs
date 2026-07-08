using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerRideCompanionCoordinator : MonoBehaviour {
    [Header("References")]
    [Tooltip("Player controlled by this coordinator. Empty uses this GameObject or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("Ride controller this coordinator listens to. Empty uses the player's PlayerRideController.")]
    [SerializeField] PlayerRideController rideControllerOverride;
    [Tooltip("Policy that controls ride companion capacity and overflow behavior.")]
    [SerializeField] RideCompanionPolicyDefinition policy;
    [Tooltip("Ride companion log used to save coordination history. Empty uses or installs PlayerRideCompanionLog on the player.")]
    [SerializeField] PlayerRideCompanionLog logOverride;
    [Tooltip("If enabled, PlayerRideCompanionLog is added automatically when missing.")]
    [SerializeField] bool autoInstallLog = true;

    [Header("Runtime")]
    [Tooltip("If enabled, detached companions are restored automatically when the player dismounts.")]
    [SerializeField] bool restoreDetachedOnDismount = true;
    [Tooltip("If enabled, delayed pending returns are checked when TimeSystem time changes.")]
    [SerializeField] bool checkDelayedReturnsOnTimeChange = true;
    [Tooltip("If enabled, short debug messages are written for ride companion coordination.")]
    [SerializeField] bool writeDebugLogs;

    readonly List<DetachedRideCompanionRuntimeState> detachedCompanions = new List<DetachedRideCompanionRuntimeState>();
    PlayerController player;
    PlayerRideController rideController;
    PlayerRideCompanionLog log;
    bool subscribedToRide;
    bool subscribedToTime;

    void Awake() {
        ResolveReferences();
    }

    void OnEnable() {
        Subscribe();
    }

    void Start() {
        ResolveReferences();
        Subscribe();
    }

    void OnDisable() {
        Unsubscribe();
    }

    public void CoordinateCurrentRide() {
        ResolveReferences();
        if(rideController != null && rideController.IsMounted) {
            HandleMounted(rideController.ActiveRide, rideController.ActivePokemon);
        }
    }

    public int RestoreDetachedCompanions(string reason = "manual") {
        ResolveReferences();
        int restored = 0;
        for(int i = detachedCompanions.Count - 1; i >= 0; i--) {
            var state = detachedCompanions[i];
            if(state == null || state.companion == null) {
                detachedCompanions.RemoveAt(i);
                continue;
            }

            state.companion.StartFollowing(player);
            log?.RemovePendingReturn(state.companion.CompanionId);
            detachedCompanions.RemoveAt(i);
            restored++;
        }

        if(restored > 0) {
            policy?.PublishRestoreEvent(player, rideController != null ? rideController.ActiveRide : null, restored, this);
            WriteDebug($"{restored} ride companion(s) restored after {reason}.");
        }

        return restored;
    }

    void HandleMounted(RidePokemonDefinition ride, Pokemon mountedPokemon) {
        ResolveReferences();
        if(player == null || policy == null || ride == null) {
            return;
        }

        var resolved = policy.Resolve(ride, mountedPokemon);
        var following = CompanionController.GetFollowingCompanions(player).ToList();
        var summary = BuildSummary(ride, mountedPokemon, resolved, following);

        if(resolved.OverflowMode == RideCompanionOverflowMode.DismountRide && summary.detachedCompanionIds.Count > 0) {
            log?.RecordCoordination(ride, mountedPokemon, summary, "blocked");
            policy.PublishCoordinationEvent(player, ride, mountedPokemon, summary, this);
            rideController?.Dismount("companion-capacity", out _);
            return;
        }

        ApplyOverflow(ride, mountedPokemon, resolved, following, summary);
        log?.RecordCoordination(ride, mountedPokemon, summary, "mounted");
        policy.PublishCoordinationEvent(player, ride, mountedPokemon, summary, this);
        WriteDebug(summary.BuildMessage());
    }

    void HandleDismounted(RidePokemonDefinition ride, Pokemon mountedPokemon, string reason) {
        if(restoreDetachedOnDismount) {
            int restored = RestoreDetachedCompanions(reason);
            if(restored > 0) {
                var summary = new RideCompanionCoordinationSummary {
                    rideId = ride != null ? ride.Id : string.Empty,
                    rideName = ride != null ? ride.DisplayName : string.Empty,
                    pokemonName = mountedPokemon != null ? mountedPokemon.NickName : string.Empty,
                    totalRiderCapacity = 0,
                    companionCapacity = 0,
                    followingCompanionCount = restored,
                    overflowMode = RideCompanionOverflowMode.KeepFollowing,
                    policyNote = reason,
                    keptCompanionIds = new List<string>(),
                    selfTravelCompanionIds = new List<string>(),
                    detachedCompanionIds = new List<string>()
                };
                log?.RecordCoordination(ride, mountedPokemon, summary, "restored");
            }
        }
    }

    RideCompanionCoordinationSummary BuildSummary(RidePokemonDefinition ride, Pokemon mountedPokemon, RideCompanionResolvedPolicy resolved, List<CompanionController> following) {
        return new RideCompanionCoordinationSummary {
            rideId = ride != null ? ride.Id : string.Empty,
            rideName = ride != null ? ride.DisplayName : string.Empty,
            pokemonName = mountedPokemon != null ? mountedPokemon.NickName : string.Empty,
            totalRiderCapacity = resolved.TotalRiderCapacity,
            companionCapacity = resolved.CompanionCapacity,
            followingCompanionCount = following != null ? following.Count : 0,
            overflowMode = resolved.OverflowMode,
            policyNote = resolved.Notes,
            keptCompanionIds = new List<string>(),
            selfTravelCompanionIds = new List<string>(),
            detachedCompanionIds = new List<string>()
        };
    }

    void ApplyOverflow(RidePokemonDefinition ride, Pokemon mountedPokemon, RideCompanionResolvedPolicy resolved, List<CompanionController> following, RideCompanionCoordinationSummary summary) {
        int remainingSeats = resolved.CompanionCapacity;
        foreach(var companion in following) {
            if(companion == null) {
                continue;
            }

            if(remainingSeats > 0) {
                summary.keptCompanionIds.Add(companion.CompanionId);
                remainingSeats--;
                continue;
            }

            if(resolved.AllowSelfTravelCompanions && TrySelfTravel(companion, ride, mountedPokemon)) {
                summary.selfTravelCompanionIds.Add(companion.CompanionId);
                continue;
            }

            summary.detachedCompanionIds.Add(companion.CompanionId);
            DetachOverflowCompanion(companion, ride, resolved);
        }
    }

    bool TrySelfTravel(CompanionController companion, RidePokemonDefinition ride, Pokemon mountedPokemon) {
        var capability = companion != null ? companion.GetComponent<CompanionRideCapability>() : null;
        if(capability == null || !capability.CanSelfTravel(ride, mountedPokemon)) {
            return false;
        }

        if(!capability.KeepFollowingWhileSelfTravelling) {
            companion.StopFollowing();
        }

        return true;
    }

    void DetachOverflowCompanion(CompanionController companion, RidePokemonDefinition ride, RideCompanionResolvedPolicy resolved) {
        if(companion == null) {
            return;
        }

        companion.StopFollowing();
        var state = new DetachedRideCompanionRuntimeState {
            companion = companion,
            companionId = companion.CompanionId,
            returnDueAbsoluteHour = GetCurrentAbsoluteHour() + Mathf.Max(0, resolved.ReturnDelayHours),
            returnOnDismount = resolved.OverflowMode == RideCompanionOverflowMode.DetachUntilDismount
                || (resolved.OverflowMode == RideCompanionOverflowMode.DetachAndReturnAfterDelay && policy.DelayedReturnRequiresDismount)
        };
        detachedCompanions.RemoveAll(entry => entry != null && entry.companionId == state.companionId);
        detachedCompanions.Add(state);

        if(resolved.OverflowMode == RideCompanionOverflowMode.DetachAndReturnAfterDelay) {
            log?.AddPendingReturn(companion, ride, state.returnDueAbsoluteHour, "ride-capacity");
        }
    }

    void HandleTimeChanged() {
        if(!checkDelayedReturnsOnTimeChange || policy == null) {
            return;
        }

        int now = GetCurrentAbsoluteHour();
        for(int i = detachedCompanions.Count - 1; i >= 0; i--) {
            var state = detachedCompanions[i];
            if(state == null || state.companion == null) {
                detachedCompanions.RemoveAt(i);
                continue;
            }

            if(state.returnDueAbsoluteHour > now) {
                continue;
            }

            if(state.returnOnDismount && rideController != null && rideController.IsMounted) {
                continue;
            }

            state.companion.StartFollowing(player);
            log?.RemovePendingReturn(state.companionId);
            detachedCompanions.RemoveAt(i);
            policy.PublishRestoreEvent(player, rideController != null ? rideController.ActiveRide : null, 1, this);
            WriteDebug($"{state.companion.CompanionName} returned after ride delay.");
        }
    }

    void ResolveReferences() {
        player = playerOverride != null ? playerOverride : GetComponent<PlayerController>();
        player = player != null ? player : PlayerController.i;
        rideController = rideControllerOverride != null ? rideControllerOverride : player != null ? player.GetComponent<PlayerRideController>() : null;
        log = ResolveLog();
    }

    PlayerRideCompanionLog ResolveLog() {
        if(logOverride != null) {
            return logOverride;
        }

        var target = playerOverride != null ? playerOverride : GetComponent<PlayerController>();
        target = target != null ? target : PlayerController.i;
        if(target == null) {
            return null;
        }

        var found = target.GetComponent<PlayerRideCompanionLog>();
        if(found == null && autoInstallLog) {
            found = target.gameObject.AddComponent<PlayerRideCompanionLog>();
        }
        return found;
    }

    void Subscribe() {
        ResolveReferences();
        if(rideController != null && !subscribedToRide) {
            rideController.OnMounted += HandleMounted;
            rideController.OnDismounted += HandleDismounted;
            subscribedToRide = true;
        }

        if(TimeSystem.i != null && !subscribedToTime) {
            TimeSystem.i.OnTimeChanged += HandleTimeChanged;
            TimeSystem.i.OnDayChanged += HandleTimeChanged;
            subscribedToTime = true;
        }
    }

    void Unsubscribe() {
        if(rideController != null && subscribedToRide) {
            rideController.OnMounted -= HandleMounted;
            rideController.OnDismounted -= HandleDismounted;
        }
        subscribedToRide = false;

        if(TimeSystem.i != null && subscribedToTime) {
            TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
            TimeSystem.i.OnDayChanged -= HandleTimeChanged;
        }
        subscribedToTime = false;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void WriteDebug(string message) {
        if(writeDebugLogs && !string.IsNullOrWhiteSpace(message)) {
            GameDebug.Step(message, GameDebugCategory.Transit, this, "PlayerRideCompanionCoordinator");
        }
    }
}

public class DetachedRideCompanionRuntimeState {
    public CompanionController companion;
    public string companionId;
    public int returnDueAbsoluteHour;
    public bool returnOnDismount;
}
