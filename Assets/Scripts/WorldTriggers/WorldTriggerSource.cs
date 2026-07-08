using System.Collections.Generic;
using UnityEngine;

public class WorldTriggerSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Source")]
    [Tooltip("Stable source id used by trigger repeat rules. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Manual world triggers applied when this source is used.")]
    [SerializeField] List<WorldTriggerDefinition> triggers = new List<WorldTriggerDefinition>();

    [Header("Access")]
    [Tooltip("Optional access profile checked before manual triggers apply.")]
    [SerializeField] AccessProfileDefinition accessProfile = null;
    [Tooltip("If enabled, access checks are published to access logs/events.")]
    [SerializeField] bool publishAccessChecks = true;

    [Header("Trigger")]
    [Tooltip("If enabled, repeated player triggers can call this source more than once.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Debug")]
    [Tooltip("If enabled, source attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<WorldTriggerDefinition> Triggers => triggers;
    public AccessProfileDefinition AccessProfile => accessProfile;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        Apply(player);
    }

    public List<WorldTriggerRunResult> Apply(PlayerController player) {
        var results = new List<WorldTriggerRunResult>();
        if(player == null) {
            Log("A player is required to apply world triggers.", GameDebugSeverity.Warning);
            return results;
        }

        if(accessProfile != null && !accessProfile.CanAccess(player, out var failureMessage)) {
            if(publishAccessChecks) {
                accessProfile.PublishChecked(player, false, SourceId, failureMessage, this);
            }
            Log(failureMessage, GameDebugSeverity.Warning);
            return results;
        }

        if(accessProfile != null && publishAccessChecks) {
            accessProfile.PublishChecked(player, true, SourceId, accessProfile.PassedMessage, this);
        }

        foreach(var trigger in triggers) {
            if(trigger == null) {
                continue;
            }

            var result = trigger.Apply(player, WorldTriggerKind.Manual, null, SourceId, DisplayName, this);
            if(result != null) {
                results.Add(result);
            }
        }

        if(logAttempts) {
            Log($"{DisplayName} applied {results.Count} manual world trigger(s).", GameDebugSeverity.Info);
        }

        return results;
    }

    void Log(string message, GameDebugSeverity severity) {
        if(!logAttempts && severity < GameDebugSeverity.Warning) {
            return;
        }

        GameDebugLogger.Ensure().Record(severity, GameDebugCategory.WorldTrigger, message, this, "WorldTriggerSource");
    }
}
