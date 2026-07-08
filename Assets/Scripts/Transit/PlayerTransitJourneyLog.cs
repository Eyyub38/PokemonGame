using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TransitJourneyPhase {
    None,
    InTransit,
    AtStop,
    Completed,
    Cancelled
}

public class PlayerTransitJourneyLog : MonoBehaviour, ISavable {
    [Tooltip("Currently active vehicle journey, if any.")]
    [SerializeField] PlayerTransitJourneyState activeJourney;
    [Tooltip("Completed, cancelled or disembarked journey records.")]
    [SerializeField] List<PlayerTransitJourneyHistoryRecord> journeyHistory = new List<PlayerTransitJourneyHistoryRecord>();
    [Tooltip("Small activity log for things done while inside the vehicle, such as sleep, research or NPC conversations.")]
    [SerializeField] List<PlayerTransitOnboardActivityRecord> onboardActivityHistory = new List<PlayerTransitOnboardActivityRecord>();

    public PlayerTransitJourneyState ActiveJourney => activeJourney;
    public IReadOnlyList<PlayerTransitJourneyHistoryRecord> JourneyHistory => journeyHistory;
    public IReadOnlyList<PlayerTransitOnboardActivityRecord> OnboardActivityHistory => onboardActivityHistory;
    public bool HasActiveJourney => activeJourney != null && activeJourney.IsActive;
    public event Action OnTransitJourneyChanged;

    public bool TryStartJourney(PlayerController player, TransitJourneyDefinition journey, string originStopId, string sourceId, out string failureMessage) {
        if(HasActiveJourney) {
            failureMessage = "A transit journey is already active.";
            return false;
        }

        if(journey == null) {
            failureMessage = "No transit journey was selected.";
            return false;
        }

        var transitLog = GetTransitLog(player, createIfMissing: true);
        if(!journey.CanStart(player, transitLog, originStopId, out failureMessage)) {
            journey.PublishBlocked(player, sourceId, originStopId, failureMessage);
            return false;
        }

        var firstLeg = journey.GetLeg(0);
        if(firstLeg == null || firstLeg.Route == null) {
            failureMessage = $"{journey.DisplayName} has no first route leg.";
            journey.PublishBlocked(player, sourceId, originStopId, failureMessage);
            return false;
        }

        if(!firstLeg.Route.TryPayCosts(player, out failureMessage)) {
            journey.PublishBlocked(player, sourceId, originStopId, failureMessage);
            return false;
        }

        activeJourney = PlayerTransitJourneyState.Create(journey, firstLeg, originStopId, sourceId, GetCurrentDay(), GetCurrentAbsoluteHour());
        firstLeg.Route.PublishDeparted(player, activeJourney.currentStopId);
        journey.PublishStarted(player, sourceId, activeJourney.currentStopId);
        journey.TriggerIncidentHooks(TransitJourneyIncidentTrigger.JourneyStarted, player, activeJourney, firstLeg, sourceId, this);
        journey.TriggerIncidentHooks(TransitJourneyIncidentTrigger.LegDeparted, player, activeJourney, firstLeg, sourceId, this);
        OnTransitJourneyChanged?.Invoke();
        return true;
    }

