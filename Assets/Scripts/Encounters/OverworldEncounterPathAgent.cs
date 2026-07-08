using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum OverworldEncounterPathMode {
    HoldPosition,
    RandomConnectedNode,
    PatrolList,
    PingPongPatrol,
    RandomGroupNode,
    ManualRoute
}

public enum OverworldPathAgentKind {
    Generic,
    Player,
    Pokemon,
    NPC,
    Companion
}

public class OverworldEncounterPathAgent : MonoBehaviour {
    [Header("Player Context")]
    [Tooltip("Player used for node/requirement checks. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Agent")]
    [Tooltip("Actor kind used by node flags such as No Player, No NPC or No Pokemon.")]
    [SerializeField] OverworldPathAgentKind agentKind = OverworldPathAgentKind.Pokemon;
    [Tooltip("Movement capabilities this agent can use when checking nodes and connections.")]
    [SerializeField] OverworldMovementCapabilityFlags movementCapabilities = OverworldMovementCapabilityFlags.Walk;

    [Header("Graph")]
    [Tooltip("Node group this agent can use when choosing nearest or random nodes.")]
    [SerializeField] OverworldEncounterNodeGroup nodeGroup = null;
    [Tooltip("Node this agent starts from. Empty can use nearest/group random behavior.")]
    [SerializeField] OverworldEncounterNode startNode = null;
    [Tooltip("Ordered patrol route used by Patrol List and Ping Pong Patrol modes.")]
    [SerializeField] List<OverworldEncounterNode> patrolNodes = new List<OverworldEncounterNode>();
    [Tooltip("If enabled, the agent moves to the start node position when enabled.")]
    [SerializeField] bool snapToStartNode = true;
    [Tooltip("If enabled and Start Node is empty, the nearest valid node from Node Group is used.")]
    [SerializeField] bool chooseNearestStartNode = true;
    [Tooltip("Maximum distance for nearest start node lookup. 0 means unlimited.")]
    [Min(0f)]
    [SerializeField] float nearestStartMaxDistance = 0f;

    [Header("Routing")]
    [Tooltip("How the next node is selected after reaching the current target.")]
    [SerializeField] OverworldEncounterPathMode pathMode = OverworldEncounterPathMode.RandomConnectedNode;
    [Tooltip("If enabled, random connected movement avoids immediately returning to the previous node when another option exists.")]
    [SerializeField] bool avoidImmediateBacktracking = true;
    [Tooltip("If enabled, movement starts automatically when this component is enabled.")]
    [SerializeField] bool startMovingOnEnable = true;
    [Tooltip("If enabled, movement pauses when there is no valid next node instead of choosing a random group node.")]
    [SerializeField] bool stopWhenNoRoute;

    [Header("Movement")]
    [Tooltip("Movement speed in world units per second.")]
    [Min(0f)]
    [SerializeField] float moveSpeed = 1.5f;
    [Tooltip("Distance at which the target node is considered reached.")]
    [Min(0.001f)]
    [SerializeField] float arriveDistance = 0.05f;
    [Tooltip("If enabled, Rigidbody2D.MovePosition is used when a Rigidbody2D is available.")]
    [SerializeField] bool useRigidbody2D = true;
    [Tooltip("Optional Rigidbody2D override. Empty uses the Rigidbody2D on this GameObject.")]
    [SerializeField] Rigidbody2D body = null;
    [Tooltip("If enabled, local scale X flips toward movement direction.")]
    [SerializeField] bool flipSpriteByDirection = true;

    [Header("Filtering")]
    [Tooltip("Free-form agent tags used by node connections, such as small, flying, water, rare or aggressive.")]
    [SerializeField] List<string> agentTags = new List<string>();
    [Tooltip("If not empty, destination nodes must include all of these flags.")]
    [SerializeField] OverworldEncounterNodeFlags requiredNodeFlags = OverworldEncounterNodeFlags.None;
    [Tooltip("If not empty, destination nodes with any of these flags are ignored.")]
    [SerializeField] OverworldEncounterNodeFlags blockedNodeFlags = OverworldEncounterNodeFlags.None;
    [Tooltip("If not empty, destination nodes must have at least one of these tags.")]
    [SerializeField] List<string> requiredNodeTags = new List<string>();
    [Tooltip("If not empty, destination nodes with any of these tags are ignored.")]
    [SerializeField] List<string> blockedNodeTags = new List<string>();

