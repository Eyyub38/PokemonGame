using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RadialInventoryActionKind {
    Use,
    Give,
    Equip,
    Unequip,
    Teach,
    Favorite,
    Info,
    Drop,
    Cancel,
    Custom
}

[Serializable]
public class RadialInventoryActionDefinition {
    [Tooltip("Action kind represented by this radial option.")]
    public RadialInventoryActionKind actionKind = RadialInventoryActionKind.Use;
    [Tooltip("Stable option id. Empty uses the action kind.")]
    public string optionId = string.Empty;
    [Tooltip("Label shown by the radial option tag/frame.")]
    public string label = string.Empty;
    [Tooltip("Description shown by the radial option tag/frame.")]
    [TextArea]
    public string description = string.Empty;
    [Tooltip("Icon shown inside the radial segment. Empty uses the selected item's icon when enabled.")]
    public Sprite icon;
    [Tooltip("Lower priority appears earlier around the ring.")]
    public int priority;
    [Tooltip("If enabled, this option is always shown even when it is not currently usable.")]
    public bool showWhenDisabled = true;
}

public class RadialInventoryMenuProvider : MonoBehaviour, IRadialMenuProvider {
    [Header("Inventory")]
    [Tooltip("Inventory UI used to resolve the currently selected item. Empty uses Inventory.GetInventory or the explicit inventory override.")]
    [SerializeField] InventoryUI inventoryUI;
    [Tooltip("Explicit inventory override. Empty tries to find the player inventory at runtime.")]
    [SerializeField] Inventory inventoryOverride;
    [Tooltip("Explicit item override used for shelf, reward or custom item contexts.")]
    [SerializeField] ItemBase itemOverride;
    [Tooltip("Explicit category index used when resolving items from context index. -1 uses the InventoryUI selected category.")]
    [SerializeField] int categoryIndex = -1;
    [Tooltip("If enabled, the context index is used as the selected item index when available.")]
    [SerializeField] bool preferContextIndex = true;

    [Header("Actions")]
    [Tooltip("Actions exposed for normal inventory item context.")]
    [SerializeField] List<RadialInventoryActionDefinition> actions = new List<RadialInventoryActionDefinition> {
        new RadialInventoryActionDefinition { actionKind = RadialInventoryActionKind.Use, label = "Use", priority = 0 },
        new RadialInventoryActionDefinition { actionKind = RadialInventoryActionKind.Give, label = "Give", priority = 10 },
        new RadialInventoryActionDefinition { actionKind = RadialInventoryActionKind.Equip, label = "Equip", priority = 20 },
        new RadialInventoryActionDefinition { actionKind = RadialInventoryActionKind.Teach, label = "Teach", priority = 30 },
        new RadialInventoryActionDefinition { actionKind = RadialInventoryActionKind.Info, label = "Info", priority = 80 },
        new RadialInventoryActionDefinition { actionKind = RadialInventoryActionKind.Cancel, label = "Back", priority = 100 }
    };
    [Tooltip("If enabled, Use is disabled for items that report unusable in and outside battle.")]
    [SerializeField] bool requireUsableItemForUse = true;
    [Tooltip("If enabled, Equip is only enabled for BattleHeldItem definitions.")]
    [SerializeField] bool requireHeldItemForEquip = true;
    [Tooltip("If enabled, Teach is only enabled for TM item definitions.")]
    [SerializeField] bool requireTmItemForTeach = true;
    [Tooltip("If enabled, Drop is available as a radial action. The provider only emits the event; it does not remove items itself.")]
    [SerializeField] bool allowDrop;

    [Header("Debug")]
    [Tooltip("If enabled, selected radial inventory actions are written to GameDebug.")]
    [SerializeField] bool logSelectedActions = true;

    public InventoryUI InventoryUI => inventoryUI;
    public Inventory InventoryOverride => inventoryOverride;
    public ItemBase ItemOverride => itemOverride;
    public int CategoryIndex => categoryIndex;
    public IReadOnlyList<RadialInventoryActionDefinition> Actions => actions;
    public event Action<RadialInventoryActionKind, ItemBase, int, int, RadialMenuOption> OnInventoryActionSelected;

    public IReadOnlyList<RadialMenuOption> BuildRadialOptions(RadialMenuContext context) {
        var item = ResolveItem(context, out int selectedItemIndex, out int selectedCategoryIndex);
        if(item == null) {
            return new List<RadialMenuOption> {
                BuildOption(new RadialInventoryActionDefinition { actionKind = RadialInventoryActionKind.Cancel, label = "Back", priority = 100 }, null, selectedItemIndex, selectedCategoryIndex, false, null)
            };
        }

        var result = new List<RadialMenuOption>();
        foreach(var action in actions.OrderBy(action => action != null ? action.priority : int.MaxValue)) {
            if(action == null) {
                continue;
            }

            bool disabled = IsDisabled(action.actionKind, item, out var reason);
            if(disabled && !action.showWhenDisabled) {
                continue;
            }

            result.Add(BuildOption(action, item, selectedItemIndex, selectedCategoryIndex, disabled, reason));
        }

        return result;
    }

    public void OnRadialOptionSelected(RadialMenuOption option, RadialMenuContext context) {
        var item = ResolveItem(context, out int selectedItemIndex, out int selectedCategoryIndex);
        var actionKind = ResolveActionKind(option);
        OnInventoryActionSelected?.Invoke(actionKind, item, selectedItemIndex, selectedCategoryIndex, option);

        if(logSelectedActions) {
            string itemName = item != null ? item.Name : "No Item";
            GameDebug.Step($"Inventory radial action selected: {actionKind} for {itemName}.", GameDebugCategory.UI, this, "RadialInventoryMenuProvider");
        }
    }

