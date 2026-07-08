using UnityEngine;

public enum NPCReactionRequirementMode {
    ReactionCount,
    ReactionTagCount,
    ReactionCategoryCount,
    HoursSinceLastReactionAtMost
}

[CreateAssetMenu(menuName = "Activities/Requirements/NPC Reaction Requirement")]
public class NPCReactionRequirement : ActivityRequirement {
    [Tooltip("Which reaction value this requirement checks.")]
    [SerializeField] NPCReactionRequirementMode mode = NPCReactionRequirementMode.ReactionCount;
    [Tooltip("Reaction definition checked by reaction-specific modes. Empty accepts any reaction.")]
    [SerializeField] NPCReactionDefinition reaction;
    [Tooltip("NPC id filter. Empty accepts any reacting NPC.")]
    [SerializeField] string npcId;
    [Tooltip("Source id filter. Empty accepts any source.")]
    [SerializeField] string sourceId;
    [Tooltip("Tag checked by Reaction Tag Count mode.")]
    [SerializeField] string reactionTag;
    [Tooltip("Category checked by Reaction Category Count mode.")]
    [SerializeField] NPCReactionCategory category = NPCReactionCategory.General;
    [Tooltip("Minimum value required by count modes, or max hours for Hours Since Last Reaction At Most.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("If enabled, the selected reaction condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerNPCReactionLog>() : null;
        bool result = mode switch {
            NPCReactionRequirementMode.ReactionTagCount => log != null && log.GetCountWithTag(reactionTag, npcId, sourceId) >= Mathf.Max(0, requiredValue),
            NPCReactionRequirementMode.ReactionCategoryCount => log != null && log.GetCountByCategory(category, npcId, sourceId) >= Mathf.Max(0, requiredValue),
            NPCReactionRequirementMode.HoursSinceLastReactionAtMost => log != null && log.GetHoursSinceLastReaction(reaction, npcId, sourceId) >= 0 && log.GetHoursSinceLastReaction(reaction, npcId, sourceId) <= Mathf.Max(0, requiredValue),
            _ => log != null && log.GetCount(reaction, npcId, sourceId) >= Mathf.Max(0, requiredValue)
        };

        return mustBeMet ? result : !result;
    }
}
