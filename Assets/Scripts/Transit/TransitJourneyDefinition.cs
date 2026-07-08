using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TransitJourneyStopRule {
    OptionalDisembark,
    RequiredDisembark,
    PassThrough
}

public enum TransitJourneyIncidentTrigger {
    JourneyStarted,
    LegDeparted,
    StopReached,
    ContinuedFromStop,
    Disembarked,
    JourneyCompleted,
    JourneyCancelled,
    OnboardActivityRecorded
}

[CreateAssetMenu(menuName = "Transit/Journey Definition")]
public class TransitJourneyDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this journey. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future transit UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing description for this vehicle journey.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad vehicle type used by UI filters and journey events.")]
    [SerializeField] TransitRouteType journeyType = TransitRouteType.Train;
    [Tooltip("Free-form tags used by requirements, jobs, dialog conditions and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Vehicle Space")]
    [Tooltip("Optional scene name used for the vehicle interior while the journey is active.")]
    [SerializeField] string vehicleInteriorSceneName = string.Empty;
    [Tooltip("Optional spawn/portal id used when placing the player inside the vehicle interior.")]
    [SerializeField] string vehicleInteriorSpawnId = string.Empty;
    [Tooltip("If enabled, future scene flow should move the player into the vehicle interior when the journey starts.")]
    [SerializeField] bool useVehicleInterior;
    [Tooltip("If enabled, future scene flow should keep the player in the vehicle interior until they disembark.")]
    [SerializeField] bool stayInVehicleInteriorBetweenStops = true;

    [Header("Flow")]
    [Tooltip("Ordered travel legs and stop windows that make up this journey.")]
    [SerializeField] List<TransitJourneyLeg> legs = new List<TransitJourneyLeg>();
    [Tooltip("If enabled, the journey automatically departs after a stop dwell timer reaches zero.")]
    [SerializeField] bool autoContinueAfterDwell;
    [Tooltip("If enabled, route costs are paid for every leg as it departs. If disabled, only the first leg cost is paid when the journey starts.")]
    [SerializeField] bool payEachLeg = true;
    [Tooltip("If enabled, each route applies arrival rewards/effects when the vehicle reaches that leg's destination.")]
    [SerializeField] bool applyRouteArrivalEffects = true;
    [Tooltip("If enabled, PlayerTransitLog records every completed leg.")]
    [SerializeField] bool recordEachLegTravel = true;

    [Header("Journey Incidents")]
    [Tooltip("Optional incident hooks triggered by journey phases such as start, stop reached, disembark or onboard activity.")]
    [SerializeField] List<TransitJourneyIncidentHook> incidentHooks = new List<TransitJourneyIncidentHook>();

    [Header("Events")]
    [Tooltip("Optional event published when the journey starts. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition startedEvent;
    [Tooltip("Optional event published when the journey reaches a stop. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition stopReachedEvent;
    [Tooltip("Optional event published when the player disembarks. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition disembarkedEvent;
    [Tooltip("Optional event published when the full journey completes. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("Optional event published when the journey is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, journey events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, journey events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public TransitRouteType JourneyType => journeyType;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public string VehicleInteriorSceneName => vehicleInteriorSceneName;
    public string VehicleInteriorSpawnId => vehicleInteriorSpawnId;
    public bool UseVehicleInterior => useVehicleInterior;
    public bool StayInVehicleInteriorBetweenStops => stayInVehicleInteriorBetweenStops;
    public IReadOnlyList<TransitJourneyLeg> Legs => legs != null ? (IReadOnlyList<TransitJourneyLeg>)legs : Array.Empty<TransitJourneyLeg>();
    public bool AutoContinueAfterDwell => autoContinueAfterDwell;
    public bool PayEachLeg => payEachLeg;
    public bool ApplyRouteArrivalEffects => applyRouteArrivalEffects;
    public bool RecordEachLegTravel => recordEachLegTravel;
    public IReadOnlyList<TransitJourneyIncidentHook> IncidentHooks => incidentHooks != null ? (IReadOnlyList<TransitJourneyIncidentHook>)incidentHooks : Array.Empty<TransitJourneyIncidentHook>();

    public bool CanStart(PlayerController player, PlayerTransitLog transitLog, string originStopId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to start transit journeys.";
            return false;
        }

        var firstLeg = GetLeg(0);
        if(firstLeg == null || firstLeg.Route == null) {
            failureMessage = $"{DisplayName} has no first route leg.";
            return false;
        }

        if(!firstLeg.CanDepartFrom(originStopId)) {
            failureMessage = $"{DisplayName} cannot depart from this stop.";
            return false;
        }

        if(!firstLeg.Route.CanUse(player, transitLog, originStopId, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public TransitJourneyLeg GetLeg(int index) {
        return index >= 0 && index < Legs.Count ? Legs[index] : null;
    }

    public bool HasNextLeg(int currentLegIndex) {
        return GetLeg(currentLegIndex + 1) != null;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishStarted(PlayerController player, string sourceId, string originStopId) {
        PublishJourneyEvent(startedEvent, "started", GameEventImportance.Info, player, sourceId, originStopId, null, null);
    }

    public void PublishStopReached(PlayerController player, string sourceId, string stopId, TransitJourneyLeg leg) {
        PublishJourneyEvent(stopReachedEvent, "stop-reached", GameEventImportance.Info, player, sourceId, stopId, leg, null);
    }

    public void PublishDisembarked(PlayerController player, string sourceId, string stopId, TransitJourneyLeg leg) {
        PublishJourneyEvent(disembarkedEvent, "disembarked", GameEventImportance.Success, player, sourceId, stopId, leg, null);
    }

    public void PublishCompleted(PlayerController player, string sourceId, string stopId) {
        PublishJourneyEvent(completedEvent, "completed", GameEventImportance.Success, player, sourceId, stopId, null, null);
    }

    public void PublishBlocked(PlayerController player, string sourceId, string stopId, string reason) {
        PublishJourneyEvent(blockedEvent, "blocked", GameEventImportance.Warning, player, sourceId, stopId, null, reason);
    }

    public TransitJourneyIncidentHookRunResult TriggerIncidentHooks(
        TransitJourneyIncidentTrigger trigger,
        PlayerController player,
        PlayerTransitJourneyState state,
        TransitJourneyLeg leg,
        string sourceId,
        UnityEngine.Object context = null) {
        var result = new TransitJourneyIncidentHookRunResult {
            journeyId = Id,
            journeyName = DisplayName,
            trigger = trigger,
            sourceId = sourceId,
            stopId = state != null ? state.currentStopId : leg != null ? leg.DestinationStopId : string.Empty
        };

        foreach(var hook in IncidentHooks) {
            if(hook == null || !hook.Matches(trigger, state, leg)) {
                continue;
            }

            result.attemptedHooks++;
            if(hook.TryRun(player, this, state, leg, sourceId, context != null ? context : this, out var hookMessage)) {
                result.successfulHooks++;
            } else {
                result.blockedHooks++;
            }

            if(!string.IsNullOrWhiteSpace(hookMessage)) {
                result.messages.Add(hookMessage);
            }
        }

        return result;
    }

    void PublishJourneyEvent(GameEventDefinition eventDefinition, string phase, GameEventImportance importance, PlayerController player, string sourceId, string stopId, TransitJourneyLeg leg, string reason) {
        string fallbackMessage = phase switch {
            "started" => $"{DisplayName} started.",
            "stop-reached" => $"{DisplayName} reached {leg?.DestinationDisplayName ?? stopId}.",
            "disembarked" => $"Disembarked from {DisplayName}.",
            "completed" => $"{DisplayName} completed.",
            "blocked" => string.IsNullOrWhiteSpace(reason) ? $"{DisplayName} is blocked." : reason,
            _ => $"{DisplayName}: {phase}."
        };

        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"transit.journey.{phase}.{Id}",
            fallbackMessage,
            GameEventCategory.Transit,
            importance,
            player != null ? player : this,
            "TransitJourneyDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("journeyId", Id),
            GameEventPublishing.Value("journeyName", DisplayName),
            GameEventPublishing.Value("journeyType", journeyType),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("stopId", stopId),
            GameEventPublishing.Value("routeId", leg?.Route != null ? leg.Route.Id : string.Empty),
            GameEventPublishing.Value("routeName", leg?.Route != null ? leg.Route.DisplayName : string.Empty),
            GameEventPublishing.Value("reason", reason));
    }
}

[Serializable]
public class TransitJourneyLeg {
    [Tooltip("Route used by this leg. Its origin/destination/cost/access data are reused.")]
    [SerializeField] TransitRouteDefinition route;
    [Tooltip("Optional origin stop id override. Empty uses the route origin or the current journey stop.")]
    [SerializeField] string originStopId = string.Empty;
    [Tooltip("Optional destination stop id override. Empty uses the route destination.")]
    [SerializeField] string destinationStopId = string.Empty;
    [Tooltip("Optional destination name shown by UI when no stop asset is needed.")]
    [SerializeField] string destinationDisplayName = string.Empty;
    [Tooltip("How the player can interact with this stop after arrival.")]
    [SerializeField] TransitJourneyStopRule stopRule = TransitJourneyStopRule.OptionalDisembark;
    [Tooltip("How many in-game hours the vehicle waits at this stop before it can auto-continue.")]
    [Min(0)]
    [SerializeField] int dwellHours = 1;
    [Tooltip("Optional route duration override. Negative values use the route estimated hours.")]
    [SerializeField] int travelHoursOverride = -1;

    public TransitRouteDefinition Route => route;
    public string OriginStopId => string.IsNullOrWhiteSpace(originStopId) ? route != null ? route.OriginStopId : string.Empty : originStopId;
    public string DestinationStopId => string.IsNullOrWhiteSpace(destinationStopId) ? route != null ? route.DestinationStopId : string.Empty : destinationStopId;
    public string DestinationDisplayName => string.IsNullOrWhiteSpace(destinationDisplayName) ? DestinationStopId : destinationDisplayName;
    public TransitJourneyStopRule StopRule => stopRule;
    public int DwellHours => Mathf.Max(0, dwellHours);
    public int TravelHours => travelHoursOverride >= 0 ? travelHoursOverride : route != null ? route.EstimatedTravelHours : 0;
    public bool CanDisembark => stopRule == TransitJourneyStopRule.OptionalDisembark || stopRule == TransitJourneyStopRule.RequiredDisembark;

    public bool CanDepartFrom(string currentStopId) {
        if(route == null) {
            return false;
        }

        string origin = OriginStopId;
        return string.IsNullOrWhiteSpace(origin)
            || string.IsNullOrWhiteSpace(currentStopId)
            || string.Equals(origin, currentStopId, StringComparison.OrdinalIgnoreCase);
    }
}

[Serializable]
public class TransitJourneyIncidentHook {
    [Tooltip("If disabled, this incident hook is ignored.")]
    [SerializeField] bool enabled = true;
    [Tooltip("Journey phase that triggers this hook.")]
    [SerializeField] TransitJourneyIncidentTrigger trigger = TransitJourneyIncidentTrigger.StopReached;
    [Tooltip("Optional direct incident activated when this hook runs.")]
    [SerializeField] JourneyIncidentDefinition incident;
    [Tooltip("Optional incident board rolled when this hook runs. If both Incident and Board are assigned, both are attempted.")]
    [SerializeField] JourneyIncidentBoardDefinition board;
    [Tooltip("Chance that this hook runs after trigger and filters match.")]
    [Range(0f, 1f)]
    [SerializeField] float chance = 1f;
    [Tooltip("If enabled, this hook only runs on the configured leg index.")]
    [SerializeField] bool requireLegIndex;
    [Tooltip("Leg index required when Require Leg Index is enabled.")]
    [Min(0)]
    [SerializeField] int legIndex;
    [Tooltip("Optional stop id filter. For departure triggers this checks origin; for arrival/stop triggers this checks current/destination stop.")]
    [SerializeField] string stopIdFilter = string.Empty;
    [Tooltip("Optional source id suffix appended to the journey source id for repeat/cooldown separation.")]
    [SerializeField] string sourceIdSuffix = string.Empty;
    [Tooltip("Optional region context passed into incident filters. Empty means no explicit region.")]
    [SerializeField] RegionInfoDefinition regionOverride;
    [Tooltip("Optional zone context passed into incident filters. Empty can fall back to PlayerActivityContext.CurrentZone if Use Current Player Zone is enabled.")]
    [SerializeField] ActivityZoneDefinition zoneOverride;
    [Tooltip("If enabled and Zone Override is empty, PlayerActivityContext.CurrentZone is passed to incident filters.")]
    [SerializeField] bool useCurrentPlayerZone = true;

    public bool Enabled => enabled;
    public TransitJourneyIncidentTrigger Trigger => trigger;
    public JourneyIncidentDefinition Incident => incident;
    public JourneyIncidentBoardDefinition Board => board;
    public float Chance => Mathf.Clamp01(chance);
    public bool RequireLegIndex => requireLegIndex;
    public int LegIndex => Mathf.Max(0, legIndex);
    public string StopIdFilter => stopIdFilter;
    public string SourceIdSuffix => sourceIdSuffix;
    public RegionInfoDefinition RegionOverride => regionOverride;
    public ActivityZoneDefinition ZoneOverride => zoneOverride;
    public bool UseCurrentPlayerZone => useCurrentPlayerZone;

    public bool Matches(TransitJourneyIncidentTrigger candidateTrigger, PlayerTransitJourneyState state, TransitJourneyLeg leg) {
        if(!enabled || trigger != candidateTrigger) {
            return false;
        }

        if(incident == null && board == null) {
            return false;
        }

        if(requireLegIndex && (state == null || state.currentLegIndex != LegIndex)) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(stopIdFilter)) {
            string candidateStop = ResolveStopForTrigger(candidateTrigger, state, leg);
            if(!string.Equals(candidateStop, stopIdFilter, StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
        }

        return true;
    }

    public bool TryRun(PlayerController player, TransitJourneyDefinition journey, PlayerTransitJourneyState state, TransitJourneyLeg leg, string sourceId, UnityEngine.Object context, out string message) {
        message = null;
        if(player == null) {
            message = "A player is required to run transit journey incident hooks.";
            return false;
        }

        if(UnityEngine.Random.value > Chance) {
            message = "Transit journey incident hook chance failed.";
            return false;
        }

        string resolvedSourceId = ResolveSourceId(journey, sourceId);
        string sourceName = journey != null ? journey.DisplayName : "Transit Journey";
        var region = regionOverride;
        var zone = zoneOverride != null ? zoneOverride : useCurrentPlayerZone ? PlayerActivityContext.CurrentZone : null;
        bool anySuccess = false;
        var messages = new List<string>();

        if(incident != null) {
            var activation = incident.Activate(player, region, zone, resolvedSourceId, sourceName, context);
            if(activation != null && !activation.blocked) {
                anySuccess = true;
                messages.Add($"{incident.DisplayName} activated.");
            } else if(activation != null && !string.IsNullOrWhiteSpace(activation.failureMessage)) {
                messages.Add($"{incident.DisplayName}: {activation.failureMessage}");
            }
        }

        if(board != null) {
            var roll = board.Roll(player, region, zone, resolvedSourceId, sourceName, context);
            if(roll != null && roll.activatedIncidents > 0 && !roll.blocked) {
                anySuccess = true;
                messages.Add($"{board.DisplayName}: {roll.activatedIncidents} incident(s) activated.");
            } else if(roll != null && !string.IsNullOrWhiteSpace(roll.failureMessage)) {
                messages.Add($"{board.DisplayName}: {roll.failureMessage}");
            }
        }

        message = string.Join(" ", messages.Where(entry => !string.IsNullOrWhiteSpace(entry)));
        return anySuccess;
    }

    string ResolveSourceId(TransitJourneyDefinition journey, string baseSourceId) {
        string prefix = !string.IsNullOrWhiteSpace(baseSourceId) ? baseSourceId : journey != null ? $"transit-journey:{journey.Id}" : "transit-journey";
        return string.IsNullOrWhiteSpace(sourceIdSuffix) ? prefix : $"{prefix}:{sourceIdSuffix}";
    }

    string ResolveStopForTrigger(TransitJourneyIncidentTrigger candidateTrigger, PlayerTransitJourneyState state, TransitJourneyLeg leg) {
        return candidateTrigger switch {
            TransitJourneyIncidentTrigger.LegDeparted => state != null ? state.originStopId : leg != null ? leg.OriginStopId : string.Empty,
            TransitJourneyIncidentTrigger.ContinuedFromStop => state != null ? state.currentStopId : string.Empty,
            TransitJourneyIncidentTrigger.StopReached => state != null ? state.currentStopId : leg != null ? leg.DestinationStopId : string.Empty,
            TransitJourneyIncidentTrigger.Disembarked => state != null ? state.currentStopId : string.Empty,
            TransitJourneyIncidentTrigger.JourneyCompleted => state != null ? state.currentStopId : string.Empty,
            _ => state != null ? state.currentStopId : leg != null ? leg.OriginStopId : string.Empty
        };
    }
}

[Serializable]
public class TransitJourneyIncidentHookRunResult {
    [Tooltip("Journey id that ran the hooks.")]
    public string journeyId;
    [Tooltip("Journey display name that ran the hooks.")]
    public string journeyName;
    [Tooltip("Trigger phase that ran.")]
    public TransitJourneyIncidentTrigger trigger;
    [Tooltip("Source id used for hook activations.")]
    public string sourceId;
    [Tooltip("Stop id context for this hook run.")]
    public string stopId;
    [Tooltip("Hook count that matched filters and was attempted.")]
    public int attemptedHooks;
    [Tooltip("Hook count that activated at least one incident.")]
    public int successfulHooks;
    [Tooltip("Hook count that did not activate an incident.")]
    public int blockedHooks;
    [Tooltip("Readable messages returned by hooks.")]
    public List<string> messages = new List<string>();
}
