using UnityEngine;

public enum ServiceRequirementMode {
    ServiceUsedAtLeast,
    ServiceUsedAtMost,
    ServiceUsedToday,
    ServiceNotUsedToday,
    HoursSinceLastServiceUseAtLeast,
    CategoryUsedAtLeast,
    CategoryUsedAtMost,
    AnyServiceUsedAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/Service Requirement")]
public class ServiceRequirement : ActivityRequirement {
    [Header("Target")]
    [Tooltip("How service history should be checked.")]
    [SerializeField] ServiceRequirementMode mode = ServiceRequirementMode.ServiceUsedAtLeast;
    [Tooltip("Specific service checked by service-based modes.")]
    [SerializeField] ServiceDefinition service;
    [Tooltip("Provider/source id filter. Empty means any provider.")]
    [SerializeField] string providerId = string.Empty;
    [Tooltip("Service category checked by category-based modes.")]
    [SerializeField] PlayerServiceCategory category = PlayerServiceCategory.General;

    [Header("Threshold")]
    [Tooltip("Required use count for count-based modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("Required in-game hours since last use for Hours Since Last Service Use At Least.")]
    [Min(0)]
    [SerializeField] int requiredHours;
    [Tooltip("If enabled, blocked service attempts also count.")]
    [SerializeField] bool includeBlockedAttempts;
    [Tooltip("If enabled, the final result is inverted.")]
    [SerializeField] bool invertResult;

    public ServiceRequirementMode Mode => mode;
    public ServiceDefinition Service => service;
    public string ProviderId => providerId;
    public PlayerServiceCategory Category => category;
    public int RequiredCount => Mathf.Max(0, requiredCount);
    public int RequiredHours => Mathf.Max(0, requiredHours);
    public bool IncludeBlockedAttempts => includeBlockedAttempts;
    public bool InvertResult => invertResult;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerServiceLog>() : null;
        bool met = log != null && Evaluate(log);
        return invertResult ? !met : met;
    }

    bool Evaluate(PlayerServiceLog log) {
        switch(mode) {
            case ServiceRequirementMode.ServiceUsedAtLeast:
                return service != null && log.GetUseCount(service, providerId, includeBlockedAttempts) >= RequiredCount;
            case ServiceRequirementMode.ServiceUsedAtMost:
                return service != null && log.GetUseCount(service, providerId, includeBlockedAttempts) <= RequiredCount;
            case ServiceRequirementMode.ServiceUsedToday:
                return service != null && log.GetTodayUseCount(service, providerId, includeBlockedAttempts) > 0;
            case ServiceRequirementMode.ServiceNotUsedToday:
                return service != null && log.GetTodayUseCount(service, providerId, includeBlockedAttempts) == 0;
            case ServiceRequirementMode.HoursSinceLastServiceUseAtLeast:
                if(service == null) {
                    return false;
                }
                int hours = log.GetHoursSinceLastUse(service, providerId, includeBlockedAttempts);
                return hours >= 0 && hours >= RequiredHours;
            case ServiceRequirementMode.CategoryUsedAtLeast:
                return log.GetCategoryUseCount(category, includeBlockedAttempts) >= RequiredCount;
            case ServiceRequirementMode.CategoryUsedAtMost:
                return log.GetCategoryUseCount(category, includeBlockedAttempts) <= RequiredCount;
            case ServiceRequirementMode.AnyServiceUsedAtLeast:
                return log.Records != null && GetAnyServiceUseCount(log) >= RequiredCount;
            default:
                return false;
        }
    }

    int GetAnyServiceUseCount(PlayerServiceLog log) {
        int count = 0;
        foreach(var record in log.Records) {
            if(record != null && (includeBlockedAttempts || !record.blocked)) {
                count++;
            }
        }
        return count;
    }
}
