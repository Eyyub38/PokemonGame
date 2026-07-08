using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WitnessReportTargetMode {
    ThisNPC,
    ExplicitWitnesses,
    NearbyWitnesses,
    AllLoadedNPCs
}

public class WitnessReportSource : MonoBehaviour {
    [Header("Source")]
    [Tooltip("Stable id for this witnessed event source. Empty uses the GameObject name.")]
    [SerializeField] string sourceId;
    [Tooltip("Witness report definitions applied when this source is triggered.")]
    [SerializeField] List<WitnessReportDefinition> reports = new List<WitnessReportDefinition>();
    [Tooltip("If enabled, reports are triggered automatically when this component starts.")]
    [SerializeField] bool triggerOnStart;

    [Header("Witnesses")]
    [Tooltip("How witnessing NPCs are chosen.")]
    [SerializeField] WitnessReportTargetMode targetMode = WitnessReportTargetMode.NearbyWitnesses;
    [Tooltip("Explicit NPC memory profiles used by Explicit Witnesses mode.")]
    [SerializeField] List<NPCMemoryProfile> explicitWitnesses = new List<NPCMemoryProfile>();
    [Tooltip("Search radius used by Nearby Witnesses mode.")]
    [Min(0f)]
    [SerializeField] float nearbyRadius = 5f;
    [Tooltip("If enabled, inactive NPC profiles can be selected by broad target modes.")]
    [SerializeField] bool includeInactiveWitnesses;
    [Tooltip("If enabled and no witness is found, each report is applied once without a reporter NPC.")]
    [SerializeField] bool applyWithoutWitnessWhenNoWitnesses;

    [Header("Runtime")]
    [Tooltip("If enabled, PlayerWitnessReportLog is added to the player if missing.")]
    [SerializeField] bool installLogIfMissing = true;
    [Tooltip("If enabled, successful and blocked witness reports are written to GameDebug.")]
    [SerializeField] bool logDebug;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public IReadOnlyList<WitnessReportDefinition> Reports => reports;

    void Start() {
        if(triggerOnStart) {
            Trigger(PlayerController.i);
        }
    }

    [ContextMenu("Trigger Witness Reports")]
    public void TriggerFromContextMenu() {
        Trigger(PlayerController.i);
    }

    public int Trigger(PlayerController player) {
        player ??= PlayerController.i;
        if(player == null) {
            if(logDebug) {
                GameDebug.Warning($"{SourceId} could not trigger witness reports because player is missing.", GameDebugCategory.NPC, this, "WitnessReportSource");
            }
            return 0;
        }

        if(installLogIfMissing && player.GetComponent<PlayerWitnessReportLog>() == null) {
            player.gameObject.AddComponent<PlayerWitnessReportLog>();
        }

        var witnesses = ResolveWitnesses();
        int applied = 0;
        foreach(var report in reports) {
            if(report == null) {
                continue;
            }

            if(witnesses.Count == 0 && applyWithoutWitnessWhenNoWitnesses) {
                applied += ApplyReport(player, report, null);
                continue;
            }

            foreach(var witness in witnesses) {
                applied += ApplyReport(player, report, witness);
            }
        }

        if(logDebug) {
            GameDebug.Step($"{SourceId} recorded {applied} witness report(s).", GameDebugCategory.NPC, this, "WitnessReportSource");
        }

        return applied;
    }

    public int TriggerSingle(WitnessReportDefinition report, PlayerController player = null, NPCMemoryProfile witness = null) {
        if(report == null) {
            return 0;
        }

        player ??= PlayerController.i;
        if(player == null) {
            return 0;
        }

        if(installLogIfMissing && player.GetComponent<PlayerWitnessReportLog>() == null) {
            player.gameObject.AddComponent<PlayerWitnessReportLog>();
        }

        if(witness != null) {
            return ApplyReport(player, report, witness);
        }

        var witnesses = ResolveWitnesses();
        if(witnesses.Count == 0 && applyWithoutWitnessWhenNoWitnesses) {
            return ApplyReport(player, report, null);
        }

        int applied = 0;
        foreach(var resolvedWitness in witnesses) {
            applied += ApplyReport(player, report, resolvedWitness);
        }
        return applied;
    }

    int ApplyReport(PlayerController player, WitnessReportDefinition report, NPCMemoryProfile witness) {
        bool applied = report.Apply(player, witness, SourceId, this, out string failureMessage);
        if(logDebug && !applied && !string.IsNullOrWhiteSpace(failureMessage)) {
            string witnessName = witness != null ? witness.DisplayName : "No witness";
            GameDebug.Step($"{report.DisplayName} blocked for {witnessName}: {failureMessage}", GameDebugCategory.NPC, this, "WitnessReportSource");
        }
        return applied ? 1 : 0;
    }

    List<NPCMemoryProfile> ResolveWitnesses() {
        switch(targetMode) {
            case WitnessReportTargetMode.ThisNPC:
                var profile = GetComponent<NPCMemoryProfile>();
                return profile != null ? new List<NPCMemoryProfile> { profile } : new List<NPCMemoryProfile>();
            case WitnessReportTargetMode.ExplicitWitnesses:
                return explicitWitnesses.Where(witness => witness != null).Distinct().ToList();
            case WitnessReportTargetMode.AllLoadedNPCs:
                return FindLoadedProfiles().Where(profile => profile != null).Distinct().ToList();
            default:
                return FindLoadedProfiles()
                    .Where(profile => profile != null && Vector3.Distance(transform.position, profile.transform.position) <= nearbyRadius)
                    .Distinct()
                    .ToList();
        }
    }

    IEnumerable<NPCMemoryProfile> FindLoadedProfiles() {
        return FindObjectsByType<NPCMemoryProfile>(
            includeInactiveWitnesses ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
    }

    void OnDrawGizmosSelected() {
        if(targetMode != WitnessReportTargetMode.NearbyWitnesses) {
            return;
        }

        Gizmos.color = new Color(1f, 0.75f, 0.25f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, nearbyRadius);
    }
}
