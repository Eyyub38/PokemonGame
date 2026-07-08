using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldDiscoverySource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id used by discovery history and consequence chains. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Discoveries applied by this source.")]
    [SerializeField] List<WorldDiscoveryDefinition> discoveries = new List<WorldDiscoveryDefinition>();
    [Tooltip("Optional player override. Empty uses PlayerController.i or the first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Signals")]
    [Tooltip("If enabled, discoveries apply once during Start.")]
    [SerializeField] bool applyOnStart;
    [Tooltip("If enabled, discoveries apply whenever the component enables.")]
    [SerializeField] bool applyOnEnable;
    [Tooltip("If enabled, entering this trigger applies discoveries.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, interacting with this object applies discoveries.")]
    [SerializeField] bool applyOnInteract = true;
    [Tooltip("If enabled, repeated player triggers can apply this source more than once.")]
    [SerializeField] bool triggerRepeatedly = false;

    [Header("Consequences")]
    [Tooltip("Consequence chains applied after at least one discovery succeeds.")]
    [SerializeField] List<ConsequenceChainDefinition> successfulDiscoveryChains = new List<ConsequenceChainDefinition>();
    [Tooltip("Consequence chains applied when all discovery attempts are blocked or missing.")]
    [SerializeField] List<ConsequenceChainDefinition> blockedDiscoveryChains = new List<ConsequenceChainDefinition>();

    [Header("Debug")]
    [Tooltip("If enabled, discovery attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<WorldDiscoveryDefinition> Discoveries => discoveries;
    public IReadOnlyList<ConsequenceChainDefinition> SuccessfulDiscoveryChains => successfulDiscoveryChains;
    public IReadOnlyList<ConsequenceChainDefinition> BlockedDiscoveryChains => blockedDiscoveryChains;
    public bool TriggerRepeatedly => triggerRepeatedly;

    void OnEnable() {
        if(applyOnEnable) {
            ApplyDiscoveries();
        }
    }

    void Start() {
        if(applyOnStart) {
            ApplyDiscoveries();
        }
    }

    [ContextMenu("Apply Discoveries")]
    public void ApplyDiscoveriesFromContextMenu() {
        ApplyDiscoveries();
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(applyOnPlayerTrigger) {
            ApplyDiscoveries(player);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(applyOnInteract) {
            var player = initiator != null ? initiator.GetComponent<PlayerController>() : ResolvePlayer();
            ApplyDiscoveries(player);
        }

        yield break;
    }

    public List<WorldDiscoveryApplyResult> ApplyDiscoveries() {
        return ApplyDiscoveries(ResolvePlayer());
    }

    public List<WorldDiscoveryApplyResult> ApplyDiscoveries(PlayerController player) {
        var results = new List<WorldDiscoveryApplyResult>();
        int successful = 0;
        int blocked = 0;

        foreach(var discovery in discoveries) {
            if(discovery == null) {
                blocked++;
                continue;
            }

            var result = discovery.Apply(player, SourceId, DisplayName, this);
            results.Add(result);
            if(result != null && !result.blocked) {
                successful++;
            } else {
                blocked++;
            }
        }

        if(successful > 0) {
            ApplyConsequenceChains(player, successfulDiscoveryChains, "discovered");
        } else {
            ApplyConsequenceChains(player, blockedDiscoveryChains, "blocked");
        }

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
            GameDebugCategory.WorldDiscovery,
            $"{DisplayName} applied {successful} discovery record(s), blocked/skipped {blocked}.",
            this,
            "WorldDiscoverySource");
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
