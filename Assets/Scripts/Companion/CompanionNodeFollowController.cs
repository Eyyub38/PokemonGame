using System;
using System.Linq;
using UnityEngine;

public class CompanionNodeFollowController : MonoBehaviour {
    [Header("References")]
    [Tooltip("Follow profile that controls trail offset, pathfinding options and catch-up behavior.")]
    [SerializeField] CompanionNodeFollowProfile profile;
    [Tooltip("Companion component used to check whether this object is actively following. Empty uses this GameObject.")]
    [SerializeField] CompanionController companion;
    [Tooltip("Path agent that performs node movement. Empty uses this GameObject.")]
    [SerializeField] OverworldEncounterPathAgent pathAgent;
    [Tooltip("Player followed by this node controller. Empty uses CompanionController, PlayerController.i or scene lookup.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("Player trail tracker used to select follow target nodes. Empty searches the player and can be created automatically.")]
    [SerializeField] PlayerNodeTrailTracker playerTrail;
    [Tooltip("Node group used for nearest-node fallback. Empty uses the path agent's node group or player trail group.")]
    [SerializeField] OverworldEncounterNodeGroup nodeGroup;

    [Header("Behavior")]
    [Tooltip("If enabled, this controller only runs while the CompanionController says the companion is following.")]
    [SerializeField] bool requireCompanionFollowingState = true;
    [Tooltip("If enabled, a missing PlayerNodeTrailTracker is added to the player when possible.")]
    [SerializeField] bool autoCreatePlayerTrailTracker = true;
    [Tooltip("If enabled, this component requests follow routes automatically while active.")]
    [SerializeField] bool followAutomatically = true;
    [Tooltip("If enabled, the path agent resumes its configured random/patrol route after each follow route completes.")]
    [SerializeField] bool resumeConfiguredRoutingAfterRoute;
    [Tooltip("If enabled, blocked or failed follow requests are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts;
    [Tooltip("If enabled, successful follow routes are written to GameDebug.")]
    [SerializeField] bool logSuccessfulRoutes;

    float nextRepathTime;
    OverworldEncounterNode lastTargetNode;
    string lastMessage = string.Empty;

    public CompanionNodeFollowProfile Profile => profile;
    public CompanionController Companion => companion;
    public OverworldEncounterPathAgent PathAgent => pathAgent;
    public PlayerNodeTrailTracker PlayerTrail => playerTrail;
    public OverworldEncounterNodeGroup NodeGroup => nodeGroup != null ? nodeGroup : pathAgent != null ? pathAgent.NodeGroup : playerTrail != null ? playerTrail.NodeGroup : null;
    public OverworldEncounterNode LastTargetNode => lastTargetNode;
    public string LastMessage => lastMessage;
    public event Action<CompanionNodeFollowResult> OnFollowRouteRequested;

    void Awake() {
        ResolveReferences();
    }

    void OnEnable() {
        ResolveReferences();
        Subscribe();
    }

    void OnDisable() {
        Unsubscribe();
    }

    void Update() {
        if(!followAutomatically || Time.time < nextRepathTime) {
            return;
        }

        nextRepathTime = Time.time + ResolveRepathInterval();
        TryRequestFollowRoute(out _);
    }

    [ContextMenu("Request Node Follow Route")]
    public void RequestRouteFromContextMenu() {
        TryRequestFollowRoute(out _);
    }

    public bool TryRequestFollowRoute(out CompanionNodeFollowResult result) {
        ResolveReferences();
        result = new CompanionNodeFollowResult {
            companion = companion,
            pathAgent = pathAgent,
            profile = profile
        };

        if(profile == null) {
            return Block(result, "Companion node follow profile is missing.");
        }

        if(pathAgent == null) {
            return Block(result, "Companion node follow controller has no path agent.");
        }

        if(requireCompanionFollowingState && companion != null && !companion.IsFollowing) {
            return Block(result, "Companion is not currently following.");
        }

        var player = ResolvePlayer();
        if(player == null) {
            return Block(result, "No player could be resolved for node follow.");
        }

        if(playerTrail == null) {
            return Block(result, "Player node trail tracker is missing.");
        }

        if(TryApplyCatchUp(player, out result)) {
            return true;
        }

        var group = NodeGroup;
        if(group == null) {
            return Block(result, "No node group is available for companion node follow.");
        }

        var start = ResolveCurrentNode(group, player);
        if(start == null) {
            return Block(result, "Companion could not resolve a current node.");
        }

        var target = playerTrail.GetFollowTargetNode(profile, pathAgent, player);
        result.startNode = start;
        result.targetNode = target;
        if(target == null) {
            return Block(result, "No valid player trail target node was found.");
        }

        if(start == target || Vector3.Distance(transform.position, target.transform.position) <= profile.TargetTolerance) {
            lastTargetNode = target;
            return Succeed(result, "Companion is already close enough to the follow target.", startedRoute: false);
        }

        if(pathAgent.IsFollowingManualRoute && lastTargetNode == target) {
            return Succeed(result, "Companion is already following the requested target route.", startedRoute: false);
        }

        var request = new OverworldEncounterPathRequest {
            start = start,
            goal = target,
            nodeGroup = group,
            agent = pathAgent,
            player = player,
            restrictToGroup = group != null,
            allowPartialPath = profile.AllowPartialPaths,
            maxVisitedNodes = profile.MaxVisitedNodes,
            maxTotalCost = profile.MaxTotalCost,
            avoidDangerousNodes = profile.AvoidDangerousNodes,
            avoidDangerousConnections = profile.AvoidDangerousConnections
        };

        if(!OverworldEncounterPathfinding.TryFindPath(request, out var pathResult) || pathResult == null || !pathResult.Success) {
            return Block(result, pathResult != null ? pathResult.Message : "No companion follow path could be found.");
        }

        result.pathResult = pathResult;
        bool routeStarted = pathAgent.FollowPathResult(pathResult, snapToFirstNode: false, resumeConfiguredRoutingAfterRoute: resumeConfiguredRoutingAfterRoute);
        if(!routeStarted) {
            return Block(result, "Path agent rejected the companion follow route.");
        }

        lastTargetNode = pathResult.LastNode;
        return Succeed(result, pathResult.Message, startedRoute: true);
    }

    bool TryApplyCatchUp(PlayerController player, out CompanionNodeFollowResult result) {
        result = new CompanionNodeFollowResult {
            companion = companion,
            pathAgent = pathAgent,
            profile = profile
        };

        if(profile == null || profile.CatchUpDistance <= 0f || profile.CatchUpMode == CompanionNodeCatchUpMode.None || player == null) {
            return false;
        }

        if(Vector3.Distance(transform.position, player.transform.position) <= profile.CatchUpDistance) {
            return false;
        }

        var target = profile.CatchUpMode == CompanionNodeCatchUpMode.WarpNearPlayerNode
            ? playerTrail.FindNearestAllowedNode(player.transform.position, profile, pathAgent, player)
            : playerTrail.GetFollowTargetNode(profile, pathAgent, player);

        result.targetNode = target;
        if(target == null) {
            if(profile.CatchUpMode == CompanionNodeCatchUpMode.PauseUntilReachable) {
                pathAgent.PauseMovement();
                return Succeed(result, "Companion paused because no catch-up node is reachable.", startedRoute: false);
            }

            return false;
        }

        pathAgent.WarpToNode(target);
        lastTargetNode = target;
        return Succeed(result, $"Companion caught up at {target.DisplayName}.", startedRoute: false);
    }

    OverworldEncounterNode ResolveCurrentNode(OverworldEncounterNodeGroup group, PlayerController player) {
        if(pathAgent.CurrentNode != null) {
            return pathAgent.CurrentNode;
        }

        return group != null ? group.FindNearestNode(transform.position, 0f, player, pathAgent) : null;
    }

    bool Block(CompanionNodeFollowResult result, string message) {
        result.success = false;
        result.message = string.IsNullOrWhiteSpace(message) ? "Companion node follow request was blocked." : message;
        lastMessage = result.message;
        OnFollowRouteRequested?.Invoke(result);
        if(logBlockedAttempts) {
            GameDebug.Warning(result.message, GameDebugCategory.Encounter, this, "CompanionNodeFollowController");
        }

        return false;
    }

    bool Succeed(CompanionNodeFollowResult result, string message, bool startedRoute) {
        result.success = true;
        result.startedRoute = startedRoute;
        result.message = string.IsNullOrWhiteSpace(message) ? "Companion node follow route accepted." : message;
        lastMessage = result.message;
        OnFollowRouteRequested?.Invoke(result);
        if(logSuccessfulRoutes) {
            GameDebug.Step(result.message, GameDebugCategory.Encounter, this, "CompanionNodeFollowController");
        }

        return true;
    }

    void Subscribe() {
        if(pathAgent != null) {
            pathAgent.OnRouteCompleted -= HandleRouteCompleted;
            pathAgent.OnRouteCompleted += HandleRouteCompleted;
        }
    }

    void Unsubscribe() {
        if(pathAgent != null) {
            pathAgent.OnRouteCompleted -= HandleRouteCompleted;
        }
    }

    void HandleRouteCompleted(OverworldEncounterNode node) {
        nextRepathTime = Mathf.Min(nextRepathTime, Time.time + 0.05f);
    }

    float ResolveRepathInterval() {
        return profile != null ? profile.RepathIntervalSeconds : 0.35f;
    }

    void ResolveReferences() {
        if(companion == null) {
            companion = GetComponent<CompanionController>();
        }

        if(pathAgent == null) {
            pathAgent = GetComponent<OverworldEncounterPathAgent>();
        }

        var player = ResolvePlayer();
        if(playerTrail == null && player != null) {
            playerTrail = player.GetComponent<PlayerNodeTrailTracker>();
            if(playerTrail == null && autoCreatePlayerTrailTracker) {
                playerTrail = player.gameObject.AddComponent<PlayerNodeTrailTracker>();
            }
        }

        if(playerTrail != null && NodeGroup != null) {
            playerTrail.Configure(NodeGroup, null, player);
        }
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(companion != null && CompanionController.GetFollowingCompanions().Contains(companion)) {
            return PlayerController.i;
        }

        return PlayerController.i != null ? PlayerController.i : FindAnyObjectByType<PlayerController>();
    }
}

[Serializable]
public class CompanionNodeFollowResult {
    [Tooltip("True when the follow request was accepted.")]
    public bool success;
    [Tooltip("True when a new path route was started.")]
    public bool startedRoute;
    [Tooltip("Human-readable result message.")]
    public string message;
    [Tooltip("Companion that requested the node follow route.")]
    public CompanionController companion;
    [Tooltip("Path agent used for movement.")]
    public OverworldEncounterPathAgent pathAgent;
    [Tooltip("Profile used for this follow request.")]
    public CompanionNodeFollowProfile profile;
    [Tooltip("Node the follower started from.")]
    public OverworldEncounterNode startNode;
    [Tooltip("Desired follow target node.")]
    public OverworldEncounterNode targetNode;
    [Tooltip("Pathfinding result used by the request.")]
    public OverworldEncounterPathResult pathResult;
}
