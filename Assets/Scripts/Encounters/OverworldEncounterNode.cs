using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum OverworldEncounterNodeKind {
    General,
    Grass,
    Water,
    Cave,
    Tree,
    Nest,
    Lair,
    Patrol,
    Hideout,
    Rare,
    Custom
}

[Flags]
public enum OverworldEncounterNodeFlags {
    None = 0,
    Safe = 1 << 0,
    TallGrass = 1 << 1,
    Water = 1 << 2,
    Climb = 1 << 3,
    Ledge = 1 << 4,
    Tree = 1 << 5,
    Cave = 1 << 6,
    Nest = 1 << 7,
    HideSpot = 1 << 8,
    RareSpawn = 1 << 9,
    Danger = 1 << 10,
    SpawnExit = 1 << 11,
    MapExit = 1 << 12,
    NoPlayer = 1 << 13,
    NoNPC = 1 << 14,
    NoPokemon = 1 << 15
}

[Flags]
public enum OverworldMovementCapabilityFlags {
    None = 0,
    Walk = 1 << 0,
    Run = 1 << 1,
    Swim = 1 << 2,
    Surf = 1 << 3,
    Fly = 1 << 4,
    Climb = 1 << 5,
    Jump = 1 << 6,
    Cut = 1 << 7,
    SmallBody = 1 << 8,
    LargeBody = 1 << 9,
    Stealth = 1 << 10,
    Ride = 1 << 11,
    Ghost = 1 << 12
}

[Flags]
public enum OverworldEncounterConnectionFlags {
    None = 0,
    RequiresWalk = 1 << 0,
    RequiresSurf = 1 << 1,
    RequiresSwim = 1 << 2,
    RequiresFly = 1 << 3,
    RequiresClimb = 1 << 4,
    RequiresJump = 1 << 5,
    RequiresCut = 1 << 6,
    RequiresRide = 1 << 7,
    OneWay = 1 << 8,
    StealthRoute = 1 << 9,
    Dangerous = 1 << 10,
    BlockedByDefault = 1 << 11,
    MapTransition = 1 << 12
}

public class OverworldEncounterNode : MonoBehaviour {
    [Header("Identity")]
    [Tooltip("Stable id used by debug output and future save/event systems. Empty uses the GameObject name.")]
    [SerializeField] string nodeId = string.Empty;
    [Tooltip("Name shown by debug or future editor/UI tools. Empty uses the GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Broad node kind used by agents, validators and future encounter logic.")]
    [SerializeField] OverworldEncounterNodeKind kind = OverworldEncounterNodeKind.General;
    [Tooltip("Terrain, route and access flags used by movement, stealth, encounter and future map systems.")]
    [SerializeField] OverworldEncounterNodeFlags nodeFlags = OverworldEncounterNodeFlags.None;
    [Tooltip("Free-form tags such as forest, water-edge, rare, night, nest or patrol-route.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Movement")]
    [Tooltip("Movement capabilities required to stand on or use this node. Leave empty for normal walkable nodes.")]
    [SerializeField] OverworldMovementCapabilityFlags requiredCapabilities = OverworldMovementCapabilityFlags.None;
    [Tooltip("Radius around this node where an agent may stop. 0 means exactly this transform position.")]
    [Min(0f)]
    [SerializeField] float stopRadius = 0.1f;
    [Tooltip("Minimum seconds an agent waits after reaching this node.")]
    [Min(0f)]
    [SerializeField] float minWaitSeconds = 0.5f;
    [Tooltip("Maximum seconds an agent waits after reaching this node.")]
    [Min(0f)]
    [SerializeField] float maxWaitSeconds = 2f;

    [Header("Connections")]
    [Tooltip("Weighted outgoing node links. Agents can choose among these instead of walking randomly.")]
    [SerializeField] List<OverworldEncounterNodeConnection> connections = new List<OverworldEncounterNodeConnection>();

