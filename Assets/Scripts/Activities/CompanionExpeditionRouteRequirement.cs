using System.Linq;
using UnityEngine;

public enum CompanionExpeditionRouteRequirementMode {
    ActiveCountAtLeast,
    ActiveCountAtMost,
    CompletedCountAtLeast,
    SuccessCountAtLeast,
    FailureCountAtMost,
    CurrentStageAtLeast,
    AnyRouteActive
}

[CreateAssetMenu(menuName = "Activities/Requirements/Companion Expedition Route Requirement")]
public class CompanionExpeditionRouteRequirement : ActivityRequirement {
    [Tooltip("Route checked by this requirement.")]
    [SerializeField] CompanionExpeditionRouteDefinition route;
    [Tooltip("Optional board/source id. Empty checks every source for the selected route.")]
    [SerializeField] string sourceId;
    [Tooltip("Which route condition this requirement checks.")]
    [SerializeField] CompanionExpeditionRouteRequirementMode mode = CompanionExpeditionRouteRequirementMode.CompletedCountAtLeast;
    [Tooltip("Required count or stage index depending on mode.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("If enabled, the selected condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerCompanionExpeditionRouteLog>() : null;
        if(log == null) {
            return !mustBeMet;
        }

        bool result = mode switch {
            CompanionExpeditionRouteRequirementMode.ActiveCountAtMost => GetActiveCount(log) <= Mathf.Max(0, requiredValue),
            CompanionExpeditionRouteRequirementMode.SuccessCountAtLeast => log.GetCompletedCount(route, sourceId, true) >= Mathf.Max(0, requiredValue),
            CompanionExpeditionRouteRequirementMode.FailureCountAtMost => log.GetCompletedCount(route, sourceId, false) <= Mathf.Max(0, requiredValue),
            CompanionExpeditionRouteRequirementMode.CurrentStageAtLeast => HasCurrentStageAtLeast(log),
            CompanionExpeditionRouteRequirementMode.AnyRouteActive => log.ActiveRoutes.Any(),
            CompanionExpeditionRouteRequirementMode.CompletedCountAtLeast => log.GetCompletedCount(route, sourceId) >= Mathf.Max(0, requiredValue),
            _ => GetActiveCount(log) >= Mathf.Max(0, requiredValue)
        };

        return mustBeMet ? result : !result;
    }

    int GetActiveCount(PlayerCompanionExpeditionRouteLog log) {
        if(route == null) {
            return log.ActiveRoutes.Count;
        }

        return log.ActiveRoutes.Count(state => state != null
            && state.routeId == route.Id
            && (string.IsNullOrWhiteSpace(sourceId) || state.sourceId == sourceId));
    }

    bool HasCurrentStageAtLeast(PlayerCompanionExpeditionRouteLog log) {
        if(route == null) {
            return log.ActiveRoutes.Any(state => state != null && state.currentStageIndex >= Mathf.Max(0, requiredValue));
        }

        var state = log.GetActiveRoute(route, sourceId);
        return state != null && state.currentStageIndex >= Mathf.Max(0, requiredValue);
    }
}
