using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RideCompanionOverflowMode {
    KeepFollowing,
    DetachUntilDismount,
    DetachAndReturnAfterDelay,
    DismountRide
}

[CreateAssetMenu(menuName = "Ride/Ride Companion Policy")]
public class RideCompanionPolicyDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this ride companion policy. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining how this ride companion policy should be used.")]
    [TextArea]
    [SerializeField] string description = string.Empty;

    [Header("Default Capacity")]
    [Tooltip("Default total rider capacity including the player. 1 means only the player can ride.")]
    [Min(1)]
    [SerializeField] int defaultTotalRiderCapacity = 1;
    [Tooltip("Default behavior when following companions exceed the ride capacity.")]
    [SerializeField] RideCompanionOverflowMode defaultOverflowMode = RideCompanionOverflowMode.DetachUntilDismount;
    [Tooltip("Default in-game hours before detached companions return when overflow mode is Detach And Return After Delay.")]
    [Min(0)]
    [SerializeField] int defaultReturnDelayHours = 1;
    [Tooltip("If enabled, delayed companions wait until the player dismounts before returning, even if the delay has elapsed.")]
    [SerializeField] bool delayedReturnRequiresDismount = true;
    [Tooltip("If enabled, companions with CompanionRideCapability can stay with the player even when capacity is full.")]
    [SerializeField] bool allowSelfTravelCompanions = true;

    [Header("Rules")]
    [Tooltip("Capacity rules checked in order. The first matching rule overrides the defaults.")]
    [SerializeField] List<RideCompanionCapacityRule> rules = new List<RideCompanionCapacityRule>();

    [Header("Events")]
    [Tooltip("Optional event published when ride companions are coordinated after mounting.")]
    [SerializeField] GameEventDefinition coordinatedEvent;
    [Tooltip("Optional event published when detached companions are restored.")]
    [SerializeField] GameEventDefinition restoredEvent;
    [Tooltip("If enabled, coordination events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, coordination events are also written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public int DefaultTotalRiderCapacity => Mathf.Max(1, defaultTotalRiderCapacity);
    public RideCompanionOverflowMode DefaultOverflowMode => defaultOverflowMode;
    public int DefaultReturnDelayHours => Mathf.Max(0, defaultReturnDelayHours);
    public bool DelayedReturnRequiresDismount => delayedReturnRequiresDismount;
    public bool AllowSelfTravelCompanions => allowSelfTravelCompanions;
    public IReadOnlyList<RideCompanionCapacityRule> Rules => rules != null ? (IReadOnlyList<RideCompanionCapacityRule>)rules : Array.Empty<RideCompanionCapacityRule>();
    public GameEventDefinition CoordinatedEvent => coordinatedEvent;
    public GameEventDefinition RestoredEvent => restoredEvent;
    public bool ShowEventsInFeed => showEventsInFeed;
    public bool WriteEventsToDebugLog => writeEventsToDebugLog;

    public RideCompanionResolvedPolicy Resolve(RidePokemonDefinition ride, Pokemon mountedPokemon) {
        var rule = rules != null ? rules.FirstOrDefault(entry => entry != null && entry.Matches(ride, mountedPokemon)) : null;
        if(rule != null) {
            return new RideCompanionResolvedPolicy(
                Mathf.Max(1, rule.TotalRiderCapacity),
                rule.OverflowMode,
                Mathf.Max(0, rule.ReturnDelayHours),
                rule.AllowSelfTravelCompanions,
                rule.Notes);
        }

        return new RideCompanionResolvedPolicy(
            DefaultTotalRiderCapacity,
            defaultOverflowMode,
            DefaultReturnDelayHours,
            allowSelfTravelCompanions,
            "default");
    }

    public void PublishCoordinationEvent(PlayerController player, RidePokemonDefinition ride, Pokemon mountedPokemon, RideCompanionCoordinationSummary summary, UnityEngine.Object context) {
        if(summary == null) {
            return;
        }

        GameEventPublishing.PublishOptional(
            coordinatedEvent,
            $"ride.companions.coordinated.{(ride != null ? ride.Id : "unknown")}",
            summary.BuildMessage(),
            GameEventCategory.Transit,
            summary.detachedCompanionIds.Count > 0 ? GameEventImportance.Warning : GameEventImportance.Info,
            context != null ? context : player,
            "RideCompanionPolicyDefinition",
            GameEventScope.Player,
            showEventsInFeed,
            writeEventsToDebugLog,
            GameEventPublishing.Value("rideId", ride != null ? ride.Id : string.Empty),
            GameEventPublishing.Value("rideName", ride != null ? ride.DisplayName : string.Empty),
            GameEventPublishing.Value("pokemonName", mountedPokemon != null ? mountedPokemon.NickName : string.Empty),
            GameEventPublishing.Value("capacity", summary.totalRiderCapacity),
            GameEventPublishing.Value("followingCompanions", summary.followingCompanionCount),
            GameEventPublishing.Value("keptCompanions", summary.keptCompanionIds.Count),
            GameEventPublishing.Value("selfTravelCompanions", summary.selfTravelCompanionIds.Count),
            GameEventPublishing.Value("detachedCompanions", summary.detachedCompanionIds.Count),
            GameEventPublishing.Value("overflowMode", summary.overflowMode));
    }

    public void PublishRestoreEvent(PlayerController player, RidePokemonDefinition ride, int restoredCount, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            restoredEvent,
            $"ride.companions.restored.{(ride != null ? ride.Id : "unknown")}",
            $"{restoredCount} companion(s) returned after the ride.",
            GameEventCategory.Transit,
            GameEventImportance.Info,
            context != null ? context : player,
            "RideCompanionPolicyDefinition",
            GameEventScope.Player,
            showEventsInFeed,
            writeEventsToDebugLog,
            GameEventPublishing.Value("rideId", ride != null ? ride.Id : string.Empty),
            GameEventPublishing.Value("rideName", ride != null ? ride.DisplayName : string.Empty),
            GameEventPublishing.Value("restoredCompanions", restoredCount));
    }
}

