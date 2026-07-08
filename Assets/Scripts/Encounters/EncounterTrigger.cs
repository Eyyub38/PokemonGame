using UnityEngine;

public class EncounterTrigger : MonoBehaviour, IPlayerTriggerable {
    [Header("Encounter")]
    [Tooltip("Encounter table rolled when the player steps on this trigger.")]
    [SerializeField] EncounterTableDefinition encounterTable;
    [Tooltip("Optional source override. Any uses the table source type.")]
    [SerializeField] EncounterSourceType sourceOverride = EncounterSourceType.Any;
    [Tooltip("Multiplier applied to the table's base encounter chance.")]
    [Min(0f)]
    [SerializeField] float chanceMultiplier = 1f;
    [Tooltip("If enabled, this trigger can fire repeatedly while the player moves through it.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("Seconds of real time before this trigger can roll again.")]
    [Min(0f)]
    [SerializeField] float realTimeCooldownSeconds = 0.25f;

    [Header("Result")]
    [Tooltip("If enabled, a successful roll starts a normal wild battle.")]
    [SerializeField] bool startBattleOnEncounter = true;
    [Tooltip("If assigned and Attempt Stealth Capture is enabled, the encounter tries a non-battle capture before battle.")]
    [SerializeField] StealthCaptureProfileDefinition stealthCaptureProfile;
    [Tooltip("If enabled, successful rolls use the stealth capture profile instead of immediately starting battle.")]
    [SerializeField] bool attemptStealthCapture;
    [Tooltip("If enabled, player movement animation is stopped before starting the encounter.")]
    [SerializeField] bool stopPlayerMovement = true;

    float lastRollTime = -999f;

    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(player == null || Time.time < lastRollTime + realTimeCooldownSeconds) {
            return;
        }

        lastRollTime = Time.time;
        var sourceType = ResolveSourceType();
        if(!EncounterSystem.TryRoll(player, encounterTable, sourceType, chanceMultiplier, this, out var pokemon, out _)) {
            return;
        }

        if(stopPlayerMovement && player.Character != null && player.Character.Animator != null) {
            player.Character.Animator.IsMoving = false;
        }

        var trigger = encounterTable != null ? encounterTable.BattleTrigger : BattleTrigger.LongGrass;
        if(attemptStealthCapture && stealthCaptureProfile != null) {
            EncounterSystem.TryStealthCapture(player, pokemon, sourceType, encounterTable, trigger, stealthCaptureProfile, this, out _);
            return;
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
