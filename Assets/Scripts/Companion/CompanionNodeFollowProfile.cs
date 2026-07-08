using UnityEngine;

[CreateAssetMenu(menuName = "Companion/Node Follow Profile")]
public class CompanionNodeFollowProfile : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id used by validators, debug output and future save data. Empty uses the asset name.")]
    [SerializeField] string profileId = string.Empty;
    [Tooltip("Name shown in debug output or future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer notes explaining which companion, follower Pokemon or travel style this profile is meant for.")]
    [TextArea]
    [SerializeField] string description = string.Empty;

    [Header("Targeting")]
    [Tooltip("How many recorded player nodes behind the current node this follower prefers. 1 means the previous player node.")]
    [Min(0)]
    [SerializeField] int trailOffset = 1;
    [Tooltip("If enabled, the current player node can be selected when no older valid trail node exists.")]
    [SerializeField] bool allowCurrentPlayerNode = true;
    [Tooltip("If enabled, the follower may use the closest valid node to the desired trail node when the exact node is blocked.")]
    [SerializeField] bool allowNearestFallback = true;

    [Header("Pathing")]
    [Tooltip("Movement capabilities expected for this follow style. These should match the linked OverworldEncounterPathAgent.")]
    [SerializeField] OverworldMovementCapabilityFlags expectedCapabilities = OverworldMovementCapabilityFlags.Walk;
    [Tooltip("If not empty, follow target nodes must include these flags.")]
    [SerializeField] OverworldEncounterNodeFlags requiredTargetFlags = OverworldEncounterNodeFlags.None;
    [Tooltip("If not empty, follow target nodes with any of these flags are ignored.")]
    [SerializeField] OverworldEncounterNodeFlags blockedTargetFlags = OverworldEncounterNodeFlags.None;
    [Tooltip("If enabled, pathfinding may return the closest reachable partial route when the exact target cannot be reached.")]
    [SerializeField] bool allowPartialPaths = true;
    [Tooltip("If enabled, pathfinding avoids nodes marked as Danger.")]
    [SerializeField] bool avoidDangerousNodes;
    [Tooltip("If enabled, pathfinding avoids connections marked as Dangerous.")]
    [SerializeField] bool avoidDangerousConnections;
    [Tooltip("Maximum nodes visited during one follow path search. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxVisitedNodes = 256;
    [Tooltip("Maximum total route cost for one follow path search. 0 means unlimited.")]
    [Min(0f)]
    [SerializeField] float maxTotalCost;

    [Header("Timing")]
    [Tooltip("Minimum seconds between automatic follow path recalculations.")]
    [Min(0.05f)]
    [SerializeField] float repathIntervalSeconds = 0.35f;
    [Tooltip("If the follower is within this distance of the target node, no new route is requested.")]
    [Min(0f)]
    [SerializeField] float targetTolerance = 0.15f;

    [Header("Catch Up")]
    [Tooltip("Distance from player where the catch-up policy is allowed to run. 0 disables catch-up.")]
    [Min(0f)]
    [SerializeField] float catchUpDistance = 9f;
    [Tooltip("What happens when the follower is too far from the player.")]
    [SerializeField] CompanionNodeCatchUpMode catchUpMode = CompanionNodeCatchUpMode.WarpToFollowTarget;

    public string ProfileId => string.IsNullOrWhiteSpace(profileId) ? name : profileId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public int TrailOffset => Mathf.Max(0, trailOffset);
    public bool AllowCurrentPlayerNode => allowCurrentPlayerNode;
    public bool AllowNearestFallback => allowNearestFallback;
    public OverworldMovementCapabilityFlags ExpectedCapabilities => expectedCapabilities;
    public OverworldEncounterNodeFlags RequiredTargetFlags => requiredTargetFlags;
    public OverworldEncounterNodeFlags BlockedTargetFlags => blockedTargetFlags;
    public bool AllowPartialPaths => allowPartialPaths;
    public bool AvoidDangerousNodes => avoidDangerousNodes;
    public bool AvoidDangerousConnections => avoidDangerousConnections;
    public int MaxVisitedNodes => Mathf.Max(0, maxVisitedNodes);
    public float MaxTotalCost => Mathf.Max(0f, maxTotalCost);
    public float RepathIntervalSeconds => Mathf.Max(0.05f, repathIntervalSeconds);
    public float TargetTolerance => Mathf.Max(0f, targetTolerance);
    public float CatchUpDistance => Mathf.Max(0f, catchUpDistance);
    public CompanionNodeCatchUpMode CatchUpMode => catchUpMode;

    public bool IsTargetAllowed(OverworldEncounterNode node) {
        if(node == null) {
            return false;
        }

        if(requiredTargetFlags != OverworldEncounterNodeFlags.None && (node.NodeFlags & requiredTargetFlags) != requiredTargetFlags) {
            return false;
        }

        return blockedTargetFlags == OverworldEncounterNodeFlags.None || (node.NodeFlags & blockedTargetFlags) == 0;
    }
}

public enum CompanionNodeCatchUpMode {
    None,
    WarpToFollowTarget,
    WarpNearPlayerNode,
    PauseUntilReachable
}