    public void OnRadialMenuClosed(RadialMenuContext context) {
    }

    public ItemBase ResolveItem(RadialMenuContext context, out int selectedItemIndex, out int selectedCategoryIndex) {
        selectedItemIndex = -1;
        selectedCategoryIndex = ResolveCategoryIndex();

        if(itemOverride != null) {
            return itemOverride;
        }

        if(context != null && context.payload is ItemBase contextItem) {
            selectedItemIndex = FindItemIndex(contextItem, selectedCategoryIndex);
            return contextItem;
        }

        if(preferContextIndex && context != null && context.index >= 0) {
            selectedItemIndex = context.index;
            var inventory = ResolveInventory();
            if(TryGetItem(inventory, selectedItemIndex, selectedCategoryIndex, out var indexedItem)) {
                return indexedItem;
            }
        }

        var ui = ResolveInventoryUI();
        if(ui != null) {
            selectedCategoryIndex = ui.SelectedCategory;
            var selectedItem = TryGetSelectedItem(ui);
            selectedItemIndex = FindItemIndex(selectedItem, selectedCategoryIndex);
            return selectedItem;
        }

        return null;
    }

    RadialMenuOption BuildOption(RadialInventoryActionDefinition action, ItemBase item, int itemIndex, int selectedCategoryIndex, bool disabled, string disabledReason) {
        string id = !string.IsNullOrWhiteSpace(action.optionId) ? action.optionId : action.actionKind.ToString();
        string label = !string.IsNullOrWhiteSpace(action.label) ? action.label : action.actionKind.ToString();
        return new RadialMenuOption {
            id = id,
            label = label,
            description = string.IsNullOrWhiteSpace(action.description) && item != null ? item.Description : action.description,
            icon = action.icon != null ? action.icon : item != null ? item.Icon : null,
            disabled = disabled,
            disabledReason = disabledReason,
            priority = action.priority,
            payload = item
        };
    }

    bool IsDisabled(RadialInventoryActionKind actionKind, ItemBase item, out string reason) {
        reason = null;
        if(actionKind == RadialInventoryActionKind.Cancel) {
            return false;
        }

        if(item == null) {
            reason = "No item selected.";
            return true;
        }

        switch(actionKind) {
            case RadialInventoryActionKind.Use:
                if(requireUsableItemForUse && !item.CanUseInBattle && !item.CanUseInOutsideBattle) {
                    reason = $"{item.Name} cannot be used directly.";
                    return true;
                }
                return false;
            case RadialInventoryActionKind.Equip:
            case RadialInventoryActionKind.Unequip:
                if(requireHeldItemForEquip && item is not BattleHeldItem) {
                    reason = $"{item.Name} is not a held item.";
                    return true;
                }
                return false;
            case RadialInventoryActionKind.Teach:
                if(requireTmItemForTeach && item is not TmItem) {
                    reason = $"{item.Name} is not a TM.";
                    return true;
                }
                return false;
            case RadialInventoryActionKind.Drop:
                if(!allowDrop) {
                    reason = "Dropping items is disabled for this menu.";
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    RadialInventoryActionKind ResolveActionKind(RadialMenuOption option) {
        if(option == null || string.IsNullOrWhiteSpace(option.id)) {
            return RadialInventoryActionKind.Custom;
        }

        return Enum.TryParse(option.id, true, out RadialInventoryActionKind kind) ? kind : RadialInventoryActionKind.Custom;
    }

    int ResolveCategoryIndex() {
        if(categoryIndex >= 0) {
            return categoryIndex;
        }

        var ui = ResolveInventoryUI();
        return ui != null ? ui.SelectedCategory : -1;
    }

    bool TryGetItem(Inventory inventory, int itemIndex, int selectedCategoryIndex, out ItemBase item) {
        item = null;
        if(inventory == null || itemIndex < 0 || selectedCategoryIndex < 0 || selectedCategoryIndex >= Inventory.ItemCategories.Count) {
            return false;
        }

        var slots = inventory.GetItemSlotsByCategory(selectedCategoryIndex);
        if(slots == null || itemIndex >= slots.Count) {
            return false;
        }

        item = slots[itemIndex].Item;
        return item != null;
    }

    int FindItemIndex(ItemBase item, int selectedCategoryIndex) {
        if(item == null || selectedCategoryIndex < 0 || selectedCategoryIndex >= Inventory.ItemCategories.Count) {
            return -1;
        }

        var inventory = ResolveInventory();
        var slots = inventory != null ? inventory.GetItemSlotsByCategory(selectedCategoryIndex) : null;
        return slots != null ? slots.FindIndex(slot => slot != null && slot.Item == item) : -1;
    }

    InventoryUI ResolveInventoryUI() {
        if(inventoryUI != null) {
            return inventoryUI;
        }

        inventoryUI = FindAnyObjectByType<InventoryUI>();
        return inventoryUI;
    }

    Inventory ResolveInventory() {
        if(inventoryOverride != null) {
            return inventoryOverride;
        }

        try {
            inventoryOverride = Inventory.GetInventory();
        } catch {
            inventoryOverride = FindAnyObjectByType<Inventory>();
        }

        return inventoryOverride;
    }

    ItemBase TryGetSelectedItem(InventoryUI ui) {
        if(ui == null) {
            return null;
        }

        try {
            return ui.SelectedItem;
        } catch {
            return null;
        }
    }
}
