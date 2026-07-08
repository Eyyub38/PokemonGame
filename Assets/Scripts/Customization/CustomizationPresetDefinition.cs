using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Customization/Preset Definition")]
public class CustomizationPresetDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this preset. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in wardrobe/debug UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this preset.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Free-form body type key used by future filters, such as adult, child, trainer or worker.")]
    [SerializeField] string bodyType;
    [Tooltip("Free-form tags used by randomizers, requirements and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Base Visuals")]
    [Tooltip("Base visual set applied to CharacterAnimator before layered parts are rendered.")]
    [SerializeField] NPCVisualSetDefinition baseVisualSet;
    [Tooltip("Optional battle image used by future trainer/player profile UI.")]
    [SerializeField] Sprite battleImage;

    [Header("Default Parts")]
    [Tooltip("Customization parts equipped when this preset is applied with replace mode.")]
    [SerializeField] List<CustomizationPartDefinition> defaultParts = new List<CustomizationPartDefinition>();
    [Tooltip("Parts unlocked when this preset is assigned to the player.")]
    [SerializeField] List<CustomizationPartDefinition> startingUnlockedParts = new List<CustomizationPartDefinition>();

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this preset can be selected.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this preset.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional milestone required before this preset can be selected.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Message shown when access rules block this preset.")]
    [SerializeField] string lockedMessage = "This customization preset is not available yet.";

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public string BodyType => bodyType;
    public IReadOnlyList<string> Tags => tags;
    public NPCVisualSetDefinition BaseVisualSet => baseVisualSet;
    public Sprite BattleImage => battleImage;
    public IReadOnlyList<CustomizationPartDefinition> DefaultParts => defaultParts;
    public IReadOnlyList<CustomizationPartDefinition> StartingUnlockedParts => startingUnlockedParts;

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

        failureMessage = null;
        return true;
    }

    public IEnumerable<CustomizationPartDefinition> GetUniqueDefaultParts() {
        var parts = (defaultParts ?? new List<CustomizationPartDefinition>()).Where(part => part != null).ToList();
        var exclusiveParts = parts
            .Where(part => part.ExclusiveInSlot)
            .GroupBy(part => part.Slot)
            .Select(group => group.Last());
        var stackedParts = parts.Where(part => !part.ExclusiveInSlot);
        return exclusiveParts.Concat(stackedParts);
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
}
