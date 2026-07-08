using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Requirements/Shop Purchase Requirement")]
public class ShopPurchaseRequirement : ActivityRequirement {
    [Tooltip("Shop/catalog id whose purchase history is checked.")]
    [SerializeField] string shopId;
    [Tooltip("Offer id whose purchase history is checked.")]
    [SerializeField] string offerId;
    [Tooltip("Purchase period to check.")]
    [SerializeField] ShopStockLimitPeriod period = ShopStockLimitPeriod.Total;
    [Tooltip("Minimum number of bundles that must have been purchased.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the purchase count must be at least Required Count. If disabled, it must be lower.")]
    [SerializeField] bool mustHavePurchased = true;

    public override bool IsMet(PlayerController player) {
        var ledger = player != null ? player.GetComponent<PlayerShopLedger>() : null;
        int count = ledger != null ? ledger.GetPurchasedCount(shopId, offerId, period) : 0;
        bool result = count >= Mathf.Max(0, requiredCount);
        return mustHavePurchased ? result : !result;
    }
}
