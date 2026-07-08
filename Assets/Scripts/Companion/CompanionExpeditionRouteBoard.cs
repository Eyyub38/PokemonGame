using System.Collections;
using System.Linq;
using UnityEngine;

public enum CompanionExpeditionRouteBoardMode {
    StartOrAdvance,
    StartRoute,
    AdvanceOrClaimCurrentStage,
    StartCurrentStage
}

public class CompanionExpeditionRouteBoard : MonoBehaviour, Interactable {
    [Header("Route")]
    [Tooltip("Companion expedition route offered by this board/object.")]
    [SerializeField] CompanionExpeditionRouteDefinition route;
    [Tooltip("Optional stable source id for this board. Empty uses the GameObject name.")]
    [SerializeField] string sourceId;

    [Header("Interaction")]
    [Tooltip("Temporary interaction mode used until a dedicated UI chooses route and companion.")]
    [SerializeField] CompanionExpeditionRouteBoardMode mode = CompanionExpeditionRouteBoardMode.StartOrAdvance;
    [Tooltip("If enabled, DialogManager shows a result message after interaction.")]
    [SerializeField] bool showDialogResult = true;

    public CompanionExpeditionRouteDefinition Route => route;
    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;

    public IEnumerator Interact(Transform initiator) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : null;
        string message = RunInteraction(player);

        if(showDialogResult && DialogManager.i != null) {
            yield return DialogManager.i.ShowDialogText(message);
        }
    }

    public bool TryStart(PlayerController player, CompanionController companion, out string failureMessage) {
        var log = GetOrCreateRouteLog(player);
        if(log == null) {
            failureMessage = "Companion route log is missing.";
            return false;
        }

        return log.TryStart(player, route, companion, SourceId, out failureMessage);
    }

    public bool TryAdvanceOrClaim(PlayerController player, out string failureMessage) {
        var log = GetOrCreateRouteLog(player);
        if(log == null) {
            failureMessage = "Companion route log is missing.";
            return false;
        }

        return log.TryAdvanceOrClaim(player, route, SourceId, out failureMessage);
    }

    string RunInteraction(PlayerController player) {
        if(route == null) {
            return "This route board is not ready.";
        }

        if(player == null) {
            return "No player found for this route board.";
        }

        var log = GetOrCreateRouteLog(player);
        if(log == null) {
            return "Companion route log is missing.";
        }

        var activeRoute = log.GetActiveRoute(route, SourceId);
        if(mode == CompanionExpeditionRouteBoardMode.AdvanceOrClaimCurrentStage || (mode == CompanionExpeditionRouteBoardMode.StartOrAdvance && activeRoute != null)) {
            return TryAdvanceOrClaim(player, out var advanceFailure)
                ? $"{route.DisplayName} advanced."
                : advanceFailure;
        }

        if(mode == CompanionExpeditionRouteBoardMode.StartCurrentStage) {
            if(activeRoute == null) {
                return $"{route.DisplayName} is not active.";
            }

            return log.TryStartCurrentStage(player, route, activeRoute, out var stageFailure)
                ? $"{route.DisplayName} stage started."
                : stageFailure;
        }

        var companion = CompanionController.GetFollowingCompanions(player).FirstOrDefault();
        if(companion == null) {
            return "No following companion found.";
        }

        return TryStart(player, companion, out var startFailure)
            ? $"{companion.CompanionName} started {route.DisplayName}."
            : startFailure;
    }

    PlayerCompanionExpeditionRouteLog GetOrCreateRouteLog(PlayerController player) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerCompanionExpeditionRouteLog>();
        return log != null ? log : player.gameObject.AddComponent<PlayerCompanionExpeditionRouteLog>();
    }
}
