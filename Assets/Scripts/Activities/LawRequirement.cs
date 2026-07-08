using UnityEngine;

public enum LawRequirementMode {
    WantedScoreAtLeast,
    WantedLevelAtLeast,
    FineOwedAtLeast,
    ViolationCount,
    ViolationTagCount,
    ViolationCategoryCount,
    NoWantedScore,
    NoFineOwed
}

[CreateAssetMenu(menuName = "Activities/Requirements/Law Requirement")]
public class LawRequirement : ActivityRequirement {
    [Tooltip("Which law value this requirement checks.")]
    [SerializeField] LawRequirementMode mode = LawRequirementMode.WantedScoreAtLeast;
    [Tooltip("Specific violation checked by Violation Count mode.")]
    [SerializeField] LawViolationDefinition violation;
    [Tooltip("Optional authority faction filter.")]
    [SerializeField] ReputationFactionDefinition authorityFaction;
    [Tooltip("Optional authority id override. Empty uses Authority Faction or all authorities.")]
    [SerializeField] string authorityId;
    [Tooltip("Optional source id filter for Violation Count mode.")]
    [SerializeField] string sourceId;
    [Tooltip("Tag checked by Violation Tag Count mode.")]
    [SerializeField] string tag;
    [Tooltip("Category checked by Violation Category Count mode.")]
    [SerializeField] LawViolationCategory category = LawViolationCategory.General;
    [Tooltip("Minimum integer value required by score, level and count modes.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("Minimum fine value required by Fine Owed At Least mode.")]
    [Min(0f)]
    [SerializeField] float requiredFine = 1f;
    [Tooltip("If enabled, the selected law condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerLawLog>() : null;
        string resolvedAuthorityId = ResolveAuthorityId();
        bool result = mode switch {
            LawRequirementMode.WantedLevelAtLeast => log != null && log.GetWantedLevel(resolvedAuthorityId) >= Mathf.Max(0, requiredValue),
            LawRequirementMode.FineOwedAtLeast => log != null && log.GetFineOwed(resolvedAuthorityId) >= Mathf.Max(0f, requiredFine),
            LawRequirementMode.ViolationCount => log != null && log.GetViolationCount(violation, resolvedAuthorityId, sourceId) >= Mathf.Max(0, requiredValue),
            LawRequirementMode.ViolationTagCount => log != null && log.GetViolationCountWithTag(tag, resolvedAuthorityId) >= Mathf.Max(0, requiredValue),
            LawRequirementMode.ViolationCategoryCount => log != null && log.GetViolationCountByCategory(category, resolvedAuthorityId) >= Mathf.Max(0, requiredValue),
            LawRequirementMode.NoWantedScore => log == null || log.GetWantedScore(resolvedAuthorityId) <= 0,
            LawRequirementMode.NoFineOwed => log == null || log.GetFineOwed(resolvedAuthorityId) <= 0f,
            _ => log != null && log.GetWantedScore(resolvedAuthorityId) >= Mathf.Max(0, requiredValue)
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
