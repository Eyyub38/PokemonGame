using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RegionTravelPoint : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id used by regional route repeat rules. Empty uses this GameObject name.")]
    [SerializeField] string pointId = string.Empty;
    [Tooltip("Readable point name stored in travel logs. Empty uses this GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Optional current world region for this travel point. If assigned, it can update PlayerWorldRegionLog on Start.")]
    [SerializeField] WorldRegionDefinition currentRegion;

    [Header("Routes")]
    [Tooltip("Regional routes offered by this travel point.")]
    [SerializeField] List<RegionTravelRouteDefinition> routes = new List<RegionTravelRouteDefinition>();
    [Tooltip("Optional manager used to execute travel. Empty searches scene or creates one on this GameObject.")]
    [SerializeField] RegionTravelManager managerOverride;

    [Header("Behavior")]
    [Tooltip("If enabled, current region is written to PlayerWorldRegionLog when this component starts.")]
    [SerializeField] bool setCurrentRegionOnStart;
    [Tooltip("If enabled, interacting with this point attempts the first available route. Future UI can replace this with route selection.")]
    [SerializeField] bool travelFirstAvailableOnInteract = true;
    [Tooltip("If enabled, entering the trigger attempts the first available route.")]
    [SerializeField] bool travelFirstAvailableOnTrigger;
    [Tooltip("Controls IPlayerTriggerable.TriggerRepeatedly.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("If enabled, result text is shown through DialogManager when available.")]
    [SerializeField] bool showDialogFeedback;

    public string PointId => string.IsNullOrWhiteSpace(pointId) ? gameObject.name : pointId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public WorldRegionDefinition CurrentRegion => currentRegion;
    public IReadOnlyList<RegionTravelRouteDefinition> Routes => routes;
    public bool TriggerRepeatedly => triggerRepeatedly;

    void Start() {
        if(setCurrentRegionOnStart && currentRegion != null) {
            var manager = ResolveManager();
            var player = PlayerController.i != null ? PlayerController.i : FindAnyObjectByType<PlayerController>();
            manager.ResolveLog(player)?.SetCurrentRegion(currentRegion, PointId);
        }
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(travelFirstAvailableOnTrigger) {
            TryTravelFirstAvailable(player, out _);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(!travelFirstAvailableOnInteract) {
            yield break;
        }

        var player = initiator != null ? initiator.GetComponent<PlayerController>() : null;
        TryTravelFirstAvailable(player, out var result);
        if(showDialogFeedback && DialogManager.i != null) {
            yield return DialogManager.i.ShowDialogText(BuildFeedbackMessage(result));
        }
    }

    [ContextMenu("Travel First Available")]
    public void TravelFirstAvailableFromContextMenu() {
        TryTravelFirstAvailable(null, out _);
    }

    public List<RegionTravelRouteDefinition> GetAvailableRoutes(PlayerController player = null) {
        player = player != null ? player : PlayerController.i != null ? PlayerController.i : FindAnyObjectByType<PlayerController>();
        var manager = ResolveManager();
        var log = manager.ResolveLog(player);
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        return (routes ?? new List<RegionTravelRouteDefinition>())
            .Where(route => route != null && RouteCanUseDefaultOption(route, player, log, party))
            .OrderBy(route => route.TravelMode)
            .ThenBy(route => route.DisplayName)
            .ToList();
    }

    public bool TryTravelFirstAvailable(PlayerController player, out RegionTravelResult result) {
        player = player != null ? player : PlayerController.i != null ? PlayerController.i : FindAnyObjectByType<PlayerController>();
        var route = GetAvailableRoutes(player).FirstOrDefault();
        if(route == null) {
            result = new RegionTravelResult(null, PointId, DisplayName) {
                blocked = true,
                failureMessage = "No available regional route."
            };
            return false;
        }

        return ResolveManager().TryTravel(route, null, PointId, DisplayName, out result);
    }

    public bool TryTravel(RegionTravelRouteDefinition route, Pokemon selectedPokemon, out RegionTravelResult result) {
        return ResolveManager().TryTravel(route, selectedPokemon, PointId, DisplayName, out result);
    }

    public bool TryTravelWithPolicy(RegionTravelRouteDefinition route, Pokemon selectedPokemon, string policyOptionId, out RegionTravelResult result) {
        return ResolveManager().TryTravelWithPolicy(route, selectedPokemon, policyOptionId, PointId, DisplayName, out result);
    }

    bool RouteCanUseDefaultOption(RegionTravelRouteDefinition route, PlayerController player, PlayerWorldRegionLog log, PokemonParty party) {
        var policyOption = route.ResolveTravelPolicyOption(null);
        Pokemon selectedPokemon = policyOption != null && policyOption.RequireSelectedPokemon
            ? party?.GetHealthyPokemon()
            : null;
        return route.CanUse(player, log, PointId, policyOption, selectedPokemon, out _);
    }

    RegionTravelManager ResolveManager() {
        if(managerOverride != null) {
            return managerOverride;
        }

        managerOverride = FindAnyObjectByType<RegionTravelManager>();
        if(managerOverride == null) {
            managerOverride = gameObject.AddComponent<RegionTravelManager>();
        }

        return managerOverride;
    }

    string BuildFeedbackMessage(RegionTravelResult result) {
        if(result == null) {
            return "Travel is unavailable.";
        }

        if(result.blocked) {
            return string.IsNullOrWhiteSpace(result.failureMessage) ? "Travel is unavailable." : result.failureMessage;
        }

        return string.IsNullOrWhiteSpace(result.destinationRegionName)
            ? "Travel complete."
            : $"Arrived at {result.destinationRegionName}.";
    }
}
