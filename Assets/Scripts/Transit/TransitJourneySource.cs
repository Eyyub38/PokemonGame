using UnityEngine;

public class TransitJourneySource : MonoBehaviour, IPlayerTriggerable {
    [Header("Journey")]
    [Tooltip("Vehicle journey started by this source.")]
    [SerializeField] TransitJourneyDefinition journey;
    [Tooltip("Optional origin stop id override. Empty uses station, first leg or GameObject name fallback.")]
    [SerializeField] string originStopId = string.Empty;
    [Tooltip("Optional source id written into journey logs/events. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Optional station used to discover a stop before starting this journey.")]
    [SerializeField] TransitStation station;

    [Header("Trigger")]
    [Tooltip("If enabled, player trigger starts the journey immediately. Disable for UI-driven sources.")]
    [SerializeField] bool startOnTrigger;
    [Tooltip("If enabled, the player gains/discovers the station stop before journey validation runs.")]
    [SerializeField] bool discoverStationOnTrigger = true;
    [Tooltip("If enabled, a missing PlayerTransitJourneyLog is created on the player when starting.")]
    [SerializeField] bool createMissingJourneyLog = true;
    [Tooltip("If enabled, a blocked start attempt is written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;

    public TransitJourneyDefinition Journey => journey;
    public string OriginStopId => ResolveOriginStopId();
    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public TransitStation Station => station;
    public bool StartOnTrigger => startOnTrigger;
    public bool DiscoverStationOnTrigger => discoverStationOnTrigger;
    public bool CreateMissingJourneyLog => createMissingJourneyLog;
    public bool TriggerRepeatedly => true;

    public void OnPlayerTriggered(PlayerController player) {
        if(discoverStationOnTrigger) {
            station?.Discover(player, SourceId);
        }

        if(startOnTrigger) {
            TryStartJourney(player, out _);
        }
    }

    public bool CanStartJourney(PlayerController player, out string failureMessage) {
        if(journey == null) {
            failureMessage = "No transit journey is assigned.";
            return false;
        }

        if(player == null) {
            failureMessage = "A player is required to start transit journeys.";
            return false;
        }

        var transitLog = player.GetComponent<PlayerTransitLog>();
        return journey.CanStart(player, transitLog, ResolveOriginStopId(), out failureMessage);
    }

    public bool TryStartJourney(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to start transit journeys.";
            return false;
        }

        var log = player.GetComponent<PlayerTransitJourneyLog>();
        if(log == null && createMissingJourneyLog) {
            log = player.gameObject.AddComponent<PlayerTransitJourneyLog>();
        }

        if(log == null) {
            failureMessage = "PlayerTransitJourneyLog is missing.";
            LogBlocked(failureMessage);
            return false;
        }

        if(log.TryStartJourney(player, journey, ResolveOriginStopId(), SourceId, out failureMessage)) {
            return true;
        }

        LogBlocked(failureMessage);
        return false;
    }

    string ResolveOriginStopId() {
        if(!string.IsNullOrWhiteSpace(originStopId)) {
            return originStopId;
        }

        if(station != null) {
            return station.StationId;
        }

        var firstLeg = journey != null ? journey.GetLeg(0) : null;
        if(firstLeg != null && !string.IsNullOrWhiteSpace(firstLeg.OriginStopId)) {
            return firstLeg.OriginStopId;
        }

        return name;
    }

    void LogBlocked(string message) {
        if(logBlockedAttempts) {
            GameDebug.Warning(string.IsNullOrWhiteSpace(message) ? "Transit journey start blocked." : message, GameDebugCategory.Transit, this, "TransitJourneySource");
        }
    }
}
