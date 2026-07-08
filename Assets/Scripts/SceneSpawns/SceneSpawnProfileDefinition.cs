using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SceneSpawnCategory {
    General,
    NPC,
    Trainer,
    Pokemon,
    Item,
    Resource,
    Farming,
    Encounter,
    Quest,
    Market,
    Police,
    Research,
    Transit,
    Decoration,
    Custom
}

public enum SceneSpawnSelectionMode {
    WeightedRandom,
    FirstAvailable,
    AllAvailable
}

[CreateAssetMenu(menuName = "Scene Spawns/Scene Spawn Profile Definition")]
public class SceneSpawnProfileDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this spawn profile. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what this profile can spawn and why.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad category used by validators, requirements and future UI filters.")]
    [SerializeField] SceneSpawnCategory category = SceneSpawnCategory.General;
    [Tooltip("Free-form tags such as route, city, market, roaming, seasonal or story.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Selection")]
    [Tooltip("How available entries are selected when this profile spawns.")]
    [SerializeField] SceneSpawnSelectionMode selectionMode = SceneSpawnSelectionMode.WeightedRandom;
    [Tooltip("Minimum number of entries selected when Count Override is not supplied by the controller.")]
    [Min(0)]
    [SerializeField] int minSpawnCount = 1;
    [Tooltip("Maximum number of entries selected when Count Override is not supplied by the controller.")]
    [Min(0)]
    [SerializeField] int maxSpawnCount = 1;
    [Tooltip("If enabled, the same entry can be selected more than once in a single spawn batch.")]
    [SerializeField] bool allowDuplicateEntries;

    [Header("Requirements")]
    [Tooltip("How profile-level requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements checked before any entry in this profile can spawn.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Entries")]
    [Tooltip("Prefab entries this profile can choose from. The ScriptableObject stores references only; no assets are created automatically.")]
    [SerializeField] List<SceneSpawnEntry> entries = new List<SceneSpawnEntry>();

    [Header("Events")]
    [Tooltip("Optional event published when this profile spawns at least one object. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition spawnedEvent = null;
    [Tooltip("Optional event published when this profile is blocked or produces no spawn. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition failedEvent = null;
    [Tooltip("If enabled, spawn events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed;
    [Tooltip("If enabled, spawn events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public SceneSpawnCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public SceneSpawnSelectionMode SelectionMode => selectionMode;
    public int MinSpawnCount => Mathf.Max(0, minSpawnCount);
    public int MaxSpawnCount => Mathf.Max(MinSpawnCount, maxSpawnCount);
    public bool AllowDuplicateEntries => allowDuplicateEntries;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<SceneSpawnEntry> Entries => entries != null ? (IReadOnlyList<SceneSpawnEntry>)entries : Array.Empty<SceneSpawnEntry>();
    public GameEventDefinition SpawnedEvent => spawnedEvent;
    public GameEventDefinition FailedEvent => failedEvent;
    public bool ShowEventsInFeed => showEventsInFeed;
    public bool WriteEventsToDebugLog => writeEventsToDebugLog;

    public bool CanUse(PlayerController player, out string failureMessage) {
        return ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage);
    }

    public List<SceneSpawnEntry> SelectEntries(PlayerController player, int? countOverride = null) {
        var availableEntries = Entries
            .Where(entry => entry != null && entry.CanSpawn(player, out _))
            .ToList();

        if(availableEntries.Count == 0) {
            return new List<SceneSpawnEntry>();
        }

        if(selectionMode == SceneSpawnSelectionMode.AllAvailable) {
            return availableEntries;
        }

        int count = ResolveSpawnCount(countOverride);
        if(count <= 0) {
            return new List<SceneSpawnEntry>();
        }

        return selectionMode == SceneSpawnSelectionMode.FirstAvailable
            ? availableEntries.Take(count).ToList()
            : SelectWeighted(availableEntries, count);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishSpawned(SceneSpawnRunResult result, PlayerController player, UnityEngine.Object context) {
        PublishSpawnEvent(spawnedEvent, "spawned", result, player, context, GameEventImportance.Info);
    }

    public void PublishFailed(SceneSpawnRunResult result, PlayerController player, UnityEngine.Object context) {
        PublishSpawnEvent(failedEvent, "failed", result, player, context, GameEventImportance.Warning);
    }

    int ResolveSpawnCount(int? countOverride) {
        if(countOverride.HasValue && countOverride.Value >= 0) {
            return countOverride.Value;
        }

        int min = MinSpawnCount;
        int max = MaxSpawnCount;
        return max <= min ? min : UnityEngine.Random.Range(min, max + 1);
    }

    List<SceneSpawnEntry> SelectWeighted(List<SceneSpawnEntry> candidates, int count) {
        var pool = candidates.Where(entry => entry != null && entry.Weight > 0).ToList();
        var selected = new List<SceneSpawnEntry>();
        int safety = Mathf.Max(1, count * Mathf.Max(1, pool.Count + 1));

        while(selected.Count < count && pool.Count > 0 && safety-- > 0) {
            var entry = RollWeighted(pool);
            if(entry == null) {
                break;
            }

            selected.Add(entry);
            if(!allowDuplicateEntries) {
                pool.Remove(entry);
            }
        }

        return selected;
    }

    SceneSpawnEntry RollWeighted(List<SceneSpawnEntry> pool) {
        int totalWeight = pool.Sum(entry => Mathf.Max(0, entry.Weight));
        if(totalWeight <= 0) {
            return pool.FirstOrDefault();
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        foreach(var entry in pool) {
            roll -= Mathf.Max(0, entry.Weight);
            if(roll < 0) {
                return entry;
            }
        }

        return pool.LastOrDefault();
    }

    void PublishSpawnEvent(GameEventDefinition eventDefinition, string phase, SceneSpawnRunResult result, PlayerController player, UnityEngine.Object context, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"scene-spawn.{phase}.{Id}",
            phase == "spawned" ? $"{DisplayName} spawned {result?.spawnedObjects ?? 0} object(s)." : $"{DisplayName} spawn failed: {result?.failureMessage}",
            GameEventCategory.SceneSpawn,
            importance,
            context != null ? context : player != null ? player : this,
            "SceneSpawnProfileDefinition",
            GameEventScope.Scene,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("profileId", Id),
            GameEventPublishing.Value("profileName", DisplayName),
            GameEventPublishing.Value("spawnerId", result != null ? result.spawnerId : string.Empty),
            GameEventPublishing.Value("spawnerName", result != null ? result.spawnerName : string.Empty),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("selectedEntries", result != null ? result.selectedEntries : 0),
            GameEventPublishing.Value("spawnedObjects", result != null ? result.spawnedObjects : 0),
            GameEventPublishing.Value("blocked", result != null && result.blocked),
            GameEventPublishing.Value("failureMessage", result != null ? result.failureMessage : string.Empty));
    }
}

[Serializable]
public class SceneSpawnEntry {
    [Tooltip("Stable id for this entry inside the spawn profile. Empty uses the prefab or display name.")]
    [SerializeField] string entryId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the prefab name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Prefab instantiated by this entry when selected.")]
    [SerializeField] GameObject prefab = null;
    [Tooltip("If disabled, this entry is ignored by selection.")]
    [SerializeField] bool enabled = true;
    [Tooltip("Weighted Random selection weight. 0 means this entry is ignored by weighted rolls.")]
    [Min(0)]
    [SerializeField] int weight = 1;

    [Header("Requirements")]
    [Tooltip("How entry-level requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements checked before this specific entry can spawn.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Transform Offsets")]
    [Tooltip("World-space offset added to the selected spawn point position.")]
    [SerializeField] Vector3 localPositionOffset = Vector3.zero;
    [Tooltip("Rotation offset multiplied by the selected spawn point rotation.")]
    [SerializeField] Vector3 localRotationEuler = Vector3.zero;
    [Tooltip("If enabled, Local Scale Override is applied to the spawned instance.")]
    [SerializeField] bool overrideLocalScale;
    [Tooltip("Local scale applied to the spawned instance when Override Local Scale is enabled.")]
    [SerializeField] Vector3 localScale = Vector3.one;

    [Header("Consequences")]
    [Tooltip("Consequence chains applied after this entry successfully spawns.")]
    [SerializeField] List<ConsequenceChainDefinition> spawnedChains = new List<ConsequenceChainDefinition>();

    public string EntryId => !string.IsNullOrWhiteSpace(entryId) ? entryId : prefab != null ? prefab.name : !string.IsNullOrWhiteSpace(displayName) ? displayName : "scene-spawn-entry";
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : prefab != null ? prefab.name : !string.IsNullOrWhiteSpace(entryId) ? entryId : "Scene Spawn Entry";
    public GameObject Prefab => prefab;
    public bool Enabled => enabled;
    public int Weight => Mathf.Max(0, weight);
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public Vector3 LocalPositionOffset => localPositionOffset;
    public Vector3 LocalRotationEuler => localRotationEuler;
    public bool OverrideLocalScale => overrideLocalScale;
    public Vector3 LocalScale => localScale;
    public IReadOnlyList<ConsequenceChainDefinition> SpawnedChains => spawnedChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)spawnedChains : Array.Empty<ConsequenceChainDefinition>();

    public bool CanSpawn(PlayerController player, out string failureMessage) {
        if(!enabled) {
            failureMessage = "Entry is disabled.";
            return false;
        }

        if(prefab == null) {
            failureMessage = "Entry prefab is missing.";
            return false;
        }

        return ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage);
    }
}

public class SceneSpawnRunResult {
    public readonly string profileId;
    public readonly string profileName;
    public readonly string spawnerId;
    public readonly string spawnerName;
    public int selectedEntries;
    public int spawnedObjects;
    public int skippedEntries;
    public int appliedChains;
    public int blockedChains;
    public bool blocked;
    public string failureMessage;
    public readonly List<string> messages = new List<string>();

    public SceneSpawnRunResult(string profileId, string profileName, string spawnerId, string spawnerName) {
        this.profileId = profileId;
        this.profileName = profileName;
        this.spawnerId = spawnerId;
        this.spawnerName = spawnerName;
    }
}
