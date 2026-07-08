using System.Collections;
using UnityEngine;

public class RoamingPokemonEncounter : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("Pokemon")]
    [Tooltip("Exact Pokemon species for this roaming encounter. If empty, Encounter Table is rolled.")]
    [SerializeField] PokemonBase pokemon;
    [Tooltip("Minimum level used when Exact Pokemon is assigned.")]
    [Min(1)]
    [SerializeField] int minLevel = 2;
    [Tooltip("Maximum level used when Exact Pokemon is assigned.")]
    [Min(1)]
    [SerializeField] int maxLevel = 4;
    [Tooltip("Optional encounter table used when Exact Pokemon is empty.")]
    [SerializeField] EncounterTableDefinition encounterTable;

    [Header("Behavior")]
    [Tooltip("If enabled, touching this roaming Pokemon starts the encounter.")]
    [SerializeField] bool startOnTouch = true;
    [Tooltip("If enabled, interacting with this roaming Pokemon starts the encounter.")]
    [SerializeField] bool startOnInteract = true;
    [Tooltip("If enabled, this trigger can fire repeatedly. Usually disabled for visible roaming Pokemon.")]
    [SerializeField] bool triggerRepeatedly;
    [Tooltip("Battle trigger used for battle background/audio when no encounter table is assigned.")]
    [SerializeField] BattleTrigger battleTrigger = BattleTrigger.LongGrass;

    [Header("Stealth Capture")]
    [Tooltip("Optional stealth capture profile for interaction attempts.")]
    [SerializeField] StealthCaptureProfileDefinition stealthCaptureProfile;
    [Tooltip("If enabled, interaction tries stealth capture before starting battle.")]
    [SerializeField] bool attemptStealthCaptureOnInteract = true;
    [Tooltip("If enabled, this object is disabled after a successful stealth capture.")]
    [SerializeField] bool disableOnStealthCapture = true;

    [Header("Movement")]
    [Tooltip("If enabled, this Pokemon wanders around its starting point.")]
    [SerializeField] bool enableRandomWander;
    [Tooltip("Maximum distance from the starting point used by random wander.")]
    [Min(0f)]
    [SerializeField] float wanderRadius = 2f;
    [Tooltip("Movement speed used by random wander.")]
    [Min(0f)]
    [SerializeField] float wanderSpeed = 1.5f;
    [Tooltip("Seconds to wait between random wander targets.")]
    [Min(0f)]
    [SerializeField] float wanderWaitSeconds = 1f;

    Vector3 startPosition;
    Coroutine wanderRoutine;

    public bool TriggerRepeatedly => triggerRepeatedly;

    void OnEnable() {
        startPosition = transform.position;
        if(enableRandomWander) {
            wanderRoutine = StartCoroutine(WanderRoutine());
        }
    }

    void OnDisable() {
        if(wanderRoutine != null) {
            StopCoroutine(wanderRoutine);
            wanderRoutine = null;
        }
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(startOnTouch) {
            StartEncounter(player, useStealthCapture: false);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(!startOnInteract) {
            yield break;
        }

        var player = initiator != null ? initiator.GetComponent<PlayerController>() : null;
        var result = StartEncounter(player, attemptStealthCaptureOnInteract);
        if(result.captureResult != null && !string.IsNullOrWhiteSpace(result.captureResult.message) && DialogManager.i != null) {
            yield return DialogManager.i.ShowDialogText(result.captureResult.message);
        }

        if(disableOnStealthCapture && result.startResult == EncounterStartResult.Captured) {
            gameObject.SetActive(false);
        }
    }

    (EncounterStartResult startResult, EncounterCaptureResult captureResult) StartEncounter(PlayerController player, bool useStealthCapture) {
        if(player == null) {
            return (EncounterStartResult.Blocked, null);
        }

        Pokemon encounterPokemon = CreatePokemon(player);
        if(encounterPokemon == null) {
            return (EncounterStartResult.NoEncounter, null);
        }

        var trigger = encounterTable != null ? encounterTable.BattleTrigger : battleTrigger;
        if(useStealthCapture && stealthCaptureProfile != null) {
            var startResult = EncounterSystem.TryStealthCapture(
                player,
                encounterPokemon,
                EncounterSourceType.Roaming,
                encounterTable,
                trigger,
                stealthCaptureProfile,
                this,
                out var captureResult);
            return (startResult, captureResult);
        }

        return (EncounterSystem.StartBattle(player, encounterPokemon, EncounterSourceType.Roaming, encounterTable, trigger, this), null);
    }

    Pokemon CreatePokemon(PlayerController player) {
        if(pokemon != null) {
            return new Pokemon(pokemon, Random.Range(Mathf.Max(1, minLevel), Mathf.Max(minLevel, maxLevel) + 1));
        }

        if(encounterTable != null && encounterTable.RollPokemon(player, out var rolledPokemon, out _)) {
            return rolledPokemon;
        }

        return null;
    }

    IEnumerator WanderRoutine() {
        while(enabled) {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector3 target = startPosition + new Vector3(offset.x, offset.y, 0f);

            while(enabled && Vector3.Distance(transform.position, target) > 0.02f) {
                transform.position = Vector3.MoveTowards(transform.position, target, wanderSpeed * Time.deltaTime);
                yield return null;
            }

            if(wanderWaitSeconds > 0f) {
                yield return new WaitForSeconds(wanderWaitSeconds);
            } else {
                yield return null;
            }
        }
    }
}
