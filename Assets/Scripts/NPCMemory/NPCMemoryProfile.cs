using UnityEngine;

public class NPCMemoryProfile : MonoBehaviour {
    [Header("Identity")]
    [Tooltip("Stable NPC memory id. Empty uses GameObject name.")]
    [SerializeField] string npcId;
    [Tooltip("Name stored in PlayerNPCMemoryLog. Empty uses NPCController display name or GameObject name.")]
    [SerializeField] string displayName;
    [Tooltip("Optional relationship subject used when NPC reactions change this NPC's relationship value.")]
    [SerializeField] RelationshipSubjectDefinition relationshipSubject;
    [Tooltip("Optional faction represented by this NPC, used when NPC reactions change local/faction reputation.")]
    [SerializeField] ReputationFactionDefinition reputationFaction;

    [Header("Default Interaction")]
    [Tooltip("If enabled, NPCController records a conversation interaction when this NPC is spoken to.")]
    [SerializeField] bool recordConversationOnInteract = true;
    [Tooltip("Optional topic remembered whenever this NPC is spoken to.")]
    [SerializeField] NPCMemoryTopicDefinition defaultConversationTopic;
    [Tooltip("Trust added on normal conversation.")]
    [SerializeField] int conversationTrustDelta;
    [Tooltip("Suspicion added on normal conversation.")]
    [SerializeField] int conversationSuspicionDelta;
    [Tooltip("Familiarity added on normal conversation.")]
    [SerializeField] int conversationFamiliarityDelta = 1;
    [Tooltip("If enabled, PlayerNPCMemoryLog is added to the player if missing.")]
    [SerializeField] bool installLogIfMissing = true;

    [Header("Debug")]
    [Tooltip("If enabled, memory recording is written to GameDebug.")]
    [SerializeField] bool logMemoryChanges;

    public string NpcId => string.IsNullOrWhiteSpace(npcId) ? name : npcId;
    public string DisplayName {
        get {
            if(!string.IsNullOrWhiteSpace(displayName)) {
                return displayName;
            }

            var npc = GetComponent<NPCController>();
            return npc != null ? npc.DisplayName : name;
        }
    }

    public RelationshipSubjectDefinition RelationshipSubject => relationshipSubject;
    public ReputationFactionDefinition ReputationFaction => reputationFaction;
    public bool RecordConversationOnInteract => recordConversationOnInteract;

    public NPCMemoryState RecordConversation(PlayerController player, string sourceId = null) {
        return RecordInteraction(
            player,
            NPCInteractionMemoryType.Conversation,
            defaultConversationTopic,
            conversationTrustDelta,
            conversationSuspicionDelta,
            conversationFamiliarityDelta,
            sourceId);
    }

    public NPCMemoryState RecordInteraction(
        PlayerController player,
        NPCInteractionMemoryType interactionType,
        NPCMemoryTopicDefinition topic = null,
        int trustDelta = 0,
        int suspicionDelta = 0,
        int familiarityDelta = 1,
        string sourceId = null
    ) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerNPCMemoryLog>();
        if(log == null && installLogIfMissing) {
            log = player.gameObject.AddComponent<PlayerNPCMemoryLog>();
        }

        var state = log?.RecordInteraction(NpcId, DisplayName, interactionType, topic, trustDelta, suspicionDelta, familiarityDelta, sourceId, this);
        if(state != null && logMemoryChanges) {
            GameDebug.Step($"{DisplayName} memory updated: {interactionType}.", GameDebugCategory.NPC, this, "NPCMemoryProfile");
        }

        return state;
    }

    public bool RememberTopic(PlayerController player, NPCMemoryTopicDefinition topic, string sourceId = null) {
        if(player == null || topic == null) {
            return false;
        }

        var log = player.GetComponent<PlayerNPCMemoryLog>();
        if(log == null && installLogIfMissing) {
            log = player.gameObject.AddComponent<PlayerNPCMemoryLog>();
        }

        bool remembered = log != null && log.RememberTopic(NpcId, DisplayName, topic, sourceId, this);
        if(remembered && logMemoryChanges) {
            GameDebug.Step($"{DisplayName} remembered {topic.DisplayName}.", GameDebugCategory.NPC, this, "NPCMemoryProfile");
        }
        return remembered;
    }
}
