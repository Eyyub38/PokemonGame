using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RadialWorldToolActionKind {
    Interact,
    Inspect,
    UseTool,
    OpenBoard,
    Track,
    Cancel,
    Custom
}

[Serializable]
public class RadialWorldToolActionDefinition {
    [Tooltip("Action kind represented by this radial option.")]
    public RadialWorldToolActionKind actionKind = RadialWorldToolActionKind.Interact;
    [Tooltip("Stable option id. Empty uses the action kind or tool id.")]
    public string optionId = string.Empty;
    [Tooltip("Label shown by the radial option tag/frame.")]
    public string label = string.Empty;
    [Tooltip("Description shown by the radial option tag/frame.")]
    [TextArea]
    public string description = string.Empty;
    [Tooltip("Icon shown inside the radial segment. Empty uses the linked tool icon when available.")]
    public Sprite icon;
    [Tooltip("Optional tool used by UseTool actions.")]
    public ToolDefinition tool;
    [Tooltip("Required level for the linked tool.")]
    [Min(1)]
    public int requiredToolLevel = 1;
    [Tooltip("Required durability for the linked tool.")]
    [Min(0)]
    public int requiredToolDurability = 1;
    [Tooltip("Lower priority appears earlier around the ring.")]
    public int priority;
    [Tooltip("If enabled, this option is always shown even when it is not currently usable.")]
    public bool showWhenDisabled = true;
}

public class RadialWorldToolMenuProvider : MonoBehaviour, IRadialMenuProvider {
    [Header("World Context")]
    [Tooltip("Sensor used to resolve the current overworld interactable. Empty searches at runtime.")]
    [SerializeField] OverworldInteractionSensor interactionSensor;
    [Tooltip("Player used for interaction and tool inventory checks. Empty uses PlayerController.i or the local parent.")]
    [SerializeField] PlayerController player;
    [Tooltip("Explicit target interactable. Empty uses the current sensor target.")]
    [SerializeField] MonoBehaviour interactableOverride;
    [Tooltip("Explicit interaction info used when no sensor info is available.")]
    [SerializeField] InteractionPromptSource promptOverride;
    [Tooltip("If enabled, the provider refreshes the sensor before building options.")]
    [SerializeField] bool refreshSensorBeforeBuild = true;

    [Header("Tools")]
    [Tooltip("Tool inventory used for owned/durability checks. Empty uses the player component.")]
    [SerializeField] PlayerToolInventory toolInventory;
    [Tooltip("Additional tool actions exposed by this radial menu.")]
    [SerializeField] List<RadialWorldToolActionDefinition> toolActions = new List<RadialWorldToolActionDefinition>();
    [Tooltip("If enabled, UseTool actions require the player to own the linked tool.")]
    [SerializeField] bool requireOwnedTool = true;

    [Header("Default Actions")]
    [Tooltip("If enabled, the current interaction action is shown as the primary radial option.")]
    [SerializeField] bool includeInteractAction = true;
    [Tooltip("If enabled, an Inspect option is shown for the current target.")]
    [SerializeField] bool includeInspectAction = true;
    [Tooltip("If enabled, a Back/Cancel option is shown.")]
    [SerializeField] bool includeCancelAction = true;
    [Tooltip("If enabled, selecting Interact starts the target Interact coroutine. Disabled by default so the provider can be used as a safe UI adapter.")]
    [SerializeField] bool runInteractOnSelect;

    [Header("Debug")]
    [Tooltip("If enabled, selected radial world/tool actions are written to GameDebug.")]
    [SerializeField] bool logSelectedActions = true;

    public OverworldInteractionSensor InteractionSensor => interactionSensor;
    public PlayerController Player => player;
    public MonoBehaviour InteractableOverride => interactableOverride;
    public InteractionPromptSource PromptOverride => promptOverride;
    public PlayerToolInventory ToolInventory => toolInventory;
    public IReadOnlyList<RadialWorldToolActionDefinition> ToolActions => toolActions;
    public event Action<RadialWorldToolActionKind, OverworldInteractionInfo, ToolDefinition, RadialMenuOption> OnWorldToolActionSelected;

