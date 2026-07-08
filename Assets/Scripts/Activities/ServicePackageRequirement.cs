using UnityEngine;

public enum ServicePackageRequirementMode {
    UsedPackage,
    PackageUseCountAtLeast,
    UsedPackageToday,
    NotUsedPackageToday,
    HoursSinceLastPackageUseAtLeast,
    CategoryUseCountAtLeast,
    CategoryUseCountAtMost,
    UsedPackageWithTag,
    CanUsePackage
}

[CreateAssetMenu(menuName = "Activities/Requirements/Service Package Requirement")]
public class ServicePackageRequirement : ActivityRequirement {
    [Header("Target")]
    [Tooltip("How service package history or availability should be checked.")]
    [SerializeField] ServicePackageRequirementMode mode = ServicePackageRequirementMode.UsedPackage;
    [Tooltip("Specific package checked by package-specific modes.")]
    [SerializeField] ServicePackageDefinition package;
    [Tooltip("Source id filter. Empty means any source.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Service package category checked by category-based modes.")]
    [SerializeField] ServicePackageCategory category = ServicePackageCategory.General;
    [Tooltip("Tag checked by Used Package With Tag mode.")]
    [SerializeField] string requiredTag = string.Empty;
    [Tooltip("Optional shop context used by Can Use Package price/access checks.")]
    [SerializeField] ShopCatalog shopContext;

    [Header("Threshold")]
    [Tooltip("Required use count for count-based modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("Required in-game hours since last use for Hours Since Last Package Use At Least.")]
    [Min(0)]
    [SerializeField] int requiredHours;
    [Tooltip("If enabled, blocked package attempts also count.")]
    [SerializeField] bool includeBlockedAttempts;
    [Tooltip("If enabled, the final result is inverted.")]
    [SerializeField] bool invertResult;

    public ServicePackageRequirementMode Mode => mode;
    public ServicePackageDefinition Package => package;
    public string SourceId => sourceId;
    public ServicePackageCategory Category => category;
    public string RequiredTag => requiredTag;
    public ShopCatalog ShopContext => shopContext;
    public int RequiredCount => Mathf.Max(0, requiredCount);
    public int RequiredHours => Mathf.Max(0, requiredHours);
    public bool IncludeBlockedAttempts => includeBlockedAttempts;
    public bool InvertResult => invertResult;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerServicePackageLog>() : null;
        bool met = Evaluate(player, log);
        return invertResult ? !met : met;
    }

    bool Evaluate(PlayerController player, PlayerServicePackageLog log) {
        switch(mode) {
            case ServicePackageRequirementMode.PackageUseCountAtLeast:
                return log != null && log.GetUseCount(package, sourceId, includeBlockedAttempts) >= RequiredCount;
            case ServicePackageRequirementMode.UsedPackageToday:
                return log != null && log.GetTodayUseCount(package, sourceId, includeBlockedAttempts) > 0;
            case ServicePackageRequirementMode.NotUsedPackageToday:
                return log == null || log.GetTodayUseCount(package, sourceId, includeBlockedAttempts) == 0;
            case ServicePackageRequirementMode.HoursSinceLastPackageUseAtLeast:
                if(log == null) {
                    return false;
                }
                int hours = log.GetHoursSinceLastUse(package, sourceId, includeBlockedAttempts);
                return hours >= 0 && hours >= RequiredHours;
            case ServicePackageRequirementMode.CategoryUseCountAtLeast:
                return log != null && log.GetCategoryUseCount(category, includeBlockedAttempts) >= RequiredCount;
            case ServicePackageRequirementMode.CategoryUseCountAtMost:
                return log != null && log.GetCategoryUseCount(category, includeBlockedAttempts) <= RequiredCount;
            case ServicePackageRequirementMode.UsedPackageWithTag:
                return log != null && log.GetTaggedUseCount(requiredTag, sourceId, includeBlockedAttempts) >= Mathf.Max(1, RequiredCount);
            case ServicePackageRequirementMode.CanUsePackage:
                return package != null
                    && package.CanUse(player, log, sourceId, shopContext != null ? shopContext.Catalog : null, out _);
            default:
                return log != null && log.HasUsed(package, sourceId, includeBlockedAttempts);
        }
    }
}
