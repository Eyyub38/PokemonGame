using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CustomizationWardrobe : MonoBehaviour, IPlayerTriggerable {
    [Header("Wardrobe")]
    [Tooltip("Optional id used by future UI/debug logs. Empty uses GameObject name.")]
    [SerializeField] string wardrobeId;
    [Tooltip("Presets available from this wardrobe.")]
    [SerializeField] List<CustomizationPresetDefinition> availablePresets = new List<CustomizationPresetDefinition>();
    [Tooltip("Parts available from this wardrobe.")]
    [SerializeField] List<CustomizationPartDefinition> availableParts = new List<CustomizationPartDefinition>();

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required to use this wardrobe.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this wardrobe.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Message shown when wardrobe access is blocked.")]
    [SerializeField] string lockedMessage = "This wardrobe is not available yet.";

    [Header("Trigger")]
    [Tooltip("If enabled, repeated player triggers can call OnPlayerTriggered more than once.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("If enabled, interacting with the wardrobe unlocks all currently available parts and presets.")]
    [SerializeField] bool unlockAvailableOnTrigger = true;

    public string WardrobeId => string.IsNullOrWhiteSpace(wardrobeId) ? name : wardrobeId;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(player == null) {
            PublishWardrobeEvent(null, "blocked", "A player is required to use this wardrobe.", GameEventImportance.Warning);
            return;
        }

        if(!CanUse(player, out var failureMessage)) {
            PublishWardrobeEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
            return;
        }

        if(unlockAvailableOnTrigger) {
            var customization = player.GetComponent<PlayerCustomization>() ?? player.gameObject.AddComponent<PlayerCustomization>();
            foreach(var preset in GetAvailablePresets(player)) {
                customization.UnlockPreset(preset, WardrobeId);
            }

            foreach(var part in GetAvailableParts(player)) {
                customization.UnlockPart(part, WardrobeId);
            }
        }

        PublishWardrobeEvent(player, "opened", $"{WardrobeId} opened.", GameEventImportance.Info);
    }

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

        failureMessage = null;
        return true;
    }

    public List<CustomizationPresetDefinition> GetAvailablePresets(PlayerController player) {
        return availablePresets
            .Where(preset => preset != null && preset.CanUse(player, out _))
            .OrderBy(preset => preset.DisplayName)
            .ToList();
    }

    public List<CustomizationPartDefinition> GetAvailableParts(PlayerController player) {
        return availableParts
            .Where(part => part != null && part.CanUse(player, out _))
            .OrderBy(part => part.Slot)
            .ThenBy(part => part.DisplayName)
            .ToList();
    }

    public bool TryApplyPreset(PlayerController player, CustomizationPresetDefinition preset, out string failureMessage) {
        if(!CanUse(player, out failureMessage)) {
            return false;
        }

        if(!availablePresets.Contains(preset)) {
            failureMessage = "This preset is not available in this wardrobe.";
            return false;
        }

        var customization = player.GetComponent<PlayerCustomization>() ?? player.gameObject.AddComponent<PlayerCustomization>();
        customization.UnlockPreset(preset, WardrobeId);
        return customization.ApplyPreset(preset, replaceParts: true, unlockPresetParts: true, out failureMessage);
    }

    public bool TryEquipPart(PlayerController player, CustomizationPartDefinition part, out string failureMessage) {
        if(!CanUse(player, out failureMessage)) {
            return false;
        }

        if(!availableParts.Contains(part)) {
            failureMessage = "This part is not available in this wardrobe.";
            return false;
        }

        var customization = player.GetComponent<PlayerCustomization>() ?? player.gameObject.AddComponent<PlayerCustomization>();
        customization.UnlockPart(part, WardrobeId);
        return customization.EquipPart(part, out failureMessage);
    }

    void PublishWardrobeEvent(PlayerController player, string phase, string message, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            null,
            $"customization.wardrobe.{phase}.{WardrobeId}",
            message,
            GameEventCategory.Customization,
            importance,
            player != null ? player : this,
            "CustomizationWardrobe",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: importance >= GameEventImportance.Warning,
            GameEventPublishing.Value("wardrobeId", WardrobeId),
            GameEventPublishing.Value("phase", phase));
    }
}