    [Header("Debug")]
    [Tooltip("If enabled, state changes are written to GameDebug.")]
    [SerializeField] bool logStateChanges;

    OverworldEncounterNode currentNode;
    OverworldEncounterNode targetNode;
    OverworldEncounterNode previousNode;
    Vector3 targetPosition;
    readonly List<OverworldEncounterNode> activeManualRoute = new List<OverworldEncounterNode>();
    int manualRouteIndex;
    bool followingManualRoute;
    bool resumeConfiguredRoutingAfterManualRoute;
    int patrolIndex;
    int patrolDirection = 1;
    bool moving;
    bool waiting;
    Coroutine waitRoutine;

    public PlayerController PlayerOverride => playerOverride;
    public OverworldEncounterNodeGroup NodeGroup => nodeGroup;
    public OverworldEncounterNode StartNode => startNode;
    public IReadOnlyList<OverworldEncounterNode> PatrolNodes => patrolNodes != null ? (IReadOnlyList<OverworldEncounterNode>)patrolNodes : Array.Empty<OverworldEncounterNode>();
    public OverworldEncounterPathMode PathMode => pathMode;
    public OverworldPathAgentKind AgentKind => agentKind;
    public OverworldMovementCapabilityFlags MovementCapabilities => movementCapabilities;
    public OverworldEncounterNodeFlags RequiredNodeFlags => requiredNodeFlags;
    public OverworldEncounterNodeFlags BlockedNodeFlags => blockedNodeFlags;
    public IReadOnlyList<string> AgentTags => agentTags != null ? (IReadOnlyList<string>)agentTags : Array.Empty<string>();
    public IReadOnlyList<string> RequiredNodeTags => requiredNodeTags != null ? (IReadOnlyList<string>)requiredNodeTags : Array.Empty<string>();
    public IReadOnlyList<string> BlockedNodeTags => blockedNodeTags != null ? (IReadOnlyList<string>)blockedNodeTags : Array.Empty<string>();
    public OverworldEncounterNode CurrentNode => currentNode;
    public OverworldEncounterNode TargetNode => targetNode;
    public IReadOnlyList<OverworldEncounterNode> ActiveManualRoute => activeManualRoute;
    public bool IsFollowingManualRoute => followingManualRoute;
    public bool IsMoving => moving;
    public bool IsWaiting => waiting;
    public event Action<OverworldEncounterNode> OnNodeReached;
    public event Action<OverworldEncounterNode> OnTargetChanged;
    public event Action<IReadOnlyList<OverworldEncounterNode>> OnRouteStarted;
    public event Action<OverworldEncounterNode> OnRouteCompleted;

    void Awake() {
        if(body == null) {
            body = GetComponent<Rigidbody2D>();
        }
    }

    void OnEnable() {
        InitializeRoute();
        if(startMovingOnEnable) {
            ResumeMovement();
        }
    }

    void OnDisable() {
        if(waitRoutine != null) {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }

        waiting = false;
    }

    void Update() {
        if(!moving || waiting || targetNode == null) {
            return;
        }

        MoveTowardsTarget();
    }

    public void ResumeMovement() {
        moving = followingManualRoute || pathMode != OverworldEncounterPathMode.HoldPosition;
        if(moving && targetNode == null) {
            SelectNextTarget();
        }
    }

    public void PauseMovement() {
        moving = false;
    }

    public bool FollowPathResult(OverworldEncounterPathResult pathResult, bool snapToFirstNode = false, bool resumeConfiguredRoutingAfterRoute = false) {
        if(pathResult == null || !pathResult.Success || pathResult.Nodes == null || pathResult.Nodes.Count == 0) {
            return false;
        }

        return SetManualRoute(pathResult.Nodes, snapToFirstNode, true, resumeConfiguredRoutingAfterRoute);
    }

