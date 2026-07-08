using System.Collections;
using UnityEngine;

public class FarmNode : MonoBehaviour, Interactable, ISavable, IOverworldInteractionInfoProvider {
    [Header("Definition")]
    [Tooltip("Farmable asset planted in this node.")]
    [SerializeField] FarmableDefinition farmable;
    [Tooltip("Fallback skill used for harvest yield bonuses when the activity has no bonus skill.")]
    [SerializeField] PlayerSkillDefinition yieldBonusSkill;

    [Header("State")]
    [Tooltip("If enabled, this node starts planted. If disabled, it starts harvested/empty.")]
    [SerializeField] bool plantedOnStart = true;
    [Tooltip("Current in-game growth hours. Usually left at 0 for new scene nodes.")]
    [SerializeField] int growthProgressHours;
    [Tooltip("Runtime/initial harvested state for this node.")]
    [SerializeField] bool harvested;

    [Header("Visuals")]
    [Tooltip("Sprite renderer updated by growth stages. Auto-filled from children if empty.")]
    [SerializeField] SpriteRenderer spriteRenderer;

    public FarmableDefinition Farmable => farmable;
    public bool IsPlanted => farmable != null && !harvested;
    public bool IsReady => IsPlanted && growthProgressHours >= farmable.GrowthHours;
    public float NormalizedGrowth => farmable == null ? 0f : Mathf.Clamp01(growthProgressHours / (float)farmable.GrowthHours);

