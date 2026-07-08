using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum OverworldFleeRecoveryAction {
    MarkRecoveredOnly,
    SpawnPrefabAndMarkRecovered,
    EnableExistingObjectAndMarkRecovered
}

public class OverworldFleeRecoverySource : MonoBehaviour {
    [Header("References")]
    [Tooltip("Player context used to find PlayerOverworldFleeLog. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride = null;
    [Tooltip("Optional direct flee log reference. Empty uses the player or the first PlayerOverworldFleeLog in the scene.")]
    [SerializeField] PlayerOverworldFleeLog fleeLogOverride = null;
    [Tooltip("Escape node represented by this recovery source. Empty can use Escape Node Id or this transform position.")]
    [SerializeField] OverworldEncounterNode escapeNode = null;
    [Tooltip("Optional prefab spawned when recovering a virtual flee record.")]
    [SerializeField] GameObject recoveredPrefab = null;
    [Tooltip("Optional existing object enabled/moved when recovering a virtual flee record.")]
    [SerializeField] GameObject existingObject = null;

    [Header("Filtering")]
    [Tooltip("If not empty, only flee records with this entity id can be recovered.")]
    [SerializeField] string entityIdFilter = string.Empty;
    [Tooltip("If not empty, only flee records with this species id can be recovered.")]
    [SerializeField] string speciesIdFilter = string.Empty;
    [Tooltip("If not empty, only flee records with this source id can be recovered.")]
    [SerializeField] string sourceIdFilter = string.Empty;
    [Tooltip("If not empty, only flee records from this scene can be recovered. Empty can use the active scene when Match Active Scene is enabled.")]
    [SerializeField] string sceneNameFilter = string.Empty;
    [Tooltip("If enabled, only records from the currently active scene can be recovered when Scene Name Filter is empty.")]
    [SerializeField] bool matchActiveScene = true;
    [Tooltip("If not empty, only records with this escape node id can be recovered. Empty can use Escape Node.")]
    [SerializeField] string escapeNodeIdFilter = string.Empty;
    [Tooltip("If enabled, expired records are marked before candidate lookup.")]
    [SerializeField] bool pruneExpiredBeforeLookup = true;

    [Header("Recovery")]
    [Tooltip("What this source does when recovering a matching flee record.")]
    [SerializeField] OverworldFleeRecoveryAction recoveryAction = OverworldFleeRecoveryAction.SpawnPrefabAndMarkRecovered;
    [Tooltip("If enabled, recovered actors spawn at Escape Node or this source transform. If disabled, the saved last position is used.")]
    [SerializeField] bool spawnAtRecoverySource = true;
    [Tooltip("Local/world offset applied to recovered actor placement.")]
    [SerializeField] Vector3 spawnOffset = Vector3.zero;
    [Tooltip("If enabled, the recovered object name is set from the flee record display name.")]
    [SerializeField] bool renameRecoveredObject = true;
    [Tooltip("Message saved into the flee record when it is recovered.")]
    [SerializeField] string recoveredMessage = "Recovered from virtual flee state.";
    [Tooltip("If enabled, the first matching record is recovered automatically during Start.")]
    [SerializeField] bool recoverOnStart = false;

    [Header("Debug")]
    [Tooltip("If enabled, recover attempts and failures are written to GameDebug.")]
    [SerializeField] bool logStateChanges = false;

    public PlayerOverworldFleeLog FleeLog => ResolveFleeLog();
    public OverworldEncounterNode EscapeNode => escapeNode;
    public GameObject RecoveredPrefab => recoveredPrefab;
    public GameObject ExistingObject => existingObject;
    public string EntityIdFilter => entityIdFilter;
    public string SpeciesIdFilter => speciesIdFilter;
    public string SourceIdFilter => sourceIdFilter;
    public string SceneNameFilter => sceneNameFilter;
    public bool MatchActiveScene => matchActiveScene;
    public string EscapeNodeIdFilter => ResolveEscapeNodeId();
    public OverworldFleeRecoveryAction RecoveryAction => recoveryAction;
    public event Action<OverworldFleeRecoveryResult> OnRecovered;
    public event Action<string> OnRecoveryFailed;

    void Start() {
        if(recoverOnStart) {
            TryRecoverFirst();
        }
    }

    [ContextMenu("Recover First Virtual Flee Record")]
    public void RecoverFirstFromContext() {
        TryRecoverFirst();
    }

    public IReadOnlyList<OverworldFleeRecord> GetRecoverableRecords() {
        var log = ResolveFleeLog();
        if(log == null) {
            return Array.Empty<OverworldFleeRecord>();
        }

        if(pruneExpiredBeforeLookup) {
            log.MarkExpiredRecords();
        }

        string sceneName = ResolveSceneName();
        string escapeNodeId = ResolveEscapeNodeId();
        return log.GetActiveRecords(sceneName, escapeNodeId)
            .Where(MatchesFilters)
            .OrderByDescending(record => record.absoluteHour)
            .ToList();
    }

    public bool TryRecoverFirst() {
        var record = GetRecoverableRecords().FirstOrDefault();
        if(record == null) {
            return Fail("No active virtual flee record matched this recovery source.");
        }

        return TryRecover(record, out _);
    }

    public bool TryRecover(OverworldFleeRecord record, out OverworldFleeRecoveryResult result) {
        result = null;
        if(record == null) {
            Fail("Cannot recover a missing flee record.");
            return false;
        }

        var log = ResolveFleeLog();
        if(log == null) {
            Fail("Cannot recover virtual flee record because PlayerOverworldFleeLog is missing.");
            return false;
        }

        if(!MatchesFilters(record)) {
            Fail("Flee record does not match this recovery source.");
            return false;
        }

        GameObject recoveredObject = null;
        if(recoveryAction == OverworldFleeRecoveryAction.SpawnPrefabAndMarkRecovered) {
            if(recoveredPrefab == null) {
                Fail("Cannot spawn recovered flee actor because Recovered Prefab is missing.");
                return false;
            }

            recoveredObject = Instantiate(recoveredPrefab, ResolveSpawnPosition(record), Quaternion.identity);
        } else if(recoveryAction == OverworldFleeRecoveryAction.EnableExistingObjectAndMarkRecovered) {
            if(existingObject == null) {
                Fail("Cannot enable recovered flee actor because Existing Object is missing.");
                return false;
            }

            recoveredObject = existingObject;
            recoveredObject.transform.position = ResolveSpawnPosition(record);
            recoveredObject.SetActive(true);
        }

        if(recoveredObject != null && renameRecoveredObject && !string.IsNullOrWhiteSpace(record.entityName)) {
            recoveredObject.name = record.entityName;
        }

        log.MarkRecovered(record.recordId, recoveredMessage);
        result = new OverworldFleeRecoveryResult {
            success = true,
            record = record,
            recoveredObject = recoveredObject,
            message = recoveredMessage
        };

        if(logStateChanges) {
            GameDebug.Success($"{record.entityName} recovered from virtual flee state.", GameDebugCategory.Encounter, this, "OverworldFleeRecoverySource");
        }

        OnRecovered?.Invoke(result);
        return true;
    }

    bool MatchesFilters(OverworldFleeRecord record) {
        if(record == null || record.state != OverworldVirtualFleeState.Active) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(entityIdFilter) && record.entityId != entityIdFilter) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(speciesIdFilter) && record.speciesId != speciesIdFilter) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(sourceIdFilter) && record.sourceId != sourceIdFilter) {
            return false;
        }