    public bool TryAdvanceTime(PlayerController player, int hours, out string failureMessage) {
        if(!HasActiveJourney) {
            failureMessage = "No active transit journey.";
            return false;
        }

        int remaining = Mathf.Max(0, hours);
        if(remaining == 0) {
            failureMessage = null;
            return true;
        }

        while(remaining > 0 && HasActiveJourney) {
            if(activeJourney.phase == TransitJourneyPhase.InTransit) {
                int spend = Mathf.Min(remaining, Mathf.Max(0, activeJourney.remainingTravelHours));
                activeJourney.remainingTravelHours -= spend;
                activeJourney.totalHoursSpent += spend;
                remaining -= spend;

                if(activeJourney.remainingTravelHours <= 0) {
                    ArriveCurrentLeg(player);
                }
                continue;
            }

            if(activeJourney.phase == TransitJourneyPhase.AtStop) {
                int spend = Mathf.Min(remaining, Mathf.Max(0, activeJourney.remainingDwellHours));
                activeJourney.remainingDwellHours -= spend;
                activeJourney.totalHoursSpent += spend;
                remaining -= spend;

                if(activeJourney.remainingDwellHours <= 0 && activeJourney.autoContinueAfterDwell && activeJourney.canContinue) {
                    if(!TryContinueJourney(player, out failureMessage)) {
                        return false;
                    }
                    continue;
                }

                if(spend == 0) {
                    break;
                }
            }
        }

        activeJourney.lastUpdatedAbsoluteHour = GetCurrentAbsoluteHour();
        OnTransitJourneyChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public bool TryContinueJourney(PlayerController player, out string failureMessage) {
        if(!HasActiveJourney) {
            failureMessage = "No active transit journey.";
            return false;
        }

        if(activeJourney.phase != TransitJourneyPhase.AtStop) {
            failureMessage = "The vehicle has not reached a stop yet.";
            return false;
        }

        var journey = ResolveJourney(activeJourney.journeyId);
        if(journey == null) {
            failureMessage = "Active transit journey definition could not be resolved.";
            return false;
        }

        var nextLeg = journey.GetLeg(activeJourney.currentLegIndex + 1);
        if(nextLeg == null) {
            CompleteJourney(player, journey, disembarked: false);
            failureMessage = null;
            return true;
        }

        if(journey.PayEachLeg && nextLeg.Route != null && !nextLeg.Route.TryPayCosts(player, out failureMessage)) {
            journey.PublishBlocked(player, activeJourney.sourceId, activeJourney.currentStopId, failureMessage);
            return false;
        }

        journey.TriggerIncidentHooks(TransitJourneyIncidentTrigger.ContinuedFromStop, player, activeJourney, nextLeg, activeJourney.sourceId, this);
        activeJourney.currentLegIndex++;
        activeJourney.phase = TransitJourneyPhase.InTransit;
        activeJourney.canDisembark = false;
        activeJourney.canContinue = false;
        activeJourney.originStopId = string.IsNullOrWhiteSpace(nextLeg.OriginStopId) ? activeJourney.currentStopId : nextLeg.OriginStopId;
        activeJourney.destinationStopId = nextLeg.DestinationStopId;
        activeJourney.destinationDisplayName = nextLeg.DestinationDisplayName;
        activeJourney.routeId = nextLeg.Route != null ? nextLeg.Route.Id : string.Empty;
        activeJourney.routeName = nextLeg.Route != null ? nextLeg.Route.DisplayName : string.Empty;
        activeJourney.remainingTravelHours = Mathf.Max(0, nextLeg.TravelHours);
        activeJourney.remainingDwellHours = 0;
        activeJourney.lastUpdatedAbsoluteHour = GetCurrentAbsoluteHour();
        nextLeg.Route?.PublishDeparted(player, activeJourney.originStopId);
        journey.TriggerIncidentHooks(TransitJourneyIncidentTrigger.LegDeparted, player, activeJourney, nextLeg, activeJourney.sourceId, this);
        OnTransitJourneyChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public bool TryDisembark(PlayerController player, out string failureMessage) {
        if(!HasActiveJourney) {
            failureMessage = "No active transit journey.";
            return false;
        }

        if(activeJourney.phase != TransitJourneyPhase.AtStop || !activeJourney.canDisembark) {
            failureMessage = "The player cannot disembark at the current journey state.";
            return false;
        }

        var journey = ResolveJourney(activeJourney.journeyId);
        if(journey == null) {
            failureMessage = "Active transit journey definition could not be resolved.";
            return false;
        }

        CompleteJourney(player, journey, disembarked: true);
        failureMessage = null;
        return true;
    }

    public bool TryCancelJourney(PlayerController player, string reason, out string failureMessage) {
        if(!HasActiveJourney) {
            failureMessage = "No active transit journey.";
            return false;
        }

        var journey = ResolveJourney(activeJourney.journeyId);
        journey?.TriggerIncidentHooks(TransitJourneyIncidentTrigger.JourneyCancelled, player, activeJourney, journey.GetLeg(activeJourney.currentLegIndex), activeJourney.sourceId, this);
        journeyHistory.Add(PlayerTransitJourneyHistoryRecord.FromState(activeJourney, TransitJourneyPhase.Cancelled, false, true, reason, GetCurrentDay(), GetCurrentAbsoluteHour()));
        activeJourney = null;
        OnTransitJourneyChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public void RecordOnboardActivity(string activityId, string displayName, int hoursSpent, string sourceId = null) {
        if(!HasActiveJourney) {
            return;
        }

        onboardActivityHistory.Add(new PlayerTransitOnboardActivityRecord {
            journeyId = activeJourney.journeyId,
            journeyName = activeJourney.journeyName,
            routeId = activeJourney.routeId,
            routeName = activeJourney.routeName,
            activityId = activityId,
            displayName = displayName,
            hoursSpent = Mathf.Max(0, hoursSpent),
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            sourceId = sourceId
        });
        var journey = ResolveJourney(activeJourney.journeyId);
        journey?.TriggerIncidentHooks(TransitJourneyIncidentTrigger.OnboardActivityRecorded, GetComponent<PlayerController>(), activeJourney, journey.GetLeg(activeJourney.currentLegIndex), activeJourney.sourceId, this);
        OnTransitJourneyChanged?.Invoke();
    }

    void ArriveCurrentLeg(PlayerController player) {
        var journey = ResolveJourney(activeJourney.journeyId);
        var leg = journey != null ? journey.GetLeg(activeJourney.currentLegIndex) : null;
        var route = leg?.Route;
        var transitLog = GetTransitLog(player, createIfMissing: true);

        activeJourney.phase = TransitJourneyPhase.AtStop;
        activeJourney.currentStopId = string.IsNullOrWhiteSpace(leg?.DestinationStopId) ? activeJourney.destinationStopId : leg.DestinationStopId;
        activeJourney.destinationStopId = activeJourney.currentStopId;
        activeJourney.destinationDisplayName = leg?.DestinationDisplayName ?? activeJourney.currentStopId;
        activeJourney.remainingDwellHours = leg != null ? leg.DwellHours : 0;
        activeJourney.canDisembark = leg == null || leg.CanDisembark;
        activeJourney.canContinue = journey != null && journey.HasNextLeg(activeJourney.currentLegIndex) && (leg == null || leg.StopRule != TransitJourneyStopRule.RequiredDisembark);

        if(route != null) {
            if(journey == null || journey.RecordEachLegTravel) {
                transitLog?.RecordTravel(route, activeJourney.originStopId, activeJourney.currentStopId, route.EstimatedTravelHours);
            }

            if(journey == null || journey.ApplyRouteArrivalEffects) {
                route.ApplyArrivalEffects(player);
            }

            route.PublishArrived(player, activeJourney.originStopId);
        }

        journey?.PublishStopReached(player, activeJourney.sourceId, activeJourney.currentStopId, leg);
        journey?.TriggerIncidentHooks(TransitJourneyIncidentTrigger.StopReached, player, activeJourney, leg, activeJourney.sourceId, this);
        if(activeJourney.canDisembark && !activeJourney.canContinue && activeJourney.remainingDwellHours <= 0) {
            CompleteJourney(player, journey, disembarked: false);
        }
    }

    void CompleteJourney(PlayerController player, TransitJourneyDefinition journey, bool disembarked) {
        if(activeJourney == null) {
            return;
        }

        var leg = journey != null ? journey.GetLeg(activeJourney.currentLegIndex) : null;
        if(disembarked) {
            journey?.PublishDisembarked(player, activeJourney.sourceId, activeJourney.currentStopId, leg);
            journey?.TriggerIncidentHooks(TransitJourneyIncidentTrigger.Disembarked, player, activeJourney, leg, activeJourney.sourceId, this);
        } else {
            journey?.PublishCompleted(player, activeJourney.sourceId, activeJourney.currentStopId);
            journey?.TriggerIncidentHooks(TransitJourneyIncidentTrigger.JourneyCompleted, player, activeJourney, leg, activeJourney.sourceId, this);
        }

        journeyHistory.Add(PlayerTransitJourneyHistoryRecord.FromState(activeJourney, TransitJourneyPhase.Completed, disembarked, false, null, GetCurrentDay(), GetCurrentAbsoluteHour()));
        activeJourney = null;
        OnTransitJourneyChanged?.Invoke();
    }

    PlayerTransitLog GetTransitLog(PlayerController player, bool createIfMissing) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerTransitLog>();
        return log != null || !createIfMissing ? log : player.gameObject.AddComponent<PlayerTransitLog>();
    }

    TransitJourneyDefinition ResolveJourney(string journeyId) {
        if(string.IsNullOrWhiteSpace(journeyId)) {
            return null;
        }

        return Resources.LoadAll<TransitJourneyDefinition>("")
            .FirstOrDefault(journey => journey != null && string.Equals(journey.Id, journeyId, StringComparison.OrdinalIgnoreCase));
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerTransitJourneyLogSaveData {
            activeJourney = activeJourney != null ? activeJourney.Clone() : null,
            journeyHistory = journeyHistory.Where(record => record != null).Select(record => record.Clone()).ToList(),
            onboardActivityHistory = onboardActivityHistory.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerTransitJourneyLogSaveData;
        activeJourney = saveData?.activeJourney?.Clone();
        journeyHistory = saveData?.journeyHistory?.Where(record => record != null).Select(record => record.Clone()).ToList() ?? new List<PlayerTransitJourneyHistoryRecord>();
        onboardActivityHistory = saveData?.onboardActivityHistory?.Where(record => record != null).Select(record => record.Clone()).ToList() ?? new List<PlayerTransitOnboardActivityRecord>();
        OnTransitJourneyChanged?.Invoke();
    }
}

[Serializable]
public class PlayerTransitJourneyState {
    [Tooltip("Active journey id.")]
    public string journeyId;
    [Tooltip("Active journey display name.")]
    public string journeyName;
    [Tooltip("Current route id.")]
    public string routeId;
    [Tooltip("Current route display name.")]
    public string routeName;
    [Tooltip("Source id that started the journey.")]
    public string sourceId;
    [Tooltip("Current journey phase.")]
    public TransitJourneyPhase phase;
    [Tooltip("Current leg index inside the journey definition.")]
    public int currentLegIndex;
    [Tooltip("Current stop id or last reached stop id.")]
    public string currentStopId;
    [Tooltip("Origin stop id for the current leg.")]
    public string originStopId;
    [Tooltip("Destination stop id for the current leg.")]
    public string destinationStopId;
    [Tooltip("Destination stop display name for the current leg.")]
    public string destinationDisplayName;
    [Tooltip("Remaining in-game hours before the current leg arrives.")]
    public int remainingTravelHours;
    [Tooltip("Remaining in-game hours before the current stop dwell window closes.")]
    public int remainingDwellHours;
    [Tooltip("Total in-game hours spent in this journey.")]
    public int totalHoursSpent;
    [Tooltip("If enabled, the player can leave the vehicle at the current stop.")]
    public bool canDisembark;
    [Tooltip("If enabled, the journey can continue to another leg.")]
    public bool canContinue;
    [Tooltip("If enabled, the journey will continue automatically after dwell time expires.")]
    public bool autoContinueAfterDwell;
    [Tooltip("In-game day when the journey started.")]
    public int startedDay;
    [Tooltip("Absolute in-game hour when the journey started.")]
    public int startedAbsoluteHour;
    [Tooltip("Absolute in-game hour when this state was last updated.")]
    public int lastUpdatedAbsoluteHour;

    public bool IsActive => phase == TransitJourneyPhase.InTransit || phase == TransitJourneyPhase.AtStop;

    public static PlayerTransitJourneyState Create(TransitJourneyDefinition journey, TransitJourneyLeg leg, string originStopId, string sourceId, int day, int absoluteHour) {
        return new PlayerTransitJourneyState {
            journeyId = journey != null ? journey.Id : string.Empty,
            journeyName = journey != null ? journey.DisplayName : string.Empty,
            routeId = leg?.Route != null ? leg.Route.Id : string.Empty,
            routeName = leg?.Route != null ? leg.Route.DisplayName : string.Empty,
            sourceId = sourceId,
            phase = TransitJourneyPhase.InTransit,
            currentLegIndex = 0,
            currentStopId = string.IsNullOrWhiteSpace(originStopId) ? leg?.OriginStopId ?? string.Empty : originStopId,
            originStopId = string.IsNullOrWhiteSpace(originStopId) ? leg?.OriginStopId ?? string.Empty : originStopId,
            destinationStopId = leg?.DestinationStopId ?? string.Empty,
            destinationDisplayName = leg?.DestinationDisplayName ?? string.Empty,
            remainingTravelHours = Mathf.Max(0, leg?.TravelHours ?? 0),
            remainingDwellHours = 0,
            canDisembark = false,
            canContinue = false,
            autoContinueAfterDwell = journey != null && journey.AutoContinueAfterDwell,
            startedDay = day,
            startedAbsoluteHour = absoluteHour,
            lastUpdatedAbsoluteHour = absoluteHour
        };
    }

    public PlayerTransitJourneyState Clone() {
        return (PlayerTransitJourneyState)MemberwiseClone();
    }
}

[Serializable]
public class PlayerTransitJourneyHistoryRecord {
    [Tooltip("Journey id.")]
    public string journeyId;
    [Tooltip("Journey display name.")]
    public string journeyName;
    [Tooltip("Final stop id.")]
    public string finalStopId;
    [Tooltip("Final stop display name.")]
    public string finalStopName;
    [Tooltip("Final journey phase.")]
    public TransitJourneyPhase finalPhase;
    [Tooltip("If enabled, the player manually disembarked.")]
    public bool disembarked;
    [Tooltip("If enabled, the journey was cancelled before normal completion.")]
    public bool cancelled;
    [Tooltip("Reason recorded when cancelled or blocked.")]
    public string reason;
    [Tooltip("Total in-game hours spent in the journey.")]
    public int totalHoursSpent;
    [Tooltip("In-game day when the journey started.")]
    public int startedDay;
    [Tooltip("Absolute in-game hour when the journey started.")]
    public int startedAbsoluteHour;
    [Tooltip("In-game day when the journey ended.")]
    public int endedDay;
    [Tooltip("Absolute in-game hour when the journey ended.")]
    public int endedAbsoluteHour;

    public static PlayerTransitJourneyHistoryRecord FromState(PlayerTransitJourneyState state, TransitJourneyPhase phase, bool disembarked, bool cancelled, string reason, int endedDay, int endedAbsoluteHour) {
        return new PlayerTransitJourneyHistoryRecord {
            journeyId = state != null ? state.journeyId : string.Empty,
            journeyName = state != null ? state.journeyName : string.Empty,
            finalStopId = state != null ? state.currentStopId : string.Empty,
            finalStopName = state != null ? state.destinationDisplayName : string.Empty,
            finalPhase = phase,
            disembarked = disembarked,
            cancelled = cancelled,
            reason = reason,
            totalHoursSpent = state != null ? state.totalHoursSpent : 0,
            startedDay = state != null ? state.startedDay : -1,
            startedAbsoluteHour = state != null ? state.startedAbsoluteHour : -1,
            endedDay = endedDay,
            endedAbsoluteHour = endedAbsoluteHour
        };
    }

    public PlayerTransitJourneyHistoryRecord Clone() {
        return (PlayerTransitJourneyHistoryRecord)MemberwiseClone();
    }
}

[Serializable]
public class PlayerTransitOnboardActivityRecord {
    [Tooltip("Journey id active when this onboard activity was recorded.")]
    public string journeyId;
    [Tooltip("Journey display name active when this onboard activity was recorded.")]
    public string journeyName;
    [Tooltip("Route id active when this onboard activity was recorded.")]
    public string routeId;
    [Tooltip("Route display name active when this onboard activity was recorded.")]
    public string routeName;
    [Tooltip("Activity id such as sleep, research, talk or wait.")]
    public string activityId;
    [Tooltip("Display name shown by future journey UI.")]
    public string displayName;
    [Tooltip("In-game hours spent on this onboard activity.")]
    public int hoursSpent;
    [Tooltip("In-game day when recorded.")]
    public int day;
    [Tooltip("Absolute in-game hour when recorded.")]
    public int absoluteHour;
    [Tooltip("Source id that recorded this activity.")]
    public string sourceId;

    public PlayerTransitOnboardActivityRecord Clone() {
        return (PlayerTransitOnboardActivityRecord)MemberwiseClone();
    }
}

[Serializable]
public class PlayerTransitJourneyLogSaveData {
    public PlayerTransitJourneyState activeJourney;
    public List<PlayerTransitJourneyHistoryRecord> journeyHistory = new List<PlayerTransitJourneyHistoryRecord>();
    public List<PlayerTransitOnboardActivityRecord> onboardActivityHistory = new List<PlayerTransitOnboardActivityRecord>();
}