    public IReadOnlyList<RadialMenuOption> BuildRadialOptions(RadialMenuContext context) {
        var info = ResolveInteractionInfo(context, out var interactable);
        var result = new List<RadialMenuOption>();

        if(includeInteractAction) {
            var action = BuildInteractionAction(info);
            bool disabled = IsInteractionDisabled(action.actionKind, info, interactable, out var reason);
            if(!disabled || action.showWhenDisabled) {
                result.Add(BuildOption(action, info, null, disabled, reason));
            }
        }

        if(includeInspectAction) {
            var inspect = new RadialWorldToolActionDefinition {
                actionKind = RadialWorldToolActionKind.Inspect,
                label = "Inspect",
                description = info != null ? info.Description : "Inspect the current target.",
                priority = 80
            };
            bool disabled = info == null;
            result.Add(BuildOption(inspect, info, null, disabled, disabled ? "No target to inspect." : null));
        }

        foreach(var action in toolActions.OrderBy(action => action != null ? action.priority : int.MaxValue)) {
            if(action == null) {
                continue;
            }

            bool disabled = IsToolActionDisabled(action, out var reason);
            if(disabled && !action.showWhenDisabled) {
                continue;
            }

            result.Add(BuildOption(action, info, action.tool, disabled, reason));
        }

        if(includeCancelAction) {
            var cancel = new RadialWorldToolActionDefinition {
                actionKind = RadialWorldToolActionKind.Cancel,
                label = "Back",
                priority = 100
            };
            result.Add(BuildOption(cancel, info, null, false, null));
        }

        return result;
    }

    public void OnRadialOptionSelected(RadialMenuOption option, RadialMenuContext context) {
        var info = ResolveInteractionInfo(context, out var interactable);
        var actionKind = ResolveActionKind(option);
        var tool = option != null && option.payload is ToolDefinition payloadTool ? payloadTool : null;
        OnWorldToolActionSelected?.Invoke(actionKind, info, tool, option);

        if(runInteractOnSelect && actionKind == RadialWorldToolActionKind.Interact && interactable is Interactable target) {
            var resolvedPlayer = ResolvePlayer();
            if(resolvedPlayer != null) {
                StartCoroutine(target.Interact(resolvedPlayer.transform));
            }
        }

        if(logSelectedActions) {
            string targetName = !string.IsNullOrWhiteSpace(info?.TargetName) ? info.TargetName : "No Target";
            string toolName = tool != null ? $" using {tool.DisplayName}" : string.Empty;
            GameDebug.Step($"World radial action selected: {actionKind} on {targetName}{toolName}.", GameDebugCategory.UI, this, "RadialWorldToolMenuProvider");
        }
    }

    public void OnRadialMenuClosed(RadialMenuContext context) {
    }

    public OverworldInteractionInfo ResolveInteractionInfo(RadialMenuContext context, out MonoBehaviour interactable) {
        interactable = ResolveInteractable(context);
        if(context != null && context.payload is InteractionPromptSource promptSource && promptSource.TryGetInteractionInfo(ResolvePlayer(), out var promptInfo)) {
            return promptInfo;
        }

        if(promptOverride != null && promptOverride.TryGetInteractionInfo(ResolvePlayer(), out var overrideInfo)) {
            return overrideInfo;
        }

        var sensor = ResolveSensor(context);
        if(sensor != null) {
            if(refreshSensorBeforeBuild) {
                sensor.Refresh();
            }

            interactable = interactable != null ? interactable : sensor.CurrentInteractable as MonoBehaviour;
            if(sensor.CurrentInfo != null) {
                return sensor.CurrentInfo;
            }
        }

        if(interactable is IOverworldInteractionInfoProvider provider && provider.TryGetInteractionInfo(ResolvePlayer(), out var providedInfo)) {
            return providedInfo;
        }

        return interactable != null
            ? OverworldInteractionInfo.Basic(interactable.name, "Interact", "Use this object.", interactable)
            : null;
    }

    RadialWorldToolActionDefinition BuildInteractionAction(OverworldInteractionInfo info) {
        return new RadialWorldToolActionDefinition {
            actionKind = RadialWorldToolActionKind.Interact,
            label = !string.IsNullOrWhiteSpace(info?.ActionName) ? info.ActionName : "Interact",
            description = info != null ? info.Description : "Interact with the current target.",
            priority = 0,
            showWhenDisabled = true
        };
    }

