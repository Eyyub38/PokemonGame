using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(50)]
public class CharacterCustomizationRenderer : MonoBehaviour {
    [Header("Defaults")]
    [Tooltip("Preset applied on Start when Apply Preset On Start is enabled.")]
    [SerializeField] CustomizationPresetDefinition defaultPreset;
    [Tooltip("If enabled, Default Preset is applied during Start.")]
    [SerializeField] bool applyPresetOnStart;
    [Tooltip("If enabled, applying a preset also applies its base visual set to CharacterAnimator.")]
    [SerializeField] bool applyBaseVisualSet = true;

    [Header("Layer Rendering")]
    [Tooltip("Parent transform for generated layer renderers. Empty uses this transform.")]
    [SerializeField] Transform layerRoot;
    [Tooltip("If enabled, missing child SpriteRenderers are created at runtime for equipped parts.")]
    [SerializeField] bool createMissingRenderers = true;
    [Tooltip("Runtime/customization layers currently rendered on this character.")]
    [SerializeField] List<CustomizationLayerState> layers = new List<CustomizationLayerState>();

    CharacterAnimator characterAnimator;
    SpriteRenderer baseRenderer;

    public CustomizationPresetDefinition DefaultPreset => defaultPreset;
    public IReadOnlyList<CustomizationLayerState> Layers => layers;
    public IEnumerable<CustomizationPartDefinition> EquippedParts => layers.Where(l => l != null && l.Part != null).Select(l => l.Part);

    void Awake() {
        characterAnimator = GetComponent<CharacterAnimator>();
        baseRenderer = GetComponent<SpriteRenderer>();
        if(layerRoot == null) {
            layerRoot = transform;
        }
    }

    void Start() {
        if(applyPresetOnStart && defaultPreset != null) {
            ApplyPreset(defaultPreset, replaceParts: true);
        } else {
            RefreshLayerRenderers();
        }
    }

    void LateUpdate() {
        UpdateLayerSprites();
    }

    public void ApplyPreset(CustomizationPresetDefinition preset, bool replaceParts) {
        if(preset == null) {
            return;
        }

        if(applyBaseVisualSet && preset.BaseVisualSet != null) {
            characterAnimator = characterAnimator != null ? characterAnimator : GetComponent<CharacterAnimator>();
            characterAnimator?.ApplyVisualSet(preset.BaseVisualSet);
        }

        if(replaceParts) {
            SetParts(preset.GetUniqueDefaultParts(), replaceExisting: true);
        } else {
            EquipParts(preset.GetUniqueDefaultParts());
        }
    }

    public void SetParts(IEnumerable<CustomizationPartDefinition> parts, bool replaceExisting) {
        if(replaceExisting) {
            HideLayerRenderers();
            layers.Clear();
        }

        EquipParts(parts);
        RefreshLayerRenderers();
        UpdateLayerSprites();
    }

    public void EquipParts(IEnumerable<CustomizationPartDefinition> parts) {
        if(parts == null) {
            return;
        }

        foreach(var part in parts) {
            EquipPart(part, part == null || part.ExclusiveInSlot);
        }
    }

    public bool EquipPart(CustomizationPartDefinition part, bool replaceSlot = true) {
        if(part == null) {
            return false;
        }

        if(replaceSlot) {
            layers.RemoveAll(layer => layer == null || (layer.Part != null && layer.Part.Slot == part.Slot));
        }

        var existing = layers.FirstOrDefault(layer => layer != null && layer.Part == part);
        if(existing == null) {
            layers.Add(new CustomizationLayerState { part = part });
        }

        RefreshLayerRenderers();
        UpdateLayerSprites();
        return true;
    }

    public bool UnequipSlot(CustomizationSlot slot) {
        foreach(var layer in layers.Where(layer => layer != null && layer.Part != null && layer.Part.Slot == slot)) {
            if(layer.Renderer != null) {
                layer.Renderer.sprite = null;
                layer.Renderer.enabled = false;
            }
        }

        bool removed = layers.RemoveAll(layer => layer != null && layer.Part != null && layer.Part.Slot == slot) > 0;
        if(removed) {
            UpdateLayerSprites();
        }

        return removed;
    }