        string sceneName = ResolveSceneName();
        if(!string.IsNullOrWhiteSpace(sceneName) && record.sceneName != sceneName) {
            return false;
        }

        string escapeNodeId = ResolveEscapeNodeId();
        if(!string.IsNullOrWhiteSpace(escapeNodeId) && record.escapeNodeId != escapeNodeId) {
            return false;
        }

        return true;
    }

    Vector3 ResolveSpawnPosition(OverworldFleeRecord record) {
        Vector3 basePosition;
        if(spawnAtRecoverySource) {
            basePosition = escapeNode != null ? escapeNode.transform.position : transform.position;
        } else {
            basePosition = record != null ? record.lastPosition.ToVector3() : transform.position;
        }

        return basePosition + spawnOffset;
    }

    string ResolveSceneName() {
        if(!string.IsNullOrWhiteSpace(sceneNameFilter)) {
            return sceneNameFilter;
        }

        return matchActiveScene ? SceneManager.GetActiveScene().name : string.Empty;
    }

    string ResolveEscapeNodeId() {
        if(!string.IsNullOrWhiteSpace(escapeNodeIdFilter)) {
            return escapeNodeIdFilter;
        }

        return escapeNode != null ? escapeNode.NodeId : string.Empty;
    }

    bool Fail(string message) {
        if(logStateChanges) {
            GameDebug.Warning(message, GameDebugCategory.Encounter, this, "OverworldFleeRecoverySource");
        }

        OnRecoveryFailed?.Invoke(message);
        return false;
    }

    PlayerOverworldFleeLog ResolveFleeLog() {
        if(fleeLogOverride != null) {
            return fleeLogOverride;
        }

        if(playerOverride != null) {
            return playerOverride.GetComponent<PlayerOverworldFleeLog>();
        }

        if(PlayerController.i != null) {
            return PlayerController.i.GetComponent<PlayerOverworldFleeLog>();
        }

        return FindAnyObjectByType<PlayerOverworldFleeLog>();
    }
}

[Serializable]
public class OverworldFleeRecoveryResult {
    [Tooltip("If enabled, recovery succeeded.")]
    public bool success;
    [Tooltip("Recovered virtual flee record.")]
    public OverworldFleeRecord record;
    [Tooltip("Instantiated or enabled recovered object, if any.")]
    public GameObject recoveredObject;
    [Tooltip("Recovery result message.")]
    public string message;
}
