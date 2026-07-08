using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum NPCVariantRole {
    Civilian,
    Trainer,
    Merchant,
    Healer,
    Researcher,
    Police,
    Worker,
    Special
}

public enum NPCPersonalitySelectionMode {
    KeepExisting,
    RandomFromDatabase,
    Fixed,
    WeightedPool
}

[CreateAssetMenu(menuName = "NPC Generation/NPC Variant Pool")]
public class NPCVariantPoolDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this NPC variant pool. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in editor/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note explaining where this random pool should be used.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad role of NPCs generated from this pool.")]
    [SerializeField] NPCVariantRole role = NPCVariantRole.Civilian;
    [Tooltip("Free-form tags used by future map filters and validation.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Defaults")]
    [Tooltip("Fallback names used when a variant entry has no names.")]
    [SerializeField] List<string> fallbackNames = new List<string>();
    [Tooltip("Fallback dialog used when a variant entry has no dialog.")]
    [SerializeField] Dialog fallbackDialog;
    [Tooltip("Fallback dialog used after a generated trainer loses.")]
    [SerializeField] Dialog fallbackAfterBattleDialog;
    [Tooltip("Fallback party template used when a trainer variant has no template.")]
    [SerializeField] TrainerPartyTemplateDefinition fallbackTrainerParty;

    [Header("Variants")]
    [Tooltip("Weighted random variants available in this pool.")]
    [SerializeField] List<NPCVariantEntry> variants = new List<NPCVariantEntry>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public NPCVariantRole Role => role;
    public IReadOnlyList<string> Tags => tags;
    public IReadOnlyList<string> FallbackNames => fallbackNames;
    public Dialog FallbackDialog => fallbackDialog;
    public Dialog FallbackAfterBattleDialog => fallbackAfterBattleDialog;
    public TrainerPartyTemplateDefinition FallbackTrainerParty => fallbackTrainerParty;
    public IReadOnlyList<NPCVariantEntry> Variants => variants;

    public NPCGeneratedProfile Generate(int seed) {
        var random = new System.Random(seed);
        var variant = PickVariant(random);
        if(variant == null) {
            return NPCGeneratedProfile.Empty(seed, Id);
        }

        string generatedName = PickName(variant, random);
        var visualSet = variant.VisualSet;
        var partyTemplate = variant.TrainerParty != null ? variant.TrainerParty : fallbackTrainerParty;

        return new NPCGeneratedProfile {
            seed = seed,
            poolId = Id,
            variantId = variant.Id,
            displayName = generatedName,
            role = variant.RoleOverrideEnabled ? variant.Role : role,
            visualSet = visualSet,
            battleImage = variant.BattleImage != null ? variant.BattleImage : visualSet != null ? visualSet.TrainerBattleImage : null,
            dialog = variant.Dialog ?? fallbackDialog,
            dialogAfterBattle = variant.DialogAfterBattle ?? fallbackAfterBattleDialog,
            conditionalDialog = variant.ConditionalDialog,
            conditionalDialogAfterBattle = variant.ConditionalDialogAfterBattle,
            personalityId = variant.ResolvePersonality(random),
            trainerPartyTemplate = partyTemplate,
            battleAIProfile = variant.BattleAIProfile,
            battleUnitCount = variant.BattleUnitCount,
            movementPattern = variant.MovementPattern != null ? new List<Vector2>(variant.MovementPattern) : null,
            customizationPreset = variant.CustomizationPreset,
            customizationParts = variant.CustomizationParts,
            replacePresetCustomizationParts = variant.ReplacePresetCustomizationParts
        };
    }

    public NPCVariantEntry GetVariant(string variantId) {
        if(string.IsNullOrWhiteSpace(variantId) || variants == null) {
            return null;
        }

        return variants.FirstOrDefault(v => v != null && v.Id == variantId);
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

    NPCVariantEntry PickVariant(System.Random random) {
        var valid = variants.Where(v => v != null && v.Weight > 0).ToList();
        if(valid.Count == 0) {
            return null;
        }

        int totalWeight = valid.Sum(v => v.Weight);
        int roll = random.Next(0, totalWeight);
        int current = 0;
        foreach(var variant in valid) {
            current += variant.Weight;
            if(roll < current) {
                return variant;
            }
        }

        return valid[0];
    }

    string PickName(NPCVariantEntry variant, System.Random random) {
        var names = variant.Names != null && variant.Names.Count > 0 ? variant.Names : fallbackNames;
        var validNames = names?.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        if(validNames == null || validNames.Count == 0) {
            return variant.DisplayName;
        }

        return validNames[random.Next(0, validNames.Count)];
    }
}