    [Header("Access")]
    [Tooltip("How extra requirements are evaluated before an agent can use this node.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before an agent can use this node.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message used when this node is blocked.")]
    [TextArea]
    [SerializeField] string blockedMessage = "This encounter node is not available right now.";

    [Header("Debug")]
    [Tooltip("Gizmo color used for this node in the Scene view.")]
    [SerializeField] Color gizmoColor = new Color(0.25f, 0.8f, 0.45f, 0.8f);
    [Tooltip("If enabled, connection lines are drawn from this node in the Scene view.")]
    [SerializeField] bool drawConnections = true;

    public string NodeId => string.IsNullOrWhiteSpace(nodeId) ? name : nodeId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public OverworldEncounterNodeKind Kind => kind;
    public OverworldEncounterNodeFlags NodeFlags => nodeFlags;
    public OverworldMovementCapabilityFlags RequiredCapabilities => requiredCapabilities;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public float StopRadius => Mathf.Max(0f, stopRadius);
    public float MinWaitSeconds => Mathf.Max(0f, minWaitSeconds);
    public float MaxWaitSeconds => Mathf.Max(MinWaitSeconds, maxWaitSeconds);
    public IReadOnlyList<OverworldEncounterNodeConnection> Connections => connections != null ? (IReadOnlyList<OverworldEncounterNodeConnection>)connections : Array.Empty<OverworldEncounterNodeConnection>();
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();

    public Vector3 GetStopPosition() {
        if(StopRadius <= 0f) {
            return transform.position;
        }

        Vector2 offset = UnityEngine.Random.insideUnitCircle * StopRadius;
        return transform.position + new Vector3(offset.x, offset.y, 0f);
    }

    public float RollWaitSeconds() {
        if(MaxWaitSeconds <= 0f) {
            return 0f;
        }

        return UnityEngine.Random.Range(MinWaitSeconds, MaxWaitSeconds);
    }