    void Awake() {
        if(spriteRenderer == null) {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if(!plantedOnStart) {
            harvested = true;
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

    public void Plant(FarmableDefinition definition) {
        farmable = definition;
        harvested = false;
        growthProgressHours = 0;
        RefreshVisuals();
    }

    public IEnumerator Interact(Transform initiator) {
        if(farmable == null) {
            yield return DialogManager.i.ShowDialogText("There is nothing planted here.");
            yield break;
        }

        var player = initiator.GetComponent<PlayerController>();
        var activity = farmable.Activity;
        if(activity == null) {
            yield return DialogManager.i.ShowDialogText($"{farmable.DisplayName} has no activity configured.");
            yield break;
        }

        if(!activity.CanPerform(player, out var failureMessage)) {
            yield return DialogManager.i.ShowDialogText(failureMessage);
            yield break;
        }

        if(harvested) {
            yield return DialogManager.i.ShowDialogText($"{farmable.DisplayName} has already been harvested.");
            yield break;
        }

        if(!IsReady) {
            var stage = farmable.GetStage(NormalizedGrowth);
            var label = stage != null && !string.IsNullOrWhiteSpace(stage.label) ? stage.label : "growing";
            yield return DialogManager.i.ShowDialogText($"{farmable.DisplayName} is still {label}.");
            yield break;
        }

        if(!activity.TryPayCosts(player, out failureMessage)) {
            yield return DialogManager.i.ShowDialogText(failureMessage);
            yield break;
        }

        Harvest(player);
        yield return DialogManager.i.ShowDialogText($"{farmable.DisplayName} was harvested.");
    }

    public bool TryGetInteractionInfo(PlayerController player, out OverworldInteractionInfo info) {
        string target = farmable != null ? farmable.DisplayName : name;
        string description = "Nothing is planted here.";
        bool canInteract = farmable != null;
        string blockedMessage = null;
        var activity = farmable != null ? farmable.Activity : null;

        if(farmable != null) {
            if(activity == null) {
                canInteract = false;
                blockedMessage = $"{farmable.DisplayName} has no activity configured.";
            } else if(!activity.CanPerform(player, out blockedMessage)) {
                canInteract = false;
            } else if(harvested) {
                canInteract = false;
                blockedMessage = $"{farmable.DisplayName} has already been harvested.";
            } else if(!IsReady) {
                var stage = farmable.GetStage(NormalizedGrowth);
                var label = stage != null && !string.IsNullOrWhiteSpace(stage.label) ? stage.label : "growing";
                canInteract = false;
                blockedMessage = $"{farmable.DisplayName} is still {label}.";
            }

            description = IsReady ? "Ready to harvest." : $"Growth {Mathf.RoundToInt(NormalizedGrowth * 100f)}%.";
        }

        info = new OverworldInteractionInfo {
            TargetName = target,
            ActionName = IsReady ? "Harvest" : "Inspect",
            Description = description,
            PermissionHint = PlayerActivityContext.CurrentZone != null ? PlayerActivityContext.CurrentZone.DisplayName : string.Empty,
            BlockedMessage = blockedMessage,
            CanInteract = canInteract,
            Activity = activity,
            Zone = PlayerActivityContext.CurrentZone,
            Source = this
        };
        return true;
    }

    void HandleTimeChanged() {
        if(!IsPlanted || IsReady) {
            return;
        }

        if(TimeSystem.i.Minute != 0) {
            return;
        }

        growthProgressHours++;
        RefreshVisuals();
    }

    void Harvest(PlayerController player) {
        var inventory = Inventory.GetInventory();
        int bonus = GetYieldBonus(player);
        int totalItems = 0;

        foreach(var farmYield in farmable.Yields) {
            if(farmYield.item == null) {
                continue;
            }

            int count = farmYield.RollCount(bonus);
            totalItems += count;
            inventory.AddItem(farmYield.item, count);
        }

        var activity = farmable.Activity;
        int experienceReward = (activity?.BaseExperience ?? 15) + bonus * 5;
        experienceReward = PlayerActivityContext.ModifyExperience(activity, experienceReward);
        if(WorldEventManager.i != null) {
            experienceReward = WorldEventManager.i.ModifyExperience(activity, experienceReward);
            WorldEventManager.i.ApplyActivityReputation(player, activity);
        } else {
            player?.GetComponent<PlayerReputation>()?.ApplyChanges(activity?.ReputationChanges);
        }

        player?.GetComponent<PlayerProgression>()?.AddExperience(
            experienceReward,
            activity?.ExperienceSource ?? PlayerExperienceSource.Farming);
        activity?.ApplyRelationshipRewards(player);
        activity?.RecordCompletion(player);
        activity?.CompleteMilestones(player);
        activity?.ApplyLifePathRewards(player);
        activity?.ApplyOutcomes(player);
        PublishHarvestEvent(player, experienceReward, totalItems);

        if(farmable.Repeatable) {
            growthProgressHours = 0;
        } else {
            harvested = true;
        }

        RefreshVisuals();
    }

    void PublishHarvestEvent(PlayerController player, int experienceReward, int totalItems) {
        var activity = farmable.Activity;
        GameEventPublishing.PublishOptional(
            farmable.HarvestEvent,
            $"farming.harvested.{farmable.Id}",
            $"{farmable.DisplayName} harvested.",
            GameEventCategory.Farming,
            GameEventImportance.Success,
            player,
            "FarmNode",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("farmableId", farmable.Id),
            GameEventPublishing.Value("farmableName", farmable.DisplayName),
            GameEventPublishing.Value("activityId", activity != null ? activity.Id : null),
            GameEventPublishing.Value("experience", experienceReward),
            GameEventPublishing.Value("itemCount", totalItems));
    }

    int GetYieldBonus(PlayerController player) {
        var skill = farmable.Activity?.BonusSkill ?? yieldBonusSkill;
        int areaBonus = PlayerActivityContext.GetYieldBonus(farmable.Activity);
        if(player == null || skill == null) {
            return areaBonus;
        }

        return (player.GetComponent<PlayerProgression>()?.GetSkillLevel(skill) ?? 0)
            + areaBonus;
    }

    void RefreshVisuals() {
        if(spriteRenderer == null || farmable == null) {
            return;
        }

        var stage = farmable.GetStage(NormalizedGrowth);
        if(stage != null && stage.sprite != null) {
            spriteRenderer.sprite = stage.sprite;
        }
    }

    public object CaptureState() {
        return new FarmNodeSaveData() {
            growthProgressHours = growthProgressHours,
            harvested = harvested
        };
    }

    public void RestoreState(object state) {
        var saveData = state as FarmNodeSaveData;
        if(saveData == null) {
            return;
        }

        growthProgressHours = saveData.growthProgressHours;
        harvested = saveData.harvested;
        RefreshVisuals();
    }
}

[System.Serializable]
public class FarmNodeSaveData {
    public int growthProgressHours;
    public bool harvested;
}
