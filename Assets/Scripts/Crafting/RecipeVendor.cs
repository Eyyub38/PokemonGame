using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RecipeVendor : MonoBehaviour {
    [Header("Offers")]
    [Tooltip("Recipes sold or taught by this vendor.")]
    [SerializeField] List<RecipeOffer> offers = new List<RecipeOffer>();
    [Tooltip("If enabled, already-known recipes are hidden from GetAvailableOffers.")]
    [SerializeField] bool hideKnownRecipes = true;

    public IReadOnlyList<RecipeOffer> Offers => offers;

    public List<RecipeOffer> GetAvailableOffers(PlayerController player) {
        var book = player != null ? player.GetComponent<PlayerRecipeBook>() : null;
        return offers
            .Where(o => o != null && o.recipe != null)
            .Where(o => !hideKnownRecipes || book == null || !book.KnowsRecipe(o.recipe))
            .Where(o => MeetsAccess(player, o))
            .ToList();
    }

    public bool CanBuy(PlayerController player, RecipeOffer offer, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to buy recipes.";
            return false;
        }

        if(offer == null || offer.recipe == null) {
            failureMessage = "No recipe offer selected.";
            return false;
        }

        var book = player.GetComponent<PlayerRecipeBook>();
        if(book == null) {
            failureMessage = "The player has no recipe book.";
            return false;
        }

        if(book.KnowsRecipe(offer.recipe)) {
            failureMessage = $"You already know {offer.recipe.DisplayName}.";
            return false;
        }

        if(!MeetsAccess(player, offer)) {
            failureMessage = offer.lockedMessage;
            return false;
        }

        if(offer.price > 0f && (Wallet.i == null || !Wallet.i.HasMoney(offer.price))) {
            failureMessage = $"You need {offer.price:0} money to learn {offer.recipe.DisplayName}.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryBuy(PlayerController player, RecipeOffer offer, out string failureMessage) {
        if(!CanBuy(player, offer, out failureMessage)) {
            return false;
        }

        if(offer.price > 0f) {
            Wallet.i.TakeMoney(offer.price);
        }

        player.GetComponent<PlayerRecipeBook>().Learn(offer.recipe, offer.source, true, this);
        PublishPurchaseEvent(player, offer);
        failureMessage = null;
        return true;
    }

    bool MeetsAccess(PlayerController player, RecipeOffer offer) {
        if(offer == null) {
            return false;
        }

        if(offer.requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(offer.requiredTitle) ?? false)) {
            return false;
        }

        if(offer.requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(offer.requiredFaction) ?? 0;
            if(reputation < offer.requiredReputation) {
                return false;
            }
        }

        return true;
    }

    void PublishPurchaseEvent(PlayerController player, RecipeOffer offer) {
        GameEventPublishing.PublishOptional(
            null,
            $"recipe.purchased.{offer.recipe.Id}",
            $"{offer.recipe.DisplayName} recipe purchased.",
            GameEventCategory.Crafting,
            GameEventImportance.Success,
            player != null ? player : this,
            "RecipeVendor",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("recipeId", offer.recipe.Id),
            GameEventPublishing.Value("recipeName", offer.recipe.DisplayName),
            GameEventPublishing.Value("price", offer.price),
            GameEventPublishing.Value("vendor", name));
    }
}

[System.Serializable]
public class RecipeOffer {
    [Tooltip("Recipe sold or taught by this offer.")]
    public RecipeDefinition recipe;
    [Tooltip("Money cost to learn this recipe. 0 means free.")]
    [Min(0f)]
    public float price;
    [Tooltip("Optional title, badge or permit required before this offer is available.")]
    public TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this offer.")]
    public ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    public int requiredReputation;
    [Tooltip("Message used when access requirements block this offer.")]
    public string lockedMessage = "This recipe is not available yet.";
    [Tooltip("Short source stored in PlayerRecipeBook when learned.")]
    public string source = "Vendor";
}
