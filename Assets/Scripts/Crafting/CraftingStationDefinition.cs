using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Crafting/Crafting Station Definition")]
public class CraftingStationDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this crafting station type. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this station type.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Free-form tags used by recipe filters, vendors and future UI.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Recipe Rules")]
    [Tooltip("If enabled, this station can craft any recipe unless it is blocked below.")]
    [SerializeField] bool allowAllRecipes = true;
    [Tooltip("Specific recipes this station can craft when allow all recipes is disabled.")]
    [SerializeField] List<RecipeDefinition> allowedRecipes = new List<RecipeDefinition>();
    [Tooltip("Recipe tags this station can craft when allow all recipes is disabled.")]
    [SerializeField] List<string> allowedRecipeTags = new List<string>();
    [Tooltip("Specific recipes this station can never craft.")]
    [SerializeField] List<RecipeDefinition> blockedRecipes = new List<RecipeDefinition>();
    [Tooltip("Recipe tags this station can never craft.")]
    [SerializeField] List<string> blockedRecipeTags = new List<string>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags;
    public IReadOnlyList<RecipeDefinition> AllowedRecipes => allowedRecipes;
    public IReadOnlyList<string> AllowedRecipeTags => allowedRecipeTags;
    public IReadOnlyList<RecipeDefinition> BlockedRecipes => blockedRecipes;
    public IReadOnlyList<string> BlockedRecipeTags => blockedRecipeTags;

    public bool CanCraft(RecipeDefinition recipe) {
        if(recipe == null) {
            return false;
        }

        if(ContainsRecipe(blockedRecipes, recipe) || HasAnyRecipeTag(recipe, blockedRecipeTags)) {
            return false;
        }

        return allowAllRecipes
            || ContainsRecipe(allowedRecipes, recipe)
            || HasAnyRecipeTag(recipe, allowedRecipeTags);
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        foreach(var entry in tags) {
            if(string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }

    bool ContainsRecipe(IEnumerable<RecipeDefinition> recipes, RecipeDefinition recipe) {
        if(recipes == null || recipe == null) {
            return false;
        }

        foreach(var entry in recipes) {
            if(entry != null && entry.Id == recipe.Id) {
                return true;
            }
        }

        return false;
    }

    bool HasAnyRecipeTag(RecipeDefinition recipe, IEnumerable<string> recipeTags) {
        if(recipe == null || recipeTags == null) {
            return false;
        }

        foreach(var tag in recipeTags) {
            if(recipe.HasTag(tag)) {
                return true;
            }
        }

        return false;
    }
}
