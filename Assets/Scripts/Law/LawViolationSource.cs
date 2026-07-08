using UnityEngine;

public enum LawViolationSourceTriggerMode {
    ManualOnly,
    ReportOnTrigger,
    ReportWhenAccessFails
}

public class LawViolationSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Source")]
    [Tooltip("Stable source id stored in PlayerLawLog. Empty uses GameObject name.")]
    [SerializeField] string sourceId;
    [Tooltip("Name shown in debug and future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName;
    [Tooltip("Violation reported by this source.")]
    [SerializeField] LawViolationDefinition violation;
    [Tooltip("Optional reporter id, such as NPC, zone, camera or system id.")]
    [SerializeField] string reporterId;

    [Header("Trigger")]
    [Tooltip("How this source behaves when the player triggers it.")]
    [SerializeField] LawViolationSourceTriggerMode triggerMode = LawViolationSourceTriggerMode.ManualOnly;
    [Tooltip("If enabled, repeated player triggers can report this violation more than once.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("If enabled, a PlayerLawLog component is added to the player when missing.")]
    [SerializeField] bool installLogIfMissing = true;
    [Tooltip("Optional access profile checked when Trigger Mode is Report When Access Fails.")]
    [SerializeField] AccessProfileDefinition accessProfile;

    [Header("Debug")]
    [Tooltip("If enabled, violation reports are written to GameDebug.")]
    [SerializeField] bool logReports;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public LawViolationDefinition Violation => violation;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(triggerMode == LawViolationSourceTriggerMode.ReportOnTrigger) {
            Report(player);
            return;
        }

        if(triggerMode == LawViolationSourceTriggerMode.ReportWhenAccessFails && accessProfile != null) {
            if(!accessProfile.CanAccess(player, out _)) {
                Report(player);
            }
        }
    }

    public PlayerLawIncident Report(PlayerController player, bool applyConsequences = true) {
        if(player == null || violation == null) {
            return null;
        }

        var log = player.GetComponent<PlayerLawLog>();
        if(log == null && installLogIfMissing) {
            log = player.gameObject.AddComponent<PlayerLawLog>();
        }

        var incident = log?.RecordViolation(violation, SourceId, reporterId, applyConsequences, this);
        if(incident != null && logReports) {
            GameDebug.Warning($"{DisplayName} reported {violation.DisplayName}.", GameDebugCategory.Law, this, "LawViolationSource");
        }

        return incident;
    }
}
