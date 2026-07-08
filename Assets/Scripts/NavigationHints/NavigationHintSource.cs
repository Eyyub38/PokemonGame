using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigationHintSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id used by navigation hint history and consequence chains. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Navigation hints applied by this source.")]
    [SerializeField] List<NavigationHintDefinition> hints = new List<NavigationHintDefinition>();
    [Tooltip("Operation this source performs on its hints.")]
    [SerializeField] NavigationHintOperation operation = NavigationHintOperation.Activate;
    [Tooltip("Optional player override. Empty uses PlayerController.i or the first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Signals")]
    [Tooltip("If enabled, hints apply once during Start.")]
    [SerializeField] bool applyOnStart;
    [Tooltip("If enabled, hints apply whenever the component enables.")]
    [SerializeField] bool applyOnEnable;
    [Tooltip("If enabled, entering this trigger applies hints.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, interacting with this object applies hints.")]
    [SerializeField] bool applyOnInteract = true;
    [Tooltip("If enabled, repeated player triggers can apply this source more than once.")]
    [SerializeField] bool triggerRepeatedly;

    [Header("Runtime Target")]
    [Tooltip("Optional transform used as runtime target position when activating hints. Empty can use this transform.")]
    [SerializeField] Transform targetTransform = null;
    [Tooltip("Optional offset added to the runtime target position.")]
    [SerializeField] Vector3 targetPositionOffset;
    [Tooltip("If enabled and Target Transform is empty, this transform is used as the runtime target position.")]
    [SerializeField] bool useOwnTransformAsTarget = true;
    [Tooltip("If enabled, source position is passed to the hint even when the hint has a stored fallback position.")]
    [SerializeField] bool preferRuntimeTargetPosition = true;

    [Header("Consequences")]
    [Tooltip("Consequence chains applied after at least one navigation hint operation succeeds.")]
    [SerializeField] List<ConsequenceChainDefinition> successfulHintChains = new List<ConsequenceChainDefinition>();
    [Tooltip("Consequence chains applied when all navigation hint attempts are blocked or missing.")]
    [SerializeField] List<ConsequenceChainDefinition> blockedHintChains = new List<ConsequenceChainDefinition>();

    [Header("Debug")]
    [Tooltip("If enabled, hint attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<NavigationHintDefinition> Hints => hints;
    public NavigationHintOperation Operation => operation;
    public IReadOnlyList<ConsequenceChainDefinition> SuccessfulHintChains => successfulHintChains;
    public IReadOnlyList<ConsequenceChainDefinition> BlockedHintChains => blockedHintChains;
    public bool TriggerRepeatedly => triggerRepeatedly;

    void OnEnable() {
        if(applyOnEnable) {
            ApplyHints();
        }
    }

    void Start() {
        if(applyOnStart) {
            ApplyHints();
        }
    }

    [ContextMenu("Apply Navigation Hints")]
    public void ApplyHintsFromContextMenu() {
        ApplyHints();
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(applyOnPlayerTrigger) {
            ApplyHints(player);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(applyOnInteract) {
            var player = initiator != null ? initiator.GetComponent<PlayerController>() : ResolvePlayer();
            ApplyHints(player);
        }

        yield break;
    }

    public List<NavigationHintResult> ApplyHints() {
        return ApplyHints(ResolvePlayer());
    }

    public List<NavigationHintResult> ApplyHints(PlayerController player) {
        var results = new List<NavigationHintResult>();
        int successful = 0;
        int blocked = 0;
        Vector3? runtimeTargetPosition = ResolveRuntimeTargetPosition();

        foreach(var hint in hints) {
            if(hint == null) {
                blocked++;
                continue;
            }

            var result = ApplyHint(hint, player, runtimeTargetPosition);
            results.Add(result);
            if(result != null && !result.blocked) {
                successful++;
            } else {
                blocked++;
            }
        }

        if(successful > 0) {
            ApplyConsequenceChains(player, successfulHintChains, $"{operation.ToString().ToLowerInvariant()}-success");
        } else {
            ApplyConsequenceChains(player, blockedHintChains, $"{operation.ToString().ToLowerInvariant()}-blocked");
        }

        WriteAttemptLog(successful, blocked);
        return results;
    }

    NavigationHintResult ApplyHint(NavigationHintDefinition hint, PlayerController player, Vector3? runtimeTargetPosition) {
        switch(operation) {
            case NavigationHintOperation.Complete:
                return hint.Complete(player, SourceId, DisplayName, this);
            case NavigationHintOperation.Clear:
                return hint.Clear(player, SourceId, DisplayName, this);
            default:
                return hint.Activate(player, SourceId, DisplayName, this, preferRuntimeTargetPosition ? runtimeTargetPosition : null);
        }
    }

    Vector3? ResolveRuntimeTargetPosition() {
        var target = targetTransform != null ? targetTransform : useOwnTransformAsTarget ? transform : null;
        if(target == null) {
            return null;
        }

        return target.position + targetPositionOffset;
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
            GameDebugCategory.Navigation,
            $"{DisplayName} applied {successful} navigation hint operation(s), blocked/skipped {blocked}.",
            this,
            "NavigationHintSource");
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
