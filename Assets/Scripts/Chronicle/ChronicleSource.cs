using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChronicleSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id used by chronicle history and consequence chains. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Chronicle entries applied by this source.")]
    [SerializeField] List<ChronicleEntryDefinition> entries = new List<ChronicleEntryDefinition>();
    [Tooltip("Optional player override. Empty uses PlayerController.i or the first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Signals")]
    [Tooltip("If enabled, entries apply once during Start.")]
    [SerializeField] bool applyOnStart;
    [Tooltip("If enabled, entries apply whenever the component enables.")]
    [SerializeField] bool applyOnEnable;
    [Tooltip("If enabled, entering this trigger applies entries.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, interacting with this object applies entries.")]
    [SerializeField] bool applyOnInteract = true;
    [Tooltip("If enabled, repeated player triggers can apply this source more than once.")]
    [SerializeField] bool triggerRepeatedly;

    [Header("Content Overrides")]
    [Tooltip("Optional title override passed to every entry applied by this source.")]
    [SerializeField] string titleOverride = string.Empty;
    [Tooltip("Optional message override passed to every entry applied by this source.")]
    [TextArea]
    [SerializeField] string messageOverride = string.Empty;

    [Header("Consequences")]
    [Tooltip("Consequence chains applied after at least one chronicle entry succeeds.")]
    [SerializeField] List<ConsequenceChainDefinition> successfulEntryChains = new List<ConsequenceChainDefinition>();
    [Tooltip("Consequence chains applied when all chronicle entry attempts are blocked or missing.")]
    [SerializeField] List<ConsequenceChainDefinition> blockedEntryChains = new List<ConsequenceChainDefinition>();

    [Header("Debug")]
    [Tooltip("If enabled, entry attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<ChronicleEntryDefinition> Entries => entries;
    public IReadOnlyList<ConsequenceChainDefinition> SuccessfulEntryChains => successfulEntryChains;
    public IReadOnlyList<ConsequenceChainDefinition> BlockedEntryChains => blockedEntryChains;
    public bool TriggerRepeatedly => triggerRepeatedly;

    void OnEnable() {
        if(applyOnEnable) {
            ApplyEntries();
        }
    }

    void Start() {
        if(applyOnStart) {
            ApplyEntries();
        }
    }

    [ContextMenu("Apply Chronicle Entries")]
    public void ApplyEntriesFromContextMenu() {
        ApplyEntries();
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(applyOnPlayerTrigger) {
            ApplyEntries(player);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(applyOnInteract) {
            var player = initiator != null ? initiator.GetComponent<PlayerController>() : ResolvePlayer();
            ApplyEntries(player);
        }

        yield break;
    }

    public List<ChronicleEntryResult> ApplyEntries() {
        return ApplyEntries(ResolvePlayer());
    }

    public List<ChronicleEntryResult> ApplyEntries(PlayerController player) {
        var results = new List<ChronicleEntryResult>();
        int successful = 0;
        int blocked = 0;

        foreach(var entry in entries) {
            if(entry == null) {
                blocked++;
                continue;
            }

            var result = entry.Apply(
                player,
                SourceId,
                DisplayName,
                this,
                titleOverride: titleOverride,
                messageOverride: messageOverride);
            results.Add(result);
            if(result != null && !result.blocked) {
                successful++;
            } else {
                blocked++;
            }
        }

        if(successful > 0) {
            ApplyConsequenceChains(player, successfulEntryChains, "recorded");
        } else {
            ApplyConsequenceChains(player, blockedEntryChains, "blocked");
        }

        WriteAttemptLog(successful, blocked);
        return results;
    }

    void ApplyConsequenceChains(PlayerController player, IEnumerable<ConsequenceChainDefinition> chains, string phase) {
        if(player == null || chains == null) {
            return;
        }

        var context = new ConsequenceChainContext {
            SourceId = $"{SourceId}:{phase}",
            SourceName = DisplayName,
            ContextObject = this
        };

        foreach(var chain in chains) {
            chain?.Apply(player, context, this);
        }
    }

    void WriteAttemptLog(int successful, int blocked) {
        if(!logAttempts) {
            return;
        }

        GameDebugLogger.Ensure().Record(
            successful > 0 ? GameDebugSeverity.Info : GameDebugSeverity.Warning,
            GameDebugCategory.Chronicle,
            $"{DisplayName} recorded {successful} chronicle entry/entries, blocked/skipped {blocked}.",
            this,
            "ChronicleSource");
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }
}