[System.Serializable]
public class NPCVariantEntry {
    [Header("Identity")]
    [Tooltip("Stable id for this variant entry. Empty uses display name or visual set id.")]
    [SerializeField] string id;
    [Tooltip("Editor/debug label for this variant.")]
    [SerializeField] string displayName;
    [Tooltip("Relative chance for this variant inside the pool.")]
    [Min(0)]
    [SerializeField] int weight = 10;
    [Tooltip("If enabled, this variant overrides the parent pool role.")]
    [SerializeField] bool roleOverrideEnabled;
    [Tooltip("Role used when role override is enabled.")]
    [SerializeField] NPCVariantRole role = NPCVariantRole.Civilian;

    [Header("Names And Visuals")]
    [Tooltip("Names this variant can choose from.")]
    [SerializeField] List<string> names = new List<string>();
    [Tooltip("Visual sprite set applied to CharacterAnimator.")]
    [SerializeField] NPCVisualSetDefinition visualSet;
    [Tooltip("Optional trainer battle image override.")]
    [SerializeField] Sprite battleImage;
    [Tooltip("Optional layered customization preset applied on top of or instead of the visual set.")]
    [SerializeField] CustomizationPresetDefinition customizationPreset;
    [Tooltip("Additional layered customization parts applied by this variant.")]
    [SerializeField] List<CustomizationPartDefinition> customizationParts = new List<CustomizationPartDefinition>();
    [Tooltip("If enabled, customization parts replace the preset's default parts. If disabled, they are added on top.")]
    [SerializeField] bool replacePresetCustomizationParts = true;

    [Header("Dialog")]
    [Tooltip("Fallback/generated dialog used by NPCController or pre-battle TrainerController.")]
    [SerializeField] Dialog dialog;
    [Tooltip("Fallback/generated dialog used after a trainer loses.")]
    [SerializeField] Dialog dialogAfterBattle;
    [Tooltip("Optional conditional dialog used by NPCController or pre-battle TrainerController.")]
    [SerializeField] ConditionalDialogDefinition conditionalDialog;
    [Tooltip("Optional conditional dialog used after a trainer loses.")]
    [SerializeField] ConditionalDialogDefinition conditionalDialogAfterBattle;

    [Header("Personality")]
    [Tooltip("How this variant selects a PersonalityProfile value.")]
    [SerializeField] NPCPersonalitySelectionMode personalityMode = NPCPersonalitySelectionMode.RandomFromDatabase;
    [Tooltip("Fixed personality used when personality mode is Fixed.")]
    [SerializeField] PersonalityID fixedPersonality = PersonalityID.Balanced;
    [Tooltip("Weighted personality options used when personality mode is Weighted Pool.")]
    [SerializeField] List<NPCPersonalityWeight> personalityPool = new List<NPCPersonalityWeight>();

    [Header("Trainer")]
    [Tooltip("Optional party template applied if this object has PokemonParty/TrainerController.")]
    [SerializeField] TrainerPartyTemplateDefinition trainerParty;
    [Tooltip("Optional AI profile applied if this object has TrainerController.")]
    [SerializeField] BattleAIProfile battleAIProfile;
    [Tooltip("How many Pokemon this trainer can use in battle. 0 keeps existing value.")]
    [Min(0)]
    [SerializeField] int battleUnitCount;

    [Header("Movement")]
    [Tooltip("Optional movement pattern applied to NPCController.")]
    [SerializeField] List<Vector2> movementPattern = new List<Vector2>();

