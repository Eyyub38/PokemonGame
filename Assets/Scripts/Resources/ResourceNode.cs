using System.Collections;
using UnityEngine;

public class ResourceNode : MonoBehaviour, Interactable, ISavable, IOverworldInteractionInfoProvider {
    [Tooltip("Resource definition used by this gather node.")]
    [SerializeField] ResourceNodeDefinition definition;
    [Tooltip("Sprite renderer swapped between available/depleted sprites. Auto-filled from children if empty.")]
    [SerializeField] SpriteRenderer spriteRenderer;

    bool depleted;
    int respawnProgressHours;

    public ResourceNodeDefinition Definition => definition;
    public bool IsAvailable => definition != null && !depleted;

    void Awake() {
        if(spriteRenderer == null) {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    void OnEnable() {
        if(TimeSystem.i != null) {
            TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        }
        RefreshVisuals();
    }

    void OnDisable() {
        if(TimeSystem.i != null) {
            TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        }
    }

    public IEnumerator Interact(Transform initiator) {
        var player = initiator.GetComponent<PlayerController>();

        if(definition == null) {
            yield return DialogManager.i.ShowDialogText("There is nothing useful here.");
            yield break;
        }

        var activity = definition.Activity;
        if(activity == null) {
            yield return DialogManager.i.ShowDialogText($"{definition.DisplayName} has no activity configured.");
            yield break;
        }

        if(!activity.CanPerform(player, out var failureMessage)) {
            yield return DialogManager.i.ShowDialogText(failureMessage);
            yield break;
        }

        if(depleted) {
            yield return DialogManager.i.ShowDialogText($"{definition.DisplayName} is depleted.");
            yield break;
        }

        if(!HasRequiredTool(player)) {
            yield return DialogManager.i.ShowDialogText($"You need the right tool to gather from {definition.DisplayName}.");
            yield break;
        }

        if(!activity.TryPayCosts(player, out failureMessage)) {
            yield return DialogManager.i.ShowDialogText(failureMessage);
            yield break;
        }

        Gather(player);
        yield return DialogManager.i.ShowDialogText($"{definition.DisplayName} gathered.");
    }

    public bool TryGetInteractionInfo(PlayerController player, out OverworldInteractionInfo info) {
        string target = definition != null ? definition.DisplayName : name;
        string description = definition != null ? definition.Description : "There is nothing useful here.";
        string blockedMessage = null;
        bool canInteract = definition != null;
        var activity = definition != null ? definition.Activity : null;

        if(definition == null) {
            blockedMessage = "There is nothing useful here.";
        } else if(activity == null) {
            canInteract = false;
            blockedMessage = $"{definition.DisplayName} has no activity configured.";
        } else if(!activity.CanPerform(player, out blockedMessage)) {
            canInteract = false;
        } else if(depleted) {
            canInteract = false;
            blockedMessage = $"{definition.DisplayName} is depleted.";
        } else if(!HasRequiredTool(player)) {
            canInteract = false;
            blockedMessage = $"You need the right tool to gather from {definition.DisplayName}.";
        }

        info = new OverworldInteractionInfo {
            TargetName = target,
            ActionName = "Gather",
            Description = description,
            ToolHint = BuildToolHint(),
            PermissionHint = PlayerActivityContext.CurrentZone != null ? PlayerActivityContext.CurrentZone.DisplayName : string.Empty,
            BlockedMessage = blockedMessage,
            CanInteract = canInteract,
            Activity = activity,
            Zone = PlayerActivityContext.CurrentZone,
            Source = this
        };
        return true;
    }

    void Gather(PlayerController player) {
        var inventory = Inventory.GetInventory();
        int bonus = GetYieldBonus(player);
        int totalItems = 0;

        foreach(var resourceYield in definition.Yields) {
            if(resourceYield.TryRoll(out int count, bonus)) {
                totalItems += count;
                inventory.AddItem(resourceYield.item, count);
            }
        }

        var activity = definition.Activity;
        int experienceReward = (activity?.BaseExperience ?? 10) + bonus * 5;
        experienceReward = PlayerActivityContext.ModifyExperience(activity, experienceReward);
        if(WorldEventManager.i != null) {
            experienceReward = WorldEventManager.i.ModifyExperience(activity, experienceReward);
            WorldEventManager.i.ApplyActivityReputation(player, activity);
        } else {
            player?.GetComponent<PlayerReputation>()?.ApplyChanges(activity?.ReputationChanges);
        }

        player?.GetComponent<PlayerProgression>()?.AddExperience(
            experienceReward,
            activity?.ExperienceSource ?? PlayerExperienceSource.Exploration);
        activity?.ApplyRelationshipRewards(player);
        activity?.RecordCompletion(player);
        activity?.CompleteMilestones(player);
        activity?.ApplyLifePathRewards(player);
        activity?.ApplyOutcomes(player);
        PublishGatherEvent(player, experienceReward, totalItems);

        player?.GetComponent<PlayerToolInventory>()?.ConsumeDurability(
            definition.RequiredToolDefinition,
            PlayerActivityContext.ModifyToolDurabilityCost(activity, definition.ToolDurabilityCost));

        if(definition.DepleteAfterGather) {
            depleted = true;
            respawnProgressHours = 0;
        }

        RefreshVisuals();
    }

    void PublishGatherEvent(PlayerController player, int experienceReward, int totalItems) {
        GameEventPublishing.PublishOptional(
            definition.GatherEvent,
            $"resource.gathered.{definition.Id}",
            $"{definition.DisplayName} gathered.",
            GameEventCategory.Resource,
            GameEventImportance.Success,
            player,
            "ResourceNode",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("resourceId", definition.Id),
            GameEventPublishing.Value("resourceName", definition.DisplayName),
            GameEventPublishing.Value("activityId", definition.Activity != null ? definition.Activity.Id : null),
            GameEventPublishing.Value("experience", experienceReward),
            GameEventPublishing.Value("itemCount", totalItems));
    }

    bool HasRequiredTool(PlayerController player) {
        if(definition.RequiredTool == null && definition.RequiredToolDefinition == null) {
            return true;
        }

        if(definition.RequiredTool != null) {
            var inventory = player != null ? player.GetComponent<Inventory>() : Inventory.GetInventory();
            if(inventory == null || !inventory.HasItemEnough(definition.RequiredTool)) {
                return false;
            }
        }

        if(definition.RequiredToolDefinition != null) {
            var toolInventory = player != null ? player.GetComponent<PlayerToolInventory>() : null;
            int durabilityCost = PlayerActivityContext.ModifyToolDurabilityCost(definition.Activity, definition.ToolDurabilityCost);
            if(toolInventory == null || !toolInventory.HasTool(definition.RequiredToolDefinition, 1, durabilityCost)) {
                return false;
            }
        }

        return true;
    }

    string BuildToolHint() {
        if(definition == null) {
            return string.Empty;
        }

        if(definition.RequiredToolDefinition != null) {
            return definition.RequiredToolDefinition.DisplayName;
        }

        if(definition.RequiredTool != null) {
            return definition.RequiredTool.Name;
        }

        return string.Empty;
    }

    int GetYieldBonus(PlayerController player) {
        var skill = definition.Activity?.BonusSkill;
        int areaBonus = PlayerActivityContext.GetYieldBonus(definition.Activity);
        if(player == null || skill == null) {
            return areaBonus;
        }

        return (player.GetComponent<PlayerProgression>()?.GetSkillLevel(skill) ?? 0)
            + areaBonus;
    }

    void HandleTimeChanged() {
        if(definition == null || !depleted || definition.RespawnHours <= 0) {
            return;
        }

        if(TimeSystem.i.Minute != 0) {
            return;
        }

        respawnProgressHours++;
        if(respawnProgressHours >= definition.RespawnHours) {
            depleted = false;
            respawnProgressHours = 0;
            RefreshVisuals();
        }
    }

    void RefreshVisuals() {
        if(spriteRenderer == null || definition == null) {
            return;
        }

        var sprite = depleted ? definition.DepletedSprite : definition.AvailableSprite;
        if(sprite != null) {
            spriteRenderer.sprite = sprite;
        }
    }

    public object CaptureState() {
        return new ResourceNodeSaveData() {
            depleted = depleted,
            respawnProgressHours = respawnProgressHours
        };
    }

    public void RestoreState(object state) {
        var saveData = state as ResourceNodeSaveData;
        if(saveData == null) {
            return;
        }

        depleted = saveData.depleted;
        respawnProgressHours = saveData.respawnProgressHours;
        RefreshVisuals();
    }
}

[System.Serializable]
public class ResourceNodeSaveData {
    public bool depleted;
    public int respawnProgressHours;
}
