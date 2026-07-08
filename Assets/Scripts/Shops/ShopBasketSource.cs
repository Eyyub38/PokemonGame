using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShopBasketSourceAction {
    BeginBasket,
    AddPresetOffers,
    BeginAndAddPresetOffers,
    CheckoutBasket,
    ClearBasket
}

public class ShopBasketSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Basket")]
    [Tooltip("Shop whose catalog, prices and checkout rules this source uses.")]
    [SerializeField] ShopCatalog shop;
    [Tooltip("Action applied when this source is triggered.")]
    [SerializeField] ShopBasketSourceAction action = ShopBasketSourceAction.BeginBasket;
    [Tooltip("Short source id written into basket logs. Empty uses this GameObject name.")]
    [SerializeField] string sourceId = "shop-basket-source";
    [Tooltip("If enabled, Begin Basket clears any existing basket for this shop.")]
    [SerializeField] bool clearExistingOnBegin;
    [Tooltip("Optional payment rule used when Action is Checkout Basket. Empty uses default money checkout.")]
    [SerializeField] ShopPaymentRuleDefinition checkoutPaymentRule;

    [Header("Preset Offers")]
    [Tooltip("Offer ids and bundle counts added by preset actions. Useful for shelf objects, sample bundles or quick-buy counters.")]
    [SerializeField] List<ShopBasketPresetLine> presetOffers = new List<ShopBasketPresetLine>();

    [Header("Trigger")]
    [Tooltip("If enabled, player trigger applies the selected basket action.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Debug")]
    [Tooltip("If enabled, blocked basket actions are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful basket actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public ShopCatalog Shop => shop;
    public ShopBasketSourceAction Action => action;
    public ShopPaymentRuleDefinition CheckoutPaymentRule => checkoutPaymentRule;
    public IReadOnlyList<ShopBasketPresetLine> PresetOffers => presetOffers;

    public void OnPlayerTriggered(PlayerController player) {
        if(!applyOnPlayerTrigger) {
            return;
        }

        TryApply(player, out _);
    }

    public bool TryApply(PlayerController player, out string failureMessage) {
        failureMessage = null;
        if(player == null) {
            failureMessage = "A player is required for shop basket actions.";
            LogBlocked(failureMessage, player);
            return false;
        }

        if(shop == null) {
            failureMessage = "Shop basket source has no shop assigned.";
            LogBlocked(failureMessage, player);
            return false;
        }

        var log = player.GetComponent<PlayerShopBasketLog>() ?? player.gameObject.AddComponent<PlayerShopBasketLog>();
        bool result = action switch {
            ShopBasketSourceAction.AddPresetOffers => AddPresetOffers(log, player, beginFirst: false, out failureMessage),
            ShopBasketSourceAction.BeginAndAddPresetOffers => AddPresetOffers(log, player, beginFirst: true, out failureMessage),
            ShopBasketSourceAction.CheckoutBasket => log.TryCheckout(shop, ResolveSourceId(), checkoutPaymentRule, out _, out failureMessage),
            ShopBasketSourceAction.ClearBasket => log.ClearBasket(ResolveSourceId()),
            _ => log.BeginBasket(shop, clearExistingOnBegin, ResolveSourceId())
        };

        if(!result) {
            if(string.IsNullOrWhiteSpace(failureMessage)) {
                failureMessage = "Shop basket action did not change anything.";
            }
            LogBlocked(failureMessage, player);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"Shop basket action applied: {action}.", GameDebugCategory.Shop, this, "ShopBasketSource");
        }

        return true;
    }

    bool AddPresetOffers(PlayerShopBasketLog log, PlayerController player, bool beginFirst, out string failureMessage) {
        failureMessage = null;
        if(log == null) {
            failureMessage = "Player is missing PlayerShopBasketLog.";
            return false;
        }

        if(beginFirst && !log.BeginBasket(shop, clearExistingOnBegin, ResolveSourceId())) {
            failureMessage = "Could not begin basket.";
            return false;
        }

        if(presetOffers == null || presetOffers.Count == 0) {
            failureMessage = "No preset offers are assigned.";
            return false;
        }

        bool changed = false;
        foreach(var preset in presetOffers) {
            if(preset == null || string.IsNullOrWhiteSpace(preset.offerId)) {
                failureMessage = "Preset offer has an empty offer id.";
                continue;
            }

            var entry = shop.FindOffer(preset.offerId);
            if(entry == null) {
                failureMessage = $"Shop does not contain offer '{preset.offerId}'.";
                continue;
            }

            if(log.AddOffer(shop, entry, preset.Bundles, ResolveSourceId(), out failureMessage)) {
                changed = true;
            }
        }

        return changed;
    }

    string ResolveSourceId() {
        return string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    }

    void LogBlocked(string failureMessage, PlayerController player) {
        if(!logBlockedAttempts) {
            return;
        }

        GameDebug.Warning(failureMessage, GameDebugCategory.Shop, player != null ? player : this, "ShopBasketSource");
    }
}

[Serializable]
public class ShopBasketPresetLine {
    [Tooltip("Offer id from the assigned shop catalog.")]
    public string offerId;
    [Tooltip("Number of purchase bundles to add.")]
    [Min(1)]
    public int bundles = 1;

    public int Bundles => Mathf.Max(1, bundles);
}
