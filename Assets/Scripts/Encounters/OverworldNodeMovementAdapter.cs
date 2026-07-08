using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OverworldNodeMovementAdapter : MonoBehaviour {
    [Header("References")]
    [Tooltip("Path agent that performs actual movement and capability checks. Empty uses the component on this GameObject.")]
    [SerializeField] OverworldEncounterPathAgent pathAgent = null;
    [Tooltip("Node group used to resolve the nearest current node when the path agent has no current node.")]
    [SerializeField] OverworldEncounterNodeGroup nodeGroup = null;
    [Tooltip("Player context used by node requirement checks. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Current Node")]
    [Tooltip("If enabled, Start tries to resolve the nearest usable node and optionally warp to it.")]
    [SerializeField] bool resolveCurrentNodeOnStart = true;
    [Tooltip("If enabled, the path agent is warped to the resolved nearest node on Start.")]
    [SerializeField] bool snapToResolvedNodeOnStart = false;
    [Tooltip("Maximum distance used when resolving the nearest node. 0 means unlimited.")]
    [Min(0f)]
    [SerializeField] float nearestNodeMaxDistance = 0f;

    [Header("Input Direction")]
    [Tooltip("Minimum direction magnitude required before a move attempt is accepted.")]
    [Min(0.001f)]
    [SerializeField] float minDirectionMagnitude = 0.1f;
    [Tooltip("Minimum dot product between input direction and connection direction. Higher values require stricter cardinal/diagonal alignment.")]
    [Range(-1f, 1f)]
    [SerializeField] float directionDotThreshold = 0.35f;
    [Tooltip("If enabled, diagonal input is collapsed to the dominant cardinal axis.")]
    [SerializeField] bool preferCardinalDirection = true;
    [Tooltip("If enabled, attempts are rejected while the path agent is already moving or following a manual route.")]
    [SerializeField] bool blockInputWhileMoving = true;

    [Header("Movement")]
    [Tooltip("If enabled, a successful move is executed immediately through the path agent.")]
    [SerializeField] bool moveImmediately = true;
    [Tooltip("If enabled, the path agent resumes its configured patrol/random routing after this one-step movement completes.")]
    [SerializeField] bool resumeConfiguredRoutingAfterMove = false;
    [Tooltip("If enabled, failed movement attempts are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = false;
    [Tooltip("If enabled, successful movement attempts are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts = false;

    public OverworldEncounterPathAgent PathAgent => pathAgent;
    public OverworldEncounterNodeGroup NodeGroup => nodeGroup != null ? nodeGroup : pathAgent != null ? pathAgent.NodeGroup : null;
    public bool BlockInputWhileMoving => blockInputWhileMoving;
    public float DirectionDotThreshold => directionDotThreshold;
    public float MinDirectionMagnitude => Mathf.Max(0.001f, minDirectionMagnitude);
    public event Action<OverworldNodeMoveResult> OnMoveAttempted;
    public event Action<OverworldNodeMoveResult> OnMoveBlocked;
    public event Action<OverworldNodeMoveResult> OnMoveStarted;

    void Awake() {
        if(pathAgent == null) {
            pathAgent = GetComponent<OverworldEncounterPathAgent>();
        }
    }

    void Start() {
        if(resolveCurrentNodeOnStart) {
            ResolveCurrentNode(snapToResolvedNodeOnStart);
        }
    }

    [ContextMenu("Try Move Up")]
    public void TryMoveUp() {
        TryMove(Vector2.up);
    }

    [ContextMenu("Try Move Down")]
    public void TryMoveDown() {
        TryMove(Vector2.down);
    }

    [ContextMenu("Try Move Left")]
    public void TryMoveLeft() {
        TryMove(Vector2.left);
    }

    [ContextMenu("Try Move Right")]
    public void TryMoveRight() {
        TryMove(Vector2.right);
    }

    public bool TryMove(Vector2 inputDirection) {
        var result = BuildMoveResult(inputDirection);
        OnMoveAttempted?.Invoke(result);

        if(!result.success) {
            LogBlocked(result.message);
            OnMoveBlocked?.Invoke(result);
            return false;
        }

        if(moveImmediately && pathAgent != null) {
            pathAgent.SetManualRoute(new[] { result.fromNode, result.toNode }, snapToFirstNode: false, startMoving: true, resumeConfiguredRoutingAfterRoute: resumeConfiguredRoutingAfterMove);
        }

        if(logSuccessfulAttempts) {
            GameDebug.Step(result.message, GameDebugCategory.Encounter, this, "OverworldNodeMovementAdapter");
        }

        OnMoveStarted?.Invoke(result);
        return true;
    }

    public bool TryGetMoveTarget(Vector2 inputDirection, out OverworldEncounterNode targetNode, out OverworldNodeMoveResult result) {
        result = BuildMoveResult(inputDirection);
        targetNode = result.success ? result.toNode : null;
        return result.success;
    }

    public OverworldEncounterNode ResolveCurrentNode(bool snapToNode) {
        if(pathAgent == null) {
            return null;
        }

        if(pathAgent.CurrentNode != null) {
            return pathAgent.CurrentNode;
        }

        var group = NodeGroup;
        if(group == null) {
            return null;
        }

        var node = group.FindNearestNode(transform.position, nearestNodeMaxDistance, ResolvePlayer(), pathAgent);
        if(node != null && snapToNode) {
            pathAgent.WarpToNode(node);
        }

        return node;
    }

    OverworldNodeMoveResult BuildMoveResult(Vector2 inputDirection) {
        var result = new OverworldNodeMoveResult {
            requestedDirection = inputDirection
        };

        if(pathAgent == null) {
            result.message = "Node movement adapter has no path agent.";
            return result;
        }

        if(blockInputWhileMoving && (pathAgent.IsMoving || pathAgent.IsFollowingManualRoute)) {
            result.message = "Path agent is already moving.";
            return result;
        }

        Vector2 direction = NormalizeInputDirection(inputDirection);
        result.normalizedDirection = direction;
        if(direction.sqrMagnitude <= 0f) {
            result.message = "Input direction is too small.";
            return result;
        }

        var current = ResolveCurrentNode(snapToNode: false);
        if(current == null) {
            result.message = "No current node could be resolved.";
            return result;
        }

        result.fromNode = current;
        var player = ResolvePlayer();
        var candidates = current.GetAvailableConnections(pathAgent, player)
            .Where(connection => connection?.Node != null)
            .Select(connection => new DirectionalConnection(connection, CalculateDirectionScore(current, connection.Node, direction)))
            .Where(candidate => candidate.score >= directionDotThreshold)
            .OrderByDescending(candidate => candidate.score)
            .ThenBy(candidate => candidate.connection.TraversalCost)
            .ToList();

        var best = candidates.FirstOrDefault();
        if(best == null || best.connection == null) {
            result.message = "No usable node connection exists in the requested direction.";
            return result;
        }

        result.success = true;
        result.toNode = best.connection.Node;
        result.connection = best.connection;
        result.directionScore = best.score;
        result.message = $"Moving from {current.DisplayName} to {result.toNode.DisplayName}.";
        return result;
    }

    Vector2 NormalizeInputDirection(Vector2 inputDirection) {
        if(inputDirection.magnitude < MinDirectionMagnitude) {
            return Vector2.zero;
        }

        if(preferCardinalDirection) {
            return Mathf.Abs(inputDirection.x) >= Mathf.Abs(inputDirection.y)
                ? new Vector2(Mathf.Sign(inputDirection.x), 0f)
                : new Vector2(0f, Mathf.Sign(inputDirection.y));
        }

        return inputDirection.normalized;
    }

    float CalculateDirectionScore(OverworldEncounterNode from, OverworldEncounterNode to, Vector2 requestedDirection) {
        if(from == null || to == null || requestedDirection.sqrMagnitude <= 0f) {
            return -1f;
        }

        Vector3 delta = to.transform.position - from.transform.position;
        Vector2 connectionDirection = new Vector2(delta.x, delta.y);
        if(connectionDirection.sqrMagnitude <= 0.0001f) {
            return -1f;
        }

        return Vector2.Dot(requestedDirection.normalized, connectionDirection.normalized);
    }

    void LogBlocked(string message) {
        if(logBlockedAttempts) {
            GameDebug.Warning(message, GameDebugCategory.Encounter, this, "OverworldNodeMovementAdapter");
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

    class DirectionalConnection {
        public readonly OverworldEncounterNodeConnection connection;
        public readonly float score;

        public DirectionalConnection(OverworldEncounterNodeConnection connection, float score) {
            this.connection = connection;
            this.score = score;
        }
    }
}

[Serializable]
public class OverworldNodeMoveResult {
    [Tooltip("If enabled, the requested move found a valid destination.")]
    public bool success;
    [Tooltip("Raw requested input direction.")]
    public Vector2 requestedDirection;
    [Tooltip("Normalized/cardinal direction used for connection matching.")]
    public Vector2 normalizedDirection;
    [Tooltip("Node where the move starts.")]
    public OverworldEncounterNode fromNode;
    [Tooltip("Node where the move goes.")]
    public OverworldEncounterNode toNode;
    [Tooltip("Connection selected for this move.")]
    public OverworldEncounterNodeConnection connection;
    [Tooltip("Dot-product score between requested direction and selected connection direction.")]
    public float directionScore;
    [Tooltip("Success or failure message.")]
    public string message;
}
