using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum LoyaltyProgramSourceAction {
    None,
    JoinFirstAvailable,
    JoinAllAvailable,
    GrantManualPoints
}

public class LoyaltyProgramSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id written into loyalty logs. Empty uses shop id or this GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Readable source name for debug/future UI. Empty uses this GameObject name.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Programs")]
    [Tooltip("Loyalty programs offered or affected by this source.")]
    [SerializeField] List<LoyaltyProgramDefinition> programs = new List<LoyaltyProgramDefinition>();
    [Tooltip("Optional shop context used for source identity and future UI filtering.")]
    [SerializeField] ShopCatalog shopContext;
    [Tooltip("If enabled, blocked programs are hidden from GetAvailablePrograms.")]
    [SerializeField] bool hideUnavailablePrograms = true;
    [Tooltip("Optional explicit player. Empty uses the triggering/interacting player or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;

    [Header("Manual Points")]
    [Tooltip("Point amount granted by Grant Manual Points actions.")]
    [Min(0)]
    [SerializeField] int manualPoints;
    [Tooltip("Target id written into manual point records.")]
    [SerializeField] string manualTargetId = string.Empty;
    [Tooltip("Target display name written into manual point records.")]
    [SerializeField] string manualTargetName = string.Empty;

    [Header("Triggers")]
    [Tooltip("Action applied when this source starts. UI can ignore this and call TryJoin or GrantManualPoints directly.")]
    [SerializeField] LoyaltyProgramSourceAction startAction = LoyaltyProgramSourceAction.None;
    [Tooltip("Action applied when player trigger calls this source.")]
    [SerializeField] LoyaltyProgramSourceAction triggerAction = LoyaltyProgramSourceAction.None;
    [Tooltip("Action applied when an Interactable flow calls Interact.")]
    [SerializeField] LoyaltyProgramSourceAction interactAction = LoyaltyProgramSourceAction.None;
    [Tooltip("If enabled, player trigger applies Trigger Action.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Feedback")]
    [Tooltip("If enabled, result text is shown through the existing DialogManager when available.")]
    [SerializeField] bool showDialogFeedback;
    [Tooltip("If enabled, blocked attempts are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful loyalty actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public string SourceId => ResolveSourceId();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public IReadOnlyList<LoyaltyProgramDefinition> Programs => programs;
    public ShopCatalog ShopContext => shopContext;
    public LoyaltyProgramSourceAction StartAction => startAction;
    public LoyaltyProgramSourceAction TriggerAction => triggerAction;
    public LoyaltyProgramSourceAction InteractAction => interactAction;
    public int ManualPoints => Mathf.Max(0, manualPoints);
    public bool TriggerRepeatedly => triggerRepeatedly;

    void Start() {
        if(startAction != LoyaltyProgramSourceAction.None) {
            ApplyAction(startAction, ResolvePlayer(null), out _);
        }
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(!applyOnPlayerTrigger || triggerAction == LoyaltyProgramSourceAction.None) {
            return;
        }

        ApplyAction(triggerAction, ResolvePlayer(player), out _);
    }

    public IEnumerator Interact(Transform initiator) {
        if(interactAction == LoyaltyProgramSourceAction.None) {
            yield break;
        }

        var player = ResolvePlayer(initiator != null ? initiator.GetComponent<PlayerController>() : null);
        ApplyAction(interactAction, player, out var feedback);
        if(showDialogFeedback && DialogManager.i != null && !string.IsNullOrWhiteSpace(feedback)) {
            yield return DialogManager.i.ShowDialogText(feedback);
        }
    }

    public List<LoyaltyProgramDefinition> GetAvailablePrograms(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerLoyaltyLog>() : null;
        return (programs ?? new List<LoyaltyProgramDefinition>())
            .Where(program => program != null)
            .Where(program => !hideUnavailablePrograms || program.CanJoin(player, log, out _))
            .OrderBy(program => program.Kind)
            .ThenBy(program => program.DisplayName)
            .ToList();
    }

    public bool TryJoin(PlayerController player, LoyaltyProgramDefinition program, out PlayerLoyaltyRecord record, out string failureMessage) {
        record = null;
        player = ResolvePlayer(player);
        if(player == null) {
            failureMessage = "A player is required to join loyalty programs.";
            LogBlocked(failureMessage, null);
            return false;
        }

        if(program == null) {
            failureMessage = "No loyalty program selected.";
            LogBlocked(failureMessage, player);
            return false;
        }

        if(programs == null || !programs.Contains(program)) {
            failureMessage = $"{program.DisplayName} is not offered by this source.";
            LogBlocked(failureMessage, player);
            return false;
        }

        if(!program.TryJoin(player, SourceId, out record, out failureMessage)) {
            LogBlocked(failureMessage, player);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{program.DisplayName} joined.", GameDebugCategory.Shop, this, "LoyaltyProgramSource");
        }

        return true;
    }

    public bool TryJoinFirstAvailable(PlayerController player, out string failureMessage) {
        var program = GetAvailablePrograms(ResolvePlayer(player)).FirstOrDefault();
        if(program == null) {
            failureMessage = "No available loyalty programs.";
            LogBlocked(failureMessage, player);
            return false;
        }

        return TryJoin(player, program, out _, out failureMessage);
    }

    public int TryJoinAll(PlayerController player, out string failureMessage) {
        player = ResolvePlayer(player);
        failureMessage = null;
        int joined = 0;
        foreach(var program in GetAvailablePrograms(player).ToList()) {
            if(TryJoin(player, program, out _, out failureMessage)) {
                joined++;
            }
        }

        if(joined == 0 && string.IsNullOrWhiteSpace(failureMessage)) {
            failureMessage = "No loyalty programs were joined.";
        }

        return joined;
    }

    public int GrantManualPoints(PlayerController player, out string failureMessage) {
        player = ResolvePlayer(player);
        if(player == null) {
            failureMessage = "A player is required to grant loyalty points.";
            LogBlocked(failureMessage, null);
            return 0;
        }

        if(ManualPoints <= 0) {
            failureMessage = "Manual point amount is 0.";
            LogBlocked(failureMessage, player);
            return 0;
        }

        var log = player.GetComponent<PlayerLoyaltyLog>() ?? player.gameObject.AddComponent<PlayerLoyaltyLog>();
        int granted = 0;
        foreach(var program in programs.Where(program => program != null)) {
            if(log.AddPoints(program, ManualPoints, LoyaltyPointSourceKind.Manual, SourceId, manualTargetId, manualTargetName) != null) {
                granted++;
            }
        }

        failureMessage = granted > 0 ? null : "No loyalty points were granted.";
        if(granted == 0) {
            LogBlocked(failureMessage, player);
        } else if(logSuccessfulAttempts) {
            GameDebug.Success($"Granted {ManualPoints} loyalty point(s) to {granted} program(s).", GameDebugCategory.Shop, this, "LoyaltyProgramSource");
        }

        return granted;
    }

    bool ApplyAction(LoyaltyProgramSourceAction action, PlayerController player, out string feedback) {
        feedback = null;
        if(action == LoyaltyProgramSourceAction.JoinAllAvailable) {
            int joined = TryJoinAll(player, out var failureMessage);
            feedback = joined > 0 ? $"{joined} membership(s) joined." : failureMessage;
            return joined > 0;
        }

        if(action == LoyaltyProgramSourceAction.JoinFirstAvailable) {
            bool joined = TryJoinFirstAvailable(player, out var failureMessage);
            feedback = joined ? "Membership joined." : failureMessage;
            return joined;
        }

        if(action == LoyaltyProgramSourceAction.GrantManualPoints) {
            int granted = GrantManualPoints(player, out var failureMessage);
            feedback = granted > 0 ? "Loyalty points granted." : failureMessage;
            return granted > 0;
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

        GameDebug.Warning(failureMessage, GameDebugCategory.Shop, player != null ? player : this, "LoyaltyProgramSource");
    }
}
