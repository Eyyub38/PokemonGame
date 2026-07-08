using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class OverworldEncounterPathRequest {
    [Tooltip("Node where the path search starts.")]
    public OverworldEncounterNode start;
    [Tooltip("Node the path search tries to reach.")]
    public OverworldEncounterNode goal;
    [Tooltip("Optional node group used to limit search when Restrict To Group is enabled.")]
    public OverworldEncounterNodeGroup nodeGroup;
    [Tooltip("Agent whose tags, actor kind and movement capabilities are used for access checks.")]
    public OverworldEncounterPathAgent agent;
    [Tooltip("Player context used by reusable ActivityRequirement checks on nodes.")]
    public PlayerController player;
    [Tooltip("If enabled, only nodes inside Node Group can be used.")]
    public bool restrictToGroup;
    [Tooltip("If enabled, the best partial route is returned when the goal cannot be reached.")]
    public bool allowPartialPath;
    [Tooltip("Maximum number of visited nodes before the search stops. 0 means unlimited.")]
    [Min(0)]
    public int maxVisitedNodes = 256;
    [Tooltip("Maximum total path cost. 0 means unlimited.")]
    [Min(0f)]
    public float maxTotalCost;
    [Tooltip("If enabled, connections flagged as Dangerous are ignored.")]
    public bool avoidDangerousConnections;
    [Tooltip("If enabled, nodes flagged as Danger are ignored.")]
    public bool avoidDangerousNodes;
}

[Serializable]
public class OverworldEncounterPathResult {
    [Tooltip("True when a full or allowed partial path was found.")]
    [SerializeField] bool success;
    [Tooltip("True when the returned path does not reach the requested goal.")]
    [SerializeField] bool partialPath;
    [Tooltip("Reason for a failed search, or a note explaining why a partial route was returned.")]
    [SerializeField] string message = string.Empty;
    [Tooltip("Total route cost calculated by the pathfinding service.")]
    [SerializeField] float totalCost;
    [Tooltip("Number of graph nodes visited during the search.")]
    [SerializeField] int visitedNodeCount;
    [Tooltip("Ordered nodes in the resulting route.")]
    [SerializeField] List<OverworldEncounterNode> nodes = new List<OverworldEncounterNode>();

    public bool Success => success;
    public bool PartialPath => partialPath;
    public string Message => message;
    public float TotalCost => totalCost;
    public int VisitedNodeCount => visitedNodeCount;
    public IReadOnlyList<OverworldEncounterNode> Nodes => nodes != null ? (IReadOnlyList<OverworldEncounterNode>)nodes : Array.Empty<OverworldEncounterNode>();
    public OverworldEncounterNode LastNode => nodes != null && nodes.Count > 0 ? nodes[nodes.Count - 1] : null;

    public static OverworldEncounterPathResult Failed(string failureMessage, int visitedCount = 0) {
        return new OverworldEncounterPathResult {
            success = false,
            partialPath = false,
            message = failureMessage,
            totalCost = 0f,
            visitedNodeCount = visitedCount,
            nodes = new List<OverworldEncounterNode>()
        };
    }

    public static OverworldEncounterPathResult FromPath(List<OverworldEncounterNode> path, float cost, int visitedCount, bool isPartial, string resultMessage) {
        return new OverworldEncounterPathResult {
            success = path != null && path.Count > 0,
            partialPath = isPartial,
            message = resultMessage ?? string.Empty,
            totalCost = Mathf.Max(0f, cost),
            visitedNodeCount = Mathf.Max(0, visitedCount),
            nodes = path ?? new List<OverworldEncounterNode>()
        };
    }
}

