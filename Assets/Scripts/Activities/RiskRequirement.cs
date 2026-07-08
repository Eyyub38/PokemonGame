using UnityEngine;

public enum RiskRequirementMode {
    HeatAtLeast,
    SuspicionAtLeast,
    EvidenceAtLeast,
    HeatLevelAtLeast,
    SuspicionLevelAtLeast,
    EvidenceLevelAtLeast,
    IncidentCount,
    IncidentTagCount,
    IncidentCategoryCount,
    NoHeat,
    NoSuspicion,
    NoEvidence
}

[CreateAssetMenu(menuName = "Activities/Requirements/Risk Requirement")]
public class RiskRequirement : ActivityRequirement {
    [Tooltip("Which risk value this requirement checks.")]
    [SerializeField] RiskRequirementMode mode = RiskRequirementMode.HeatAtLeast;
    [Tooltip("Specific risk incident checked by Incident Count mode.")]
    [SerializeField] RiskIncidentDefinition incident = null;
    [Tooltip("Optional authority faction filter.")]
    [SerializeField] ReputationFactionDefinition authorityFaction = null;
    [Tooltip("Optional authority id override. Empty uses Authority Faction or all authorities.")]
    [SerializeField] string authorityId = string.Empty;
    [Tooltip("Optional region filter.")]
    [SerializeField] RegionInfoDefinition region = null;
    [Tooltip("Optional source id filter for incident count mode.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Tag checked by Incident Tag Count mode.")]
    [SerializeField] string tag = string.Empty;
    [Tooltip("Category checked by Incident Category Count mode.")]
    [SerializeField] RiskIncidentCategory category = RiskIncidentCategory.General;
    [Tooltip("Minimum integer value required by score and count modes.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("Minimum risk level required by level modes.")]
    [Range(0, 4)]
    [SerializeField] int requiredLevel = 1;
    [Tooltip("If enabled, count modes only look at non-expired and non-cleared incidents.")]
    [SerializeField] bool activeIncidentsOnly = true;
    [Tooltip("If enabled, the selected risk condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerRiskLog>() : null;
        string resolvedAuthorityId = ResolveAuthorityId();
        string resolvedRegionId = region != null ? region.Id : null;

        bool result = mode switch {
            RiskRequirementMode.SuspicionAtLeast => log != null && log.GetSuspicion(resolvedAuthorityId, resolvedRegionId) >= Mathf.Max(0, requiredValue),
            RiskRequirementMode.EvidenceAtLeast => log != null && log.GetEvidence(resolvedAuthorityId, resolvedRegionId) >= Mathf.Max(0, requiredValue),
            RiskRequirementMode.HeatLevelAtLeast => log != null && log.GetHeatLevel(resolvedAuthorityId, resolvedRegionId) >= Mathf.Clamp(requiredLevel, 0, 4),
            RiskRequirementMode.SuspicionLevelAtLeast => log != null && log.GetSuspicionLevel(resolvedAuthorityId, resolvedRegionId) >= Mathf.Clamp(requiredLevel, 0, 4),
            RiskRequirementMode.EvidenceLevelAtLeast => log != null && log.GetEvidenceLevel(resolvedAuthorityId, resolvedRegionId) >= Mathf.Clamp(requiredLevel, 0, 4),
            RiskRequirementMode.IncidentCount => log != null && log.GetIncidentCount(incident, resolvedAuthorityId, resolvedRegionId, sourceId, activeIncidentsOnly) >= Mathf.Max(0, requiredValue),
            RiskRequirementMode.IncidentTagCount => log != null && log.GetIncidentCountWithTag(tag, resolvedAuthorityId, resolvedRegionId, activeIncidentsOnly) >= Mathf.Max(0, requiredValue),
            RiskRequirementMode.IncidentCategoryCount => log != null && log.GetIncidentCountByCategory(category, resolvedAuthorityId, resolvedRegionId, activeIncidentsOnly) >= Mathf.Max(0, requiredValue),
            RiskRequirementMode.NoHeat => log == null || log.GetHeat(resolvedAuthorityId, resolvedRegionId) <= 0,
            RiskRequirementMode.NoSuspicion => log == null || log.GetSuspicion(resolvedAuthorityId, resolvedRegionId) <= 0,
            RiskRequirementMode.NoEvidence => log == null || log.GetEvidence(resolvedAuthorityId, resolvedRegionId) <= 0,
            _ => log != null && log.GetHeat(resolvedAuthorityId, resolvedRegionId) >= Mathf.Max(0, requiredValue)
        };

        return mustBeMet ? result : !result;
    }

    string ResolveAuthorityId() {
        if(!string.IsNullOrWhiteSpace(authorityId)) {
            return authorityId;
        }

        return authorityFaction != null ? authorityFaction.Id : null;
    }
}