    public bool CanUse(PlayerController player, out string failureMessage) {
        var activeRequirements = requirements?.Where(requirement => requirement != null).ToList() ?? new List<ActivityRequirement>();
        if(activeRequirements.Count == 0) {
            failureMessage = null;
            return true;
        }

        if(requirementMatchMode == ConsequenceRequirementMatchMode.Any) {
            foreach(var requirement in activeRequirements) {
                if(requirement.IsMet(player)) {
                    failureMessage = null;
                    return true;
                }
            }

            failureMessage = activeRequirements.FirstOrDefault()?.FailureMessage ?? blockedMessage;
            return false;
        }

        foreach(var requirement in activeRequirements) {
            if(!requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<OverworldEncounterNodeConnection> GetAvailableConnections(OverworldEncounterPathAgent agent, PlayerController player) {
        foreach(var connection in connections ?? Enumerable.Empty<OverworldEncounterNodeConnection>()) {
            if(connection == null || !connection.IsUsable(agent, player, out _)) {
                continue;
            }

            yield return connection;
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.05f, StopRadius));

        if(!drawConnections || connections == null) {
            return;
        }

        foreach(var connection in connections) {
            if(connection?.Node == null) {
                continue;
            }

            Gizmos.DrawLine(transform.position, connection.Node.transform.position);
        }
    }
}

[Serializable]
public class OverworldEncounterNodeConnection {
    [Tooltip("Destination node that can be reached from the current node.")]
    [SerializeField] OverworldEncounterNode node;
    [Tooltip("Weighted chance for random-connected agents to choose this link.")]
    [Min(0)]
    [SerializeField] int weight = 1;
    [Tooltip("If enabled, agents may use this connection.")]
    [SerializeField] bool enabled = true;
    [Tooltip("Route flags such as one-way, climb, surf, fly, dangerous, stealth or map transition.")]
    [SerializeField] OverworldEncounterConnectionFlags flags = OverworldEncounterConnectionFlags.RequiresWalk;
    [Tooltip("Extra movement capabilities required to traverse this connection. Route flags also contribute requirements.")]
    [SerializeField] OverworldMovementCapabilityFlags requiredCapabilities = OverworldMovementCapabilityFlags.None;
    [Tooltip("Movement capabilities that cannot use this connection, such as LargeBody for tight crawl paths.")]
    [SerializeField] OverworldMovementCapabilityFlags blockedCapabilities = OverworldMovementCapabilityFlags.None;
    [Tooltip("Additional pathfinding cost for this connection. Higher values make routes less preferred.")]
    [Min(0.01f)]
    [SerializeField] float traversalCost = 1f;
    [Tooltip("Required agent tag. Empty allows all agents.")]
    [SerializeField] string requiredAgentTag = string.Empty;
    [Tooltip("Connection-specific block message.")]
    [TextArea]
    [SerializeField] string blockedMessage = "This route is not available right now.";

    public OverworldEncounterNode Node => node;
    public int Weight => Mathf.Max(0, weight);
    public bool Enabled => enabled;
    public OverworldEncounterConnectionFlags Flags => flags;
    public OverworldMovementCapabilityFlags RequiredCapabilities => requiredCapabilities | GetRequiredCapabilitiesFromFlags();
    public OverworldMovementCapabilityFlags BlockedCapabilities => blockedCapabilities;
    public float TraversalCost => Mathf.Max(0.01f, traversalCost);
    public string RequiredAgentTag => requiredAgentTag;

    public bool IsUsable(OverworldEncounterPathAgent agent, PlayerController player, out string failureMessage) {
        if(!enabled) {
            failureMessage = blockedMessage;
            return false;
        }

        if((flags & OverworldEncounterConnectionFlags.BlockedByDefault) != 0) {
            failureMessage = blockedMessage;
            return false;
        }

        if(node == null) {
            failureMessage = "Connection has no destination node.";
            return false;
        }

        if(Weight <= 0) {
            failureMessage = "Connection has no weight.";
            return false;
        }

        var neededCapabilities = RequiredCapabilities;
        if(neededCapabilities != OverworldMovementCapabilityFlags.None && (agent == null || !agent.HasAllCapabilities(neededCapabilities))) {
            failureMessage = blockedMessage;
            return false;
        }

        if(blockedCapabilities != OverworldMovementCapabilityFlags.None && agent != null && agent.HasAnyCapability(blockedCapabilities)) {
            failureMessage = blockedMessage;
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredAgentTag) && (agent == null || !agent.HasAgentTag(requiredAgentTag))) {
            failureMessage = blockedMessage;
            return false;
        }

        if(agent != null) {
            if(!agent.IsNodeAllowed(node, player, out failureMessage)) {
                return false;
            }
        } else if(!node.CanUse(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    OverworldMovementCapabilityFlags GetRequiredCapabilitiesFromFlags() {
        var capabilities = OverworldMovementCapabilityFlags.None;
        if((flags & OverworldEncounterConnectionFlags.RequiresWalk) != 0) capabilities |= OverworldMovementCapabilityFlags.Walk;
        if((flags & OverworldEncounterConnectionFlags.RequiresSurf) != 0) capabilities |= OverworldMovementCapabilityFlags.Surf;
        if((flags & OverworldEncounterConnectionFlags.RequiresSwim) != 0) capabilities |= OverworldMovementCapabilityFlags.Swim;
        if((flags & OverworldEncounterConnectionFlags.RequiresFly) != 0) capabilities |= OverworldMovementCapabilityFlags.Fly;
        if((flags & OverworldEncounterConnectionFlags.RequiresClimb) != 0) capabilities |= OverworldMovementCapabilityFlags.Climb;
        if((flags & OverworldEncounterConnectionFlags.RequiresJump) != 0) capabilities |= OverworldMovementCapabilityFlags.Jump;
        if((flags & OverworldEncounterConnectionFlags.RequiresCut) != 0) capabilities |= OverworldMovementCapabilityFlags.Cut;
        if((flags & OverworldEncounterConnectionFlags.RequiresRide) != 0) capabilities |= OverworldMovementCapabilityFlags.Ride;
        return capabilities;
    }
}
