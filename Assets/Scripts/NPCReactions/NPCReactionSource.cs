using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum NPCReactionTargetMode {
    ThisNPC,
    ExplicitTargets,
    NearbyNPCs,
    AllLoadedNPCs
}

public class NPCReactionSource : MonoBehaviour {
    [Header("Source")]
    [Tooltip("Stable id for this trigger/source. Empty uses the GameObject name.")]
    [SerializeField] string sourceId;
    [Tooltip("Reaction definitions applied when this source is triggered.")]
    [SerializeField] List<NPCReactionDefinition> reactions = new List<NPCReactionDefinition>();
    [Tooltip("If enabled, reactions are triggered automatically when this component starts.")]
    [SerializeField] bool triggerOnStart;

    [Header("Targets")]
    [Tooltip("How reacting NPCs are chosen.")]
    [SerializeField] NPCReactionTargetMode targetMode = NPCReactionTargetMode.ThisNPC;
    [Tooltip("Explicit NPC memory profiles used by Explicit Targets mode.")]
    [SerializeField] List<NPCMemoryProfile> explicitTargets = new List<NPCMemoryProfile>();
    [Tooltip("Search radius used by Nearby NPCs mode.")]
    [Min(0f)]
    [SerializeField] float nearbyRadius = 4f;
    [Tooltip("If enabled, inactive NPC profiles can be selected by broad target modes.")]
    [SerializeField] bool includeInactiveTargets;
    [Tooltip("If enabled and no target is found, each reaction is applied once without an NPC target.")]
    [SerializeField] bool applyWithoutTargetWhenNoTargets;

    [Header("Runtime")]
    [Tooltip("If enabled, PlayerNPCReactionLog is added to the player if missing.")]
    [SerializeField] bool installLogIfMissing = true;
    [Tooltip("If enabled, successful and blocked reactions are written to GameDebug.")]
    [SerializeField] bool logDebug;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public IReadOnlyList<NPCReactionDefinition> Reactions => reactions;

    void Start() {
        if(triggerOnStart) {
            Trigger(PlayerController.i);
        }
    }

    [ContextMenu("Trigger NPC Reactions")]
    public void TriggerFromContextMenu() {
        Trigger(PlayerController.i);
    }

    public int Trigger(PlayerController player) {
        if(player == null) {
            player = PlayerController.i;
        }

        if(player == null) {
            if(logDebug) {
                GameDebug.Warning($"{SourceId} could not trigger NPC reactions because player is missing.", GameDebugCategory.NPC, this, "NPCReactionSource");
            }
            return 0;
        }

        if(installLogIfMissing && player.GetComponent<PlayerNPCReactionLog>() == null) {
            player.gameObject.AddComponent<PlayerNPCReactionLog>();
        }

        var targets = ResolveTargets();
        int applied = 0;
        foreach(var reaction in reactions) {
            if(reaction == null) {
                continue;
            }

            if(targets.Count == 0 && applyWithoutTargetWhenNoTargets) {
                applied += ApplyReaction(player, reaction, null);
                continue;
            }

            foreach(var target in targets) {
                applied += ApplyReaction(player, reaction, target);
            }
        }

        if(logDebug) {
            GameDebug.Step($"{SourceId} applied {applied} NPC reaction(s).", GameDebugCategory.NPC, this, "NPCReactionSource");
        }

        return applied;
    }

    public int TriggerSingle(NPCReactionDefinition reaction, PlayerController player = null, NPCMemoryProfile target = null) {
        if(reaction == null) {
            return 0;
        }

        player ??= PlayerController.i;
        if(player == null) {
            return 0;
        }

        if(installLogIfMissing && player.GetComponent<PlayerNPCReactionLog>() == null) {
            player.gameObject.AddComponent<PlayerNPCReactionLog>();
        }

        if(target != null) {
            return ApplyReaction(player, reaction, target);
        }

        var targets = ResolveTargets();
        if(targets.Count == 0 && applyWithoutTargetWhenNoTargets) {
            return ApplyReaction(player, reaction, null);
        }

        int applied = 0;
        foreach(var resolvedTarget in targets) {
            applied += ApplyReaction(player, reaction, resolvedTarget);
        }
        return applied;
    }

    int ApplyReaction(PlayerController player, NPCReactionDefinition reaction, NPCMemoryProfile target) {
        bool applied = reaction.Apply(player, target, SourceId, this, out string failureMessage);
        if(logDebug && !applied && !string.IsNullOrWhiteSpace(failureMessage)) {
            string targetName = target != null ? target.DisplayName : "No NPC";
            GameDebug.Step($"{reaction.DisplayName} blocked for {targetName}: {failureMessage}", GameDebugCategory.NPC, this, "NPCReactionSource");
        }
        return applied ? 1 : 0;
    }

    List<NPCMemoryProfile> ResolveTargets() {
        switch(targetMode) {
            case NPCReactionTargetMode.ExplicitTargets:
                return explicitTargets.Where(target => target != null).Distinct().ToList();
            case NPCReactionTargetMode.NearbyNPCs:
                return FindLoadedProfiles()
                    .Where(profile => profile != null && Vector3.Distance(transform.position, profile.transform.position) <= nearbyRadius)
                    .Distinct()
                    .ToList();
            case NPCReactionTargetMode.AllLoadedNPCs:
                return FindLoadedProfiles().Where(profile => profile != null).Distinct().ToList();
            default:
                var profile = GetComponent<NPCMemoryProfile>();
                return profile != null ? new List<NPCMemoryProfile> { profile } : new List<NPCMemoryProfile>();
        }
    }

    IEnumerable<NPCMemoryProfile> FindLoadedProfiles() {
        return FindObjectsByType<NPCMemoryProfile>(
            includeInactiveTargets ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
    }

    void OnDrawGizmosSelected() {
        if(targetMode != NPCReactionTargetMode.NearbyNPCs) {
            return;
        }

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, nearbyRadius);
    }
}
