using UnityEngine;

public enum SponsorRequirementMode {
    HasSponsor,
    HasActiveSponsor,
    SponsorPointsAtLeast,
    AnyActiveSponsorWithTag,
    ActiveSponsorAppliesToShop,
    BenefitUseCountAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/Sponsor Requirement")]
public class SponsorRequirement : ActivityRequirement {
    [Header("Sponsor")]
    [Tooltip("Sponsor checked by sponsor-specific modes.")]
    [SerializeField] SponsorDefinition sponsor;
    [Tooltip("How this sponsor requirement is evaluated.")]
    [SerializeField] SponsorRequirementMode mode = SponsorRequirementMode.HasActiveSponsor;

    [Header("Filters")]
    [Tooltip("Tag checked by Any Active Sponsor With Tag mode.")]
    [SerializeField] string requiredTag;
    [Tooltip("Shop catalog checked by Active Sponsor Applies To Shop mode.")]
    [SerializeField] ShopCatalogDefinition shopCatalog;
    [Tooltip("Minimum count or point total required by count-based modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("Benefit type counted by Benefit Use Count At Least mode.")]
    [SerializeField] SponsorBenefitType benefitType = SponsorBenefitType.ShopBuyPrice;
    [Tooltip("Optional target id counted by Benefit Use Count At Least mode. Empty accepts any target.")]
    [SerializeField] string targetId;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerSponsorLog>() : null;
        return mode switch {
            SponsorRequirementMode.HasSponsor => log != null && log.HasSponsor(sponsor),
            SponsorRequirementMode.SponsorPointsAtLeast => log != null && log.GetSponsorPoints(sponsor) >= Mathf.Max(0, requiredCount),
            SponsorRequirementMode.AnyActiveSponsorWithTag => log != null && log.HasActiveSponsorWithTag(requiredTag),
            SponsorRequirementMode.ActiveSponsorAppliesToShop => log != null && log.TryGetBestBuyPriceSponsor(shopCatalog, null, out _, out _),
            SponsorRequirementMode.BenefitUseCountAtLeast => log != null && log.GetBenefitUseCount(sponsor, benefitType, targetId) >= Mathf.Max(0, requiredCount),
            _ => log != null && log.HasActiveSponsor(sponsor, out _)
        };
    }
}
