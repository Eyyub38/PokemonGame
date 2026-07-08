using System.Collections;
using UnityEngine;

public enum CompanionPokemonTeamSourceAction {
    ResetFromRoster,
    HealAll,
    ReportSummary
}

public class CompanionPokemonTeamSource : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("Team")]
    [Tooltip("Companion Pokemon team affected by this source. Empty uses this GameObject.")]
    [SerializeField] CompanionPokemonTeam team;
    [Tooltip("Action performed when this source is used.")]
    [SerializeField] CompanionPokemonTeamSourceAction action = CompanionPokemonTeamSourceAction.ReportSummary;

    [Header("Trigger")]
    [Tooltip("If enabled, entering this trigger runs the source action.")]
    [SerializeField] bool runOnPlayerTrigger;
    [Tooltip("Controls IPlayerTriggerable repeat behavior.")]
    [SerializeField] bool triggerRepeatedly;

    [Header("Feedback")]
    [Tooltip("If enabled, DialogManager shows the source result.")]
    [SerializeField] bool showDialogResult = true;

    public bool TriggerRepeatedly => triggerRepeatedly;

    public IEnumerator Interact(Transform initiator) {
        string message = Run();
        if(showDialogResult && DialogManager.i != null && !string.IsNullOrWhiteSpace(message)) {
            yield return DialogManager.i.ShowDialogText(message);
        }
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(runOnPlayerTrigger) {
            Run();
        }
    }

    string Run() {
        var targetTeam = team != null ? team : GetComponent<CompanionPokemonTeam>();
        if(targetTeam == null) {
            return "Companion Pokemon team is missing.";
        }

        switch(action) {
            case CompanionPokemonTeamSourceAction.ResetFromRoster:
                targetTeam.ResetFromRoster();
                return "Companion Pokemon team reset from roster.";
            case CompanionPokemonTeamSourceAction.HealAll:
                targetTeam.HealAll();
                return "Companion Pokemon team healed.";
            default:
                return $"Companion has {targetTeam.Pokemon.Count} Pokemon.";
        }
    }
}