[Serializable]
public class RideCompanionCapacityRule {
    [Header("Match")]
    [Tooltip("If assigned, this rule only applies to this exact ride definition.")]
    [SerializeField] RidePokemonDefinition ride;
    [Tooltip("If enabled, Ride Mode must match the selected value.")]
    [SerializeField] bool matchRideMode;
    [Tooltip("Ride mode required when Match Ride Mode is enabled.")]
    [SerializeField] PokemonRideMode rideMode = PokemonRideMode.Ground;
    [Tooltip("Ride tags that can match this rule. Empty means no tag filter.")]
    [SerializeField] List<string> rideTags = new List<string>();
    [Tooltip("Exact mounted Pokemon species required by this rule. Empty means no species filter.")]
    [SerializeField] PokemonBase mountedPokemon;
    [Tooltip("Mounted Pokemon types that can match this rule. Empty means no type filter.")]
    [SerializeField] List<PokemonType> mountedPokemonTypes = new List<PokemonType>();

    [Header("Capacity")]
    [Tooltip("Total rider capacity including the player. 1 means no companion seat; 2 means player plus one companion.")]
    [Min(1)]
    [SerializeField] int totalRiderCapacity = 1;
    [Tooltip("Behavior when following companions exceed available companion seats.")]
    [SerializeField] RideCompanionOverflowMode overflowMode = RideCompanionOverflowMode.DetachUntilDismount;
    [Tooltip("In-game hours before detached companions return when overflow mode is Detach And Return After Delay.")]
    [Min(0)]
    [SerializeField] int returnDelayHours = 1;
    [Tooltip("If enabled, companions with CompanionRideCapability can stay with the player even when capacity is full.")]
    [SerializeField] bool allowSelfTravelCompanions = true;
    [Tooltip("Designer note saved into logs when this rule is selected.")]
    [SerializeField] string notes;

