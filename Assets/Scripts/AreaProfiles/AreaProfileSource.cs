using System.Collections;
using UnityEngine;

public class AreaProfileSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id used by area profile history and consequence chains. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Area profile applied by this source.")]
    [SerializeField] AreaProfileDefinition profile = null;
    [Tooltip("Optional player override. Empty uses PlayerController.i or the first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Signals")]
    [Tooltip("If enabled, the area profile enters once during Start.")]
    [SerializeField] bool enterOnStart;
    [Tooltip("If enabled, the area profile enters whenever the component enables.")]
    [SerializeField] bool enterOnEnable;
    [Tooltip("If enabled, entering this trigger through PlayerController applies area enter.")]
    [SerializeField] bool enterOnPlayerTrigger = true;
    [Tooltip("If enabled, Collider2D OnTriggerExit2D applies area exit.")]
    [SerializeField] bool exitOnTriggerExit = true;
    [Tooltip("If enabled, interacting with this object applies the selected interact operation.")]
    [SerializeField] bool applyOnInteract = true;
    [Tooltip("Operation applied when Interact is called.")]
    [SerializeField] AreaProfileOperation interactOperation = AreaProfileOperation.Enter;
    [Tooltip("If enabled, repeated player triggers can call OnPlayerTriggered more than once.")]
    [SerializeField] bool triggerRepeatedly;

    [Header("Runtime Position")]
    [Tooltip("Optional transform used as area world position. Empty can use this transform.")]
    [SerializeField] Transform positionOverride = null;
    [Tooltip("Optional offset added to the runtime area position.")]
    [SerializeField] Vector3 positionOffset;
    [Tooltip("If enabled and Position Override is empty, this transform is used as the runtime area position.")]
    [SerializeField] bool useOwnTransformAsPosition = true;

    [Header("Activity Context")]
    [Tooltip("If enabled, entering this profile also sets PlayerActivityContext to the profile activity zone.")]
    [SerializeField] bool setActivityContextOnEnter = true;
    [Tooltip("If enabled, exiting this profile clears PlayerActivityContext for the profile activity zone.")]
    [SerializeField] bool clearActivityContextOnExit = true;

    [Header("Debug")]
    [Tooltip("If enabled, area attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public AreaProfileDefinition Profile => profile;
    public bool TriggerRepeatedly => triggerRepeatedly;

    void OnEnable() {
        if(enterOnEnable) {
            EnterArea();
        }
    }

    void Start() {
        if(enterOnStart) {
            EnterArea();
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        if(!exitOnTriggerExit) {
            return;
        }

        var player = other.GetComponentInParent<PlayerController>();
        if(player != null) {
            ExitArea(player);
        }
    }

    [ContextMenu("Enter Area")]
    public void EnterAreaFromContextMenu() {
        EnterArea();
    }

    [ContextMenu("Exit Area")]
    public void ExitAreaFromContextMenu() {
        ExitArea();
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(enterOnPlayerTrigger) {
            EnterArea(player);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(applyOnInteract) {
            var player = initiator != null ? initiator.GetComponent<PlayerController>() : ResolvePlayer();
            if(interactOperation == AreaProfileOperation.Exit) {
                ExitArea(player);
            } else {
                EnterArea(player);
            }
        }

        yield break;
    }

    public AreaProfileResult EnterArea() {
        return EnterArea(ResolvePlayer());
    }

    public AreaProfileResult EnterArea(PlayerController player) {
        var result = profile != null
            ? profile.Enter(player, SourceId, DisplayName, this, ResolveRuntimePosition())
            : CreateMissingProfileResult(AreaProfileOperation.Enter);

        if(profile != null && setActivityContextOnEnter && profile.ActivityZone != null && result != null && !result.blocked) {
            PlayerActivityContext.SetCurrentZone(profile.ActivityZone);
        }

        WriteAttemptLog(result);
        return result;
    }

    public AreaProfileResult ExitArea() {
        return ExitArea(ResolvePlayer());
    }

    public AreaProfileResult ExitArea(PlayerController player) {
        var result = profile != null
            ? profile.Exit(player, SourceId, DisplayName, this, ResolveRuntimePosition())
            : CreateMissingProfileResult(AreaProfileOperation.Exit);

        if(profile != null && clearActivityContextOnExit && profile.ActivityZone != null && result != null && !result.blocked) {
            PlayerActivityContext.ClearCurrentZone(profile.ActivityZone);
        }

        WriteAttemptLog(result);
        return result;
    }

    Vector3? ResolveRuntimePosition() {
        var target = positionOverride != null ? positionOverride : useOwnTransformAsPosition ? transform : null;
        if(target == null) {
            return null;
        }

        return target.position + positionOffset;
    }

    AreaProfileResult CreateMissingProfileResult(AreaProfileOperation operation) {
        return new AreaProfileResult(string.Empty, string.Empty, AreaProfileCategory.General, operation, SourceId, DisplayName, gameObject.scene.name, SourceId, ResolveRuntimePosition()) {
            blocked = true,
            failureMessage = "Area profile is missing."
        };
    }

    void WriteAttemptLog(AreaProfileResult result) {
        if(!logAttempts || result == null) {
            return;
        }

        GameDebugLogger.Ensure().Record(
            result.blocked ? GameDebugSeverity.Warning : GameDebugSeverity.Info,
            GameDebugCategory.AreaProfile,
            result.blocked ? $"{DisplayName} area {result.operation} blocked: {result.failureMessage}" : $"{DisplayName} area {result.operation} applied.",
            this,
            "AreaProfileSource");
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
