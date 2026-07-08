using UnityEngine;

public enum NPCMemoryRequirementMode {
    HasMet,
    InteractionCount,
    InteractionTypeCount,
    HasTopic,
    TopicCount,
    TopicTagCount,
    TrustAtLeast,
    SuspicionAtLeast,
    FamiliarityAtLeast,
    HoursSinceLastInteractionAtMost
}

[CreateAssetMenu(menuName = "Activities/Requirements/NPC Memory Requirement")]
public class NPCMemoryRequirement : ActivityRequirement {
    [Tooltip("Which NPC memory value this requirement checks.")]
    [SerializeField] NPCMemoryRequirementMode mode = NPCMemoryRequirementMode.HasMet;
    [Tooltip("NPC id checked by this requirement. Empty cannot match because requirements do not have speaker context.")]
    [SerializeField] string npcId;
    [Tooltip("Topic checked by topic-specific modes.")]
    [SerializeField] NPCMemoryTopicDefinition topic;
    [Tooltip("Tag checked by Topic Tag Count mode.")]
    [SerializeField] string topicTag;
    [Tooltip("Interaction type checked by Interaction Type Count mode.")]
    [SerializeField] NPCInteractionMemoryType interactionType = NPCInteractionMemoryType.Conversation;
    [Tooltip("Minimum value required by count/stat modes.")]
    [Min(0)]
    [SerializeField] int requiredValue = 1;
    [Tooltip("If enabled, the selected NPC memory condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerNPCMemoryLog>() : null;
        bool result = mode switch {
            NPCMemoryRequirementMode.InteractionCount => log != null && log.GetInteractionCount(npcId) >= Mathf.Max(0, requiredValue),
            NPCMemoryRequirementMode.InteractionTypeCount => log != null && log.GetInteractionCountByType(npcId, interactionType) >= Mathf.Max(0, requiredValue),
            NPCMemoryRequirementMode.HasTopic => log != null && log.HasTopic(npcId, topic),
            NPCMemoryRequirementMode.TopicCount => log != null && log.GetTopicCount(npcId, topic) >= Mathf.Max(0, requiredValue),
            NPCMemoryRequirementMode.TopicTagCount => log != null && log.GetTopicCountWithTag(npcId, topicTag) >= Mathf.Max(0, requiredValue),
            NPCMemoryRequirementMode.TrustAtLeast => log != null && log.GetTrust(npcId) >= requiredValue,
            NPCMemoryRequirementMode.SuspicionAtLeast => log != null && log.GetSuspicion(npcId) >= requiredValue,
            NPCMemoryRequirementMode.FamiliarityAtLeast => log != null && log.GetFamiliarity(npcId) >= requiredValue,
            NPCMemoryRequirementMode.HoursSinceLastInteractionAtMost => log != null && log.GetHoursSinceLastInteraction(npcId) >= 0 && log.GetHoursSinceLastInteraction(npcId) <= Mathf.Max(0, requiredValue),
            _ => log != null && log.HasMet(npcId)
        };

        return mustBeMet ? result : !result;
    }
}