    public bool HasEquippedPart(CustomizationPartDefinition part) {
        return part != null && layers.Any(layer => layer != null && layer.Part == part);
    }

    public bool HasEquippedPartWithTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag) && layers.Any(layer => layer != null && layer.Part != null && layer.Part.HasTag(tag));
    }

    public bool HasEquippedSlot(CustomizationSlot slot) {
        return layers.Any(layer => layer != null && layer.Part != null && layer.Part.Slot == slot);
    }

    public void RefreshLayerRenderers() {
        baseRenderer = baseRenderer != null ? baseRenderer : GetComponent<SpriteRenderer>();
        for(int i = 0; i < layers.Count; i++) {
            var layer = layers[i];
            if(layer == null || layer.Part == null) {
                continue;
            }

            layer.Renderer = layer.Renderer != null ? layer.Renderer : FindExistingRenderer(layer.Part);
            if(layer.Renderer == null && createMissingRenderers) {
                layer.Renderer = CreateRenderer(layer.Part);
            }

            ApplyRendererSettings(layer);
        }
    }

    void UpdateLayerSprites() {
        characterAnimator = characterAnimator != null ? characterAnimator : GetComponent<CharacterAnimator>();
        if(characterAnimator == null) {
            return;
        }

        var animationState = characterAnimator.CurrentAnimationState;
        var direction = characterAnimator.CurrentFacingDirection;
        int frameIndex = characterAnimator.CurrentFrameIndex;

        foreach(var layer in layers) {
            if(layer == null || layer.Part == null || layer.Renderer == null) {
                continue;
            }

            var sprite = layer.Part.GetSprite(animationState, direction, frameIndex);
            layer.Renderer.sprite = sprite;
            layer.Renderer.enabled = sprite != null;
            layer.Renderer.color = layer.Part.Tint;
            ApplyRendererSettings(layer);
        }
    }

    void HideLayerRenderers() {
        foreach(var layer in layers) {
            if(layer?.Renderer == null) {
                continue;
            }

            layer.Renderer.sprite = null;
            layer.Renderer.enabled = false;
        }
    }

    SpriteRenderer FindExistingRenderer(CustomizationPartDefinition part) {
        string layerName = BuildLayerName(part);
        var renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        return renderers.FirstOrDefault(renderer => renderer != null && renderer.gameObject.name == layerName);
    }

    SpriteRenderer CreateRenderer(CustomizationPartDefinition part) {
        var go = new GameObject(BuildLayerName(part));
        go.transform.SetParent(layerRoot != null ? layerRoot : transform, worldPositionStays: false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.AddComponent<SpriteRenderer>();
    }

    void ApplyRendererSettings(CustomizationLayerState layer) {
        if(layer?.Renderer == null || layer.Part == null || baseRenderer == null) {
            return;
        }

        layer.Renderer.sortingLayerID = baseRenderer.sortingLayerID;
        layer.Renderer.sortingOrder = baseRenderer.sortingOrder + layer.Part.SortingOrderOffset;
        layer.Renderer.flipX = baseRenderer.flipX;
    }

    string BuildLayerName(CustomizationPartDefinition part) {
        return part != null ? $"Customization_{part.Slot}_{part.Id}" : "Customization_Layer";
    }
}

[Serializable]
public class CustomizationLayerState {
    [Tooltip("Customization part rendered by this layer.")]
    public CustomizationPartDefinition part;
    [Tooltip("SpriteRenderer used for this layer. Can be assigned manually or created at runtime.")]
    public SpriteRenderer renderer;

    public CustomizationPartDefinition Part {
        get => part;
        set => part = value;
    }

    public SpriteRenderer Renderer {
        get => renderer;
        set => renderer = value;
    }
}
