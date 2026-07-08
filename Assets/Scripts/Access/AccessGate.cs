using UnityEngine;

public enum AccessGateTriggerMode {
    CheckOnly,
    PublishPassedOnly,
    PublishDeniedOnly
}

public class AccessGate : MonoBehaviour, IPlayerTriggerable {
    [Header("Gate")]
    [Tooltip("Stable gate/context id used by PlayerAccessLog. Empty uses GameObject name.")]
    [SerializeField] string gateId;
    [Tooltip("Name shown in debug and future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName;
    [Tooltip("Access profile checked by this gate.")]
    [SerializeField] AccessProfileDefinition accessProfile;

    [Header("Trigger")]
    [Tooltip("How this gate reacts when the player triggers it.")]
    [SerializeField] AccessGateTriggerMode triggerMode = AccessGateTriggerMode.CheckOnly;
    [Tooltip("If enabled, repeated player triggers can call this gate more than once.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("If enabled, a PlayerAccessLog component is added to the player when missing.")]
    [SerializeField] bool installLogIfMissing = true;

    [Header("Messages")]
    [Tooltip("Optional message published when access succeeds. Empty uses the profile message.")]
    [SerializeField] string passedMessageOverride;
    [Tooltip("Optional message published when access fails. Empty uses the profile failure message.")]
    [SerializeField] string deniedMessageOverride;

    [Header("Debug")]
    [Tooltip("If enabled, gate checks are written to GameDebug.")]
    [SerializeField] bool logChecks;

    public string GateId => string.IsNullOrWhiteSpace(gateId) ? name : gateId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public AccessProfileDefinition AccessProfile => accessProfile;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        CheckAccess(player, out _);
    }

    public bool CheckAccess(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for access checks.";
            return false;
        }

        if(accessProfile == null) {
            failureMessage = "No access profile assigned.";
            GameDebug.Warning($"{DisplayName} has no access profile.", GameDebugCategory.Validation, this, "AccessGate");
            return false;
        }

        bool passed = accessProfile.CanAccess(player, out failureMessage);
        string message = passed
            ? (string.IsNullOrWhiteSpace(passedMessageOverride) ? accessProfile.PassedMessage : passedMessageOverride)
            : (string.IsNullOrWhiteSpace(deniedMessageOverride) ? failureMessage : deniedMessageOverride);

        var log = player.GetComponent<PlayerAccessLog>();
        if(log == null && installLogIfMissing) {
            log = player.gameObject.AddComponent<PlayerAccessLog>();
        }

        log?.RecordCheck(accessProfile, passed, message, GateId, this);
        PublishGateEvent(player, passed, message);

        if(logChecks) {
            var severity = passed ? GameDebugSeverity.Success : GameDebugSeverity.Warning;
            GameDebugLogger.Ensure().Record(severity, GameDebugCategory.Access, message, this, "AccessGate");
        }

        return passed;
    }

    void PublishGateEvent(PlayerController player, bool passed, string message) {
        if(triggerMode == AccessGateTriggerMode.PublishPassedOnly && !passed) {
            return;
        }

        if(triggerMode == AccessGateTriggerMode.PublishDeniedOnly && passed) {
            return;
        }

        GameEventPublishing.PublishOptional(
            null,
            $"access-gate.{(passed ? "passed" : "denied")}.{GateId}",
            message,
            GameEventCategory.Access,
            passed ? GameEventImportance.Success : GameEventImportance.Warning,
            this,
            "AccessGate",
            GameEventScope.Scene,
            showInFeed: false,
            writeToDebugLog: logChecks,
            GameEventPublishing.Value("gateId", GateId),
            GameEventPublishing.Value("gateName", DisplayName),
            GameEventPublishing.Value("profileId", accessProfile != null ? accessProfile.Id : null),
            GameEventPublishing.Value("passed", passed));
    }
}