    public bool SetManualRoute(IEnumerable<OverworldEncounterNode> route, bool snapToFirstNode = false, bool startMoving = true, bool resumeConfiguredRoutingAfterRoute = false) {
        var nodes = route?.Where(node => node != null).ToList() ?? new List<OverworldEncounterNode>();
        if(nodes.Count == 0) {
            return false;
        }

        var player = ResolvePlayer();
        foreach(var node in nodes) {
            if(!IsNodeAllowed(node, player, out _)) {
                return false;
            }
        }

        if(waitRoutine != null) {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }

        waiting = false;
        activeManualRoute.Clear();
        activeManualRoute.AddRange(nodes);
        followingManualRoute = true;
        resumeConfiguredRoutingAfterManualRoute = resumeConfiguredRoutingAfterRoute;
        manualRouteIndex = 0;

        if(snapToFirstNode && activeManualRoute.Count > 0) {
            WarpToNode(activeManualRoute[0]);
            manualRouteIndex = 1;
        } else if(currentNode != null && activeManualRoute.Count > 0 && activeManualRoute[0] == currentNode) {
            manualRouteIndex = 1;
        }

        moving = startMoving;
        OnRouteStarted?.Invoke(ActiveManualRoute);

        if(startMoving && !TrySelectNextManualRouteTarget()) {
            FinishManualRoute();
        }

        return true;
    }

    public void ClearManualRoute(bool resumeConfiguredRouting = false) {
        followingManualRoute = false;
        activeManualRoute.Clear();
        manualRouteIndex = 0;
        resumeConfiguredRoutingAfterManualRoute = resumeConfiguredRouting;
        targetNode = null;

        if(resumeConfiguredRouting) {
            ResumeMovement();
        }
    }

    public void SetTargetNode(OverworldEncounterNode node, bool snap = false) {
        if(node == null || !IsNodeAllowed(node, ResolvePlayer(), out _)) {
            return;
        }

        targetNode = node;
        targetPosition = node.GetStopPosition();
        OnTargetChanged?.Invoke(targetNode);

        if(snap) {
            MoveToPosition(targetPosition);
            MarkReachedTarget();
        }
    }

    public void WarpToNode(OverworldEncounterNode node) {
        if(node == null) {
            return;
        }

        currentNode = node;
        targetNode = node;
        targetPosition = node.GetStopPosition();
        MoveToPosition(targetPosition);
        OnNodeReached?.Invoke(node);
    }

    public bool HasAgentTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && AgentTags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasAllCapabilities(OverworldMovementCapabilityFlags requiredCapabilities) {
        return requiredCapabilities == OverworldMovementCapabilityFlags.None
            || (movementCapabilities & requiredCapabilities) == requiredCapabilities;
    }

    public bool HasAnyCapability(OverworldMovementCapabilityFlags capabilities) {
        return capabilities != OverworldMovementCapabilityFlags.None
            && (movementCapabilities & capabilities) != 0;
    }

    public bool TryFindPathTo(OverworldEncounterNode destination, out OverworldEncounterPathResult result, bool allowPartialPath = false) {
        var player = ResolvePlayer();
        var start = currentNode;
        if(start == null && nodeGroup != null) {
            start = nodeGroup.FindNearestNode(transform.position, nearestStartMaxDistance, player, this);
        }

        if(start == null) {
            start = startNode;
        }

        var request = new OverworldEncounterPathRequest {
            start = start,
            goal = destination,
            nodeGroup = nodeGroup,
            agent = this,
            player = player,
            restrictToGroup = false,
            allowPartialPath = allowPartialPath,
            maxVisitedNodes = 256
        };

        return OverworldEncounterPathfinding.TryFindPath(request, out result);
    }

    public bool IsNodeAllowed(OverworldEncounterNode node, PlayerController player, out string failureMessage) {
        if(node == null) {
            failureMessage = "Node is missing.";
            return false;
        }

        if(IsBlockedByActorKind(node.NodeFlags)) {
            failureMessage = "Node does not allow this actor kind.";
            return false;
        }

        if(requiredNodeFlags != OverworldEncounterNodeFlags.None && (node.NodeFlags & requiredNodeFlags) != requiredNodeFlags) {
            failureMessage = "Node does not match required flags.";
            return false;
        }

        if(blockedNodeFlags != OverworldEncounterNodeFlags.None && (node.NodeFlags & blockedNodeFlags) != 0) {
            failureMessage = "Node has a blocked flag.";
            return false;
        }

        if(node.RequiredCapabilities != OverworldMovementCapabilityFlags.None && !HasAllCapabilities(node.RequiredCapabilities)) {
            failureMessage = "Node requires movement capabilities this agent does not have.";
            return false;
        }

        var requiredTags = requiredNodeTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() ?? new List<string>();
        if(requiredTags.Count > 0 && !requiredTags.Any(node.HasTag)) {
            failureMessage = "Node does not match required tags.";
            return false;
        }

        var blockedTags = blockedNodeTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() ?? new List<string>();
        if(blockedTags.Any(node.HasTag)) {
            failureMessage = "Node has a blocked tag.";
            return false;
        }

        return node.CanUse(player, out failureMessage);
    }

