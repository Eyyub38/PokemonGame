using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SceneObjectCategory {
    General,
    NPC,
    Item,
    Door,
    Portal,
    Shop,
    Resource,
    Farming,
    Encounter,
    Quest,
    Research,
    Police,
    Transit,
    Decoration,
    Custom
}

public enum SceneObjectState {
    Available,
    Hidden,
    Disabled,
    Resolved
}

[CreateAssetMenu(menuName = "Scene Objects/Scene Object Definition")]
public class SceneObjectDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this scene object. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what this logical scene object represents.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad category used by validators, requirements and future UI filters.")]
    [SerializeField] SceneObjectCategory category = SceneObjectCategory.General;
    [Tooltip("Free-form tags such as lab, police, market, route-blocker, seasonal or story.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Default State")]
    [Tooltip("State used when PlayerSceneObjectLog has no saved override for this object.")]
    [SerializeField] SceneObjectState defaultState = SceneObjectState.Available;
    [Tooltip("If enabled, Available state means scene components should be usable.")]
    [SerializeField] bool availableStateAllowsInteraction = true;
    [Tooltip("If enabled, Resolved state is treated as unavailable by conditional scene object components.")]
    [SerializeField] bool resolvedStateIsUnavailable = true;

    [Header("Events")]
    [Tooltip("Optional event published when this object's saved state changes. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition stateChangedEvent = null;
    [Tooltip("Optional event published when this object records an interaction. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition interactionEvent = null;
    [Tooltip("If enabled, scene object events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed;
    [Tooltip("If enabled, scene object events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public SceneObjectCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public SceneObjectState DefaultState => defaultState;
    public GameEventDefinition StateChangedEvent => stateChangedEvent;
    public GameEventDefinition InteractionEvent => interactionEvent;
    public bool ShowEventsInFeed => showEventsInFeed;
    public bool WriteEventsToDebugLog => writeEventsToDebugLog;

    public bool IsAvailableState(SceneObjectState state) {
        if(state == SceneObjectState.Available) {
            return availableStateAllowsInteraction;
        }

        if(state == SceneObjectState.Resolved) {
            return !resolvedStateIsUnavailable;
        }

        return false;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}