public static class OverworldEncounterPathfinding {
    public static bool TryFindPath(OverworldEncounterPathRequest request, out OverworldEncounterPathResult result) {
        if(request == null) {
            result = OverworldEncounterPathResult.Failed("Path request is missing.");
            return false;
        }

        if(request.start == null) {
            result = OverworldEncounterPathResult.Failed("Path request has no start node.");
            return false;
        }

        if(request.goal == null) {
            result = OverworldEncounterPathResult.Failed("Path request has no goal node.");
            return false;
        }

        if(request.agent != null && !request.agent.IsNodeAllowed(request.start, request.player, out var startFailure)) {
            result = OverworldEncounterPathResult.Failed(startFailure);
            return false;
        }

        if(request.start == request.goal) {
            result = OverworldEncounterPathResult.FromPath(new List<OverworldEncounterNode> { request.start }, 0f, 1, false, "Already at goal node.");
            return true;
        }

        request.nodeGroup?.RefreshNodes();

        var open = new List<PathSearchNode>();
        var visited = new HashSet<OverworldEncounterNode>();
        var cameFrom = new Dictionary<OverworldEncounterNode, OverworldEncounterNode>();
        var costs = new Dictionary<OverworldEncounterNode, float> {
            [request.start] = 0f
        };

        OverworldEncounterNode bestPartialNode = request.start;
        float bestPartialDistance = EstimateDistance(request.start, request.goal);
        int maxVisited = request.maxVisitedNodes > 0 ? request.maxVisitedNodes : int.MaxValue;

        open.Add(new PathSearchNode(request.start, 0f, bestPartialDistance));

        while(open.Count > 0) {
            int bestIndex = FindBestOpenIndex(open);
            var current = open[bestIndex];
            open.RemoveAt(bestIndex);

            if(current.node == null || !visited.Add(current.node)) {
                continue;
            }

            if(visited.Count > maxVisited) {
                break;
            }

            if(current.node == request.goal) {
                var path = BuildPath(request.start, request.goal, cameFrom);
                result = OverworldEncounterPathResult.FromPath(path, costs[request.goal], visited.Count, false, "Path found.");
                return true;
            }

            float distanceToGoal = EstimateDistance(current.node, request.goal);
            if(current.node != request.start && distanceToGoal < bestPartialDistance) {
                bestPartialNode = current.node;
                bestPartialDistance = distanceToGoal;
            }

            foreach(var connection in current.node.GetAvailableConnections(request.agent, request.player)) {
                if(!CanVisitConnection(request, connection, out _)) {
                    continue;
                }

                var next = connection.Node;
                float candidateCost = costs[current.node] + CalculateTraversalCost(current.node, next, connection);
                if(request.maxTotalCost > 0f && candidateCost > request.maxTotalCost) {
                    continue;
                }

                if(costs.TryGetValue(next, out var existingCost) && candidateCost >= existingCost) {
                    continue;
                }

                cameFrom[next] = current.node;
                costs[next] = candidateCost;
                float score = candidateCost + EstimateDistance(next, request.goal);
                open.Add(new PathSearchNode(next, candidateCost, score));
            }
        }

        if(request.allowPartialPath && bestPartialNode != null && bestPartialNode != request.start && costs.TryGetValue(bestPartialNode, out var partialCost)) {
            var partialPath = BuildPath(request.start, bestPartialNode, cameFrom);
            result = OverworldEncounterPathResult.FromPath(partialPath, partialCost, visited.Count, true, "Goal could not be reached. Best partial path returned.");
            return true;
        }

        result = OverworldEncounterPathResult.Failed("No route found between the requested nodes.", visited.Count);
        return false;
    }

    static bool CanVisitConnection(OverworldEncounterPathRequest request, OverworldEncounterNodeConnection connection, out string failureMessage) {
        failureMessage = null;
        if(connection == null || connection.Node == null) {
            failureMessage = "Connection or destination node is missing.";
            return false;
        }

        if(request.avoidDangerousConnections && (connection.Flags & OverworldEncounterConnectionFlags.Dangerous) != 0) {
            failureMessage = "Connection is marked dangerous.";
            return false;
        }

        var destination = connection.Node;
        if(request.avoidDangerousNodes && (destination.NodeFlags & OverworldEncounterNodeFlags.Danger) != 0) {
            failureMessage = "Destination node is marked dangerous.";
            return false;
        }

        if(request.restrictToGroup && request.nodeGroup != null && !request.nodeGroup.Nodes.Contains(destination)) {
            failureMessage = "Destination node is outside the requested group.";
            return false;
        }

        if(request.agent != null) {
            return request.agent.IsNodeAllowed(destination, request.player, out failureMessage);
        }

        return destination.CanUse(request.player, out failureMessage);
    }

    static float CalculateTraversalCost(OverworldEncounterNode from, OverworldEncounterNode to, OverworldEncounterNodeConnection connection) {
        float distance = from != null && to != null ? Vector3.Distance(from.transform.position, to.transform.position) : 0f;
        return Mathf.Max(0.01f, distance) + connection.TraversalCost;
    }

    static float EstimateDistance(OverworldEncounterNode from, OverworldEncounterNode to) {
        if(from == null || to == null) {
            return 0f;
        }

        return Vector3.Distance(from.transform.position, to.transform.position);
    }

    static int FindBestOpenIndex(List<PathSearchNode> open) {
        int bestIndex = 0;
        float bestScore = open[0].score;
        for(int i = 1; i < open.Count; i++) {
            if(open[i].score < bestScore) {
                bestScore = open[i].score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    static List<OverworldEncounterNode> BuildPath(OverworldEncounterNode start, OverworldEncounterNode end, Dictionary<OverworldEncounterNode, OverworldEncounterNode> cameFrom) {
        var path = new List<OverworldEncounterNode>();
        var current = end;
        while(current != null) {
            path.Add(current);
            if(current == start) {
                break;
            }

            cameFrom.TryGetValue(current, out current);
        }

        path.Reverse();
        return path;
    }

    class PathSearchNode {
        public readonly OverworldEncounterNode node;
        public readonly float cost;
        public readonly float score;

        public PathSearchNode(OverworldEncounterNode node, float cost, float score) {
            this.node = node;
            this.cost = cost;
            this.score = score;
        }
    }
}
