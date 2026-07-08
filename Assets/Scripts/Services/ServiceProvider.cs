using System.Collections;
using UnityEngine;

public class ServiceProvider : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source/provider id used by service repeat rules. Empty uses this GameObject name.")]
    [SerializeField] string providerId = string.Empty;
    [Tooltip("Readable provider name stored in service logs. Empty uses this GameObject name.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Service")]
    [Tooltip("Service definition applied by this provider.")]
    [SerializeField] ServiceDefinition service;
    [Tooltip("Optional explicit player. Empty uses the triggering/interacting player or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;

    [Header("Triggers")]
    [Tooltip("If enabled, the service is used when this component starts.")]
    [SerializeField] bool useOnStart;
    [Tooltip("If enabled, the service is used when PlayerController enters this trigger through IPlayerTriggerable.")]
    [SerializeField] bool useOnPlayerTrigger = true;
    [Tooltip("If enabled, the service is used when an Interactable flow calls Interact.")]
    [SerializeField] bool useOnInteract = true;
    [Tooltip("Controls IPlayerTriggerable.TriggerRepeatedly.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Feedback")]
    [Tooltip("If enabled, result text is shown through the existing DialogManager when available.")]
    [SerializeField] bool showDialogFeedback;
    [Tooltip("If enabled, service results are written through GameDebug.")]
    [SerializeField] bool writeDebugLogs = true;

    public string ProviderId => string.IsNullOrWhiteSpace(providerId) ? gameObject.name : providerId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public ServiceDefinition Service => service;
    public bool TriggerRepeatedly => triggerRepeatedly;

    void Start() {
        if(useOnStart) {
            UseService();
        }
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(useOnPlayerTrigger) {
            UseService(player);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(!useOnInteract) {
            yield break;
        }

        var player = playerOverride != null ? playerOverride : initiator != null ? initiator.GetComponent<PlayerController>() : null;
        var result = UseService(player);
        if(showDialogFeedback && DialogManager.i != null) {
            yield return DialogManager.i.ShowDialogText(BuildFeedbackMessage(result));
        }
    }

    [ContextMenu("Use Service")]
    public void UseServiceFromContextMenu() {
        UseService();
    }

    public ServiceUseResult UseService(PlayerController player = null) {
        player = ResolvePlayer(player);
        if(service == null) {
            var missingResult = new ServiceUseResult(string.Empty, "Missing Service", PlayerServiceCategory.General, ProviderId, DisplayName) {
                blocked = true,
                failureMessage = "No service definition assigned."
            };
            LogResult(missingResult);
            return missingResult;
        }

        var result = service.Use(player, ProviderId, DisplayName, this);
        LogResult(result);
        return result;
    }

    PlayerController ResolvePlayer(PlayerController player) {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(player != null) {
            return player;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }

    void LogResult(ServiceUseResult result) {
        if(!writeDebugLogs || result == null) {
            return;
        }

        string source = $"ServiceProvider/{DisplayName}";
        if(result.blocked) {
            GameDebug.Warning($"{result.serviceName} blocked: {result.failureMessage}", GameDebugCategory.Activity, this, source);
            return;
        }

        GameDebug.Success($"{result.serviceName} completed.", GameDebugCategory.Activity, this, source);
    }

    string BuildFeedbackMessage(ServiceUseResult result) {
        if(result == null) {
            return "Service is unavailable.";
        }

        if(result.blocked) {
            return string.IsNullOrWhiteSpace(result.failureMessage) ? $"{result.serviceName} is unavailable." : result.failureMessage;
        }

        return $"{result.serviceName} completed.";
    }
}
