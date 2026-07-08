using System.Collections.Generic;
using UnityEngine;

public enum RecipeCategory {
    General,
    Medicine,
    Bait,
    Food,
    Tool,
    Pokeball,
    Care,
    Utility
}

[CreateAssetMenu(menuName = "Crafting/Recipe Definition")]
public class RecipeDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this recipe. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this recipe.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad recipe category used by filters and future UI.")]
    [SerializeField] RecipeCategory category = RecipeCategory.General;
    [Tooltip("Free-form tags used by stations, vendors, requirements and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Knowledge")]
    [Tooltip("If enabled, this recipe can be crafted without being learned by PlayerRecipeBook.")]
    [SerializeField] bool knownByDefault;
    [Tooltip("If enabled, PlayerRecipeBook must know this recipe before it can be crafted.")]
    [SerializeField] bool requiresKnownRecipe = true;

    [Header("Station")]
    [Tooltip("If enabled, crafting requires a CraftingStation or station definition.")]
    [SerializeField] bool requiresCraftingStation;
    [Tooltip("Optional exact station type required by this recipe.")]
    [SerializeField] CraftingStationDefinition requiredStation;

    [Header("Output")]
    [Tooltip("Item created by this recipe.")]
    [SerializeField] ItemBase outputItem;
    [Tooltip("Amount of output item created per craft.")]
    [Min(1)]
    [SerializeField] int outputCount = 1;

    [Header("Costs")]
    [Tooltip("Inventory ingredients required by this recipe.")]
    [SerializeField] List<CraftingIngredient> ingredients = new List<CraftingIngredient>();
    [Tooltip("Tool durability consumed by this recipe.")]
    [SerializeField] List<ActivityToolCost> toolCosts = new List<ActivityToolCost>();
    [Tooltip("Survival need values consumed by this recipe.")]
    [SerializeField] List<ActivityNeedCost> needCosts = new List<ActivityNeedCost>();

    [Header("Requirements")]
    [Tooltip("Optional skill required to craft this recipe.")]
    [SerializeField] PlayerSkillDefinition requiredSkill;
    [Tooltip("Minimum level of the required skill.")]
    [Min(0)]
    [SerializeField] int requiredSkillLevel;
    [Tooltip("Optional title, badge or permit required to craft this recipe.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Additional reusable requirements checked before crafting.")]
    [SerializeField] List<ActivityRequirement> extraRequirements = new List<ActivityRequirement>();

    [Header("Rewards")]
    [Tooltip("Trainer XP granted when this recipe is crafted.")]
    [Min(0)]
    [SerializeField] int trainerExperience;
    [Tooltip("Progression source used for crafting XP.")]
    [SerializeField] PlayerExperienceSource experienceSource = PlayerExperienceSource.Exploration;

    [Header("Events")]
    [Tooltip("Optional event published when this recipe is crafted. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition craftedEvent;
    [Tooltip("If enabled, craft events can appear in the notification feed.")]
    [SerializeField] bool showCraftEventsInFeed = true;
    [Tooltip("If enabled, craft events are written to the debug log.")]
    [SerializeField] bool writeCraftEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public RecipeCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public bool KnownByDefault => knownByDefault;
    public bool RequiresKnownRecipe => requiresKnownRecipe;
    public bool RequiresCraftingStation => requiresCraftingStation;
    public CraftingStationDefinition RequiredStation => requiredStation;
    public ItemBase OutputItem => outputItem;
    public int OutputCount => Mathf.Max(1, outputCount);
    public IReadOnlyList<CraftingIngredient> Ingredients => ingredients;
    public IReadOnlyList<ActivityToolCost> ToolCosts => toolCosts;
    public IReadOnlyList<ActivityNeedCost> NeedCosts => needCosts;
    public PlayerSkillDefinition RequiredSkill => requiredSkill;
    public int RequiredSkillLevel => Mathf.Max(0, requiredSkillLevel);
    public TitleDefinition RequiredTitle => requiredTitle;
    public IReadOnlyList<ActivityRequirement> ExtraRequirements => extraRequirements;
    public int TrainerExperience => Mathf.Max(0, trainerExperience);
    public PlayerExperienceSource ExperienceSource => experienceSource;
    public GameEventDefinition CraftedEvent => craftedEvent;

    public bool CanCraft(PlayerController player, CraftingStationDefinition station, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to craft this recipe.";
            return false;
        }

        if(outputItem == null) {
            failureMessage = $"{DisplayName} has no output item.";
            return false;
        }

        if(player.GetComponent<Inventory>() == null) {
            failureMessage = "The player has no inventory for crafting output.";
            return false;
        }

        if(!CanUseStation(station, out failureMessage)) {
            return false;
        }

        if(requiresKnownRecipe && !knownByDefault && !(player.GetComponent<PlayerRecipeBook>()?.KnowsRecipe(this) ?? false)) {
            failureMessage = $"You do not know the {DisplayName} recipe.";
            return false;
        }

        if(requiredSkill != null) {
            int skillLevel = player.GetComponent<PlayerProgression>()?.GetSkillLevel(requiredSkill) ?? 0;
            if(skillLevel < RequiredSkillLevel) {
                failureMessage = $"{DisplayName} requires {requiredSkill.DisplayName} level {RequiredSkillLevel}.";
                return false;
            }
        }

        if(requiredTitle != null && !(player.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = $"{DisplayName} requires {requiredTitle.DisplayName}.";
            return false;
        }

        foreach(var requirement in extraRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        if(!CanPayCosts(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryCraft(PlayerController player, CraftingStationDefinition station, out string failureMessage) {
        if(!CanCraft(player, station, out failureMessage)) {
            return false;
        }

        PayCosts(player);
        player.GetComponent<Inventory>()?.AddItem(outputItem, OutputCount);

        if(TrainerExperience > 0) {
            player.GetComponent<PlayerProgression>()?.AddExperience(TrainerExperience, experienceSource);
        }

        PublishCraftedEvent(player);
        failureMessage = null;
        return true;
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

    bool CanUseStation(CraftingStationDefinition station, out string failureMessage) {
        if(requiresCraftingStation && station == null) {
            failureMessage = $"{DisplayName} requires a crafting station.";
            return false;
        }

        if(requiredStation != null && station != requiredStation) {
            failureMessage = $"{DisplayName} requires {requiredStation.DisplayName}.";
            return false;
        }

        if(station != null && !station.CanCraft(this)) {
            failureMessage = $"{station.DisplayName} cannot craft {DisplayName}.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    bool CanPayCosts(PlayerController player, out string failureMessage) {
        var inventory = player.GetComponent<Inventory>();
        foreach(var ingredient in ingredients) {
            if(ingredient == null || ingredient.item == null || ingredient.count <= 0) {
                continue;
            }

            if(inventory == null || !inventory.HasItemEnough(ingredient.item, ingredient.count)) {
                failureMessage = $"You need {ingredient.count} {ingredient.item.Name} to craft {DisplayName}.";
                return false;
            }
        }

        var toolInventory = player.GetComponent<PlayerToolInventory>();
        foreach(var cost in toolCosts) {
            if(cost == null || cost.tool == null || cost.durabilityCost <= 0) {
                continue;
            }

            if(toolInventory == null || !toolInventory.HasTool(cost.tool, 1, cost.durabilityCost)) {
                failureMessage = $"{cost.tool.DisplayName} does not have enough durability for {DisplayName}.";
                return false;
            }
        }

        var survivalNeeds = player.GetComponent<SurvivalNeedsController>();
        foreach(var cost in needCosts) {
            if(cost == null || cost.need == null || cost.amount <= 0) {
                continue;
            }

            var need = survivalNeeds?.GetNeed(cost.need);
            if(need == null || need.CurrentValue < cost.amount) {
                failureMessage = $"You do not have enough {cost.need.DisplayName} for {DisplayName}.";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    void PayCosts(PlayerController player) {
        var inventory = player.GetComponent<Inventory>();
        foreach(var ingredient in ingredients) {
            if(ingredient != null && ingredient.item != null && ingredient.count > 0 && ingredient.consumeOnCraft) {
                inventory?.RemoveItem(ingredient.item, ingredient.count);
            }
        }

        var toolInventory = player.GetComponent<PlayerToolInventory>();
        foreach(var cost in toolCosts) {
            if(cost != null && cost.tool != null && cost.durabilityCost > 0) {
                toolInventory?.ConsumeDurability(cost.tool, cost.durabilityCost);
            }
        }

        var survivalNeeds = player.GetComponent<SurvivalNeedsController>();
        foreach(var cost in needCosts) {
            if(cost != null && cost.need != null && cost.amount > 0) {
                survivalNeeds?.ChangeNeed(cost.need, -cost.amount);
            }
        }
    }

    void PublishCraftedEvent(PlayerController player) {
        GameEventPublishing.PublishOptional(
            craftedEvent,
            $"recipe.crafted.{Id}",
            $"{DisplayName} crafted.",
            GameEventCategory.Crafting,
            GameEventImportance.Success,
            player,
            "RecipeDefinition",
            GameEventScope.Player,
            showInFeed: showCraftEventsInFeed,
            writeToDebugLog: writeCraftEventsToDebugLog,
            GameEventPublishing.Value("recipeId", Id),
            GameEventPublishing.Value("recipeName", DisplayName),
            GameEventPublishing.Value("outputItem", outputItem != null ? outputItem.Name : string.Empty),
            GameEventPublishing.Value("outputCount", OutputCount),
            GameEventPublishing.Value("category", category));
    }
}

[System.Serializable]
public class CraftingIngredient {
    [Tooltip("Inventory item required by the recipe.")]
    public ItemBase item;
    [Tooltip("Amount of this item required.")]
    [Min(1)]
    public int count = 1;
    [Tooltip("If enabled, this item is removed from inventory when crafting succeeds.")]
    public bool consumeOnCraft = true;
}