    bool IsBlockedByActorKind(OverworldEncounterNodeFlags flags) {
        return agentKind switch {
            OverworldPathAgentKind.Player => (flags & OverworldEncounterNodeFlags.NoPlayer) != 0,
            OverworldPathAgentKind.NPC => (flags & OverworldEncounterNodeFlags.NoNPC) != 0,
            OverworldPathAgentKind.Pokemon => (flags & OverworldEncounterNodeFlags.NoPokemon) != 0,
            OverworldPathAgentKind.Companion => (flags & OverworldEncounterNodeFlags.NoNPC) != 0,
            _ => false
        };
    }

    void InitializeRoute() {
        var player = ResolvePlayer();
        currentNode = startNode;
        if(currentNode == null && chooseNearestStartNode && nodeGroup != null) {
            currentNode = nodeGroup.FindNearestNode(transform.position, nearestStartMaxDistance, player, this);
        }

        if(currentNode == null && nodeGroup != null) {
            currentNode = nodeGroup.GetRandomNode(player, this);
        }

        if(currentNode != null && snapToStartNode) {
            MoveToPosition(currentNode.GetStopPosition());
        }

        if(pathMode == OverworldEncounterPathMode.PatrolList || pathMode == OverworldEncounterPathMode.PingPongPatrol) {
            patrolNodes = patrolNodes?.Where(node => node != null).ToList() ?? new List<OverworldEncounterNode>();
            if(currentNode != null && patrolNodes.Contains(currentNode)) {
                patrolIndex = patrolNodes.IndexOf(currentNode);
            }
        }

        targetNode = null;
        targetPosition = transform.position;
    }

    void MoveTowardsTarget() {
        Vector3 currentPosition = transform.position;
        Vector3 nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);
        MoveToPosition(nextPosition);
        FaceMovement(nextPosition - currentPosition);

