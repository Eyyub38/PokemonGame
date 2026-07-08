using UnityEngine;

public enum ConsequenceChainRequirementMode {
    HasRun,
    NeverRun,
    RunCountAtLeast,
    SourceRunCountAtLeast,
    HoursSinceLastRunAtLeast,
    HoursSinceLastRunAtMost
}

[CreateAssetMenu(menuName = "Activities/Requirements/Consequence Chain Requirement")]
public class ConsequenceChainRequirement : ActivityRequirement {
    [Tooltip("Which consequence chain history check this requirement performs.")]
    [SerializeField] ConsequenceChainRequirementMode mode = ConsequenceChainRequirementMode.HasRun;
    [Tooltip("Consequence chain checked by this requirement.")]
    [SerializeField] ConsequenceChainDefinition chain = null;
    [Tooltip("Optional source id filter for source/count/time modes. Empty checks all sources except Source Run Count At Least.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Minimum count required by count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("Minimum/maximum in-game hours used by Hours Since Last Run modes.")]
    [Min(0)]
    [SerializeField] int requiredHours = 1;
    [Tooltip("If enabled, blocked attempts are counted too. If disabled, only successful runs count.")]
    [SerializeField] bool includeBlockedAttempts;
    [Tooltip("If enabled, the selected condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerConsequenceChainLog>() : null;
        bool result = mode switch {
            ConsequenceChainRequirementMode.NeverRun => log == null || !log.HasRun(chain, sourceId, includeBlockedAttempts),
            ConsequenceChainRequirementMode.RunCountAtLeast => log != null && log.GetRunCount(chain, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            ConsequenceChainRequirementMode.SourceRunCountAtLeast => log != null && log.GetRunCount(chain, sourceId, includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            ConsequenceChainRequirementMode.HoursSinceLastRunAtLeast => HasHoursSinceLastRun(log, atLeast: true),
            ConsequenceChainRequirementMode.HoursSinceLastRunAtMost => HasHoursSinceLastRun(log, atLeast: false),
            _ => log != null && log.HasRun(chain, sourceId, includeBlockedAttempts)
        };

        return mustBeMet ? result : !result;
    }

    bool HasHoursSinceLastRun(PlayerConsequenceChainLog log, bool atLeast) {
        if(log == null || chain == null) {
            return false;
        }

        int hours = log.GetHoursSinceLastRun(chain, sourceId, includeBlockedAttempts);
        if(hours < 0) {
            return false;
        }

        int required = Mathf.Max(0, requiredHours);
        return atLeast ? hours >= required : hours <= required;
    }
}
