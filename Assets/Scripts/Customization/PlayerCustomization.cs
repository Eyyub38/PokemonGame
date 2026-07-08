using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCustomization : MonoBehaviour, ISavable {
    [Header("Defaults")]
    [Tooltip("Preset applied when no saved customization exists.")]
    [SerializeField] CustomizationPresetDefinition startingPreset;
    [Tooltip("If enabled, starting preset parts are unlocked during Start.")]
    [SerializeField] bool unlockStartingPresetParts = true;
    [Tooltip("If enabled, the current loadout is applied to CharacterCustomizationRenderer during Start and after save restore.")]
    [SerializeField] bool applyOnStart = true;
    [Tooltip("If enabled, equipped parts must be unlocked before EquipPart succeeds.")]
    [SerializeField] bool requireUnlockToEquip = true;

    [Header("Runtime Loadout")]
    [Tooltip("Current preset selected by the player.")]
    [SerializeField] CustomizationPresetDefinition currentPreset;
    [Tooltip("Parts currently equipped by the player.")]
    [SerializeField] List<CustomizationPartDefinition> equippedParts = new List<CustomizationPartDefinition>();
    [Tooltip("Runtime/save ids for unlocked customization parts.")]
    [SerializeField] List<string> unlockedPartIds = new List<string>();
    [Tooltip("Runtime/save ids for unlocked customization presets.")]
    [SerializeField] List<string> unlockedPresetIds = new List<string>();

    public CustomizationPresetDefinition CurrentPreset => currentPreset;
    public IReadOnlyList<CustomizationPartDefinition> EquippedParts => equippedParts;
    public IReadOnlyList<string> UnlockedPartIds => unlockedPartIds;
    public IReadOnlyList<string> UnlockedPresetIds => unlockedPresetIds;
    public event Action<CustomizationPartDefinition> OnPartUnlocked;
    public event Action<CustomizationPresetDefinition> OnPresetUnlocked;
    public event Action OnLoadoutChanged;

    void Start() {
        if(currentPreset == null && startingPreset != null) {
            UnlockPreset(startingPreset, "starting-preset");
            ApplyPreset(startingPreset, replaceParts: true, unlockPresetParts: unlockStartingPresetParts, out _);
        } else if(applyOnStart) {
            ApplyCurrentLoadout();
        }
    }

    public bool HasUnlockedPart(CustomizationPartDefinition part) {
        return part != null && HasUnlockedPart(part.Id);
    }

    public bool HasUnlockedPart(string partId) {
        return !string.IsNullOrWhiteSpace(partId) && unlockedPartIds.Contains(partId);
    }

    public bool UnlockPart(CustomizationPartDefinition part, string source = null) {
        if(part == null || HasUnlockedPart(part)) {
            return false;
        }

        unlockedPartIds.Add(part.Id);
        OnPartUnlocked?.Invoke(part);
        PublishCustomizationEvent("part-unlocked", part.Id, part.DisplayName, source, GameEventImportance.Success);
        return true;
    }

    public bool HasUnlockedPreset(CustomizationPresetDefinition preset) {
        return preset != null && HasUnlockedPreset(preset.Id);
    }

    public bool HasUnlockedPreset(string presetId) {
        return !string.IsNullOrWhiteSpace(presetId) && unlockedPresetIds.Contains(presetId);
    }

    public bool UnlockPreset(CustomizationPresetDefinition preset, string source = null) {
        if(preset == null || HasUnlockedPreset(preset)) {
            return false;
        }

        unlockedPresetIds.Add(preset.Id);
        OnPresetUnlocked?.Invoke(preset);
        PublishCustomizationEvent("preset-unlocked", preset.Id, preset.DisplayName, source, GameEventImportance.Success);
        return true;
    }

    public bool CanEquipPart(CustomizationPartDefinition part, out string failureMessage) {
        if(part == null) {
            failureMessage = "No customization part selected.";
            return false;
        }

        if(requireUnlockToEquip && !HasUnlockedPart(part)) {
            failureMessage = $"{part.DisplayName} is not unlocked.";
            return false;
        }

        if(!part.CanUse(GetComponent<PlayerController>(), out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool EquipPart(CustomizationPartDefinition part, out string failureMessage) {
        if(!CanEquipPart(part, out failureMessage)) {
            return false;
        }

        if(part.ExclusiveInSlot) {
            equippedParts.RemoveAll(p => p == null || p.Slot == part.Slot);
        }

        if(!equippedParts.Contains(part)) {
            equippedParts.Add(part);
        }

        ApplyCurrentLoadout();
        PublishCustomizationEvent("part-equipped", part.Id, part.DisplayName, null, GameEventImportance.Info);
        failureMessage = null;
        return true;
    }

    public bool UnequipSlot(CustomizationSlot slot) {
        bool removed = equippedParts.RemoveAll(part => part != null && part.Slot == slot) > 0;
        if(removed) {
            ApplyCurrentLoadout();
            PublishCustomizationEvent("slot-unequipped", slot.ToString(), slot.ToString(), null, GameEventImportance.Info);
        }

        return removed;
    }

    public bool ApplyPreset(CustomizationPresetDefinition preset, bool replaceParts, bool unlockPresetParts, out string failureMessage) {
        if(preset == null) {
            failureMessage = "No customization preset selected.";
            return false;
        }

        if(requireUnlockToEquip && !HasUnlockedPreset(preset)) {
            failureMessage = $"{preset.DisplayName} is not unlocked.";
            return false;
        }

        if(!preset.CanUse(GetComponent<PlayerController>(), out failureMessage)) {
            return false;
        }

        currentPreset = preset;
        if(unlockPresetParts) {
            foreach(var part in preset.StartingUnlockedParts) {
                UnlockPart(part, "preset");
            }

            foreach(var part in preset.DefaultParts) {
                UnlockPart(part, "preset");
            }
        }

        if(replaceParts) {
            equippedParts = preset.GetUniqueDefaultParts().ToList();
        } else {
            foreach(var part in preset.GetUniqueDefaultParts()) {
                if(part != null && !equippedParts.Contains(part)) {
                    equippedParts.Add(part);
                }
            }
        }

        ApplyCurrentLoadout();
        PublishCustomizationEvent("preset-applied", preset.Id, preset.DisplayName, null, GameEventImportance.Info);
        failureMessage = null;
        return true;
    }

    public bool HasEquippedPart(CustomizationPartDefinition part) {
        return part != null && equippedParts.Contains(part);
    }

    public bool HasEquippedPartWithTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag) && equippedParts.Any(part => part != null && part.HasTag(tag));
    }

    public bool HasEquippedSlot(CustomizationSlot slot) {
        return equippedParts.Any(part => part != null && part.Slot == slot);
    }

    public void ApplyCurrentLoadout() {
        var renderer = GetComponent<CharacterCustomizationRenderer>() ?? gameObject.AddComponent<CharacterCustomizationRenderer>();
        if(currentPreset != null) {
            renderer.ApplyPreset(currentPreset, replaceParts: false);
        }

        renderer.SetParts(equippedParts.Where(part => part != null), replaceExisting: true);
        OnLoadoutChanged?.Invoke();
    }

    void PublishCustomizationEvent(string phase, string targetId, string targetName, string source, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            null,
            $"customization.{phase}.{targetId}",
            $"{targetName} {phase}.",
            GameEventCategory.Customization,
            importance,
            this,
            "PlayerCustomization",
            GameEventScope.Player,
            showInFeed: phase.Contains("unlocked"),
            writeToDebugLog: false,
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("targetId", targetId),
            GameEventPublishing.Value("targetName", targetName),
            GameEventPublishing.Value("source", source));
    }

    public object CaptureState() {
        return new PlayerCustomizationSaveData {
            currentPresetId = currentPreset != null ? currentPreset.Id : string.Empty,
            equippedPartIds = equippedParts.Where(part => part != null).Select(part => part.Id).Distinct().ToList(),
            unlockedPartIds = unlockedPartIds.Distinct().ToList(),
            unlockedPresetIds = unlockedPresetIds.Distinct().ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCustomizationSaveData;
        if(saveData == null) {
            return;
        }

        currentPreset = ResolvePreset(saveData.currentPresetId);
        equippedParts = saveData.equippedPartIds?.Select(ResolvePart).Where(part => part != null).ToList() ?? new List<CustomizationPartDefinition>();
        unlockedPartIds = saveData.unlockedPartIds?.Distinct().ToList() ?? new List<string>();
        unlockedPresetIds = saveData.unlockedPresetIds?.Distinct().ToList() ?? new List<string>();

        if(applyOnStart) {
            ApplyCurrentLoadout();
        }
    }

    CustomizationPartDefinition ResolvePart(string partId) {
        if(string.IsNullOrWhiteSpace(partId)) {
            return null;
        }

        return Resources.LoadAll<CustomizationPartDefinition>("").FirstOrDefault(part => part != null && part.Id == partId);
    }

    CustomizationPresetDefinition ResolvePreset(string presetId) {
        if(string.IsNullOrWhiteSpace(presetId)) {
            return null;
        }

        return Resources.LoadAll<CustomizationPresetDefinition>("").FirstOrDefault(preset => preset != null && preset.Id == presetId);
    }
}

[Serializable]
public class PlayerCustomizationSaveData {
    public string currentPresetId;
    public List<string> equippedPartIds;
    public List<string> unlockedPartIds;
    public List<string> unlockedPresetIds;
}