        if(Vector3.Distance(transform.position, targetPosition) <= arriveDistance) {
            MarkReachedTarget();
        }
    }

    void MoveToPosition(Vector3 position) {
        if(useRigidbody2D && body != null) {
            body.MovePosition(position);
        } else {
            transform.position = position;
        }
    }

    void FaceMovement(Vector3 delta) {
        if(!flipSpriteByDirection || Mathf.Abs(delta.x) <= 0.001f) {
            return;
        }

        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (delta.x < 0f ? -1f : 1f);
        transform.localScale = scale;
    }

    void MarkReachedTarget() {
        previousNode = currentNode;
        currentNode = targetNode;
        OnNodeReached?.Invoke(currentNode);

        if(logStateChanges && currentNode != null) {
            GameDebug.Step($"{name} reached encounter node {currentNode.DisplayName}.", GameDebugCategory.Encounter, this, "OverworldEncounterPathAgent");
        }

        if(followingManualRoute) {
            if(TrySelectNextManualRouteTarget()) {
                return;
            }

            FinishManualRoute();
            return;
        }

        if(currentNode == null || pathMode == OverworldEncounterPathMode.HoldPosition) {
            targetNode = null;
            return;
        }

        float waitSeconds = currentNode.RollWaitSeconds();
        if(waitSeconds > 0f) {
            waitRoutine = StartCoroutine(WaitThenSelectNext(waitSeconds));
        } else {
            SelectNextTarget();
        }
    }

    IEnumerator WaitThenSelectNext(float seconds) {
        waiting = true;
        yield return new WaitForSeconds(seconds);
        waiting = false;
        waitRoutine = null;
        SelectNextTarget();
    }

    void SelectNextTarget() {
        if(!moving) {
            return;
        }

        if(followingManualRoute) {
            if(!TrySelectNextManualRouteTarget()) {
                FinishManualRoute();
            }

            return;
        }

        var next = pathMode switch {
            OverworldEncounterPathMode.PatrolList => SelectPatrolNode(loop: true),
            OverworldEncounterPathMode.PingPongPatrol => SelectPingPongNode(),
            OverworldEncounterPathMode.RandomGroupNode => SelectRandomGroupNode(),
            OverworldEncounterPathMode.RandomConnectedNode => SelectRandomConnectedNode(),
            _ => null
        };

        if(next == null && !stopWhenNoRoute && pathMode != OverworldEncounterPathMode.RandomGroupNode) {
            next = SelectRandomGroupNode();
        }

        if(next == null) {
            targetNode = null;
            moving = false;
            return;
        }

        SetTargetNode(next);
    }

    bool TrySelectNextManualRouteTarget() {
        while(manualRouteIndex < activeManualRoute.Count) {
            var next = activeManualRoute[manualRouteIndex];
            manualRouteIndex++;

            if(next == null || next == currentNode) {
                continue;
            }

            SetTargetNode(next);
            return targetNode == next;
        }

        return false;
    }

    void FinishManualRoute() {
        followingManualRoute = false;
        activeManualRoute.Clear();
        manualRouteIndex = 0;
        targetNode = null;
        OnRouteCompleted?.Invoke(currentNode);

        if(resumeConfiguredRoutingAfterManualRoute && pathMode != OverworldEncounterPathMode.HoldPosition && pathMode != OverworldEncounterPathMode.ManualRoute) {
            moving = true;
            SelectNextTarget();
            return;
        }

        moving = false;
    }

    OverworldEncounterNode SelectRandomConnectedNode() {
        if(currentNode == null) {
            return SelectRandomGroupNode();
        }

        var player = ResolvePlayer();
        var candidates = currentNode.GetAvailableConnections(this, player)
            .Where(connection => connection.Node != null && IsNodeAllowed(connection.Node, player, out _))
            .ToList();

        if(avoidImmediateBacktracking && previousNode != null && candidates.Count > 1) {
            candidates = candidates.Where(connection => connection.Node != previousNode).ToList();
        }

        int totalWeight = candidates.Sum(connection => connection.Weight);
        if(totalWeight <= 0) {
            return null;
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        foreach(var connection in candidates) {
            roll -= connection.Weight;
            if(roll < 0) {
                return connection.Node;
            }
        }

        return candidates.LastOrDefault()?.Node;
    }

    OverworldEncounterNode SelectPatrolNode(bool loop) {
        var player = ResolvePlayer();
        var validPatrol = patrolNodes?.Where(node => node != null && IsNodeAllowed(node, player, out _)).ToList() ?? new List<OverworldEncounterNode>();
        if(validPatrol.Count == 0) {
            return null;
        }

        patrolIndex++;
        if(patrolIndex >= validPatrol.Count) {
            if(!loop) {
                patrolIndex = validPatrol.Count - 1;
                return null;
            }

            patrolIndex = 0;
        }

        return validPatrol[patrolIndex];
    }

    OverworldEncounterNode SelectPingPongNode() {
        var player = ResolvePlayer();
        var validPatrol = patrolNodes?.Where(node => node != null && IsNodeAllowed(node, player, out _)).ToList() ?? new List<OverworldEncounterNode>();
        if(validPatrol.Count == 0) {
            return null;
        }

        if(validPatrol.Count == 1) {
            return validPatrol[0];
        }

        patrolIndex += patrolDirection;
        if(patrolIndex >= validPatrol.Count) {
            patrolIndex = validPatrol.Count - 2;
            patrolDirection = -1;
        } else if(patrolIndex < 0) {
            patrolIndex = 1;
            patrolDirection = 1;
        }

        return validPatrol[Mathf.Clamp(patrolIndex, 0, validPatrol.Count - 1)];
    }

    OverworldEncounterNode SelectRandomGroupNode() {
        return nodeGroup != null ? nodeGroup.GetRandomNode(ResolvePlayer(), this) : null;
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
