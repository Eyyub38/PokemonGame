using UnityEngine;

public enum LoyaltyProgramRequirementMode {
    HasProgram,
    HasActiveProgram,
    PointsAtLeast,
    LifetimePointsAtLeast,
    CurrentTierIs,
    UnlockedTier,
    ActiveProgramWithTag,
    PointGainCountAtLeast,
    CanJoinProgram
}

[CreateAssetMenu(menuName = "Activities/Requirements/Loyalty Program Requirement")]
public class LoyaltyProgramRequirement : ActivityRequirement {
    [Header("Target")]
    [Tooltip("How loyalty program state should be checked.")]
    [SerializeField] LoyaltyProgramRequirementMode mode = LoyaltyProgramRequirementMode.HasActiveProgram;
    [Tooltip("Specific loyalty program checked by program-specific modes.")]
    [SerializeField] LoyaltyProgramDefinition program;
    [Tooltip("Tier id checked by tier-based modes.")]
    [SerializeField] string tierId = string.Empty;
    [Tooltip("Tag checked by Active Program With Tag mode.")]
    [SerializeField] string requiredTag = string.Empty;
    [Tooltip("Point source kind checked by Point Gain Count At Least mode.")]
    [SerializeField] LoyaltyPointSourceKind sourceKind = LoyaltyPointSourceKind.Manual;
    [Tooltip("Optional source id checked by Point Gain Count At Least mode.")]
    [SerializeField] string sourceId = string.Empty;

    [Header("Threshold")]
    [Tooltip("Required point or record count for threshold-based modes.")]
    [Min(0)]
    [SerializeField] int requiredAmount = 1;
    [Tooltip("If enabled, the final result is inverted.")]
    [SerializeField] bool invertResult;

    public LoyaltyProgramRequirementMode Mode => mode;
    public LoyaltyProgramDefinition Program => program;
    public string TierId => tierId;
    public string RequiredTag => requiredTag;
    public LoyaltyPointSourceKind SourceKind => sourceKind;
    public string SourceId => sourceId;
    public int RequiredAmount => Mathf.Max(0, requiredAmount);
    public bool InvertResult => invertResult;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerLoyaltyLog>() : null;
        bool met = Evaluate(player, log);
        return invertResult ? !met : met;
    }

    bool Evaluate(PlayerController player, PlayerLoyaltyLog log) {
        switch(mode) {
            case LoyaltyProgramRequirementMode.HasProgram:
                return log != null && log.HasProgram(program);
            case LoyaltyProgramRequirementMode.PointsAtLeast:
                return log != null && log.GetPoints(program) >= RequiredAmount;
            case LoyaltyProgramRequirementMode.LifetimePointsAtLeast:
                return GetLifetimePoints(log, program) >= RequiredAmount;
            case LoyaltyProgramRequirementMode.CurrentTierIs:
                return log != null && !string.IsNullOrWhiteSpace(tierId) && log.GetCurrentTierId(program) == tierId;
            case LoyaltyProgramRequirementMode.UnlockedTier:
                return log != null && log.HasUnlockedTier(program, tierId);
            case LoyaltyProgramRequirementMode.ActiveProgramWithTag:
                return log != null && log.HasActiveProgramWithTag(requiredTag);
            case LoyaltyProgramRequirementMode.PointGainCountAtLeast:
                return log != null && log.GetPointGainCount(program, sourceKind, sourceId) >= RequiredAmount;
            case LoyaltyProgramRequirementMode.CanJoinProgram:
                return program != null && program.CanJoin(player, log, out _);
            default:
                return log != null && log.HasActiveProgram(program, out _);
        }
    }

    int GetLifetimePoints(PlayerLoyaltyLog log, LoyaltyProgramDefinition targetProgram) {
        if(log == null || targetProgram == null) {
            return 0;
        }

        foreach(var membership in log.Memberships) {
            if(membership != null && membership.programId == targetProgram.Id) {
                return Mathf.Max(0, membership.lifetimePoints);
            }
        }

        return 0;
    }
}
