using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OverworldEncounterDebugSource : MonoBehaviour {
    [Header("References")]
    [Tooltip("Node group inspected by this debug source.")]
    [SerializeField] OverworldEncounterNodeGroup nodeGroup = null;
    [Tooltip("Optional path agent inspected by this debug source.")]
    [SerializeField] OverworldEncounterPathAgent pathAgent = null;
    [Tooltip("Optional movement adapter inspected by this debug source.")]
    [SerializeField] OverworldNodeMovementAdapter movementAdapter = null;
    [Tooltip("Optional player flee log inspected by this debug source.")]
    [SerializeField] PlayerOverworldFleeLog fleeLog = null;
    [Tooltip("Player context used by node and path checks. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Test Path")]
    [Tooltip("Start node used by Test Path context action. Empty uses the path agent current/start node or nearest node.")]
    [SerializeField] OverworldEncounterNode testStartNode = null;
    [Tooltip("Goal node used by Test Path context action.")]
    [SerializeField] OverworldEncounterNode testGoalNode = null;
    [Tooltip("If enabled, Test Path may return a partial route when the goal cannot be reached.")]
    [SerializeField] bool allowPartialTestPath = true;
    [Tooltip("If enabled, Test Path ignores dangerous connections.")]
    [SerializeField] bool avoidDangerousConnections = false;
    [Tooltip("If enabled, Test Path ignores dangerous nodes.")]
    [SerializeField] bool avoidDangerousNodes = false;
    [Tooltip("Maximum visited nodes for Test Path. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxVisitedNodes = 256;

    [Header("Test Move")]
    [Tooltip("Direction used by Test Move context action.")]
    [SerializeField] Vector2 testMoveDirection = Vector2.right;

    [Header("Output")]
    [Tooltip("If enabled, context actions write summaries to GameDebug.")]
    [SerializeField] bool logToGameDebug = true;

    public OverworldEncounterNodeGroup NodeGroup => nodeGroup;
    public OverworldEncounterPathAgent PathAgent => pathAgent;
    public OverworldNodeMovementAdapter MovementAdapter => movementAdapter;
    public PlayerOverworldFleeLog FleeLog => fleeLog;
    public OverworldEncounterNode TestStartNode => testStartNode;
    public OverworldEncounterNode TestGoalNode => testGoalNode;
    public event Action<OverworldEncounterDebugSnapshot> OnSnapshotBuilt;
    public event Action<OverworldEncounterPathResult> OnTestPathCompleted;
    public event Action<OverworldNodeMoveResult> OnTestMoveCompleted;

    void Awake() {
        if(pathAgent == null) {
            pathAgent = GetComponent<OverworldEncounterPathAgent>();
        }

        if(movementAdapter == null) {
            movementAdapter = GetComponent<OverworldNodeMovementAdapter>();
        }
    }

    [ContextMenu("Build Encounter Debug Snapshot")]
    public void BuildSnapshotContext() {
        var snapshot = BuildSnapshot();
        if(logToGameDebug) {
            GameDebug.Step(snapshot.summary, GameDebugCategory.Encounter, this, "OverworldEncounterDebugSource");
        }
    }

    [ContextMenu("Test Path")]
    public void TestPathContext() {
        TryTestPath(out _);
    }

    [ContextMenu("Test Move")]
    public void TestMoveContext() {
        TryTestMove(out _);
    }

    public OverworldEncounterDebugSnapshot BuildSnapshot() {
        ResolveReferences();
        nodeGroup?.RefreshNodes();

        var snapshot = new OverworldEncounterDebugSnapshot {
            groupId = nodeGroup != null ? nodeGroup.GroupId : string.Empty,
            groupName = nodeGroup != null ? nodeGroup.DisplayName : string.Empty,
            agentName = pathAgent != null ? pathAgent.name : string.Empty,
            currentNodeId = pathAgent != null && pathAgent.CurrentNode != null ? pathAgent.CurrentNode.NodeId : string.Empty,
            currentNodeName = pathAgent != null && pathAgent.CurrentNode != null ? pathAgent.CurrentNode.DisplayName : string.Empty,
            targetNodeId = pathAgent != null && pathAgent.TargetNode != null ? pathAgent.TargetNode.NodeId : string.Empty,
            targetNodeName = pathAgent != null && pathAgent.TargetNode != null ? pathAgent.TargetNode.DisplayName : string.Empty,
            pathMode = pathAgent != null ? pathAgent.PathMode.ToString() : string.Empty,
            movementCapabilities = pathAgent != null ? pathAgent.MovementCapabilities.ToString() : string.Empty,
            isAgentMoving = pathAgent != null && pathAgent.IsMoving,
            isAgentFollowingManualRoute = pathAgent != null && pathAgent.IsFollowingManualRoute,
            nodes = BuildNodeRows(),
            activeFleeRecords = BuildFleeRows()
        };

        snapshot.nodeCount = snapshot.nodes.Count;
        snapshot.connectionCount = snapshot.nodes.Sum(node => node.connectionCount);
        snapshot.activeFleeRecordCount = snapshot.activeFleeRecords.Count;
        snapshot.summary = $"{snapshot.groupName}: {snapshot.nodeCount} node(s), {snapshot.connectionCount} connection(s), {snapshot.activeFleeRecordCount} active flee record(s).";
        OnSnapshotBuilt?.Invoke(snapshot);
        return snapshot;
    }

    public bool TryTestPath(out OverworldEncounterPathResult result) {
        ResolveReferences();
        var start = ResolveTestStartNode();
        if(start == null || testGoalNode == null) {
            result = OverworldEncounterPathResult.Failed("Test path requires a start node and goal node.");
            LogPathResult(result);
            OnTestPathCompleted?.Invoke(result);
            return false;
        }

        var request = new OverworldEncounterPathRequest {
            start = start,
            goal = testGoalNode,
            nodeGroup = nodeGroup,
            agent = pathAgent,
            player = ResolvePlayer(),
            restrictToGroup = nodeGroup != null,
            allowPartialPath = allowPartialTestPath,
            avoidDangerousConnections = avoidDangerousConnections,
            avoidDangerousNodes = avoidDangerousNodes,
            maxVisitedNodes = maxVisitedNodes
        };

        bool success = OverworldEncounterPathfinding.TryFindPath(request, out result);
        LogPathResult(result);
        OnTestPathCompleted?.Invoke(result);
        return success;
    }

    public bool TryTestMove(out OverworldNodeMoveResult result) {
        ResolveReferences();
        if(movementAdapter == null) {
            result = new OverworldNodeMoveResult {
                success = false,
                requestedDirection = testMoveDirection,
                message = "No movement adapter assigned."
            };
            LogMoveResult(result);
            OnTestMoveCompleted?.Invoke(result);
            return false;
        }

        movementAdapter.TryGetMoveTarget(testMoveDirection, out _, out result);
        LogMoveResult(result);
        OnTestMoveCompleted?.Invoke(result);
        return result.success;
    }

    List<OverworldEncounterDebugNodeRow> BuildNodeRows() {
        var player = ResolvePlayer();
        if(nodeGroup == null) {
            return new List<OverworldEncounterDebugNodeRow>();
        }

        return nodeGroup.Nodes
            .Where(node => node != null)
            .Select(node => {
                bool usable = pathAgent == null || pathAgent.IsNodeAllowed(node, player, out _);
                string failure = null;
                if(pathAgent != null) {
                    pathAgent.IsNodeAllowed(node, player, out failure);
                }

                var connections = node.Connections
                    .Where(connection => connection != null)
                    .Select(connection => new OverworldEncounterDebugConnectionRow {
                        targetNodeId = connection.Node != null ? connection.Node.NodeId : string.Empty,
                        targetNodeName = connection.Node != null ? connection.Node.DisplayName : string.Empty,
                        enabled = connection.Enabled,
                        weight = connection.Weight,
                        traversalCost = connection.TraversalCost,
                        flags = connection.Flags.ToString(),
                        requiredCapabilities = connection.RequiredCapabilities.ToString(),
                        blockedCapabilities = connection.BlockedCapabilities.ToString(),
                        usable = connection.IsUsable(pathAgent, player, out var connectionFailure),
                        failureMessage = connectionFailure
                    })
                    .ToList();

                return new OverworldEncounterDebugNodeRow {
                    nodeId = node.NodeId,
                    nodeName = node.DisplayName,
                    kind = node.Kind.ToString(),
                    flags = node.NodeFlags.ToString(),
                    requiredCapabilities = node.RequiredCapabilities.ToString(),
                    tagSummary = string.Join(", ", node.Tags),
                    connectionCount = connections.Count,
                    usableByAgent = usable,
                    failureMessage = failure,
                    connections = connections
                };
            })
            .ToList();
    }

    List<OverworldEncounterDebugFleeRow> BuildFleeRows() {
        ResolveReferences();
        if(fleeLog == null) {
            return new List<OverworldEncounterDebugFleeRow>();
        }

        return fleeLog.GetActiveRecords()
            .Select(record => new OverworldEncounterDebugFleeRow {
                recordId = record.recordId,
                entityId = record.entityId,
                entityName = record.entityName,
                speciesId = record.speciesId,
                speciesName = record.speciesName,
                sceneName = record.sceneName,
                fromNodeId = record.fromNodeId,
                escapeNodeId = record.escapeNodeId,
                state = record.state.ToString(),
                absoluteHour = record.absoluteHour,
                expiresAtAbsoluteHour = record.expiresAtAbsoluteHour
            })
            .ToList();
    }

    OverworldEncounterNode ResolveTestStartNode() {
        if(testStartNode != null) {
            return testStartNode;
        }

        if(pathAgent != null && pathAgent.CurrentNode != null) {
            return pathAgent.CurrentNode;
        }

        if(pathAgent != null && pathAgent.StartNode != null) {
            return pathAgent.StartNode;
        }

        if(nodeGroup != null) {
            return nodeGroup.FindNearestNode(transform.position, 0f, ResolvePlayer(), pathAgent);
        }

        return null;
    }

    void ResolveReferences() {
        if(pathAgent == null) {
            pathAgent = GetComponent<OverworldEncounterPathAgent>();
        }

        if(movementAdapter == null) {
            movementAdapter = GetComponent<OverworldNodeMovementAdapter>();
        }

        if(nodeGroup == null) {
            nodeGroup = pathAgent != null ? pathAgent.NodeGroup : movementAdapter != null ? movementAdapter.NodeGroup : null;
        }

        if(fleeLog == null) {
            var player = ResolvePlayer();
            fleeLog = player != null ? player.GetComponent<PlayerOverworldFleeLog>() : FindAnyObjectByType<PlayerOverworldFleeLog>();
        }
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

    void LogPathResult(OverworldEncounterPathResult result) {
        if(logToGameDebug && result != null) {
            GameDebug.Step($"Path test: {result.Message} nodes={result.Nodes.Count} cost={result.TotalCost:0.##}", GameDebugCategory.Encounter, this, "OverworldEncounterDebugSource");
        }
    }

    void LogMoveResult(OverworldNodeMoveResult result) {
        if(logToGameDebug && result != null) {
            GameDebug.Step($"Move test: {result.message}", GameDebugCategory.Encounter, this, "OverworldEncounterDebugSource");
        }
    }
}

[Serializable]
public class OverworldEncounterDebugSnapshot {
    [Tooltip("Inspected node group id.")]
    public string groupId;
    [Tooltip("Inspected node group display name.")]
    public string groupName;
    [Tooltip("Inspected path agent name.")]
    public string agentName;
    [Tooltip("Current path agent node id.")]
    public string currentNodeId;
    [Tooltip("Current path agent node display name.")]
    public string currentNodeName;
    [Tooltip("Target path agent node id.")]
    public string targetNodeId;
    [Tooltip("Target path agent node display name.")]
    public string targetNodeName;
    [Tooltip("Path mode of the inspected agent.")]
    public string pathMode;
    [Tooltip("Movement capabilities of the inspected agent.")]
    public string movementCapabilities;
    [Tooltip("If enabled, the inspected agent is currently moving.")]
    public bool isAgentMoving;
    [Tooltip("If enabled, the inspected agent is following a manual route.")]
    public bool isAgentFollowingManualRoute;
    [Tooltip("Total node count in the inspected group.")]
    public int nodeCount;
    [Tooltip("Total outgoing connection count in the inspected group.")]
    public int connectionCount;
    [Tooltip("Active virtual flee record count.")]
    public int activeFleeRecordCount;
    [Tooltip("One-line summary for logs/UI.")]
    public string summary;
    [Tooltip("Node rows for debug UI.")]
    public List<OverworldEncounterDebugNodeRow> nodes = new List<OverworldEncounterDebugNodeRow>();
    [Tooltip("Active flee records for debug UI.")]
    public List<OverworldEncounterDebugFleeRow> activeFleeRecords = new List<OverworldEncounterDebugFleeRow>();
}

[Serializable]
public class OverworldEncounterDebugNodeRow {
    [Tooltip("Node id.")]
    public string nodeId;
    [Tooltip("Node display name.")]
    public string nodeName;
    [Tooltip("Node kind.")]
    public string kind;
    [Tooltip("Node flags.")]
    public string flags;
    [Tooltip("Node movement capability requirements.")]
    public string requiredCapabilities;
    [Tooltip("Comma separated node tags.")]
    public string tagSummary;
    [Tooltip("Outgoing connection count.")]
    public int connectionCount;
    [Tooltip("If enabled, the inspected path agent can use this node.")]
    public bool usableByAgent;
    [Tooltip("Reason this node is not usable, if any.")]
    public string failureMessage;
    [Tooltip("Connection rows for this node.")]
    public List<OverworldEncounterDebugConnectionRow> connections = new List<OverworldEncounterDebugConnectionRow>();
}

[Serializable]
public class OverworldEncounterDebugConnectionRow {
    [Tooltip("Target node id.")]
    public string targetNodeId;
    [Tooltip("Target node display name.")]
    public string targetNodeName;
    [Tooltip("If enabled, this connection is enabled.")]
    public bool enabled;
    [Tooltip("Random movement weight.")]
    public int weight;
    [Tooltip("Pathfinding traversal cost.")]
    public float traversalCost;
    [Tooltip("Connection flags.")]
    public string flags;
    [Tooltip("Required movement capabilities.")]
    public string requiredCapabilities;
    [Tooltip("Blocked movement capabilities.")]
    public string blockedCapabilities;
    [Tooltip("If enabled, the inspected path agent can use this connection.")]
    public bool usable;
    [Tooltip("Reason this connection is not usable, if any.")]
    public string failureMessage;
}

[Serializable]
public class OverworldEncounterDebugFleeRow {
    [Tooltip("Virtual flee record id.")]
    public string recordId;
    [Tooltip("Fleeing entity id.")]
    public string entityId;
    [Tooltip("Fleeing entity display name.")]
    public string entityName;
    [Tooltip("Optional Pokemon species id.")]
    public string speciesId;
    [Tooltip("Optional Pokemon species display name.")]
    public string speciesName;
    [Tooltip("Scene where the flee was recorded.")]
    public string sceneName;
    [Tooltip("Start node id.")]
    public string fromNodeId;
    [Tooltip("Escape node id.")]
    public string escapeNodeId;
    [Tooltip("Virtual flee state.")]
    public string state;
    [Tooltip("Absolute hour when the flee was recorded.")]
    public int absoluteHour;
    [Tooltip("Absolute hour when the flee expires.")]
    public int expiresAtAbsoluteHour;
}
