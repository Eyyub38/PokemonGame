using System.Linq;
using UnityEngine;

public enum CompanionExpeditionRequirementMode {
    ActiveCountAtLeast,
    ActiveCountAtMost,
    ReadyCountAtLeast,
    CompletedCountAtLeast,
    SuccessCountAtLeast,
    FailureCountAtMost,
    AnyCompanionBusy
}

[CreateAssetMenu(menuName = "Activities/Requirements/Companion Expedition Requirement")]
public class CompanionExpeditionRequirement : ActivityRequirement {
    [Tooltip("Expedition checked by this requirement.")]
    [SerializeField] CompanionExpeditionDefinition expedition;
    [Tooltip("Optional board/source id. Empty checks every source for the selected expedition.")]
    [SerializeField] string sourceId;
    [Tooltip("Which expedition condition this requirement checks.")]
    [SerializeField] CompanionExpeditionRequirementMode mode = CompanionExpeditionRequirementMode.CompletedCountAtLeast;
    [Tooltip("Required count depending on mode.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("If enabled, the selected condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerCompanionExpeditionLog>() : null;
        if(log == null) {
            return !mustBeMet;
        }

        bool result = mode switch {
            CompanionExpeditionRequirementMode.ActiveCountAtMost => GetActiveCount(log) <= Mathf.Max(0, requiredValue),
            CompanionExpeditionRequirementMode.ReadyCountAtLeast => log.GetReadyExpeditions(expedition, sourceId).Count >= Mathf.Max(0, requiredValue),
            CompanionExpeditionRequirementMode.SuccessCountAtLeast => log.GetCompletedCount(expedition, sourceId, true) >= Mathf.Max(0, requiredValue),
            CompanionExpeditionRequirementMode.FailureCountAtMost => log.GetCompletedCount(expedition, sourceId, false) <= Mathf.Max(0, requiredValue),
            CompanionExpeditionRequirementMode.AnyCompanionBusy => log.ActiveExpeditions.Any(),
            CompanionExpeditionRequirementMode.CompletedCountAtLeast => log.GetCompletedCount(expedition, sourceId) >= Mathf.Max(0, requiredValue),
            _ => GetActiveCount(log) >= Mathf.Max(0, requiredValue)
        };

        return mustBeMet ? result : !result;
    }

    int GetActiveCount(PlayerCompanionExpeditionLog log) {
        if(expedition == null) {
            return log.ActiveExpeditions.Count;
        }

        return log.ActiveExpeditions.Count(state => state != null
            && state.expeditionId == expedition.Id
            && (string.IsNullOrWhiteSpace(sourceId) || state.sourceId == sourceId));
    }
}
