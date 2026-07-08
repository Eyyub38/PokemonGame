using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OverworldEncounterDebugUIManager : MonoBehaviour {
    [Header("Source")]
    [Tooltip("Debug source that builds node, connection, agent and flee snapshots.")]
    [SerializeField] OverworldEncounterDebugSource debugSource;

    [Header("Filters")]
    [Tooltip("If enabled, nodes blocked for the inspected agent remain visible.")]
    [SerializeField] bool includeBlockedNodes = true;
    [Tooltip("If enabled, blocked connections remain visible.")]
    [SerializeField] bool includeBlockedConnections = true;
    [Tooltip("Optional node tag/name/id search text.")]
    [SerializeField] string nodeSearchText = string.Empty;
    [Tooltip("Optional selected node id. Empty shows the first visible node connections.")]
    [SerializeField] string selectedNodeId = string.Empty;

    [Header("Rows")]
    [Tooltip("Maximum node rows returned to UI. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxNodeRows = 80;
    [Tooltip("Maximum connection rows returned to UI. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxConnectionRows = 40;
    [Tooltip("Maximum flee rows returned to UI. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxFleeRows = 40;

    [Header("Debug")]
    [Tooltip("If enabled, refresh/test actions write debug messages.")]
    [SerializeField] bool logDebugMessages;

    OverworldEncounterDebugUIScreenSnapshot currentSnapshot = new OverworldEncounterDebugUIScreenSnapshot();

    public OverworldEncounterDebugSource DebugSource => debugSource;
    public bool IncludeBlockedNodes => includeBlockedNodes;
    public bool IncludeBlockedConnections => includeBlockedConnections;
    public string NodeSearchText => nodeSearchText;
    public string SelectedNodeId => selectedNodeId;
    public OverworldEncounterDebugUIScreenSnapshot CurrentSnapshot => currentSnapshot;

    public event Action<OverworldEncounterDebugUIScreenSnapshot> OnSnapshotChanged;
    public event Action<OverworldEncounterPathResult> OnTestPathCompleted;
    public event Action<OverworldNodeMoveResult> OnTestMoveCompleted;

    void OnEnable() {
        Refresh();
    }

    [ContextMenu("Refresh Overworld Encounter Debug Snapshot")]
    public OverworldEncounterDebugUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public OverworldEncounterDebugUIScreenSnapshot Refresh() {
        var raw = debugSource != null ? debugSource.BuildSnapshot() : new OverworldEncounterDebugSnapshot {
            summary = "No overworld encounter debug source assigned."
        };

        var nodeRows = FilterNodeRows(raw.nodes).ToList();
        if(maxNodeRows > 0) {
            nodeRows = nodeRows.Take(maxNodeRows).ToList();
        }

        var selectedNode = ResolveSelectedNode(nodeRows);
        var connectionRows = FilterConnectionRows(selectedNode).ToList();
        if(maxConnectionRows > 0) {
            connectionRows = connectionRows.Take(maxConnectionRows).ToList();
        }

        var fleeRows = raw.activeFleeRecords ?? new List<OverworldEncounterDebugFleeRow>();
        if(maxFleeRows > 0) {
            fleeRows = fleeRows.Take(maxFleeRows).ToList();
        }

        currentSnapshot = new OverworldEncounterDebugUIScreenSnapshot {
            groupId = raw.groupId,
            groupName = raw.groupName,
            agentName = raw.agentName,
            currentNodeId = raw.currentNodeId,
            currentNodeName = raw.currentNodeName,
            targetNodeId = raw.targetNodeId,
            targetNodeName = raw.targetNodeName,
            pathMode = raw.pathMode,
            movementCapabilities = raw.movementCapabilities,
            isAgentMoving = raw.isAgentMoving,
            isAgentFollowingManualRoute = raw.isAgentFollowingManualRoute,
            rawNodeCount = raw.nodeCount,
            rawConnectionCount = raw.connectionCount,
            rawActiveFleeRecordCount = raw.activeFleeRecordCount,
            visibleNodeCount = nodeRows.Count,
            blockedNodeCount = nodeRows.Count(row => row != null && !row.usableByAgent),
            visibleConnectionCount = connectionRows.Count,
            blockedConnectionCount = connectionRows.Count(row => row != null && !row.usable),
            visibleFleeRecordCount = fleeRows.Count,
            selectedNodeId = selectedNode != null ? selectedNode.nodeId : string.Empty,
            selectedNodeName = selectedNode != null ? selectedNode.nodeName : string.Empty,
            summary = raw.summary,
            nodes = nodeRows,
            selectedNodeConnections = connectionRows,
            activeFleeRecords = fleeRows
        };

        if(logDebugMessages) {
            GameDebug.Step($"Overworld encounter debug UI refreshed: {currentSnapshot.visibleNodeCount} nodes, {currentSnapshot.visibleConnectionCount} connections.", GameDebugCategory.Encounter, this, "OverworldEncounterDebugUIManager");
        }

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public void SetNodeSearchText(string searchText) {
        nodeSearchText = searchText ?? string.Empty;
        Refresh();
    }

    public void SelectNode(string nodeId) {
        selectedNodeId = nodeId ?? string.Empty;
        Refresh();
    }

    public void ClearSelectedNode() {
        selectedNodeId = string.Empty;
        Refresh();
    }

    public bool RunTestPath() {
        if(debugSource == null) {
            return false;
        }

        bool success = debugSource.TryTestPath(out var result);
        OnTestPathCompleted?.Invoke(result);
        Refresh();
        return success;
    }

    public bool RunTestMove() {
        if(debugSource == null) {
            return false;
        }

        bool success = debugSource.TryTestMove(out var result);
        OnTestMoveCompleted?.Invoke(result);
        Refresh();
        return success;
    }

    IEnumerable<OverworldEncounterDebugNodeRow> FilterNodeRows(IEnumerable<OverworldEncounterDebugNodeRow> rows) {
        rows ??= Enumerable.Empty<OverworldEncounterDebugNodeRow>();

        foreach(var row in rows) {
            if(row == null) {
                continue;
            }

            if(!includeBlockedNodes && !row.usableByAgent) {
                continue;
            }

            if(!MatchesSearch(row)) {
                continue;
            }

            yield return row;
        }
    }

    IEnumerable<OverworldEncounterDebugConnectionRow> FilterConnectionRows(OverworldEncounterDebugNodeRow selectedNode) {
        var connections = selectedNode != null ? selectedNode.connections : null;
        foreach(var connection in connections ?? Enumerable.Empty<OverworldEncounterDebugConnectionRow>()) {
            if(connection == null) {
                continue;
            }

            if(!includeBlockedConnections && !connection.usable) {
                continue;
            }

            yield return connection;
        }
    }

    OverworldEncounterDebugNodeRow ResolveSelectedNode(IReadOnlyList<OverworldEncounterDebugNodeRow> rows) {
        if(rows == null || rows.Count == 0) {
            return null;
        }

        if(!string.IsNullOrWhiteSpace(selectedNodeId)) {
            var selected = rows.FirstOrDefault(row => row != null && string.Equals(row.nodeId, selectedNodeId, StringComparison.OrdinalIgnoreCase));
            if(selected != null) {
                return selected;
            }
        }

        return rows.FirstOrDefault();
    }

    bool MatchesSearch(OverworldEncounterDebugNodeRow row) {
        if(row == null || string.IsNullOrWhiteSpace(nodeSearchText)) {
            return true;
        }

        string search = nodeSearchText.Trim();
        return Contains(row.nodeId, search)
            || Contains(row.nodeName, search)
            || Contains(row.kind, search)
            || Contains(row.flags, search)
            || Contains(row.tagSummary, search);
    }

    bool Contains(string value, string search) {
        return !string.IsNullOrWhiteSpace(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

public class OverworldEncounterDebugUIScreenSnapshot {
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
    [Tooltip("Raw node count before UI filtering.")]
    public int rawNodeCount;
    [Tooltip("Raw connection count before UI filtering.")]
    public int rawConnectionCount;
    [Tooltip("Raw active flee record count before UI filtering.")]
    public int rawActiveFleeRecordCount;
    [Tooltip("Visible node rows after filtering.")]
    public int visibleNodeCount;
    [Tooltip("Visible node rows that are blocked for the inspected agent.")]
    public int blockedNodeCount;
    [Tooltip("Visible connection rows for the selected node.")]
    public int visibleConnectionCount;
    [Tooltip("Visible connection rows that are blocked for the inspected agent.")]
    public int blockedConnectionCount;
    [Tooltip("Visible active flee rows.")]
    public int visibleFleeRecordCount;
    [Tooltip("Selected node id.")]
    public string selectedNodeId;
    [Tooltip("Selected node display name.")]
    public string selectedNodeName;
    [Tooltip("One-line summary for UI.")]
    public string summary;
    [Tooltip("Visible node rows.")]
    public List<OverworldEncounterDebugNodeRow> nodes = new List<OverworldEncounterDebugNodeRow>();
    [Tooltip("Visible connection rows for the selected node.")]
    public List<OverworldEncounterDebugConnectionRow> selectedNodeConnections = new List<OverworldEncounterDebugConnectionRow>();
    [Tooltip("Visible active flee record rows.")]
    public List<OverworldEncounterDebugFleeRow> activeFleeRecords = new List<OverworldEncounterDebugFleeRow>();
}
