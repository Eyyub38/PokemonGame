using System;
using System.Collections.Generic;
using UnityEngine;

public enum CustomizationSlot {
    Body,
    Hair,
    Face,
    Eyes,
    Hat,
    Top,
    Bottom,
    Outfit,
    Shoes,
    Bag,
    Accessory,
    Special
}

public enum CustomizationUseTarget {
    Any,
    PlayerOnly,
    NPCOnly
}

[CreateAssetMenu(menuName = "Customization/Part Definition")]
public class CustomizationPartDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this customization part. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in wardrobe/debug UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this part.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Which equipment/body slot this part occupies.")]
    [SerializeField] CustomizationSlot slot = CustomizationSlot.Accessory;
    [Tooltip("Where this part can be used.")]
    [SerializeField] CustomizationUseTarget useTarget = CustomizationUseTarget.Any;
    [Tooltip("Free-form tags used by randomizers, requirements and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future wardrobe/shop UI.")]
    [SerializeField] Sprite icon;

    [Header("Rendering")]
    [Tooltip("Color tint applied to this layer's SpriteRenderer.")]
    [SerializeField] Color tint = Color.white;
    [Tooltip("Sorting order added on top of the base character SpriteRenderer sorting order.")]
    [SerializeField] int sortingOrderOffset = 1;
    [Tooltip("If enabled, this part replaces any existing part in the same slot when equipped.")]
    [SerializeField] bool exclusiveInSlot = true;

    [Header("Walking Sprites")]
    [Tooltip("Sprites used when idle or walking.")]
    [SerializeField] CustomizationDirectionalSprites walkSprites = new CustomizationDirectionalSprites();
    [Header("Running Sprites")]
    [Tooltip("Sprites used while running. Empty directions fall back to walking sprites.")]
    [SerializeField] CustomizationDirectionalSprites runSprites = new CustomizationDirectionalSprites();
    [Header("Jumping Sprites")]
    [Tooltip("Sprites used while jumping. Empty directions fall back to walking sprites.")]
    [SerializeField] CustomizationDirectionalSprites jumpSprites = new CustomizationDirectionalSprites();
    [Header("Surfing Sprites")]
    [Tooltip("Sprites used while surfing. Empty directions fall back to walking sprites.")]
    [SerializeField] CustomizationDirectionalSprites surfSprites = new CustomizationDirectionalSprites();

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this part can be used.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this part.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional milestone required before this part can be used.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional skill required before this part can be used.")]
    [SerializeField] PlayerSkillDefinition requiredSkill;
    [Tooltip("Minimum level of the required skill.")]
    [Min(0)]
    [SerializeField] int requiredSkillLevel;
    [Tooltip("Message shown when access rules block this part.")]
    [SerializeField] string lockedMessage = "This customization part is not available yet.";

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public CustomizationSlot Slot => slot;
    public CustomizationUseTarget UseTarget => useTarget;
    public IReadOnlyList<string> Tags => tags;
    public Sprite Icon => icon;
    public Color Tint => tint;
    public int SortingOrderOffset => sortingOrderOffset;
    public bool ExclusiveInSlot => exclusiveInSlot;

    public bool CanUse(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredMilestone != null && !(player?.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredSkill != null) {
            int skillLevel = player?.GetComponent<PlayerProgression>()?.GetSkillLevel(requiredSkill) ?? 0;
            if(skillLevel < Mathf.Max(0, requiredSkillLevel)) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} requires {requiredSkill.DisplayName} level {requiredSkillLevel}." : lockedMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public Sprite GetSprite(CharacterAnimationState animationState, FacingDirection direction, int frameIndex) {
        var frames = GetFrames(animationState, direction);
        if(frames == null || frames.Count == 0) {
            return null;
        }

        int index = animationState == CharacterAnimationState.Idle ? 0 : Mathf.Abs(frameIndex) % frames.Count;
        return frames[index];
    }

    public IReadOnlyList<Sprite> GetFrames(CharacterAnimationState animationState, FacingDirection direction) {
        var fallback = walkSprites.Get(direction);
        return animationState switch {
            CharacterAnimationState.Run => GetFallback(runSprites.Get(direction), fallback),
            CharacterAnimationState.Jump => GetFallback(jumpSprites.Get(direction), fallback),
            CharacterAnimationState.Surf => GetFallback(surfSprites.Get(direction), fallback),
            _ => fallback
        };
    }

    public bool HasAnySprite() {
        return walkSprites.HasAnySprite()
            || runSprites.HasAnySprite()
            || jumpSprites.HasAnySprite()
            || surfSprites.HasAnySprite();
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        foreach(var entry in tags) {
            if(string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }

    IReadOnlyList<Sprite> GetFallback(IReadOnlyList<Sprite> primary, IReadOnlyList<Sprite> fallback) {
        return primary != null && primary.Count > 0 ? primary : fallback;
    }
}

[Serializable]
public class CustomizationDirectionalSprites {
    [Tooltip("Sprites used while facing down.")]
    public List<Sprite> down = new List<Sprite>();
    [Tooltip("Sprites used while facing up.")]
    public List<Sprite> up = new List<Sprite>();
    [Tooltip("Sprites used while facing left.")]
    public List<Sprite> left = new List<Sprite>();
    [Tooltip("Sprites used while facing right.")]
    public List<Sprite> right = new List<Sprite>();

    public IReadOnlyList<Sprite> Get(FacingDirection direction) {
        return direction switch {
            FacingDirection.Up => up,
            FacingDirection.Left => left,
            FacingDirection.Right => right,
            _ => down
        };
    }

    public bool HasAnySprite() {
        return HasSprites(down) || HasSprites(up) || HasSprites(left) || HasSprites(right);
    }

    bool HasSprites(List<Sprite> sprites) {
        return sprites != null && sprites.Exists(sprite => sprite != null);
    }
}
