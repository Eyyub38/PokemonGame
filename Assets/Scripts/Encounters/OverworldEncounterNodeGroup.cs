using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OverworldEncounterNodeGroup : MonoBehaviour {
    [Header("Identity")]
    [Tooltip("Stable id used by debug output and future save/event systems. Empty uses the GameObject name.")]
    [SerializeField] string groupId = string.Empty;
    [Tooltip("Name shown by debug or future editor/UI tools. Empty uses the GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Free-form group tags such as route-1, forest, cave, town-edge, lake or rare-path.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Nodes")]
    [Tooltip("Nodes explicitly owned by this group.")]
    [SerializeField] List<OverworldEncounterNode> nodes = new List<OverworldEncounterNode>();
    [Tooltip("If enabled, Refresh Nodes includes child OverworldEncounterNode components.")]
    [SerializeField] bool includeChildNodes = true;
    [Tooltip("If enabled, nodes are refreshed during Awake.")]
    [SerializeField] bool refreshOnAwake = true;

    public string GroupId => string.IsNullOrWhiteSpace(groupId) ? name : groupId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public IReadOnlyList<OverworldEncounterNode> Nodes => nodes != null ? (IReadOnlyList<OverworldEncounterNode>)nodes : Array.Empty<OverworldEncounterNode>();
    public bool IncludeChildNodes => includeChildNodes;

    void Awake() {
        if(refreshOnAwake) {
            RefreshNodes();
        }
    }

    [ContextMenu("Refresh Encounter Nodes")]
    public void RefreshNodes() {
        if(nodes == null) {
            nodes = new List<OverworldEncounterNode>();
        }

        if(includeChildNodes) {
            foreach(var node in GetComponentsInChildren<OverworldEncounterNode>(includeInactive: true)) {
                if(node != null && !nodes.Contains(node)) {
                    nodes.Add(node);
                }
            }
        }

        nodes = nodes.Where(node => node != null).Distinct().ToList();
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public OverworldEncounterNode FindNearestNode(Vector3 position, float maxDistance = 0f, PlayerController player = null, OverworldEncounterPathAgent agent = null) {
        RefreshNodes();
        float maxSqrDistance = maxDistance > 0f ? maxDistance * maxDistance : float.MaxValue;
        return nodes
            .Where(node => node != null && (agent == null || agent.IsNodeAllowed(node, player, out _)))
            .Select(node => new NodeDistance(node, (node.transform.position - position).sqrMagnitude))
            .Where(entry => entry.sqrDistance <= maxSqrDistance)
            .OrderBy(entry => entry.sqrDistance)
            .Select(entry => entry.node)
            .FirstOrDefault();
    }

    public OverworldEncounterNode GetRandomNode(PlayerController player = null, OverworldEncounterPathAgent agent = null) {
        RefreshNodes();
        var candidates = nodes
            .Where(node => node != null && (agent == null || agent.IsNodeAllowed(node, player, out _)))
            .ToList();

        if(candidates.Count == 0) {
            return null;
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
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
