using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SceneSpawnPointSelectionMode {
    Sequential,
    Random
}

public class SceneSpawnController : MonoBehaviour, IPlayerTriggerable {
    [Header("Identity")]
    [Tooltip("Stable source id used by spawn history and consequence chains. Empty uses GameObject name.")]
    [SerializeField] string spawnerId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Profile")]
    [Tooltip("Spawn profile that decides which prefab entries can appear.")]
    [SerializeField] SceneSpawnProfileDefinition profile = null;
    [Tooltip("Optional player override. Empty uses PlayerController.i or the first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Signals")]
    [Tooltip("If enabled, this spawner runs once during Start.")]
    [SerializeField] bool spawnOnStart = true;
    [Tooltip("If enabled, this spawner runs whenever the component enables.")]
    [SerializeField] bool spawnOnEnable;
    [Tooltip("If enabled, entering this trigger can run the spawner.")]
    [SerializeField] bool spawnOnPlayerTrigger;
    [Tooltip("If enabled, repeated player triggers can run this spawner more than once.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("If enabled, this spawner runs when GameEventBus publishes an event. Scene Spawn events are ignored to prevent feedback loops.")]
    [SerializeField] bool spawnOnGameEvents;
    [Tooltip("If enabled, event bus history is replayed when this component enables.")]
    [SerializeField] bool replayGameEventHistoryOnEnable;
    [Tooltip("If enabled, this spawner runs when TimeSystem time changes.")]
    [SerializeField] bool spawnOnTimeChanged;
    [Tooltip("If enabled, this spawner runs when TimeSystem day changes.")]
    [SerializeField] bool spawnOnDayChanged;

    [Header("Placement")]
    [Tooltip("Optional parent for spawned prefab instances. Empty leaves them at scene root.")]
    [SerializeField] Transform spawnParent = null;
    [Tooltip("Candidate spawn points used by the controller. Empty can use this transform as a fallback.")]
    [SerializeField] List<Transform> spawnPoints = new List<Transform>();
    [Tooltip("How spawn points are chosen from the Spawn Points list.")]
    [SerializeField] SceneSpawnPointSelectionMode spawnPointSelectionMode = SceneSpawnPointSelectionMode.Sequential;
    [Tooltip("If enabled and Spawn Points is empty, this transform is used as the spawn point.")]
    [SerializeField] bool useOwnTransformAsFallbackPoint = true;
    [Tooltip("Overrides the profile min/max count when 0 or higher. -1 means use the profile count.")]
    [Min(-1)]
    [SerializeField] int countOverride = -1;
    [Tooltip("If enabled, existing objects spawned by this controller are destroyed before a new spawn batch.")]
    [SerializeField] bool clearExistingBeforeSpawn = true;
    [Tooltip("If enabled, objects spawned by this controller are destroyed when the component disables.")]
    [SerializeField] bool destroySpawnedOnDisable;

    [Header("Repeat Rules")]
    [Tooltip("How often this spawner can create successful batches for the current player.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.Unlimited;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum successful spawn records for this profile. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxSuccessfulSpawns;
    [Tooltip("If enabled, successful spawns are stored in PlayerSceneSpawnLog.")]
    [SerializeField] bool recordHistory = true;
    [Tooltip("If enabled, blocked spawn attempts are also stored in PlayerSceneSpawnLog.")]
    [SerializeField] bool recordBlockedAttempts;

    [Header("Consequences")]
    [Tooltip("Consequence chains applied after this controller successfully spawns at least one object.")]
    [SerializeField] List<ConsequenceChainDefinition> batchSpawnedChains = new List<ConsequenceChainDefinition>();
    [Tooltip("Consequence chains applied when this controller is blocked before spawning.")]
    [SerializeField] List<ConsequenceChainDefinition> blockedChains = new List<ConsequenceChainDefinition>();

    [Header("Debug")]
    [Tooltip("If enabled, spawn attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    readonly List<GameObject> spawnedObjects = new List<GameObject>();
    bool timeSubscribed;
    bool isSpawning;

    public string SpawnerId => string.IsNullOrWhiteSpace(spawnerId) ? name : spawnerId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public SceneSpawnProfileDefinition Profile => profile;
    public IReadOnlyList<Transform> SpawnPoints => spawnPoints;
    public bool UseOwnTransformAsFallbackPoint => useOwnTransformAsFallbackPoint;
    public IReadOnlyList<ConsequenceChainDefinition> BatchSpawnedChains => batchSpawnedChains;
    public IReadOnlyList<ConsequenceChainDefinition> BlockedChains => blockedChains;
    public IReadOnlyList<GameObject> SpawnedObjects => spawnedObjects;
    public bool TriggerRepeatedly => triggerRepeatedly;

    void OnEnable() {
        if(spawnOnGameEvents) {
            GameEventBus.Subscribe(HandleGameEvent, replayGameEventHistoryOnEnable);
        }

        SubscribeTime();
        if(spawnOnEnable) {
            Spawn();
        }
    }

    void Start() {
        SubscribeTime();
        if(spawnOnStart) {
            Spawn();
        }
    }

    void OnDisable() {
        if(spawnOnGameEvents) {
            GameEventBus.Unsubscribe(HandleGameEvent);
        }

        UnsubscribeTime();
        if(destroySpawnedOnDisable) {
            ClearSpawned();
        }
    }

    [ContextMenu("Spawn Now")]
    public void SpawnFromContextMenu() {
        Spawn();
    }

    [ContextMenu("Clear Spawned Objects")]
    public void ClearSpawnedFromContextMenu() {
        ClearSpawned();
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(spawnOnPlayerTrigger) {
            Spawn(player);
        }
    }

    public SceneSpawnRunResult Spawn() {
        return Spawn(ResolvePlayer());
    }

    public SceneSpawnRunResult Spawn(PlayerController player) {
        var result = new SceneSpawnRunResult(
            profile != null ? profile.Id : string.Empty,
            profile != null ? profile.DisplayName : string.Empty,
            SpawnerId,
            DisplayName);

        if(isSpawning) {
            result.blocked = true;
            result.failureMessage = "Spawner is already running.";
            return result;
        }

        isSpawning = true;
        try {
            var log = player != null ? player.GetComponent<PlayerSceneSpawnLog>() ?? player.gameObject.AddComponent<PlayerSceneSpawnLog>() : null;
            if(profile == null) {
                return HandleBlocked(result, player, log, "Scene spawn profile is missing.");
            }

            if(log != null && !log.CanSpawn(profile, SpawnerId, repeatMode, cooldownHours, maxSuccessfulSpawns, out var repeatFailure)) {
                return HandleBlocked(result, player, log, repeatFailure);
            }

            if(!profile.CanUse(player, out var requirementFailure)) {
                return HandleBlocked(result, player, log, requirementFailure);
            }

            var selectedEntries = profile.SelectEntries(player, countOverride >= 0 ? (int?)countOverride : null);
            result.selectedEntries = selectedEntries.Count;
            if(selectedEntries.Count == 0) {
                return HandleBlocked(result, player, log, "No available spawn entries were selected.");
            }

            if(clearExistingBeforeSpawn) {
                ClearSpawned();
            }

            for(int i = 0; i < selectedEntries.Count; i++) {
                SpawnEntry(selectedEntries[i], i, selectedEntries.Count, player, log, result);
            }

            if(result.spawnedObjects <= 0) {
                return HandleBlocked(result, player, log, "Selected entries could not be instantiated.");
            }

            ApplyConsequenceChains(player, batchSpawnedChains, "batch-spawned", result);
            if(recordHistory && log != null) {
                foreach(var message in result.messages.Where(message => !string.IsNullOrWhiteSpace(message))) {
                    GameDebugLogger.Ensure().Record(GameDebugSeverity.Trace, GameDebugCategory.SceneSpawn, message, this, "SceneSpawnController", echoToUnity: false);
                }
            }

            profile.PublishSpawned(result, player, this);
            WriteAttemptLog(result);
            return result;
        } finally {
            isSpawning = false;
        }
    }

    public void ClearSpawned() {
        for(int i = spawnedObjects.Count - 1; i >= 0; i--) {
            var spawned = spawnedObjects[i];
            if(spawned == null) {
                continue;
            }

            if(Application.isPlaying) {
                Destroy(spawned);
            } else {
                DestroyImmediate(spawned);
            }
        }

        spawnedObjects.Clear();
    }

    void SpawnEntry(SceneSpawnEntry entry, int batchIndex, int batchSize, PlayerController player, PlayerSceneSpawnLog log, SceneSpawnRunResult result) {
        string entryFailure = null;
        if(entry == null || !entry.CanSpawn(player, out entryFailure)) {
            result.skippedEntries++;
            if(!string.IsNullOrWhiteSpace(entryFailure)) {
                result.messages.Add(entryFailure);
            }
            return;
        }

        var point = ResolveSpawnPoint(batchIndex);
        if(point == null) {
            result.skippedEntries++;
            result.messages.Add("No spawn point was available.");
            return;
        }

        Vector3 position = point.position + entry.LocalPositionOffset;
        Quaternion rotation = point.rotation * Quaternion.Euler(entry.LocalRotationEuler);
        var instance = Instantiate(entry.Prefab, position, rotation, spawnParent);
        if(entry.OverrideLocalScale) {
            instance.transform.localScale = entry.LocalScale;
        }

        spawnedObjects.Add(instance);
        result.spawnedObjects++;
        if(recordHistory && log != null) {
            log.RecordSpawn(profile, SpawnerId, DisplayName, entry, entry.Prefab != null ? entry.Prefab.name : instance.name, batchIndex, batchSize);
        }

        ApplyConsequenceChains(player, entry.SpawnedChains, $"entry-spawned:{entry.EntryId}", result);
    }

    SceneSpawnRunResult HandleBlocked(SceneSpawnRunResult result, PlayerController player, PlayerSceneSpawnLog log, string failureMessage) {
        result.blocked = true;
        result.failureMessage = string.IsNullOrWhiteSpace(failureMessage) ? "Scene spawn was blocked." : failureMessage;
        if(profile != null && recordHistory && recordBlockedAttempts && log != null) {
            log.RecordSpawn(profile, SpawnerId, DisplayName, null, null, 0, 0, blocked: true, failureMessage: result.failureMessage, messages: result.messages);
        }

        ApplyConsequenceChains(player, blockedChains, "blocked", result);
        profile?.PublishFailed(result, player, this);
        WriteAttemptLog(result);
        return result;
    }

    void ApplyConsequenceChains(PlayerController player, IEnumerable<ConsequenceChainDefinition> chains, string phase, SceneSpawnRunResult result) {
        if(player == null || chains == null) {
            return;
        }

        var context = new ConsequenceChainContext {
            SourceId = $"{SpawnerId}:{phase}",
            SourceName = DisplayName,
            ContextObject = this
        };

        foreach(var chain in chains) {
            if(chain == null) {
                result.skippedEntries++;
                continue;
            }

            var chainResult = chain.Apply(player, context, this);
            if(chainResult != null && !chainResult.blocked) {
                result.appliedChains++;
            } else {
                result.blockedChains++;
                if(chainResult != null && !string.IsNullOrWhiteSpace(chainResult.failureMessage)) {
                    result.messages.Add($"{chain.DisplayName}: {chainResult.failureMessage}");
                }
            }
        }
    }

    Transform ResolveSpawnPoint(int batchIndex) {
        var validPoints = spawnPoints != null ? spawnPoints.Where(point => point != null).ToList() : new List<Transform>();
        if(validPoints.Count == 0) {
            return useOwnTransformAsFallbackPoint ? transform : null;
        }

        if(spawnPointSelectionMode == SceneSpawnPointSelectionMode.Random) {
            return validPoints[Random.Range(0, validPoints.Count)];
        }

        return validPoints[Mathf.Abs(batchIndex) % validPoints.Count];
    }

    void WriteAttemptLog(SceneSpawnRunResult result) {
        if(!logAttempts || result == null) {
            return;
        }

        GameDebugLogger.Ensure().Record(
            result.blocked ? GameDebugSeverity.Warning : GameDebugSeverity.Info,
            GameDebugCategory.SceneSpawn,
            result.blocked ? $"{DisplayName} spawn blocked: {result.failureMessage}" : $"{DisplayName} spawned {result.spawnedObjects} object(s).",
            this,
            "SceneSpawnController");
    }

    void HandleGameEvent(GameEventRecord record) {
        if(isSpawning || record == null || record.category == GameEventCategory.SceneSpawn) {
            return;
        }

        Spawn();
    }

    void HandleTimeChanged() {
        Spawn();
    }

    void SubscribeTime() {
        if(timeSubscribed || TimeSystem.i == null) {
            return;
        }

        if(spawnOnTimeChanged) {
            TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        }

        if(spawnOnDayChanged) {
            TimeSystem.i.OnDayChanged += HandleTimeChanged;
        }

        timeSubscribed = spawnOnTimeChanged || spawnOnDayChanged;
    }

    void UnsubscribeTime() {
        if(!timeSubscribed || TimeSystem.i == null) {
            timeSubscribed = false;
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleTimeChanged;
        timeSubscribed = false;
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }
}
