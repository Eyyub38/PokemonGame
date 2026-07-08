using UnityEngine;

public enum LearnableOfferRequirementMode {
    PurchasedOffer,
    PurchaseCountAtLeast,
    PurchasedCategoryCountAtLeast,
    PurchasedOfferWithTag,
    CanPurchaseOffer
}

[CreateAssetMenu(menuName = "Activities/Requirements/Learnable Offer Requirement")]
public class LearnableOfferRequirement : ActivityRequirement {
    [Tooltip("Which learnable offer value this requirement checks.")]
    [SerializeField] LearnableOfferRequirementMode mode = LearnableOfferRequirementMode.PurchasedOffer;
    [Tooltip("Specific offer checked by offer-specific modes.")]
    [SerializeField] LearnableOfferDefinition offer;
    [Tooltip("Category checked by Purchased Category Count At Least mode.")]
    [SerializeField] LearnableOfferCategory category = LearnableOfferCategory.General;
    [Tooltip("Tag checked by Purchased Offer With Tag mode.")]
    [SerializeField] string requiredTag = string.Empty;
    [Tooltip("Optional source id filter. Empty accepts any source.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Optional shop context used by Can Purchase Offer price/access checks.")]
    [SerializeField] ShopCatalog shopContext;
    [Tooltip("Required count used by count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected offer condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerLearnableOfferLog>() : null;
        bool result = mode switch {
            LearnableOfferRequirementMode.PurchaseCountAtLeast => log != null && log.GetPurchaseCount(offer, sourceId, includeBlocked: false) >= Mathf.Max(0, requiredCount),
            LearnableOfferRequirementMode.PurchasedCategoryCountAtLeast => log != null && log.GetPurchaseCount(null, sourceId, category, includeBlocked: false) >= Mathf.Max(0, requiredCount),
            LearnableOfferRequirementMode.PurchasedOfferWithTag => log != null && log.GetPurchaseCount(null, sourceId, requiredTag: requiredTag, includeBlocked: false) >= Mathf.Max(1, requiredCount),
            LearnableOfferRequirementMode.CanPurchaseOffer => offer != null && offer.CanPurchase(player, log, sourceId, shopContext != null ? shopContext.Catalog : null, out _),
            _ => log != null && log.HasPurchased(offer, sourceId, includeBlocked: false)
        };

        return mustBeMet ? result : !result;
    }
}
