using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ServicePackageSourceAction {
    None,
    UseFirstAvailable,
    UseAllAvailable
}

public class ServicePackageSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id used by package repeat rules. Empty uses shop id or this GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Readable source name stored in package logs. Empty uses this GameObject name.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Packages")]
    [Tooltip("Service packages offered by this source.")]
    [SerializeField] List<ServicePackageDefinition> packages = new List<ServicePackageDefinition>();
    [Tooltip("Optional shop context used for price multipliers, sponsor discounts and source identity.")]
    [SerializeField] ShopCatalog shopContext;
    [Tooltip("If enabled, blocked packages are hidden from GetAvailablePackages.")]
    [SerializeField] bool hideUnavailablePackages = true;
    [Tooltip("Optional explicit player. Empty uses the triggering/interacting player or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;

    [Header("Triggers")]
    [Tooltip("Action applied when this source starts. UI can ignore this and call TryUse directly.")]
    [SerializeField] ServicePackageSourceAction startAction = ServicePackageSourceAction.None;
    [Tooltip("Action applied when player trigger calls this source. UI can ignore this and call TryUse directly.")]
    [SerializeField] ServicePackageSourceAction triggerAction = ServicePackageSourceAction.None;
    [Tooltip("Action applied when an Interactable flow calls Interact.")]
    [SerializeField] ServicePackageSourceAction interactAction = ServicePackageSourceAction.None;
    [Tooltip("If enabled, player trigger applies Trigger Action.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Feedback")]
    [Tooltip("If enabled, result text is shown through the existing DialogManager when available.")]
    [SerializeField] bool showDialogFeedback;
    [Tooltip("If enabled, blocked attempts are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful package uses are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public string SourceId => ResolveSourceId();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public IReadOnlyList<ServicePackageDefinition> Packages => packages;
    public ShopCatalog ShopContext => shopContext;
    public ServicePackageSourceAction StartAction => startAction;
    public ServicePackageSourceAction TriggerAction => triggerAction;
    public ServicePackageSourceAction InteractAction => interactAction;
    public bool TriggerRepeatedly => triggerRepeatedly;

    void Start() {
        if(startAction != ServicePackageSourceAction.None) {
            ApplyAction(startAction, ResolvePlayer(null), out _);
        }
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(!applyOnPlayerTrigger || triggerAction == ServicePackageSourceAction.None) {
            return;
        }

        ApplyAction(triggerAction, ResolvePlayer(player), out _);
    }

    public IEnumerator Interact(Transform initiator) {
        if(interactAction == ServicePackageSourceAction.None) {
            yield break;
        }

        var player = ResolvePlayer(initiator != null ? initiator.GetComponent<PlayerController>() : null);
        ApplyAction(interactAction, player, out var feedback);
        if(showDialogFeedback && DialogManager.i != null && !string.IsNullOrWhiteSpace(feedback)) {
            yield return DialogManager.i.ShowDialogText(feedback);
        }
    }

    public List<ServicePackageDefinition> GetAvailablePackages(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerServicePackageLog>() : null;
        var catalog = shopContext != null ? shopContext.Catalog : null;
        return (packages ?? new List<ServicePackageDefinition>())
            .Where(package => package != null)
            .Where(package => !hideUnavailablePackages || package.CanUse(player, log, SourceId, catalog, out _))
            .OrderBy(package => package.Category)
            .ThenBy(package => package.DisplayName)
            .ToList();
    }

    public bool TryUse(PlayerController player, ServicePackageDefinition package, out ServicePackageUseResult result, out string failureMessage) {
        result = null;
        player = ResolvePlayer(player);
        if(player == null) {
            failureMessage = "A player is required to use service packages.";
            LogBlocked(failureMessage, null);
            return false;
        }

        if(package == null) {
            failureMessage = "No service package selected.";
            LogBlocked(failureMessage, player);
            return false;
        }

        if(packages == null || !packages.Contains(package)) {
            failureMessage = $"{package.DisplayName} is not offered by this source.";
            LogBlocked(failureMessage, player);
            return false;
        }

        result = package.Use(player, SourceId, DisplayName, shopContext != null ? shopContext.Catalog : null, this);
        if(result == null || result.blocked) {
            failureMessage = result != null ? result.failureMessage : "Package use failed.";
            LogBlocked(failureMessage, player);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{package.DisplayName} completed.", GameDebugCategory.Activity, this, "ServicePackageSource");
        }

        failureMessage = null;
        return true;
    }

    public bool TryUseFirstAvailable(PlayerController player, out string failureMessage) {
        var package = GetAvailablePackages(ResolvePlayer(player)).FirstOrDefault();
        if(package == null) {
            failureMessage = "No available service packages.";
            LogBlocked(failureMessage, player);
            return false;
        }

        return TryUse(player, package, out _, out failureMessage);
    }

    public int TryUseAll(PlayerController player, out string failureMessage) {
        player = ResolvePlayer(player);
        failureMessage = null;
        int used = 0;
        foreach(var package in GetAvailablePackages(player).ToList()) {
            if(TryUse(player, package, out _, out failureMessage)) {
                used++;
            }
        }

        if(used == 0 && string.IsNullOrWhiteSpace(failureMessage)) {
            failureMessage = "No service packages were used.";
        }

        return used;
    }

    bool ApplyAction(ServicePackageSourceAction action, PlayerController player, out string feedback) {
        feedback = null;
        if(action == ServicePackageSourceAction.UseAllAvailable) {
            int used = TryUseAll(player, out var failureMessage);
            feedback = used > 0 ? $"{used} package(s) completed." : failureMessage;
            return used > 0;
        }

        if(action == ServicePackageSourceAction.UseFirstAvailable) {
            bool used = TryUseFirstAvailable(player, out var failureMessage);
            feedback = used ? "Service package completed." : failureMessage;
            return used;
        }

        return false;
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

    string ResolveSourceId() {
        if(!string.IsNullOrWhiteSpace(sourceId)) {
            return sourceId;
        }

        if(shopContext != null) {
            return $"shop:{shopContext.ShopId}";
        }

        return gameObject.name;
    }

    void LogBlocked(string failureMessage, PlayerController player) {
        if(!logBlockedAttempts || string.IsNullOrWhiteSpace(failureMessage)) {
            return;
        }

        GameDebug.Warning(failureMessage, GameDebugCategory.Activity, player != null ? player : this, "ServicePackageSource");
    }
}