    public string Id => !string.IsNullOrWhiteSpace(id) ? id : (!string.IsNullOrWhiteSpace(displayName) ? displayName : visualSet != null ? visualSet.Id : "variant");
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
    public int Weight => Mathf.Max(0, weight);
    public bool RoleOverrideEnabled => roleOverrideEnabled;
    public NPCVariantRole Role => role;
    public IReadOnlyList<string> Names => names;
    public NPCVisualSetDefinition VisualSet => visualSet;
    public Sprite BattleImage => battleImage;
    public CustomizationPresetDefinition CustomizationPreset => customizationPreset;
    public IReadOnlyList<CustomizationPartDefinition> CustomizationParts => customizationParts;
    public bool ReplacePresetCustomizationParts => replacePresetCustomizationParts;
    public Dialog Dialog => dialog;
    public Dialog DialogAfterBattle => dialogAfterBattle;
    public ConditionalDialogDefinition ConditionalDialog => conditionalDialog;
    public ConditionalDialogDefinition ConditionalDialogAfterBattle => conditionalDialogAfterBattle;
    public TrainerPartyTemplateDefinition TrainerParty => trainerParty;
    public BattleAIProfile BattleAIProfile => battleAIProfile;
    public int BattleUnitCount => Mathf.Max(0, battleUnitCount);
    public IReadOnlyList<Vector2> MovementPattern => movementPattern;

    public PersonalityID? ResolvePersonality(System.Random random) {
        return personalityMode switch {
            NPCPersonalitySelectionMode.Fixed => fixedPersonality,
            NPCPersonalitySelectionMode.WeightedPool => PickWeightedPersonality(random),
            NPCPersonalitySelectionMode.RandomFromDatabase => PickRandomPersonality(random),
            _ => null
        };
    }

    PersonalityID? PickWeightedPersonality(System.Random random) {
        var valid = personalityPool.Where(p => p != null && p.weight > 0).ToList();
        if(valid.Count == 0) {
            return null;
        }

        int totalWeight = valid.Sum(p => p.weight);
        int roll = random.Next(0, totalWeight);
        int current = 0;
        foreach(var entry in valid) {
            current += entry.weight;
            if(roll < current) {
                return entry.personality;
            }
        }

        return valid[0].personality;
    }

    PersonalityID PickRandomPersonality(System.Random random) {
        var values = System.Enum.GetValues(typeof(PersonalityID));
        if(values.Length <= 1) {
            return PersonalityID.Balanced;
        }

        return (PersonalityID)values.GetValue(random.Next(1, values.Length));
    }
}

[System.Serializable]
public class NPCPersonalityWeight {
    [Tooltip("Personality id selected by this weighted entry.")]
    public PersonalityID personality = PersonalityID.Balanced;
    [Tooltip("Relative chance for this personality.")]
    [Min(0)]
    public int weight = 10;
}

public class NPCGeneratedProfile {
    public int seed;
    public string poolId;
    public string variantId;
    public string displayName;
    public NPCVariantRole role;
    public NPCVisualSetDefinition visualSet;
    public Sprite battleImage;
    public Dialog dialog;
    public Dialog dialogAfterBattle;
    public ConditionalDialogDefinition conditionalDialog;
    public ConditionalDialogDefinition conditionalDialogAfterBattle;
    public PersonalityID? personalityId;
    public TrainerPartyTemplateDefinition trainerPartyTemplate;
    public BattleAIProfile battleAIProfile;
    public int battleUnitCount;
    public IReadOnlyList<Vector2> movementPattern;
    public CustomizationPresetDefinition customizationPreset;
    public IReadOnlyList<CustomizationPartDefinition> customizationParts;
    public bool replacePresetCustomizationParts;

    public static NPCGeneratedProfile Empty(int seed, string poolId) {
        return new NPCGeneratedProfile {
            seed = seed,
            poolId = poolId,
            variantId = string.Empty,
            displayName = string.Empty,
            role = NPCVariantRole.Civilian
        };
    }
}
