using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum OverworldFleeTargetSelection {
    PreferEscapeFlagsThenDistance,
    FarthestFromThreat,
    LowestPathCost
}

public enum OverworldFleeFinishMode {
    PauseAtEscapeNode,
    ResumePathAgent,
    RecordVirtualAndPause,
    RecordVirtualAndResumePathAgent,
    RecordVirtualAndDisableGameObject,
    RecordVirtualAndDestroyGameObject,
    DisableGameObject,
    DestroyGameObject
}

public class OverworldEncounterFleeController : MonoBehaviour {
    [Header("References")]
    [Tooltip("Path agent used to move along the chosen escape route. Empty uses the component on this GameObject.")]
    [SerializeField] OverworldEncounterPathAgent pathAgent = null;
    [Tooltip("Node group used for escape candidates. Empty uses the path agent's node group.")]
    [SerializeField] OverworldEncounterNodeGroup nodeGroup = null;
    [Tooltip("Player context used by node requirement checks. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride = null;
    [Tooltip("Optional player flee log used to keep virtual/unloaded-map flee state.")]
    [SerializeField] PlayerOverworldFleeLog fleeLogOverride = null;

    [Header("Threat")]
    [Tooltip("Minimum distance an escape node should have from the threat position.")]
    [Min(0f)]
    [SerializeField] float minEscapeDistanceFromThreat = 4f;
    [Tooltip("Maximum distance from this actor to candidate escape nodes. 0 means unlimited.")]
    [Min(0f)]
    [SerializeField] float maxEscapeDistanceFromActor = 0f;
    [Tooltip("How the best escape route is selected when several nodes are valid.")]
    [SerializeField] OverworldFleeTargetSelection targetSelection = OverworldFleeTargetSelection.PreferEscapeFlagsThenDistance;

    [Header("Escape Filters")]
    [Tooltip("Preferred destination flags. Nodes with at least one of these flags score higher, but non-preferred safe nodes can still be used.")]
    [SerializeField] OverworldEncounterNodeFlags preferredEscapeFlags = OverworldEncounterNodeFlags.HideSpot | OverworldEncounterNodeFlags.SpawnExit | OverworldEncounterNodeFlags.MapExit;
    [Tooltip("Candidate nodes with any of these flags are ignored.")]
    [SerializeField] OverworldEncounterNodeFlags blockedEscapeFlags = OverworldEncounterNodeFlags.Danger;
    [Tooltip("If not empty, an escape node must have at least one of these tags.")]
    [SerializeField] List<string> requiredEscapeTags = new List<string>();
    [Tooltip("Escape nodes with any of these tags are ignored.")]
    [SerializeField] List<string> blockedEscapeTags = new List<string>();

    [Header("Pathfinding")]
    [Tooltip("If enabled, only nodes in the selected Node Group can be used during route search.")]
    [SerializeField] bool restrictPathToGroup = true;
    [Tooltip("If enabled, the controller may accept the best partial path when no full escape route is available.")]
    [SerializeField] bool allowPartialPath = true;
    [Tooltip("If enabled, dangerous connections are ignored while fleeing.")]
    [SerializeField] bool avoidDangerousConnections = true;
    [Tooltip("If enabled, dangerous nodes are ignored while fleeing.")]
    [SerializeField] bool avoidDangerousNodes = true;
    [Tooltip("Maximum graph nodes visited during a flee search. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxVisitedNodes = 256;
    [Tooltip("Maximum total route cost accepted during flee search. 0 means unlimited.")]
    [Min(0f)]
    [SerializeField] float maxTotalPathCost = 0f;

    [Header("Finish")]
    [Tooltip("Stable id for this fleeing actor/spawn instance. Empty uses the GameObject name.")]
    [SerializeField] string virtualEntityId = string.Empty;
    [Tooltip("Display/debug name saved into virtual flee records. Empty uses the GameObject name.")]
    [SerializeField] string virtualEntityName = string.Empty;
    [Tooltip("Optional Pokemon species/base id saved into virtual flee records.")]
    [SerializeField] string virtualSpeciesId = string.Empty;
    [Tooltip("Optional Pokemon species/base display name saved into virtual flee records.")]
    [SerializeField] string virtualSpeciesName = string.Empty;
    [Tooltip("Source id saved into virtual flee records.")]
    [SerializeField] string virtualSourceId = "overworld-flee-controller";
    [Tooltip("Source display name saved into virtual flee records.")]
    [SerializeField] string virtualSourceName = "Overworld Flee Controller";
    [Tooltip("What happens after the actor reaches its escape node.")]
    [SerializeField] OverworldFleeFinishMode finishMode = OverworldFleeFinishMode.PauseAtEscapeNode;
    [Tooltip("If enabled, virtual flee is recorded when the final node has Spawn Exit or Map Exit flags, even when the finish mode is not a record mode.")]
    [SerializeField] bool recordVirtualWhenEscapeNodeIsExit = true;
    [Tooltip("If enabled, virtual flee is recorded at the end of every successful flee route.")]
    [SerializeField] bool alwaysRecordVirtualFleeOnFinish = false;
    [Tooltip("How many in-game hours the virtual flee record stays active. 0 means it never expires automatically.")]
    [Min(0)]
    [SerializeField] int virtualFleeExpireAfterHours = 6;
    [Tooltip("Delay before applying the finish mode after the route completes.")]
    [Min(0f)]
    [SerializeField] float finishDelaySeconds = 0f;
    [Tooltip("If enabled, the GameObject is disabled when no escape route can be found.")]
    [SerializeField] bool disableWhenNoRouteFound = false;

    [Header("Debug")]
    [Tooltip("If enabled, flee start, failure and completion are written to GameDebug.")]
    [SerializeField] bool logStateChanges = false;

    bool fleeing;
    Vector3 lastThreatPosition;
    OverworldEncounterNode fleeStartNode;
    OverworldEncounterPathResult activePath;
    Coroutine finishRoutine;

    public OverworldEncounterPathAgent PathAgent => pathAgent;
    public OverworldEncounterNodeGroup NodeGroup => nodeGroup != null ? nodeGroup : pathAgent != null ? pathAgent.NodeGroup : null;
    public PlayerOverworldFleeLog FleeLog => ResolveFleeLog();
    public string VirtualEntityId => string.IsNullOrWhiteSpace(virtualEntityId) ? name : virtualEntityId;
    public string VirtualEntityName => string.IsNullOrWhiteSpace(virtualEntityName) ? name : virtualEntityName;
    public string VirtualSpeciesId => virtualSpeciesId;
    public string VirtualSpeciesName => virtualSpeciesName;
    public int VirtualFleeExpireAfterHours => Mathf.Max(0, virtualFleeExpireAfterHours);
    public float MinEscapeDistanceFromThreat => Mathf.Max(0f, minEscapeDistanceFromThreat);
    public int MaxVisitedNodes => Mathf.Max(0, maxVisitedNodes);
    public OverworldEncounterNodeFlags PreferredEscapeFlags => preferredEscapeFlags;
    public OverworldEncounterNodeFlags BlockedEscapeFlags => blockedEscapeFlags;
    public IReadOnlyList<string> RequiredEscapeTags => requiredEscapeTags != null ? (IReadOnlyList<string>)requiredEscapeTags : Array.Empty<string>();
    public IReadOnlyList<string> BlockedEscapeTags => blockedEscapeTags != null ? (IReadOnlyList<string>)blockedEscapeTags : Array.Empty<string>();
    public bool IsFleeing => fleeing;
    public Vector3 LastThreatPosition => lastThreatPosition;
    public OverworldEncounterPathResult ActivePath => activePath;
    public event Action<OverworldEncounterPathResult> OnFleeStarted;
    public event Action<OverworldEncounterNode> OnFleeFinished;
    public event Action<string> OnFleeFailed;

    void Awake() {
        if(pathAgent == null) {
            pathAgent = GetComponent<OverworldEncounterPathAgent>();
        }
    }

    void OnEnable() {
        if(pathAgent != null) {
            pathAgent.OnRouteCompleted += HandleRouteCompleted;
        }
    }

    void OnDisable() {
        if(pathAgent != null) {
            pathAgent.OnRouteCompleted -= HandleRouteCompleted;
        }

        if(finishRoutine != null) {
            StopCoroutine(finishRoutine);
            finishRoutine = null;
        }
    }

    [ContextMenu("Trigger Flee From Player")]
    public void TriggerFleeFromPlayerContext() {
        TriggerFleeFromPlayer();
    }

    public bool TriggerFleeFromPlayer() {
        var player = ResolvePlayer();
        if(player == null) {
            return FailFlee("Cannot flee because no player was found.");
        }

        return TriggerFleeFrom(player.transform);
    }

    public bool TriggerFleeFrom(Transform threat) {
        if(threat == null) {
            return FailFlee("Cannot flee because threat transform is missing.");
        }

        return TriggerFleeFromPosition(threat.position);
    }

    public bool TriggerFleeFromPosition(Vector3 threatPosition) {
        lastThreatPosition = threatPosition;

        if(pathAgent == null) {
            return FailFlee("Cannot flee because no OverworldEncounterPathAgent is assigned.");
        }

        var group = NodeGroup;
        if(group == null) {
            return FailFlee("Cannot flee because no encounter node group is available.");
        }

        if(!TryFindBestEscapeRoute(group, threatPosition, out var pathResult, out var failureMessage)) {
            if(disableWhenNoRouteFound) {
                gameObject.SetActive(false);
            }

            return FailFlee(failureMessage);
        }

        activePath = pathResult;
        fleeStartNode = pathResult.Nodes != null && pathResult.Nodes.Count > 0 ? pathResult.Nodes[0] : pathAgent.CurrentNode;
        fleeing = true;

        if(!pathAgent.FollowPathResult(pathResult, snapToFirstNode: false, resumeConfiguredRoutingAfterRoute: false)) {
            fleeing = false;
            return FailFlee("Escape route was found but the path agent could not follow it.");
        }

        if(logStateChanges) {
            GameDebug.Step($"{name} fleeing through {pathResult.Nodes.Count} node(s).", GameDebugCategory.Encounter, this, "OverworldEncounterFleeController");
        }

        OnFleeStarted?.Invoke(pathResult);
        return true;
    }

    public void CancelFlee(bool resumePathAgent = false) {
        fleeing = false;
        activePath = null;
        pathAgent?.ClearManualRoute(resumePathAgent);
    }

    bool TryFindBestEscapeRoute(OverworldEncounterNodeGroup group, Vector3 threatPosition, out OverworldEncounterPathResult bestPath, out string failureMessage) {
        group.RefreshNodes();
        var player = ResolvePlayer();
        var start = ResolveStartNode(group, player);
        if(start == null) {
            bestPath = null;
            failureMessage = "No valid start node found for flee route.";
            return false;
        }

        var candidates = group.Nodes
            .Where(node => IsEscapeCandidate(node, threatPosition, player))
            .ToList();

        if(candidates.Count == 0) {
            bestPath = null;
            failureMessage = "No valid escape node candidates found.";
            return false;
        }

        EscapeRouteCandidate bestCandidate = null;
        foreach(var candidate in candidates) {
            var request = new OverworldEncounterPathRequest {
                start = start,
                goal = candidate,
                nodeGroup = group,
                agent = pathAgent,
                player = player,
                restrictToGroup = restrictPathToGroup,
                allowPartialPath = allowPartialPath,
                avoidDangerousConnections = avoidDangerousConnections,
                avoidDangerousNodes = avoidDangerousNodes,
                maxVisitedNodes = maxVisitedNodes,
                maxTotalCost = maxTotalPathCost
            };

            if(!OverworldEncounterPathfinding.TryFindPath(request, out var pathResult) || pathResult == null || !pathResult.Success) {
                continue;
            }

            var scored = new EscapeRouteCandidate(candidate, pathResult, ScoreCandidate(candidate, pathResult, threatPosition));
            if(bestCandidate == null || scored.score > bestCandidate.score) {
                bestCandidate = scored;
            }
        }

        if(bestCandidate == null) {
            bestPath = null;
            failureMessage = "No reachable escape route found.";
            return false;
        }

        bestPath = bestCandidate.path;
        failureMessage = null;
        return true;
    }

    OverworldEncounterNode ResolveStartNode(OverworldEncounterNodeGroup group, PlayerController player) {
        if(pathAgent.CurrentNode != null) {
            return pathAgent.CurrentNode;
        }

        var nearest = group.FindNearestNode(transform.position, 0f, player, pathAgent);
        if(nearest != null) {
            return nearest;
        }

        return pathAgent.StartNode;
    }

    bool IsEscapeCandidate(OverworldEncounterNode node, Vector3 threatPosition, PlayerController player) {
        if(node == null) {
            return false;
        }

        if(blockedEscapeFlags != OverworldEncounterNodeFlags.None && (node.NodeFlags & blockedEscapeFlags) != 0) {
            return false;
        }

        if(minEscapeDistanceFromThreat > 0f && Vector3.Distance(node.transform.position, threatPosition) < minEscapeDistanceFromThreat) {
            return false;
        }

        if(maxEscapeDistanceFromActor > 0f && Vector3.Distance(node.transform.position, transform.position) > maxEscapeDistanceFromActor) {
            return false;
        }

        var requiredTags = requiredEscapeTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() ?? new List<string>();
        if(requiredTags.Count > 0 && !requiredTags.Any(node.HasTag)) {
            return false;
        }

        var blockedTags = blockedEscapeTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() ?? new List<string>();
        if(blockedTags.Any(node.HasTag)) {
            return false;
        }

        return pathAgent == null || pathAgent.IsNodeAllowed(node, player, out _);
    }

    float ScoreCandidate(OverworldEncounterNode candidate, OverworldEncounterPathResult pathResult, Vector3 threatPosition) {
        var lastNode = pathResult.LastNode != null ? pathResult.LastNode : candidate;
        float distanceFromThreat = Vector3.Distance(lastNode.transform.position, threatPosition);
        float preferredScore = preferredEscapeFlags != OverworldEncounterNodeFlags.None && (candidate.NodeFlags & preferredEscapeFlags) != 0 ? 1000f : 0f;
        float partialPenalty = pathResult.PartialPath ? 250f : 0f;
        float cost = pathResult.TotalCost;

        return targetSelection switch {
            OverworldFleeTargetSelection.FarthestFromThreat => distanceFromThreat - cost * 0.05f + preferredScore * 0.1f - partialPenalty,
            OverworldFleeTargetSelection.LowestPathCost => -cost + preferredScore * 0.25f + distanceFromThreat * 0.05f - partialPenalty,
            _ => preferredScore + distanceFromThreat - cost * 0.1f - partialPenalty
        };
    }

    void HandleRouteCompleted(OverworldEncounterNode finalNode) {
        if(!fleeing) {
            return;
        }

        if(finishRoutine != null) {
            StopCoroutine(finishRoutine);
        }

        finishRoutine = StartCoroutine(FinishFleeAfterDelay(finalNode));
    }

    IEnumerator FinishFleeAfterDelay(OverworldEncounterNode finalNode) {
        if(finishDelaySeconds > 0f) {
            yield return new WaitForSeconds(finishDelaySeconds);
        }

        finishRoutine = null;
        fleeing = false;

        if(logStateChanges) {
            string nodeName = finalNode != null ? finalNode.DisplayName : "unknown";
            GameDebug.Step($"{name} finished flee route at {nodeName}.", GameDebugCategory.Encounter, this, "OverworldEncounterFleeController");
        }

        OnFleeFinished?.Invoke(finalNode);
        if(ShouldRecordVirtualFlee(finalNode)) {
            RecordVirtualFlee(finalNode);
        }

        switch(finishMode) {
            case OverworldFleeFinishMode.ResumePathAgent:
            case OverworldFleeFinishMode.RecordVirtualAndResumePathAgent:
                pathAgent?.ResumeMovement();
                break;
            case OverworldFleeFinishMode.RecordVirtualAndPause:
                pathAgent?.PauseMovement();
                break;
            case OverworldFleeFinishMode.DisableGameObject:
            case OverworldFleeFinishMode.RecordVirtualAndDisableGameObject:
                gameObject.SetActive(false);
                break;
            case OverworldFleeFinishMode.DestroyGameObject:
            case OverworldFleeFinishMode.RecordVirtualAndDestroyGameObject:
                Destroy(gameObject);
                break;
        }
    }

    bool ShouldRecordVirtualFlee(OverworldEncounterNode finalNode) {
        if(alwaysRecordVirtualFleeOnFinish) {
            return true;
        }

        if(finishMode == OverworldFleeFinishMode.RecordVirtualAndPause
            || finishMode == OverworldFleeFinishMode.RecordVirtualAndResumePathAgent
            || finishMode == OverworldFleeFinishMode.RecordVirtualAndDisableGameObject
            || finishMode == OverworldFleeFinishMode.RecordVirtualAndDestroyGameObject) {
            return true;
        }

        if(recordVirtualWhenEscapeNodeIsExit && finalNode != null) {
            var exitFlags = OverworldEncounterNodeFlags.SpawnExit | OverworldEncounterNodeFlags.MapExit;
            return (finalNode.NodeFlags & exitFlags) != 0;
        }

        return false;
    }

    void RecordVirtualFlee(OverworldEncounterNode finalNode) {
        var log = ResolveFleeLog();
        if(log == null) {
            if(logStateChanges) {
                GameDebug.Warning($"{name} could not record virtual flee because PlayerOverworldFleeLog is missing.", GameDebugCategory.Encounter, this, "OverworldEncounterFleeController");
            }

            return;
        }

        log.RecordFlee(
            VirtualEntityId,
            VirtualEntityName,
            virtualSpeciesId,
            virtualSpeciesName,
            fleeStartNode,
            finalNode,
            transform.position,
            lastThreatPosition,
            virtualFleeExpireAfterHours,
            virtualSourceId,
            virtualSourceName
        );
    }

    bool FailFlee(string failureMessage) {
        string message = string.IsNullOrWhiteSpace(failureMessage) ? "Flee route failed." : failureMessage;
        if(logStateChanges) {
            GameDebug.Warning(message, GameDebugCategory.Encounter, this, "OverworldEncounterFleeController");
        }

        OnFleeFailed?.Invoke(message);
        return false;
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(pathAgent != null && pathAgent.PlayerOverride != null) {
            return pathAgent.PlayerOverride;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }

    PlayerOverworldFleeLog ResolveFleeLog() {
        if(fleeLogOverride != null) {
            return fleeLogOverride;
        }

        var player = ResolvePlayer();
        if(player != null) {
            return player.GetComponent<PlayerOverworldFleeLog>();
        }

        return FindAnyObjectByType<PlayerOverworldFleeLog>();
    }

    class EscapeRouteCandidate {
        public readonly OverworldEncounterNode node;
        public readonly OverworldEncounterPathResult path;
        public readonly float score;

        public EscapeRouteCandidate(OverworldEncounterNode node, OverworldEncounterPathResult path, float score) {
            this.node = node;
            this.path = path;
            this.score = score;
        }
    }
}
