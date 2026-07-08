using System.Collections;
using UnityEngine;

public class EncounterInteractable : MonoBehaviour, Interactable {
    [Header("Encounter")]
    [Tooltip("Encounter table rolled when the player interacts with this object.")]
    [SerializeField] EncounterTableDefinition encounterTable;
    [Tooltip("Optional source override. Any uses the table source type.")]
    [SerializeField] EncounterSourceType sourceOverride = EncounterSourceType.Any;
    [Tooltip("Multiplier applied to the table's base encounter chance.")]
    [Min(0f)]
    [SerializeField] float chanceMultiplier = 1f;

    [Header("Messages")]
    [Tooltip("Optional text shown before the encounter roll.")]
    [SerializeField] string interactionText;
    [Tooltip("Optional text shown when no encounter is found.")]
    [SerializeField] string noEncounterText = "Nothing happened.";
    [Tooltip("If enabled, stealth capture result messages are shown through DialogManager.")]
    [SerializeField] bool showStealthCaptureMessages = true;

    [Header("Result")]
    [Tooltip("If enabled, a successful roll starts a normal wild battle.")]
    [SerializeField] bool startBattleOnEncounter = true;
    [Tooltip("If assigned and Attempt Stealth Capture is enabled, interaction can attempt a non-battle capture.")]
    [SerializeField] StealthCaptureProfileDefinition stealthCaptureProfile;
    [Tooltip("If enabled, successful rolls use the stealth capture profile before battle.")]
    [SerializeField] bool attemptStealthCapture;

    public IEnumerator Interact(Transform initiator) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : null;
        if(player == null) {
            yield break;
        }

        if(!string.IsNullOrWhiteSpace(interactionText) && DialogManager.i != null) {
            yield return DialogManager.i.ShowDialogText(interactionText);
        }

        var sourceType = ResolveSourceType();
        if(!EncounterSystem.TryRoll(player, encounterTable, sourceType, chanceMultiplier, this, out var pokemon, out _)) {
            if(!string.IsNullOrWhiteSpace(noEncounterText) && DialogManager.i != null) {
                yield return DialogManager.i.ShowDialogText(noEncounterText);
            }
            yield break;
        }

        var trigger = encounterTable != null ? encounterTable.BattleTrigger : BattleTrigger.LongGrass;
        if(attemptStealthCapture && stealthCaptureProfile != null) {
            var result = EncounterSystem.TryStealthCapture(player, pokemon, sourceType, encounterTable, trigger, stealthCaptureProfile, this, out var captureResult);
            if(showStealthCaptureMessages && captureResult != null && !string.IsNullOrWhiteSpace(captureResult.message) && DialogManager.i != null) {
                yield return DialogManager.i.ShowDialogText(captureResult.message);
            }

            if(result != EncounterStartResult.NoEncounter) {
                yield break;
            }
        }

        if(startBattleOnEncounter) {
            EncounterSystem.StartBattle(player, pokemon, sourceType, encounterTable, trigger, this);
        }
    }

    EncounterSourceType ResolveSourceType() {
        if(sourceOverride != EncounterSourceType.Any) {
            return sourceOverride;
        }

        return encounterTable != null ? encounterTable.SourceType : EncounterSourceType.Special;
    }
}
