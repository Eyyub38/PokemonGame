using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum NPCVariantSeedMode {
    StableFromSceneAndPosition,
    FixedSeed,
    FreshRandom
}

public class NPCVariantRandomizer : MonoBehaviour, ISavable {
    [Header("Pool")]
    [Tooltip("Variant pool used to generate this NPC. No ScriptableObject assets are created by this component.")]
    [SerializeField] NPCVariantPoolDefinition variantPool;
    [Tooltip("If disabled, this object is ignored by the randomizer.")]
    [SerializeField] bool randomizationEnabled = true;
    [Tooltip("If enabled, a profile is generated during Start when no saved profile exists.")]
    [SerializeField] bool randomizeOnStart = true;
    [Tooltip("If enabled, this component applies the same generated profile every time instead of rerolling after generation.")]
    [SerializeField] bool keepGeneratedProfile = true;

    [Header("Seed")]
    [Tooltip("How the random seed is chosen.")]
    [SerializeField] NPCVariantSeedMode seedMode = NPCVariantSeedMode.StableFromSceneAndPosition;
    [Tooltip("Fixed seed used when Seed Mode is Fixed Seed.")]
    [SerializeField] int fixedSeed;
    [Tooltip("Optional stable key used with scene/position seed generation. Empty uses GameObject name.")]
    [SerializeField] string stableKey;

    [Header("Apply Targets")]
    [Tooltip("If enabled, applies selected visual set to CharacterAnimator.")]
    [SerializeField] bool applyVisuals = true;
    [Tooltip("If enabled, applies generated name/dialog to NPCController.")]
    [SerializeField] bool applyNpcDialog = true;
    [Tooltip("If enabled, applies generated name/dialog/battle image to TrainerController.")]
    [SerializeField] bool applyTrainerProfile = true;
    [Tooltip("If enabled, applies generated party template to PokemonParty when this object is a trainer.")]
    [SerializeField] bool applyTrainerParty = true;
    [Tooltip("If enabled, applies generated personality to PersonalityProfile.")]
    [SerializeField] bool applyPersonality = true;
    [Tooltip("If enabled, renames the GameObject to include the generated display name.")]
    [SerializeField] bool renameGameObject;

    [Header("Debug")]
    [Tooltip("If enabled, generation is written to GameDebugLogger/GameEventBus.")]
    [SerializeField] bool logGeneration;

    [Header("Runtime")]
    [Tooltip("Whether this NPC has already generated a profile.")]
    [SerializeField] bool hasGenerated;
    [Tooltip("Seed used by the generated profile.")]
    [SerializeField] int generatedSeed;
    [Tooltip("Variant id selected from the pool.")]
    [SerializeField] string generatedVariantId;
    [Tooltip("Display name selected from the pool.")]
    [SerializeField] string generatedDisplayName;

    public NPCVariantPoolDefinition VariantPool => variantPool;
    public bool HasGenerated => hasGenerated;
    public int GeneratedSeed => generatedSeed;
    public string GeneratedVariantId => generatedVariantId;
    public string GeneratedDisplayName => generatedDisplayName;

    void Start() {
        if(randomizeOnStart && randomizationEnabled) {
            GenerateAndApply(forceReroll: !keepGeneratedProfile);
        }
    }

    [ContextMenu("Generate And Apply NPC Variant")]
    public void GenerateAndApplyFromContextMenu() {
        GenerateAndApply(forceReroll: true);
    }

    public bool GenerateAndApply(bool forceReroll = false) {
        if(!randomizationEnabled || variantPool == null) {
            return false;
        }

        if(!hasGenerated || forceReroll || !keepGeneratedProfile) {
            generatedSeed = ResolveSeed();
            hasGenerated = true;
        }

        var profile = variantPool.Generate(generatedSeed);
        generatedVariantId = profile.variantId;
        generatedDisplayName = profile.displayName;
        ApplyProfile(profile);
        PublishGenerationEvent(profile);
        return true;
    }

    public bool GenerateAndApplyExternal(
        NPCVariantPoolDefinition pool,
        int seed,
        bool rememberGeneratedProfile = true,
        bool forceApplyWhenDisabled = false
    ) {
        if(pool == null || (!randomizationEnabled && !forceApplyWhenDisabled)) {
            return false;
        }

        variantPool = pool;
        generatedSeed = seed;
        hasGenerated = rememberGeneratedProfile;

        var profile = variantPool.Generate(generatedSeed);
        generatedVariantId = profile.variantId;
        generatedDisplayName = profile.displayName;
        ApplyProfile(profile);
        PublishGenerationEvent(profile);
        return true;
    }

