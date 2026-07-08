using System.Collections;
using UnityEngine;

public class PlayerOriginSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id used by origin history. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Origin applied by this source.")]
    [SerializeField] PlayerOriginDefinition origin = null;
    [Tooltip("Optional player override. Empty uses PlayerController.i or the first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Signals")]
    [Tooltip("If enabled, this origin applies once during Start.")]
    [SerializeField] bool applyOnStart = false;
    [Tooltip("If enabled, this origin applies whenever the component enables.")]
    [SerializeField] bool applyOnEnable = false;
    [Tooltip("If enabled, entering this trigger applies the origin.")]
    [SerializeField] bool applyOnPlayerTrigger = false;
    [Tooltip("If enabled, interacting with this object applies the origin.")]
    [SerializeField] bool applyOnInteract = true;
    [Tooltip("If enabled, repeated player triggers can apply this source more than once.")]
    [SerializeField] bool triggerRepeatedly = false;
    [Tooltip("If enabled, this source may replace an already selected origin when the origin definition allows replacement.")]
    [SerializeField] bool forceReplace = false;

    [Header("Debug")]
    [Tooltip("If enabled, origin apply attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts = true;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public PlayerOriginDefinition Origin => origin;
    public bool TriggerRepeatedly => triggerRepeatedly;
    public bool ForceReplace => forceReplace;

    void OnEnable() {
        if(applyOnEnable) {
            ApplyOrigin();
        }
    }

    void Start() {
        if(applyOnStart) {
            ApplyOrigin();
        }
    }

    [ContextMenu("Apply Player Origin")]
    public void ApplyOriginFromContextMenu() {
        ApplyOrigin();
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(applyOnPlayerTrigger) {
            ApplyOrigin(player);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(applyOnInteract) {
            var player = initiator != null ? initiator.GetComponent<PlayerController>() : ResolvePlayer();
            ApplyOrigin(player);
        }

        yield break;
    }

    public PlayerOriginApplyResult ApplyOrigin() {
        return ApplyOrigin(ResolvePlayer());
    }

    public PlayerOriginApplyResult ApplyOrigin(PlayerController player) {
        PlayerOriginApplyResult result;
        if(origin == null) {
            result = new PlayerOriginApplyResult(string.Empty, string.Empty, PlayerOriginCategory.General, SourceId, DisplayName, string.Empty, string.Empty) {
                blocked = true,
                failureMessage = "No origin assigned."
            };
        } else {
            result = origin.Apply(player, SourceId, DisplayName, this, forceReplace);
        }

        WriteAttemptLog(result);
        return result;
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

    void WriteAttemptLog(PlayerOriginApplyResult result) {
        if(!logAttempts || result == null) {
            return;
        }

        GameDebugLogger.Ensure().Record(
            result.blocked ? GameDebugSeverity.Warning : GameDebugSeverity.Success,
            GameDebugCategory.RPG,
            result.blocked
                ? $"{DisplayName} could not apply origin: {result.failureMessage}"
                : $"{DisplayName} applied origin {result.originName}.",
            this,
            "PlayerOriginSource");
    }
}