    RadialMenuOption BuildOption(RadialWorldToolActionDefinition action, OverworldInteractionInfo info, ToolDefinition tool, bool disabled, string disabledReason) {
        string fallbackId = tool != null ? tool.Id : action.actionKind.ToString();
        string id = !string.IsNullOrWhiteSpace(action.optionId) ? action.optionId : fallbackId;
        string label = !string.IsNullOrWhiteSpace(action.label)
            ? action.label
            : tool != null
                ? tool.DisplayName
                : action.actionKind.ToString();

        string description = !string.IsNullOrWhiteSpace(action.description)
            ? action.description
            : tool != null
                ? tool.Description
                : info != null
                    ? info.Description
                    : string.Empty;

        return new RadialMenuOption {
            id = id,
            label = label,
            description = description,
            icon = action.icon != null ? action.icon : tool != null ? tool.Icon : null,
            disabled = disabled,
            disabledReason = disabledReason,
            priority = action.priority,
            payload = tool != null ? tool : info?.Source
        };
    }

    bool IsInteractionDisabled(RadialWorldToolActionKind actionKind, OverworldInteractionInfo info, MonoBehaviour interactable, out string reason) {
        reason = null;
        if(actionKind == RadialWorldToolActionKind.Cancel) {
            return false;
        }

        if(info == null && interactable == null) {
            reason = "No target selected.";
            return true;
        }

        if(info != null && !info.CanInteract) {
            reason = !string.IsNullOrWhiteSpace(info.BlockedMessage) ? info.BlockedMessage : "This interaction is blocked.";
            return true;
        }

        if(actionKind == RadialWorldToolActionKind.Interact && interactable == null && info?.Source == null) {
            reason = "No interactable source is available.";
            return true;
        }

        return false;
    }

    bool IsToolActionDisabled(RadialWorldToolActionDefinition action, out string reason) {
        reason = null;
        if(action.actionKind != RadialWorldToolActionKind.UseTool || action.tool == null) {
            return false;
        }

        if(!requireOwnedTool) {
            return false;
        }

        var inventory = ResolveToolInventory();
        if(inventory == null || !inventory.HasTool(action.tool, action.requiredToolLevel, action.requiredToolDurability)) {
            reason = $"Requires {action.tool.DisplayName}.";
            return true;
        }

        return false;
    }

    RadialWorldToolActionKind ResolveActionKind(RadialMenuOption option) {
        if(option == null || string.IsNullOrWhiteSpace(option.id)) {
            return RadialWorldToolActionKind.Custom;
        }

        if(option.payload is ToolDefinition) {
            return RadialWorldToolActionKind.UseTool;
        }

        return Enum.TryParse(option.id, true, out RadialWorldToolActionKind kind) ? kind : RadialWorldToolActionKind.Custom;
    }

    MonoBehaviour ResolveInteractable(RadialMenuContext context) {
        if(interactableOverride != null) {
            return interactableOverride;
        }

        if(context != null) {
            if(context.payload is MonoBehaviour payloadBehaviour && payloadBehaviour is Interactable) {
                return payloadBehaviour;
            }

            if(context.owner is MonoBehaviour ownerBehaviour && ownerBehaviour is Interactable) {
                return ownerBehaviour;
            }
        }

        var sensor = ResolveSensor(context);
        return sensor != null ? sensor.CurrentInteractable as MonoBehaviour : null;
    }

    OverworldInteractionSensor ResolveSensor(RadialMenuContext context) {
        if(interactionSensor != null) {
            return interactionSensor;
        }

        if(context != null) {
            if(context.payload is OverworldInteractionSensor payloadSensor) {
                interactionSensor = payloadSensor;
                return interactionSensor;
            }

            if(context.owner is OverworldInteractionSensor ownerSensor) {
                interactionSensor = ownerSensor;
                return interactionSensor;
            }
        }

        interactionSensor = FindAnyObjectByType<OverworldInteractionSensor>();
        return interactionSensor;
    }

    PlayerController ResolvePlayer() {
        if(player != null) {
            return player;
        }

        player = GetComponentInParent<PlayerController>() ?? PlayerController.i;
        return player;
    }

    PlayerToolInventory ResolveToolInventory() {
        if(toolInventory != null) {
            return toolInventory;
        }

        var resolvedPlayer = ResolvePlayer();
        toolInventory = resolvedPlayer != null ? resolvedPlayer.GetComponent<PlayerToolInventory>() : FindAnyObjectByType<PlayerToolInventory>();
        return toolInventory;
    }
}
