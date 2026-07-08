using UnityEngine;

public enum RecipeRequirementMode {
    SpecificRecipe,
    RecipeTag,
    RecipeCategory
}

[CreateAssetMenu(menuName = "Activities/Requirements/Recipe Requirement")]
public class RecipeRequirement : ActivityRequirement {
    [Tooltip("Which recipe property this requirement checks.")]
    [SerializeField] RecipeRequirementMode mode = RecipeRequirementMode.SpecificRecipe;
    [Tooltip("Specific recipe required when mode is Specific Recipe.")]
    [SerializeField] RecipeDefinition requiredRecipe;
    [Tooltip("Free-form recipe tag required when mode is Recipe Tag.")]
    [SerializeField] string requiredTag;
    [Tooltip("Recipe category required when mode is Recipe Category.")]
    [SerializeField] RecipeCategory requiredCategory = RecipeCategory.General;
    [Tooltip("If enabled, the selected recipe condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustKnow = true;

    public override bool IsMet(PlayerController player) {
        var recipeBook = player != null ? player.GetComponent<PlayerRecipeBook>() : null;
        bool result = mode switch {
            RecipeRequirementMode.RecipeTag => recipeBook != null && recipeBook.KnowsRecipeWithTag(requiredTag),
            RecipeRequirementMode.RecipeCategory => recipeBook != null && recipeBook.KnowsRecipeCategory(requiredCategory),
            _ => recipeBook != null && recipeBook.KnowsRecipe(requiredRecipe)
        };

        return mustKnow ? result : !result;
    }
}
