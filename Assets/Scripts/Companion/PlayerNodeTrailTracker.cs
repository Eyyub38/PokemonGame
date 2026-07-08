using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerNodeTrailTracker : MonoBehaviour {
    [Header("References")]
    [Tooltip("Player whose node trail is recorded. Empty uses this object, PlayerController.i, or the first player in the scene.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("Node movement adapter used when the player has node movement enabled. Empty searches this object or the player.")]
    [SerializeField] OverworldNodeMovementAdapter movementAdapter;
    [Tooltip("Node group used for nearest-node fallback when the movement adapter cannot provide a current node.")]
    [SerializeField] OverworldEncounterNodeGroup nodeGroup;

    [Header("Tracking")]
    [Tooltip("If enabled, the tracker refreshes automatically while active.")]
    [SerializeField] bool trackAutomatically = true;
    [Tooltip("Seconds between nearest-node refresh checks.")]
    [Min(0.05f)]
    [SerializeField] float refreshIntervalSeconds = 0.2f;
    [Tooltip("Maximum distance for nearest-node fallback. 0 means unlimited.")]
    [Min(0f)]
    [SerializeField] float nearestNodeMaxDistance = 0f;
    [Tooltip("Maximum number of player nodes kept for follower trail targeting.")]
    [Min(1)]
    [SerializeField] int maxTrailNodes = 12;
    [Tooltip("If enabled, repeated refreshes on the same node are ignored.")]
    [SerializeField] bool ignoreDuplicateNodes = true;

    [Header("Debug")]
    [Tooltip("If enabled, node changes are written to GameDebug.")]
    [SerializeField] bool logNodeChanges;

    readonly List<OverworldEncounterNode> nodeTrail = new List<OverworldEncounterNode>();
    PlayerController player;
    float nextRefreshTime;

    public PlayerController Player => ResolvePlayer();
    public OverworldEncounterNode CurrentNode => nodeTrail.Count > 0 ? nodeTrail[0] : null;
    public IReadOnlyList<OverworldEncounterNode> NodeTrail => nodeTrail;
    public OverworldEncounterNodeGroup NodeGroup => nodeGroup != null ? nodeGroup : movementAdapter != null ? movementAdapter.NodeGroup : null;
    public float RefreshIntervalSeconds => Mathf.Max(0.05f, refreshIntervalSeconds);
    public int MaxTrailNodes => Mathf.Max(1, maxTrailNodes);
    public event Action<OverworldEncounterNode> OnCurrentNodeChanged;

    void Awake() {
        ResolveReferences();
    }

    void OnEnable() {
        ResolveReferences();
        RefreshCurrentNode(forceRecord: true);
    }

    void Update() {
        if(!trackAutomatically || Time.time < nextRefreshTime) {
            return;
        }

        nextRefreshTime = Time.time + RefreshIntervalSeconds;
        RefreshCurrentNode(forceRecord: false);
    }

    [ContextMenu("Refresh Player Node Trail")]
    public void RefreshFromContextMenu() {
        RefreshCurrentNode(forceRecord: true);
    }

    public void Configure(OverworldEncounterNodeGroup group, OverworldNodeMovementAdapter adapter = null, PlayerController playerContext = null) {
        if(group != null) {
            nodeGroup = group;
        }

        if(adapter != null) {
            movementAdapter = adapter;
        }

        if(playerContext != null) {
            playerOverride = playerContext;
            player = playerContext;
        }

        RefreshCurrentNode(forceRecord: true);
    }

    public OverworldEncounterNode RefreshCurrentNode(bool forceRecord) {
        ResolveReferences();
        var node = ResolveNearestNode();
        if(node == null) {
            return null;
        }

        RecordNode(node, forceRecord);
        return node;
    }

    public OverworldEncounterNode GetFollowTargetNode(CompanionNodeFollowProfile profile, OverworldEncounterPathAgent followerAgent, PlayerController playerContext) {
        RefreshCurrentNode(forceRecord: false);
        if(nodeTrail.Count == 0) {
            return null;
        }

        int offset = profile != null ? profile.TrailOffset : 1;
        int firstIndex = Mathf.Clamp(offset, 0, nodeTrail.Count - 1);
        if(profile != null && !profile.AllowCurrentPlayerNode && firstIndex == 0 && nodeTrail.Count > 1) {
            firstIndex = 1;
        }

        for(int i = firstIndex; i < nodeTrail.Count; i++) {
            var candidate = nodeTrail[i];
            if(IsCandidateAllowed(candidate, profile, followerAgent, playerContext)) {
                return candidate;
            }
        }

        if(profile != null && profile.AllowCurrentPlayerNode && IsCandidateAllowed(nodeTrail[0], profile, followerAgent, playerContext)) {
            return nodeTrail[0];
        }

        if(profile == null || !profile.AllowNearestFallback) {
            return null;
        }

        var anchor = nodeTrail[Mathf.Clamp(firstIndex, 0, nodeTrail.Count - 1)];
        return FindNearestAllowedNode(anchor != null ? anchor.transform.position : transform.position, profile, followerAgent, playerContext);
    }

    public OverworldEncounterNode FindNearestAllowedNode(Vector3 position, CompanionNodeFollowProfile profile, OverworldEncounterPathAgent followerAgent, PlayerController playerContext) {
        var group = NodeGroup;
        if(group == null) {
            return null;
        }

        group.RefreshNodes();
        float maxSqrDistance = nearestNodeMaxDistance > 0f ? nearestNodeMaxDistance * nearestNodeMaxDistance : float.MaxValue;
        return group.Nodes
            .Where(node => IsCandidateAllowed(node, profile, followerAgent, playerContext))
            .Select(node => new NodeDistance(node, (node.transform.position - position).sqrMagnitude))
            .Where(entry => entry.sqrDistance <= maxSqrDistance)
            .OrderBy(entry => entry.sqrDistance)
            .Select(entry => entry.node)
            .FirstOrDefault();
    }

    void RecordNode(OverworldEncounterNode node, bool forceRecord) {
        if(node == null) {
            return;
        }

        if(ignoreDuplicateNodes && !forceRecord && nodeTrail.Count > 0 && nodeTrail[0] == node) {
            return;
        }

        nodeTrail.Remove(node);
        nodeTrail.Insert(0, node);
        while(nodeTrail.Count > MaxTrailNodes) {
            nodeTrail.RemoveAt(nodeTrail.Count - 1);
        }

        if(logNodeChanges) {
            GameDebug.Step($"Player node trail current node: {node.DisplayName}.", GameDebugCategory.Encounter, this, "PlayerNodeTrailTracker");
        }

        OnCurrentNodeChanged?.Invoke(node);
    }

    OverworldEncounterNode ResolveNearestNode() {
        if(movementAdapter != null) {
            var adapterNode = movementAdapter.ResolveCurrentNode(snapToNode: false);
            if(adapterNode != null) {
                return adapterNode;
            }
        }

        var group = NodeGroup;
        if(group == null) {
            return null;
        }

        var targetPlayer = ResolvePlayer();
        return group.FindNearestNode(targetPlayer != null ? targetPlayer.transform.position : transform.position, nearestNodeMaxDistance, targetPlayer, null);
    }

    bool IsCandidateAllowed(OverworldEncounterNode node, CompanionNodeFollowProfile profile, OverworldEncounterPathAgent followerAgent, PlayerController playerContext) {
        if(node == null) {
            return false;
        }

        if(profile != null && !profile.IsTargetAllowed(node)) {
            return false;
        }

        if(followerAgent != null && !followerAgent.IsNodeAllowed(node, playerContext, out _)) {
            return false;
        }

        return node.CanUse(playerContext, out _);
    }

    void ResolveReferences() {
        player = ResolvePlayer();
        if(movementAdapter == null) {
            movementAdapter = GetComponent<OverworldNodeMovementAdapter>();
        }

        if(movementAdapter == null && player != null) {
            movementAdapter = player.GetComponent<OverworldNodeMovementAdapter>();
        }
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(player != null) {
            return player;
        }

        player = GetComponent<PlayerController>();
        player = player != null ? player : PlayerController.i;
        player = player != null ? player : FindAnyObjectByType<PlayerController>();
        return player;
    }

    class NodeDistance {
        public readonly OverworldEncounterNode node;
        public readonly float sqrDistance;

        public NodeDistance(OverworldEncounterNode node, float sqrDistance) {
            this.node = node;
            this.sqrDistance = sqrDistance;
        }
    }
}
