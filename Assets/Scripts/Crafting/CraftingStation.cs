using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CraftingStation : MonoBehaviour {
    [Header("Station")]
    [Tooltip("Station definition that decides which recipes can be crafted here.")]
    [SerializeField] CraftingStationDefinition definition;
    [Tooltip("Extra recipes shown by this specific station. If a definition is assigned, it must still allow the recipe.")]
    [SerializeField] List<RecipeDefinition> additionalRecipes = new List<RecipeDefinition>();
    [Tooltip("If enabled, GetVisibleRecipes includes all Resources recipes that this station allows.")]
    [SerializeField] bool includeResourceRecipes = true;
    [Tooltip("If enabled, visible recipes are limited to recipes the player already knows or recipes known by default.")]
    [SerializeField] bool hideUnknownRecipes = true;

    public CraftingStationDefinition Definition => definition;
    public IReadOnlyList<RecipeDefinition> AdditionalRecipes => additionalRecipes;

    public bool CanCraft(PlayerController player, RecipeDefinition recipe, out string failureMessage) {
        if(recipe == null) {
            failureMessage = "No recipe selected.";
            return false;
        }

        if(!CanOffer(recipe)) {
            failureMessage = $"{definition?.DisplayName ?? name} does not offer {recipe.DisplayName}.";
            return false;
        }

        return recipe.CanCraft(player, definition, out failureMessage);
    }

    public bool TryCraft(PlayerController player, RecipeDefinition recipe, out string failureMessage) {
        if(recipe == null) {
            failureMessage = "No recipe selected.";
            return false;
        }

        if(!CanOffer(recipe)) {
            failureMessage = $"{definition?.DisplayName ?? name} does not offer {recipe.DisplayName}.";
            return false;
        }

        return recipe.TryCraft(player, definition, out failureMessage);
    }

    public List<RecipeDefinition> GetVisibleRecipes(PlayerController player) {
        var recipes = new List<RecipeDefinition>();
        if(includeResourceRecipes) {
            recipes.AddRange(Resources.LoadAll<RecipeDefinition>(""));
        }

        if(additionalRecipes != null) {
            recipes.AddRange(additionalRecipes.Where(r => r != null));
        }

        var book = player != null ? player.GetComponent<PlayerRecipeBook>() : null;
        return recipes
            .Where(r => r != null)
            .GroupBy(r => r.Id)
            .Select(g => g.First())
            .Where(CanOffer)
            .Where(r => !hideUnknownRecipes || r.KnownByDefault || !r.RequiresKnownRecipe || (book?.KnowsRecipe(r) ?? false))
            .OrderBy(r => r.Category)
            .ThenBy(r => r.DisplayName)
            .ToList();
    }

    bool CanOffer(RecipeDefinition recipe) {
        if(recipe == null) {
            return false;
        }

        if(definition != null) {
            return definition.CanCraft(recipe);
        }

        return true;
    }
}
