using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RegionTravelManager : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Optional explicit player. Empty uses PlayerController.i or the first player in the scene.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("If enabled, PlayerWorldRegionLog is added to the player when missing.")]
    [SerializeField] bool autoInstallRegionLog = true;

    [Header("Scene Flow")]
    [Tooltip("If enabled, SceneManager.LoadScene is called after successful regional travel when a destination scene is available.")]
    [SerializeField] bool loadSceneOnTravel;
    [Tooltip("If enabled, the active region is set before scene loading. Leave enabled for save/log consistency.")]
    [SerializeField] bool setCurrentRegionBeforeSceneLoad = true;

    [Header("Party Transfer")]
    [Tooltip("If enabled, Store Party Except Selected challenge mode physically moves other party Pokemon to PokemonStorageBoxes.")]
    [SerializeField] bool applyStorageTransferOnChallengeStart;
    [Tooltip("If enabled, the first healthy Pokemon is used when a challenge needs one selected and no selected Pokemon was provided.")]
    [SerializeField] bool autoSelectFirstHealthyPokemon = true;

    [Header("Debug")]
    [Tooltip("If enabled, travel results are written through GameDebug.")]
    [SerializeField] bool writeDebugLogs = true;

    public PlayerController PlayerOverride {
        get => playerOverride;
        set => playerOverride = value;
    }

    public bool TryTravel(RegionTravelRouteDefinition route, out RegionTravelResult result) {
        return TryTravelInternal(route, null, null, null, null, out result);
    }

    public bool TryTravel(RegionTravelRouteDefinition route, Pokemon selectedPokemon, out RegionTravelResult result) {
        return TryTravelInternal(route, selectedPokemon, null, null, null, out result);
    }

    public bool TryTravel(RegionTravelRouteDefinition route, Pokemon selectedPokemon, string sourceId, string sourceName, out RegionTravelResult result) {
        return TryTravelInternal(route, selectedPokemon, null, sourceId, sourceName, out result);
    }

    public bool TryTravelWithPolicy(RegionTravelRouteDefinition route, Pokemon selectedPokemon, string policyOptionId, out RegionTravelResult result) {
        return TryTravelInternal(route, selectedPokemon, policyOptionId, null, null, out result);
    }

    public bool TryTravelWithPolicy(RegionTravelRouteDefinition route, Pokemon selectedPokemon, string policyOptionId, string sourceId, string sourceName, out RegionTravelResult result) {
        return TryTravelInternal(route, selectedPokemon, policyOptionId, sourceId, sourceName, out result);
    }

    bool TryTravelInternal(RegionTravelRouteDefinition route, Pokemon selectedPokemon, string policyOptionId, string sourceId, string sourceName, out RegionTravelResult result) {
        string resolvedSourceId = string.IsNullOrWhiteSpace(sourceId) ? gameObject.name : sourceId;
        string resolvedSourceName = string.IsNullOrWhiteSpace(sourceName) ? gameObject.name : sourceName;
        result = new RegionTravelResult(route, resolvedSourceId, resolvedSourceName);

        var player = ResolvePlayer();
        var log = ResolveLog(player);
        if(route == null) {
            result.blocked = true;
            result.failureMessage = "No regional route selected.";
            LogResult(result);
            return false;
        }

        var policyOption = route.ResolveTravelPolicyOption(policyOptionId);
        var challengeForTravel = ResolveChallengeForTravel(route, policyOption);
        var effectivePartyTransferMode = ResolveEffectivePartyTransferMode(route, policyOption, challengeForTravel);
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        if(ShouldResolveSelectedPokemon(policyOption, effectivePartyTransferMode)) {
            selectedPokemon = ResolveSelectedPokemon(party, selectedPokemon);
        }

        ApplyPolicyResultMetadata(route, policyOption, selectedPokemon, effectivePartyTransferMode, result);

        if(!route.CanUse(player, log, resolvedSourceId, policyOption, selectedPokemon, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            log?.RecordTravel(route, result);
            route.PublishBlocked(player, result, this);
            LogResult(result);
            return false;
        }

        if(!route.TryPayCosts(player, out failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            log?.RecordTravel(route, result);
            route.PublishBlocked(player, result, this);
            LogResult(result);
            return false;
        }

        result.costsPaid = true;
        policyOption?.ApplyBeforeTravel(player, log, result, this);
        route.PublishDeparted(player, result, this);

        var destination = route.DestinationRegion;
        if(setCurrentRegionBeforeSceneLoad && log != null && destination != null) {
            log.SetCurrentRegion(destination, route.Id, discover: false);
        }

        if(route.DiscoverDestinationOnArrival && destination != null) {
            destination.ApplyDiscovery(player, route.Id);
            result.destinationDiscovered = true;
        }

        route.ApplyArrivalRewards(player, this);
        StartChallengeIfNeeded(player, log, route, selectedPokemon, policyOption, challengeForTravel, effectivePartyTransferMode, result);
        ApplyPartyTransferIfNeeded(player, party, selectedPokemon, effectivePartyTransferMode, result);

        if(route.UnlockRouteAfterTravel && log != null) {
            log.UnlockRoute(route, route.Id);
        }

        log?.RecordTravel(route, result);
        destination?.PublishEntered(player, this);
        route.PublishArrived(player, result, this);
        LogResult(result);

        if(loadSceneOnTravel && !string.IsNullOrWhiteSpace(result.destinationSceneName)) {
            SceneManager.LoadScene(result.destinationSceneName);
        }

        return true;
    }

    public void CompleteActiveChallenge(bool applyRewards = true) {
        var player = ResolvePlayer();
        ResolveLog(player)?.CompleteActiveChallenge(player, applyRewards, this);
    }

    public PlayerWorldRegionLog ResolveLog(PlayerController player = null) {
        player = player != null ? player : ResolvePlayer();
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerWorldRegionLog>();
        if(log == null && autoInstallRegionLog) {
            log = player.gameObject.AddComponent<PlayerWorldRegionLog>();
        }

        return log;
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }

    void StartChallengeIfNeeded(PlayerController player, PlayerWorldRegionLog log, RegionTravelRouteDefinition route, Pokemon selectedPokemon, RegionTravelPolicyOption policyOption, RegionChallengeProfileDefinition challenge, RegionPartyTransferMode partyTransferMode, RegionTravelResult result) {
        if(player == null || log == null || challenge == null) {
            return;
        }

        var party = player.GetComponent<PokemonParty>();
        selectedPokemon = ResolveSelectedPokemon(party, selectedPokemon);
        var allowedPokemonIds = policyOption != null
            ? policyOption.BuildAllowedPokemonIds(party, selectedPokemon, challenge)
            : challenge.BuildAllowedPokemonIds(party, selectedPokemon);
        var state = log.StartChallenge(route.DestinationRegion, challenge, party, selectedPokemon, route.Id, partyTransferMode, allowedPokemonIds);

        result.challengeStarted = state != null && state.active;
        result.challengeId = challenge.Id;
        result.challengeName = challenge.DisplayName;
        challenge.PublishStarted(player, route.DestinationRegion, this);
    }

    Pokemon ResolveSelectedPokemon(PokemonParty party, Pokemon selectedPokemon) {
        if(selectedPokemon != null || !autoSelectFirstHealthyPokemon || party == null) {
            return selectedPokemon;
        }

        return party.GetHealthyPokemon() ?? (party.Pokemons != null && party.Pokemons.Count > 0 ? party.Pokemons[0] : null);
    }

    RegionChallengeProfileDefinition ResolveChallengeForTravel(RegionTravelRouteDefinition route, RegionTravelPolicyOption policyOption) {
        return policyOption != null ? policyOption.ResolveChallengeProfile(route) : route != null ? route.ChallengeProfile : null;
    }

    RegionPartyTransferMode ResolveEffectivePartyTransferMode(RegionTravelRouteDefinition route, RegionTravelPolicyOption policyOption, RegionChallengeProfileDefinition challenge) {
        if(policyOption != null) {
            return policyOption.ResolvePartyTransferMode(challenge);
        }

        return challenge != null ? challenge.PartyTransferMode : RegionPartyTransferMode.KeepCurrentParty;
    }

    bool ShouldResolveSelectedPokemon(RegionTravelPolicyOption policyOption, RegionPartyTransferMode partyTransferMode) {
        if(!autoSelectFirstHealthyPokemon) {
            return false;
        }

        return (policyOption != null && policyOption.RequireSelectedPokemon)
            || partyTransferMode == RegionPartyTransferMode.OnePokemonOnly
            || partyTransferMode == RegionPartyTransferMode.StorePartyExceptSelected;
    }

    void ApplyPolicyResultMetadata(RegionTravelRouteDefinition route, RegionTravelPolicyOption policyOption, Pokemon selectedPokemon, RegionPartyTransferMode partyTransferMode, RegionTravelResult result) {
        if(result == null) {
            return;
        }

        if(route != null && route.TravelPolicy != null) {
            result.policyId = route.TravelPolicy.Id;
            result.policyName = route.TravelPolicy.DisplayName;
        }

        if(policyOption != null) {
            result.policyOptionId = policyOption.Id;
            result.policyOptionName = policyOption.DisplayName;
        }

        result.partyTransferMode = partyTransferMode;
        result.selectedPokemonId = selectedPokemon != null ? selectedPokemon.InstanceId : string.Empty;
        result.selectedPokemonName = selectedPokemon != null ? selectedPokemon.NickName : string.Empty;
    }

    void ApplyPartyTransferIfNeeded(PlayerController player, PokemonParty party, Pokemon selectedPokemon, RegionPartyTransferMode partyTransferMode, RegionTravelResult result) {
        if(!applyStorageTransferOnChallengeStart
            || player == null
            || party == null
            || selectedPokemon == null
            || partyTransferMode != RegionPartyTransferMode.StorePartyExceptSelected) {
            return;
        }

        var storage = player.GetComponent<PokemonStorageBoxes>();
        if(storage == null) {
            result.messages.Add("Pokemon storage was not found, so party transfer was recorded but not applied.");
            return;
        }

        var kept = new List<Pokemon> { selectedPokemon };
        foreach(var pokemon in party.Pokemons ?? new List<Pokemon>()) {
            if(pokemon != null && pokemon != selectedPokemon) {
                storage.AddPokemonToEmptySlot(pokemon);
            }
        }

        party.Pokemons = kept;
        party.PartyUpdated();
        result.messages.Add("Party was moved to storage except the selected Pokemon.");
    }

    void LogResult(RegionTravelResult result) {
        if(!writeDebugLogs || result == null) {
            return;
        }

        if(result.blocked) {
            GameDebug.Warning($"{result.routeName} blocked: {result.failureMessage}", GameDebugCategory.Transit, this, "RegionTravelManager");
            return;
        }

        GameDebug.Success($"{result.routeName} completed. Destination: {result.destinationRegionName}", GameDebugCategory.Transit, this, "RegionTravelManager");
    }
}
