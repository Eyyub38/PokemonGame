using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum LearnableOfferSourceAction {
    None,
    PurchaseFirstAvailable,
    PurchaseAllAvailable
}

public class LearnableOfferSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Offers")]
    [Tooltip("Offers sold or taught by this source.")]
    [SerializeField] List<LearnableOfferDefinition> offers = new List<LearnableOfferDefinition>();
    [Tooltip("Optional shop context used for price multipliers, sponsor discounts and source identity.")]
    [SerializeField] ShopCatalog shopContext;
    [Tooltip("If enabled, already-blocked or already-owned offers are hidden from GetAvailableOffers.")]
    [SerializeField] bool hideUnavailableOffers = true;
    [Tooltip("Short source id written into offer logs. Empty uses shop id or GameObject name.")]
    [SerializeField] string sourceId = "learnable-offer-source";

    [Header("Trigger")]
    [Tooltip("Action applied when player trigger calls this source. UI can ignore this and call TryPurchase directly.")]
    [SerializeField] LearnableOfferSourceAction triggerAction = LearnableOfferSourceAction.None;
    [Tooltip("If enabled, player trigger applies Trigger Action.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Debug")]
    [Tooltip("If enabled, blocked purchases are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful purchases are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public IReadOnlyList<LearnableOfferDefinition> Offers => offers;
    public ShopCatalog ShopContext => shopContext;
    public LearnableOfferSourceAction TriggerAction => triggerAction;

    public void OnPlayerTriggered(PlayerController player) {
        if(!applyOnPlayerTrigger || triggerAction == LearnableOfferSourceAction.None) {
            return;
        }

        if(triggerAction == LearnableOfferSourceAction.PurchaseAllAvailable) {
            TryPurchaseAll(player, out _);
            return;
        }

        TryPurchaseFirstAvailable(player, out _);
    }

    public List<LearnableOfferDefinition> GetAvailableOffers(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerLearnableOfferLog>() : null;
        var catalog = shopContext != null ? shopContext.Catalog : null;
        return (offers ?? new List<LearnableOfferDefinition>())
            .Where(offer => offer != null)
            .Where(offer => !hideUnavailableOffers || offer.CanPurchase(player, log, ResolveSourceId(), catalog, out _))
            .OrderBy(offer => offer.Category)
            .ThenBy(offer => offer.DisplayName)
            .ToList();
    }

    public bool TryPurchase(PlayerController player, LearnableOfferDefinition offer, out LearnableOfferPurchaseResult result, out string failureMessage) {
        result = null;
        if(player == null) {
            failureMessage = "A player is required to purchase learnable offers.";
            LogBlocked(failureMessage, player);
            return false;
        }

        if(offer == null) {
            failureMessage = "No learnable offer selected.";
            LogBlocked(failureMessage, player);
            return false;
        }

        if(offers == null || !offers.Contains(offer)) {
            failureMessage = $"{offer.DisplayName} is not sold by this source.";
            LogBlocked(failureMessage, player);
            return false;
        }

        var catalog = shopContext != null ? shopContext.Catalog : null;
        result = offer.Purchase(player, ResolveSourceId(), catalog, this);
        if(result == null || result.blocked) {
            failureMessage = result != null ? result.failureMessage : "Purchase failed.";
            LogBlocked(failureMessage, player);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{offer.DisplayName} purchased.", GameDebugCategory.Shop, this, "LearnableOfferSource");
        }

        failureMessage = null;
        return true;
    }

    public bool TryPurchaseFirstAvailable(PlayerController player, out string failureMessage) {
        var offer = GetAvailableOffers(player).FirstOrDefault();
        if(offer == null) {
            failureMessage = "No available learnable offers.";
            LogBlocked(failureMessage, player);
            return false;
        }

        return TryPurchase(player, offer, out _, out failureMessage);
    }

    public int TryPurchaseAll(PlayerController player, out string failureMessage) {
        failureMessage = null;
        int purchased = 0;
        foreach(var offer in GetAvailableOffers(player).ToList()) {
            if(TryPurchase(player, offer, out _, out failureMessage)) {
                purchased++;
            }
        }

        if(purchased == 0 && string.IsNullOrWhiteSpace(failureMessage)) {
            failureMessage = "No learnable offers were purchased.";
        }

        return purchased;
    }

    string ResolveSourceId() {
        if(!string.IsNullOrWhiteSpace(sourceId)) {
            return sourceId;
        }

        if(shopContext != null) {
            return $"shop:{shopContext.ShopId}";
        }

        return name;
    }

    void LogBlocked(string failureMessage, PlayerController player) {
        if(!logBlockedAttempts) {
            return;
        }

        GameDebug.Warning(failureMessage, GameDebugCategory.Shop, player != null ? player : this, "LearnableOfferSource");
    }
}