    void ApplyProfile(NPCGeneratedProfile profile) {
        if(profile == null) {
            return;
        }

        if(applyVisuals) {
            GetComponent<CharacterAnimator>()?.ApplyVisualSet(profile.visualSet);
            ApplyCustomization(profile);
        }

        if(applyNpcDialog) {
            GetComponent<NPCController>()?.ApplyGeneratedProfile(
                profile.displayName,
                profile.dialog,
                profile.conditionalDialog,
                profile.movementPattern);
        }

        if(applyTrainerProfile) {
            GetComponent<TrainerController>()?.ApplyGeneratedProfile(
                profile.displayName,
                profile.battleImage,
                profile.dialog,
                profile.dialogAfterBattle,
                profile.conditionalDialog,
                profile.conditionalDialogAfterBattle,
                profile.battleUnitCount,
                profile.battleAIProfile);
        }

        if(applyTrainerParty && profile.trainerPartyTemplate != null && GetComponent<TrainerController>() != null) {
            var party = GetComponent<PokemonParty>();
            if(party != null) {
                party.Pokemons = profile.trainerPartyTemplate.CreateParty(Hash(generatedSeed, "party"));
            }
        }

        if(applyPersonality && profile.personalityId.HasValue) {
            var personality = GetComponent<PersonalityProfile>() ?? gameObject.AddComponent<PersonalityProfile>();
            personality.SetPersonality(profile.personalityId.Value);
        }

        if(renameGameObject && !string.IsNullOrWhiteSpace(profile.displayName)) {
            gameObject.name = profile.displayName;
        }
    }

    void ApplyCustomization(NPCGeneratedProfile profile) {
        bool hasPreset = profile.customizationPreset != null;
        bool hasParts = profile.customizationParts != null && profile.customizationParts.Count > 0;
        if(!hasPreset && !hasParts) {
            return;
        }

        var customization = GetComponent<CharacterCustomizationRenderer>() ?? gameObject.AddComponent<CharacterCustomizationRenderer>();
        if(hasPreset) {
            customization.ApplyPreset(profile.customizationPreset, replaceParts: profile.replacePresetCustomizationParts);
        }

        if(hasParts) {
            customization.SetParts(profile.customizationParts.Where(part => part != null), replaceExisting: profile.replacePresetCustomizationParts);
        }
    }

    int ResolveSeed() {
        if(seedMode == NPCVariantSeedMode.FixedSeed) {
            return fixedSeed;
        }

        if(seedMode == NPCVariantSeedMode.FreshRandom) {
            return UnityEngine.Random.Range(1, int.MaxValue);
        }

        string key = string.IsNullOrWhiteSpace(stableKey) ? name : stableKey;
        var scene = SceneManager.GetActiveScene().name;
        var pos = transform.position;
        string raw = $"{scene}|{key}|{variantPool.Id}|{Mathf.RoundToInt(pos.x * 10f)}|{Mathf.RoundToInt(pos.y * 10f)}";
        return Hash(raw);
    }

    void PublishGenerationEvent(NPCGeneratedProfile profile) {
        if(!logGeneration || profile == null) {
            return;
        }

        GameEventPublishing.PublishOptional(
            null,
            $"npc.generated.{variantPool.Id}.{generatedVariantId}",
            $"{name} generated NPC variant {generatedVariantId}.",
            GameEventCategory.NPC,
            GameEventImportance.Trace,
            this,
            "NPCVariantRandomizer",
            GameEventScope.Scene,
            showInFeed: false,
            writeToDebugLog: true,
            GameEventPublishing.Value("poolId", variantPool.Id),
            GameEventPublishing.Value("variantId", generatedVariantId),
            GameEventPublishing.Value("displayName", generatedDisplayName),
            GameEventPublishing.Value("seed", generatedSeed),
            GameEventPublishing.Value("role", profile.role));
    }

    public object CaptureState() {
        return new NPCVariantRandomizerSaveData {
            hasGenerated = hasGenerated,
            generatedSeed = generatedSeed,
            generatedVariantId = generatedVariantId,
            generatedDisplayName = generatedDisplayName
        };
    }

    public void RestoreState(object state) {
        var saveData = state as NPCVariantRandomizerSaveData;
        if(saveData == null) {
            return;
        }

        hasGenerated = saveData.hasGenerated;
        generatedSeed = saveData.generatedSeed;
        generatedVariantId = saveData.generatedVariantId;
        generatedDisplayName = saveData.generatedDisplayName;

        if(hasGenerated && variantPool != null) {
            var profile = variantPool.Generate(generatedSeed);
            generatedVariantId = profile.variantId;
            generatedDisplayName = profile.displayName;
            ApplyProfile(profile);
        }
    }

    static int Hash(int seed, string suffix) {
        return Hash($"{seed}|{suffix}");
    }

    static int Hash(string value) {
        unchecked {
            int hash = 23;
            for(int i = 0; i < value.Length; i++) {
                hash = hash * 31 + value[i];
            }
            return hash;
        }
    }
}

[Serializable]
public class NPCVariantRandomizerSaveData {
    public bool hasGenerated;
    public int generatedSeed;
    public string generatedVariantId;
    public string generatedDisplayName;
}
