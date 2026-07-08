using System.Collections;
using System.Linq;
using UnityEngine;

public enum CompanionExpeditionBoardMode {
    StartFirstFollowingCompanion,
    ClaimFirstReady,
    ClaimAllReady
}

public class CompanionExpeditionBoard : MonoBehaviour, Interactable {
    [Header("Expedition")]
    [Tooltip("Expedition offered by this board/object.")]
    [SerializeField] CompanionExpeditionDefinition expedition;
    [Tooltip("Optional stable source id for this board. Empty uses the GameObject name.")]
    [SerializeField] string sourceId;

    [Header("Interaction")]
    [Tooltip("Temporary interaction mode used until a dedicated UI chooses expedition and companion.")]
    [SerializeField] CompanionExpeditionBoardMode mode = CompanionExpeditionBoardMode.StartFirstFollowingCompanion;
    [Tooltip("If enabled, DialogManager shows a result message after interaction.")]
    [SerializeField] bool showDialogResult = true;

    public CompanionExpeditionDefinition Expedition => expedition;
    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;

    public IEnumerator Interact(Transform initiator) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : null;
        string message = RunInteraction(player);

        if(showDialogResult && DialogManager.i != null) {
            yield return DialogManager.i.ShowDialogText(message);
        }
    }

    public bool TryStart(PlayerController player, CompanionController companion, out string failureMessage) {
        var log = GetOrCreateLog(player);
        if(log == null) {
            failureMessage = "Companion expedition log is missing.";
            return false;
        }

        return log.TryStart(player, expedition, companion, SourceId, out failureMessage);
    }

    public bool TryClaimFirstReady(PlayerController player, out string failureMessage) {
        var log = GetOrCreateLog(player);
        if(log == null) {
            failureMessage = "Companion expedition log is missing.";
            return false;
        }

        return log.TryClaimFirstReady(player, expedition, SourceId, out failureMessage);
    }

    string RunInteraction(PlayerController player) {
        if(expedition == null) {
            return "This expedition board is not ready.";
        }

        if(player == null) {
            return "No player found for this expedition board.";
        }

        var log = GetOrCreateLog(player);
        if(log == null) {
            return "Companion expedition log is missing.";
        }

        if(mode == CompanionExpeditionBoardMode.ClaimAllReady) {
            int claimed = log.ClaimAllReady(player, expedition, SourceId);
            return claimed > 0 ? $"{claimed} expedition(s) claimed." : "No ready expedition found.";
        }

        if(mode == CompanionExpeditionBoardMode.ClaimFirstReady) {
            return TryClaimFirstReady(player, out var claimFailure)
                ? $"{expedition.DisplayName} claimed."
                : claimFailure;
        }

        var companion = CompanionController.GetFollowingCompanions(player).FirstOrDefault();
        if(companion == null) {
            return "No following companion found.";
        }

        return TryStart(player, companion, out var startFailure)
            ? $"{companion.CompanionName} started {expedition.DisplayName}."
            : startFailure;
    }

    PlayerCompanionExpeditionLog GetOrCreateLog(PlayerController player) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerCompanionExpeditionLog>();
        return log != null ? log : player.gameObject.AddComponent<PlayerCompanionExpeditionLog>();
    }
}
