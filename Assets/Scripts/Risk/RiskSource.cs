using UnityEngine;

public enum RiskSourceTriggerMode {
    ManualOnly,
    RecordOnTrigger,
    RecordWhenAccessFails
}

public class RiskSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Source")]
    [Tooltip("Stable source id stored in PlayerRiskLog. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown in debug and future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Risk incident recorded by this source.")]
    [SerializeField] RiskIncidentDefinition incident = null;
    [Tooltip("Optional region override for this risk record.")]
    [SerializeField] RegionInfoDefinition regionOverride = null;
    [Tooltip("Optional authority id override. Empty uses the incident's authority.")]
    [SerializeField] string authorityIdOverride = string.Empty;
    [Tooltip("Optional authority display name override.")]
    [SerializeField] string authorityNameOverride = string.Empty;
    [Tooltip("Optional reporter id, such as NPC, shop, camera, sign or zone id.")]
    [SerializeField] string reporterId = string.Empty;

    [Header("Trigger")]
    [Tooltip("How this source behaves when the player triggers it.")]
    [SerializeField] RiskSourceTriggerMode triggerMode = RiskSourceTriggerMode.ManualOnly;
    [Tooltip("If enabled, repeated player triggers can record this risk incident more than once.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("If enabled, a PlayerRiskLog component is added to the player when missing.")]
    [SerializeField] bool installLogIfMissing = true;
    [Tooltip("Optional access profile checked when Trigger Mode is Record When Access Fails.")]
    [SerializeField] AccessProfileDefinition accessProfile = null;
    [Tooltip("If enabled, linked consequences on the incident are applied when this source records risk.")]
    [SerializeField] bool applyConsequences = true;

    [Header("Debug")]
    [Tooltip("If enabled, risk records are written to GameDebug.")]
    [SerializeField] bool logRecords = false;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public RiskIncidentDefinition Incident => incident;
    public RegionInfoDefinition RegionOverride => regionOverride;
    public string AuthorityIdOverride => authorityIdOverride;
    public string AuthorityNameOverride => authorityNameOverride;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(triggerMode == RiskSourceTriggerMode.RecordOnTrigger) {
            Record(player);
            return;
        }

        if(triggerMode == RiskSourceTriggerMode.RecordWhenAccessFails && accessProfile != null) {
            if(!accessProfile.CanAccess(player, out _)) {
                Record(player);
            }
        }
    }

    public PlayerRiskIncidentRecord Record(PlayerController player) {
        if(player == null || incident == null) {
            return null;
        }

        var log = player.GetComponent<PlayerRiskLog>();
        if(log == null && installLogIfMissing) {
            log = player.gameObject.AddComponent<PlayerRiskLog>();
        }

        var record = log?.RecordIncident(
            incident,
            SourceId,
            reporterId,
            regionOverride,
            authorityIdOverride,
            authorityNameOverride,
            applyConsequences,
            this);

        if(record != null && logRecords) {
            GameDebug.Warning($"{DisplayName} recorded {incident.DisplayName} risk.", GameDebugCategory.Risk, this, "RiskSource");
        }

        return record;
    }
}
