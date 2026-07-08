using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationVisitSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id used by visit history and consequence chains. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Location visits applied by this source.")]
    [SerializeField] List<LocationVisitDefinition> visits = new List<LocationVisitDefinition>();
    [Tooltip("Optional player override. Empty uses PlayerController.i or the first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Signals")]
    [Tooltip("If enabled, visits apply once during Start.")]
    [SerializeField] bool applyOnStart;
    [Tooltip("If enabled, visits apply whenever the component enables.")]
    [SerializeField] bool applyOnEnable;
    [Tooltip("If enabled, entering this trigger applies visits.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, interacting with this object applies visits.")]
    [SerializeField] bool applyOnInteract;
    [Tooltip("If enabled, repeated player triggers can apply this source more than once.")]
    [SerializeField] bool triggerRepeatedly;

    [Header("Consequences")]
    [Tooltip("Consequence chains applied after at least one visit succeeds.")]
    [SerializeField] List<ConsequenceChainDefinition> successfulVisitChains = new List<ConsequenceChainDefinition>();
    [Tooltip("Consequence chains applied when all visit attempts are blocked or missing.")]
    [SerializeField] List<ConsequenceChainDefinition> blockedVisitChains = new List<ConsequenceChainDefinition>();

    [Header("Debug")]
    [Tooltip("If enabled, visit attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<LocationVisitDefinition> Visits => visits;
    public IReadOnlyList<ConsequenceChainDefinition> SuccessfulVisitChains => successfulVisitChains;
    public IReadOnlyList<ConsequenceChainDefinition> BlockedVisitChains => blockedVisitChains;
    public bool TriggerRepeatedly => triggerRepeatedly;

    void OnEnable() {
        if(applyOnEnable) {
            ApplyVisits();
        }
    }

    void Start() {
        if(applyOnStart) {
            ApplyVisits();
        }
    }

    [ContextMenu("Apply Location Visits")]
    public void ApplyVisitsFromContextMenu() {
        ApplyVisits();
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(applyOnPlayerTrigger) {
            ApplyVisits(player);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(applyOnInteract) {
            var player = initiator != null ? initiator.GetComponent<PlayerController>() : ResolvePlayer();
            ApplyVisits(player);
        }

        yield break;
    }

    public List<LocationVisitResult> ApplyVisits() {
        return ApplyVisits(ResolvePlayer());
    }

    public List<LocationVisitResult> ApplyVisits(PlayerController player) {
        var results = new List<LocationVisitResult>();
        int successful = 0;
        int blocked = 0;

        foreach(var visit in visits) {
            if(visit == null) {
                blocked++;
                continue;
            }

            var result = visit.Apply(player, SourceId, DisplayName, this);
            results.Add(result);
            if(result != null && !result.blocked) {
                successful++;
            } else {
                blocked++;
            }
        }

        ApplyConsequenceChains(player, successful > 0 ? successfulVisitChains : blockedVisitChains, successful > 0 ? "visited" : "blocked");
        WriteAttemptLog(successful, blocked);
        return results;
    }

    void ApplyConsequenceChains(PlayerController player, IEnumerable<ConsequenceChainDefinition> chains, string phase) {
        if(player == null || chains == null) {
            return;
        }

        var context = new ConsequenceChainContext {
            SourceId = $"{SourceId}:{phase}",
            SourceName = DisplayName,
            ContextObject = this
        };

        foreach(var chain in chains) {
            chain?.Apply(player, context, this);
        }
    }

    void WriteAttemptLog(int successful, int blocked) {
        if(!logAttempts) {
            return;
        }

        GameDebugLogger.Ensure().Record(
            successful > 0 ? GameDebugSeverity.Info : GameDebugSeverity.Warning,
            GameDebugCategory.LocationVisit,
            $"{DisplayName} applied {successful} location visit(s), blocked/skipped {blocked}.",
            this,
            "LocationVisitSource");
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
}
