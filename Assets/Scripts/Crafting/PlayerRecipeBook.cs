using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerRecipeBook : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save list of recipes learned by the player.")]
    [SerializeField] List<PlayerRecipeState> knownRecipes = new List<PlayerRecipeState>();

    public IReadOnlyList<PlayerRecipeState> KnownRecipes => knownRecipes;
    public event Action<RecipeDefinition> OnRecipeLearned;
    public event Action<string> OnRecipeForgotten;

    public bool CanUseRecipe(RecipeDefinition recipe) {
        return recipe != null && (recipe.KnownByDefault || !recipe.RequiresKnownRecipe || KnowsRecipe(recipe));
    }

    public bool KnowsRecipe(RecipeDefinition recipe) {
        return recipe != null && KnowsRecipe(recipe.Id);
    }

    public bool KnowsRecipe(string recipeId) {
        return GetState(recipeId) != null;
    }

    public bool KnowsRecipeWithTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        return knownRecipes.Any(r => r != null && r.MatchesTag(tag));
    }

    public bool KnowsRecipeCategory(RecipeCategory category) {
        return knownRecipes.Any(r => r != null && r.category == category);
    }

    public bool Learn(RecipeGrant grant, UnityEngine.Object context = null) {
        if(grant == null || grant.recipe == null) {
            return false;
        }

        return Learn(grant.recipe, grant.source, grant.refreshExisting, context);
    }

    public bool Learn(RecipeDefinition recipe, string source = null, bool refreshExisting = true, UnityEngine.Object context = null) {
        if(recipe == null) {
            return false;
        }

        int now = GetCurrentTotalHour();
        var state = GetState(recipe.Id);
        if(state != null) {
            if(!refreshExisting) {
                return false;
            }

            state.learnedAtHour = now;
            state.source = string.IsNullOrWhiteSpace(source) ? state.source : source;
            state.definition = recipe;
        } else {
            knownRecipes.Add(new PlayerRecipeState(recipe, now, source));
        }

        OnRecipeLearned?.Invoke(recipe);
        PublishRecipeEvent(recipe, "learned", context);
        return true;
    }

    public void ApplyGrants(IEnumerable<RecipeGrant> grants, UnityEngine.Object context = null) {
        if(grants == null) {
            return;
        }

        foreach(var grant in grants) {
            Learn(grant, context);
        }
    }

    public bool Forget(RecipeDefinition recipe, UnityEngine.Object context = null) {
        return recipe != null && Forget(recipe.Id, context);
    }

    public bool Forget(string recipeId, UnityEngine.Object context = null) {
        var state = GetState(recipeId);
        if(state == null) {
            return false;
        }

        knownRecipes.Remove(state);
        OnRecipeForgotten?.Invoke(recipeId);
        PublishRecipeEvent(state.ToDefinition(), "forgotten", context, state);
        return true;
    }

    public PlayerRecipeState GetState(string recipeId) {
        if(string.IsNullOrWhiteSpace(recipeId)) {
            return null;
        }

        return knownRecipes.FirstOrDefault(r => r != null && r.recipeId == recipeId);
    }

    int GetCurrentTotalHour() {
        if(TimeSystem.i == null) {
            return 0;
        }

        return Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour);
    }

    void PublishRecipeEvent(RecipeDefinition recipe, string phase, UnityEngine.Object context, PlayerRecipeState state = null) {
        string recipeId = recipe != null ? recipe.Id : state?.recipeId;
        string recipeName = recipe != null ? recipe.DisplayName : state?.displayName ?? recipeId;
        var category = recipe != null ? recipe.Category : state != null ? state.category : RecipeCategory.General;

        GameEventPublishing.PublishOptional(
            null,
            $"recipe.{phase}.{recipeId}",
            $"{recipeName} recipe {phase}.",
            GameEventCategory.Crafting,
            phase == "learned" ? GameEventImportance.Success : GameEventImportance.Info,
            context != null ? context : this,
            "PlayerRecipeBook",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("recipeId", recipeId),
            GameEventPublishing.Value("recipeName", recipeName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("phase", phase));
    }

    public object CaptureState() {
        return knownRecipes.Select(r => r.ToSaveData()).ToList();
    }

    public void RestoreState(object state) {
        var saveData = state as List<PlayerRecipeSaveData>;
        knownRecipes = saveData?.Select(s => new PlayerRecipeState(s)).ToList() ?? new List<PlayerRecipeState>();
    }
}

[Serializable]
public class PlayerRecipeState {
    [Tooltip("Saved recipe id.")]
    public string recipeId;
    [Tooltip("Saved recipe display name for fallback/debug output.")]
    public string displayName;
    [Tooltip("Saved recipe category for fallback/debug output.")]
    public RecipeCategory category;
    [Tooltip("In-game total hour when this recipe was learned.")]
    public int learnedAtHour;
    [Tooltip("Short source/reason for this recipe unlock.")]
    public string source;
    [Tooltip("Runtime definition reference. Not required for save restore, but useful while active.")]
    public RecipeDefinition definition;

    public PlayerRecipeState() {
    }

    public PlayerRecipeState(RecipeDefinition recipe, int learnedAtHour, string source) {
        definition = recipe;
        recipeId = recipe.Id;
        displayName = recipe.DisplayName;
        category = recipe.Category;
        this.learnedAtHour = learnedAtHour;
        this.source = source;
    }

    public PlayerRecipeState(PlayerRecipeSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recipeId = saveData.recipeId;
        displayName = saveData.displayName;
        category = saveData.category;
        learnedAtHour = saveData.learnedAtHour;
        source = saveData.source;
        definition = ResolveDefinition(recipeId);
    }

    public bool MatchesTag(string tag) {
        return ToDefinition() != null && definition.HasTag(tag);
    }

    public RecipeDefinition ToDefinition() {
        if(definition == null) {
            definition = ResolveDefinition(recipeId);
        }
        return definition;
    }

    public PlayerRecipeSaveData ToSaveData() {
        return new PlayerRecipeSaveData {
            recipeId = recipeId,
            displayName = displayName,
            category = category,
            learnedAtHour = learnedAtHour,
            source = source
        };
    }

    static RecipeDefinition ResolveDefinition(string recipeId) {
        if(string.IsNullOrWhiteSpace(recipeId)) {
            return null;
        }

        return Resources.LoadAll<RecipeDefinition>("").FirstOrDefault(r => r != null && r.Id == recipeId);
    }
}

[Serializable]
public class PlayerRecipeSaveData {
    public string recipeId;
    public string displayName;
    public RecipeCategory category;
    public int learnedAtHour;
    public string source;
}