    public RidePokemonDefinition Ride => ride;
    public bool MatchRideMode => matchRideMode;
    public PokemonRideMode RideMode => rideMode;
    public IReadOnlyList<string> RideTags => rideTags != null ? (IReadOnlyList<string>)rideTags : Array.Empty<string>();
    public PokemonBase MountedPokemon => mountedPokemon;
    public IReadOnlyList<PokemonType> MountedPokemonTypes => mountedPokemonTypes != null ? (IReadOnlyList<PokemonType>)mountedPokemonTypes : Array.Empty<PokemonType>();
    public int TotalRiderCapacity => Mathf.Max(1, totalRiderCapacity);
    public RideCompanionOverflowMode OverflowMode => overflowMode;
    public int ReturnDelayHours => Mathf.Max(0, returnDelayHours);
    public bool AllowSelfTravelCompanions => allowSelfTravelCompanions;
    public string Notes => notes;

    public bool Matches(RidePokemonDefinition candidateRide, Pokemon mountedPokemonInstance) {
        if(ride != null && candidateRide != ride) {
            return false;
        }

        if(matchRideMode && (candidateRide == null || candidateRide.RideMode != rideMode)) {
            return false;
        }

        if(rideTags != null && rideTags.Count > 0) {
            if(candidateRide == null || !rideTags.Any(tag => candidateRide.HasTag(tag))) {
                return false;
            }
        }

        if(mountedPokemon != null) {
            if(mountedPokemonInstance == null || mountedPokemonInstance.OriginalBase != mountedPokemon) {
                return false;
            }
        }

        if(mountedPokemonTypes != null && mountedPokemonTypes.Count > 0) {
            if(mountedPokemonInstance == null || !mountedPokemonTypes.Any(type => mountedPokemonInstance.HasType(type))) {
                return false;
            }
        }

        return true;
    }
}

public class RideCompanionResolvedPolicy {
    public RideCompanionResolvedPolicy(int totalRiderCapacity, RideCompanionOverflowMode overflowMode, int returnDelayHours, bool allowSelfTravelCompanions, string notes) {
        TotalRiderCapacity = Mathf.Max(1, totalRiderCapacity);
        OverflowMode = overflowMode;
        ReturnDelayHours = Mathf.Max(0, returnDelayHours);
        AllowSelfTravelCompanions = allowSelfTravelCompanions;
        Notes = notes;
    }

    public int TotalRiderCapacity { get; }
    public int CompanionCapacity => Mathf.Max(0, TotalRiderCapacity - 1);
    public RideCompanionOverflowMode OverflowMode { get; }
    public int ReturnDelayHours { get; }
    public bool AllowSelfTravelCompanions { get; }
    public string Notes { get; }
}

[Serializable]
public class RideCompanionCoordinationSummary {
    [Tooltip("Ride id used by this summary.")]
    public string rideId;
    [Tooltip("Ride display name used by this summary.")]
    public string rideName;
    [Tooltip("Mounted Pokemon display name used by this summary.")]
    public string pokemonName;
    [Tooltip("Total rider capacity including the player.")]
    public int totalRiderCapacity;
    [Tooltip("Available companion seats after reserving one seat for the player.")]
    public int companionCapacity;
    [Tooltip("Number of companions following before coordination.")]
    public int followingCompanionCount;
    [Tooltip("Overflow behavior used by this summary.")]
    public RideCompanionOverflowMode overflowMode;
    [Tooltip("Rule or policy note that produced this summary.")]
    public string policyNote;
    [Tooltip("Companion ids kept within ride capacity.")]
    public List<string> keptCompanionIds = new List<string>();
    [Tooltip("Companion ids allowed to self-travel with their own ride capability.")]
    public List<string> selfTravelCompanionIds = new List<string>();
    [Tooltip("Companion ids detached until dismount or delayed return.")]
    public List<string> detachedCompanionIds = new List<string>();

    public string BuildMessage() {
        if(detachedCompanionIds != null && detachedCompanionIds.Count > 0) {
            return $"{detachedCompanionIds.Count} companion(s) cannot fit on {rideName} and will catch up later.";
        }

        if(selfTravelCompanionIds != null && selfTravelCompanionIds.Count > 0) {
            return $"{selfTravelCompanionIds.Count} companion(s) travel alongside {rideName}.";
        }

        return $"{rideName} companion capacity checked.";
    }
}
