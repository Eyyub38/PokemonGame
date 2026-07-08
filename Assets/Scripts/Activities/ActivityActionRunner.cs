using System.Collections;
using UnityEngine;

public class ActivityActionRunner : MonoBehaviour, Interactable, IOverworldInteractionInfoProvider {
    [Header("Activity")]
    [Tooltip("Activity performed by this overworld node.")]
    [SerializeField] ActivityDefinition activity;
    [Tooltip("Name shown by interaction prompts. Empty uses this GameObject name or activity display name.")]
    [SerializeField] string targetName;
    [Tooltip("Action label shown by prompts and HUD, such as Water, Mine, Feed or Gather.")]
    [SerializeField] string actionName = "Interact";
    [Tooltip("Short prompt text shown before the player starts the activity.")]
    [TextArea]
    [SerializeField] string promptText;

    [Header("Runtime")]
    [Tooltip("Seconds the action HUD takes to complete. 0 completes instantly.")]
    [Min(0f)]
    [SerializeField] float durationSeconds = 1.25f;
    [Tooltip("If enabled, ActivityDefinition.TryPayCosts is called before progress starts.")]
    [SerializeField] bool payCostsOnStart = true;
    [Tooltip("If enabled, ActivityDefinition.ApplyRewards is called when progress completes.")]
    [SerializeField] bool applyRewardsOnComplete = true;
    [Tooltip("If enabled, this runner refuses new interactions while already running.")]
    [SerializeField] bool blockWhileRunning = true;

    [Header("HUD")]
    [Tooltip("HUD shown while this activity is running. Empty searches the scene at runtime.")]
    [SerializeField] ActivityActionHUDController actionHUD;
    [Tooltip("Optional tool text shown in the action HUD.")]
    [SerializeField] string toolHint;
    [Tooltip("Optional stamina/need cost text shown in the action HUD.")]
    [SerializeField] string staminaHint;
    [Tooltip("Optional partner/companion help text shown in the action HUD.")]
    [SerializeField] string partnerHint;
    [Tooltip("If enabled, the HUD root is hidden after completion.")]
    [SerializeField] bool hideHudOnComplete = true;
    [Tooltip("Seconds the completion toast remains visible.")]
    [Min(0f)]
    [SerializeField] float resultToastSeconds = 1.5f;

    [Header("Fallback Dialog")]
    [Tooltip("If enabled, blocked/complete messages also use DialogManager when no HUD is assigned.")]
    [SerializeField] bool useDialogFallback = true;
    [Tooltip("Message used when the activity completes. Empty uses the activity display name.")]
    [SerializeField] string completionMessage;

    bool running;

    public bool IsRunning => running;
    public ActivityDefinition Activity => activity;

    public IEnumerator Interact(Transform initiator) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : PlayerController.i;

        if(activity == null) {
            yield return ShowFallback("This activity node is not configured.");
            yield break;
        }

        if(blockWhileRunning && running) {
            yield break;
        }

        if(!activity.CanPerform(player, out var failureMessage)) {
            GameDebug.Warning(failureMessage, GameDebugCategory.Activity, this, "ActivityActionRunner");
            yield return ShowFallback(failureMessage);
            yield break;
        }

        if(payCostsOnStart && !activity.TryPayCosts(player, out failureMessage)) {
            GameDebug.Warning(failureMessage, GameDebugCategory.Activity, this, "ActivityActionRunner");
            yield return ShowFallback(failureMessage);
            yield break;
        }

        running = true;
        var hud = ResolveHUD();
        hud?.Show(BuildActionTitle(), "Working...", toolHint, staminaHint, partnerHint);

        if(durationSeconds > 0f) {
            float elapsed = 0f;
            while(elapsed < durationSeconds) {
                elapsed += Time.deltaTime;
                hud?.SetProgress(elapsed / durationSeconds);
                yield return null;
            }
        }

        hud?.SetProgress(1f);
        if(applyRewardsOnComplete) {
            activity.ApplyRewards(player);
        }

        string message = BuildCompletionMessage();
        hud?.ShowResult(message, BuildResultBody(), resultToastSeconds);
        GameDebug.Success(message, GameDebugCategory.Activity, this, "ActivityActionRunner");

        if(hud == null) {
            yield return ShowFallback(message);
        }

        if(hideHudOnComplete && hud != null) {
            yield return new WaitForSeconds(Mathf.Max(0f, resultToastSeconds));
            hud.Hide();
        }

        running = false;
    }

    public bool TryGetInteractionInfo(PlayerController player, out OverworldInteractionInfo info) {
        bool canInteract = true;
        string blockedMessage = null;
        if(activity == null) {
            canInteract = false;
            blockedMessage = "This activity node is not configured.";
        } else if(!activity.CanPerform(player, out blockedMessage)) {
            canInteract = false;
        } else if(blockWhileRunning && running) {
            canInteract = false;
            blockedMessage = $"{BuildTargetName()} is already in use.";
        }

        info = new OverworldInteractionInfo {
            TargetName = BuildTargetName(),
            ActionName = string.IsNullOrWhiteSpace(actionName) ? "Interact" : actionName,
            Description = string.IsNullOrWhiteSpace(promptText) ? activity != null ? activity.Description : string.Empty : promptText,
            ToolHint = toolHint,
            PermissionHint = PlayerActivityContext.CurrentZone != null ? PlayerActivityContext.CurrentZone.DisplayName : string.Empty,
            BlockedMessage = blockedMessage,
            CanInteract = canInteract,
            Activity = activity,
            Zone = PlayerActivityContext.CurrentZone,
            Source = this
        };
        return true;
    }

    ActivityActionHUDController ResolveHUD() {
        if(actionHUD != null) {
            return actionHUD;
        }

        actionHUD = FindAnyObjectByType<ActivityActionHUDController>(FindObjectsInactive.Include);
        return actionHUD;
    }

    string BuildTargetName() {
        if(!string.IsNullOrWhiteSpace(targetName)) {
            return targetName;
        }
        return activity != null ? activity.DisplayName : name;
    }

    string BuildActionTitle() {
        var verb = string.IsNullOrWhiteSpace(actionName) ? "Interact" : actionName;
        return $"{verb} {BuildTargetName()}";
    }

    string BuildCompletionMessage() {
        if(!string.IsNullOrWhiteSpace(completionMessage)) {
            return completionMessage;
        }
        return activity != null ? $"{activity.DisplayName} complete." : $"{name} complete.";
    }

    string BuildResultBody() {
        return activity != null
            ? $"Rewards and journal progress applied for {activity.DisplayName}."
            : "Activity completed.";
    }

    IEnumerator ShowFallback(string message) {
        if(useDialogFallback && DialogManager.i != null && !string.IsNullOrWhiteSpace(message)) {
            yield return DialogManager.i.ShowDialogText(message);
        }
    }
}
