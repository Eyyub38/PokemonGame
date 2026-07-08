using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CompanionRideCapability : MonoBehaviour {
    [Header("Capability")]
    [Tooltip("If enabled, this companion can handle ride overflow with their own travel method.")]
    [SerializeField] bool canSelfTravelWhenOverflow = true;
    [Tooltip("If enabled, the companion keeps following the player while considered self-travelling.")]
    [SerializeField] bool keepFollowingWhileSelfTravelling = true;
    [Tooltip("Optional display label for this companion's own ride/travel method.")]
    [SerializeField] string selfTravelLabel = "own ride";

    [Header("Ride Match")]
    [Tooltip("Exact ride definitions this companion can self-travel alongside. Empty means no exact ride filter.")]
    [SerializeField] List<RidePokemonDefinition> supportedRides = new List<RidePokemonDefinition>();
    [Tooltip("Ride modes this companion can self-travel alongside. Empty means no ride mode filter.")]
    [SerializeField] List<PokemonRideMode> supportedRideModes = new List<PokemonRideMode>();
    [Tooltip("Ride tags this companion can self-travel alongside. Empty means no tag filter.")]
    [SerializeField] List<string> supportedRideTags = new List<string>();

    [Header("Mounted Pokemon Match")]
    [Tooltip("Exact mounted Pokemon species this companion can keep pace with. Empty means no species filter.")]
    [SerializeField] List<PokemonBase> supportedMountedPokemon = new List<PokemonBase>();
    [Tooltip("Mounted Pokemon types this companion can keep pace with. Empty means no type filter.")]
    [SerializeField] List<PokemonType> supportedMountedPokemonTypes = new List<PokemonType>();

    [Header("Companion Pokemon")]
    [Tooltip("If enabled, this companion must have a matching Pokemon in CompanionPokemonTeam before self-travelling.")]
    [SerializeField] bool requireCompanionPokemonForSelfTravel;
    [Tooltip("If enabled, this companion's Pokemon must pass the active RidePokemonDefinition Pokemon rules.")]
    [SerializeField] bool useRideDefinitionPokemonRules = true;
    [Tooltip("Exact companion-owned Pokemon species that can support self-travel. Used when Use Ride Definition Pokemon Rules is disabled.")]
    [SerializeField] List<PokemonBase> selfTravelPokemon = new List<PokemonBase>();
    [Tooltip("Companion-owned Pokemon types that can support self-travel. Used when Use Ride Definition Pokemon Rules is disabled.")]
    [SerializeField] List<PokemonType> selfTravelPokemonTypes = new List<PokemonType>();
    [Tooltip("Optional move required on the companion-owned Pokemon. Used when Use Ride Definition Pokemon Rules is disabled.")]
    [SerializeField] MoveBase selfTravelRequiredMove;
    [Tooltip("Minimum level required on the companion-owned Pokemon. Used when Use Ride Definition Pokemon Rules is disabled.")]
    [Min(1)]
    [SerializeField] int selfTravelMinimumLevel = 1;
    [Tooltip("If enabled, fainted companion-owned Pokemon cannot support self-travel.")]
    [SerializeField] bool requireHealthySelfTravelPokemon = true;

    public bool CanSelfTravelWhenOverflow => canSelfTravelWhenOverflow;
    public bool KeepFollowingWhileSelfTravelling => keepFollowingWhileSelfTravelling;
    public string SelfTravelLabel => string.IsNullOrWhiteSpace(selfTravelLabel) ? "own ride" : selfTravelLabel;
    public Pokemon LastSelectedTravelPokemon { get; private set; }
    public string LastFailureMessage { get; private set; }

    public bool CanSelfTravel(RidePokemonDefinition ride, Pokemon mountedPokemon) {
        LastSelectedTravelPokemon = null;
        LastFailureMessage = null;

        if(!canSelfTravelWhenOverflow) {
            LastFailureMessage = "Self-travel is disabled.";
            return false;
        }

        if(supportedRides != null && supportedRides.Count > 0 && !supportedRides.Contains(ride)) {
            LastFailureMessage = "Ride definition is not supported.";
            return false;
        }

        if(supportedRideModes != null && supportedRideModes.Count > 0) {
            if(ride == null || !supportedRideModes.Contains(ride.RideMode)) {
                LastFailureMessage = "Ride mode is not supported.";
                return false;
            }
        }

        if(supportedRideTags != null && supportedRideTags.Count > 0) {
            if(ride == null || !supportedRideTags.Any(tag => ride.HasTag(tag))) {
                LastFailureMessage = "Ride tag is not supported.";
                return false;
            }
        }

        if(supportedMountedPokemon != null && supportedMountedPokemon.Count > 0) {
            if(mountedPokemon == null || !supportedMountedPokemon.Contains(mountedPokemon.OriginalBase)) {
                LastFailureMessage = "Mounted Pokemon is not supported.";
                return false;
            }
        }

        if(supportedMountedPokemonTypes != null && supportedMountedPokemonTypes.Count > 0) {
            if(mountedPokemon == null || !supportedMountedPokemonTypes.Any(type => mountedPokemon.HasType(type))) {
                LastFailureMessage = "Mounted Pokemon type is not supported.";
                return false;
            }
        }

        var team = GetComponent<CompanionPokemonTeam>();
        if(requireCompanionPokemonForSelfTravel) {
            if(team == null) {
                LastFailureMessage = "Companion Pokemon team is missing.";
                return false;
            }

            LastSelectedTravelPokemon = ResolveTravelPokemon(team, ride, out var failureMessage);
            if(LastSelectedTravelPokemon == null) {
                LastFailureMessage = failureMessage;
                return false;
            }
        } else if(team != null) {
            LastSelectedTravelPokemon = ResolveTravelPokemon(team, ride, out _);
        }

        return true;
    }

    Pokemon ResolveTravelPokemon(CompanionPokemonTeam team, RidePokemonDefinition ride, out string failureMessage) {
        if(team == null) {
            failureMessage = "Companion Pokemon team is missing.";
            return null;
        }

        if(useRideDefinitionPokemonRules && ride != null) {
            return team.FindUsableForRide(ride, out failureMessage);
        }

        return team.FindMatchingPokemon(
            selfTravelPokemon,
            selfTravelPokemonTypes,
            selfTravelRequiredMove,
            selfTravelMinimumLevel,
            requireHealthySelfTravelPokemon,
            out failureMessage);
    }
}
