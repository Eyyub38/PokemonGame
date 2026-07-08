using System.Collections.Generic;
using UnityEngine;

public class TransitStation : MonoBehaviour, IPlayerTriggerable {
    [Header("Station")]
    [Tooltip("Stop definition that controls access and available routes.")]
    [SerializeField] TransitStopDefinition stopDefinition;
    [Tooltip("Optional save/id override for this station instance. Empty uses stop definition id or GameObject name.")]
    [SerializeField] string stationInstanceId;
    [Tooltip("If enabled, the GameObject name is used when no explicit station id exists.")]
    [SerializeField] bool fallbackToGameObjectName = true;

    [Header("Trigger")]
    [Tooltip("If enabled, repeated player triggers can call OnPlayerTriggered more than once.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("If enabled, triggering this station automatically uses the first available route. Leave disabled for UI-driven stations.")]
    [SerializeField] bool autoTravelFirstAvailableRoute;
    [Tooltip("If enabled, a blocked auto-travel attempt publishes a transit blocked event.")]
    [SerializeField] bool publishBlockedAutoTravel = true;

    [Header("Local Arrival")]
    [Tooltip("Optional local point used for same-scene transit movement.")]
    [SerializeField] Transform localArrivalPoint;
    [Tooltip("If enabled, successful travel moves the player to Local Arrival Point when assigned.")]
    [SerializeField] bool movePlayerToLocalArrivalPoint;

    public TransitStopDefinition StopDefinition => stopDefinition;
    public bool TriggerRepeatedly => triggerRepeatedly;
    public string StationId {
        get {
            if(!string.IsNullOrWhiteSpace(stationInstanceId)) {
                return stationInstanceId;
            }

            if(stopDefinition != null) {
                return stopDefinition.Id;
            }

            return fallbackToGameObjectName ? name : "transit-station";
        }
    }

    public void OnPlayerTriggered(PlayerController player) {
        Discover(player, "trigger");

        if(!autoTravelFirstAvailableRoute) {
            return;
        }

        var routes = GetAvailableRoutes(player);
        if(routes.Count == 0) {
            if(publishBlockedAutoTravel) {
                GameEventPublishing.PublishOptional(
                    null,
                    $"transit.station.blocked.{StationId}",
                    $"{StationId} has no available routes.",
                    GameEventCategory.Transit,
                    GameEventImportance.Warning,
                    this,
                    "TransitStation",
                    GameEventScope.Player,
                    showInFeed: false,
                    writeToDebugLog: true,
                    GameEventPublishing.Value("stationId", StationId));
            }
            return;
        }

        TryTravel(player, routes[0], out _, out _);
    }

    public bool CanUse(PlayerController player, out string failureMessage) {
        if(stopDefinition == null) {
            failureMessage = "No transit stop definition assigned.";
            return false;
        }

        if(player == null) {
            failureMessage = "A player is required to use transit.";
            return false;
        }

        var log = player.GetComponent<PlayerTransitLog>();
        return stopDefinition.IsUnlocked(player, log, out failureMessage);
    }

    public void Discover(PlayerController player, string source = null) {
        if(player == null || stopDefinition == null) {
            return;
        }

        player.GetComponent<PlayerTransitLog>()?.UnlockStop(stopDefinition, source);
    }

    public List<TransitRouteDefinition> GetAvailableRoutes(PlayerController player) {
        if(stopDefinition == null || player == null) {
            return new List<TransitRouteDefinition>();
        }

        var log = player.GetComponent<PlayerTransitLog>();
        return stopDefinition.GetAvailableRoutes(player, log, StationId);
    }

    public bool CanTravel(PlayerController player, TransitRouteDefinition route, out string failureMessage) {
        if(!CanUse(player, out failureMessage)) {
            return false;
        }

        if(route == null) {
            failureMessage = "No transit route selected.";
            return false;
        }

        var log = player.GetComponent<PlayerTransitLog>();
        if(log == null) {
            failureMessage = "The player has no transit log.";
            return false;
        }

        return route.CanUse(player, log, StationId, out failureMessage);
    }

    public bool TryTravel(PlayerController player, TransitRouteDefinition route, out string failureMessage) {
        return TryTravel(player, route, out _, out failureMessage);
    }

    public bool TryTravel(PlayerController player, TransitRouteDefinition route, out TransitTravelResult result, out string failureMessage) {
        result = null;
        if(!CanTravel(player, route, out failureMessage)) {
            if(route != null) {
                route.PublishBlocked(player, StationId, failureMessage);
            }
            return false;
        }

        if(!route.TryPayCosts(player, out failureMessage)) {
            route.PublishBlocked(player, StationId, failureMessage);
            return false;
        }

        var log = player.GetComponent<PlayerTransitLog>();
        route.PublishDeparted(player, StationId);
        log.RecordTravel(route, StationId, route.DestinationStopId, route.EstimatedTravelHours);

        if(movePlayerToLocalArrivalPoint && localArrivalPoint != null && player.Character != null) {
            player.Character.SetPositionAndSnapToTile(localArrivalPoint.position);
        }

        route.ApplyArrivalEffects(player);
        route.PublishArrived(player, StationId);

        result = new TransitTravelResult {
            stationId = StationId,
            routeId = route.Id,
            routeName = route.DisplayName,
            originStopId = StationId,
            destinationStopId = route.DestinationStopId,
            destinationSceneName = route.DestinationSceneName,
            destinationPortalId = route.DestinationPortalId,
            moneyPaid = route.MoneyCost,
            estimatedTravelHours = route.EstimatedTravelHours
        };
        failureMessage = null;
        return true;
    }
}

[System.Serializable]
public class TransitTravelResult {
    [Tooltip("Station instance id where travel was started.")]
    public string stationId;
    [Tooltip("Route id used for this travel result.")]
    public string routeId;
    [Tooltip("Route display name captured for UI/debug output.")]
    public string routeName;
    [Tooltip("Origin stop id used for this travel result.")]
    public string originStopId;
    [Tooltip("Destination stop id used for this travel result.")]
    public string destinationStopId;
    [Tooltip("Destination scene name requested by this route.")]
    public string destinationSceneName;
    [Tooltip("Destination portal/spawn id requested by this route.")]
    public string destinationPortalId;
    [Tooltip("Money paid for this travel.")]
    public float moneyPaid;
    [Tooltip("Estimated route duration in in-game hours.")]
    public int estimatedTravelHours;
}
