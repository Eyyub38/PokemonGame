using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum NPCMemoryTopicCategory {
    General,
    Greeting,
    Favor,
    Gift,
    Quest,
    Assignment,
    Investigation,
    Law,
    Rumor,
    Battle,
    Trade,
    Helped,
    Offended,
    Custom
}

[CreateAssetMenu(menuName = "NPC Memory/Memory Topic Definition")]
public class NPCMemoryTopicDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this memory topic. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in debug and future UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing explanation of what this memory means.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad topic category used by filters, dialog and future UI.")]
    [SerializeField] NPCMemoryTopicCategory category = NPCMemoryTopicCategory.General;
    [Tooltip("Free-form tags used by requirements, dialog and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Priority used by future UI sorting. Higher priority appears first.")]
    [SerializeField] int priority;

    [Header("Memory Effects")]
    [Tooltip("Trust added when this topic is remembered.")]
    [SerializeField] int trustDelta;
    [Tooltip("Suspicion added when this topic is remembered.")]
    [SerializeField] int suspicionDelta;
    [Tooltip("Familiarity added when this topic is remembered.")]
    [SerializeField] int familiarityDelta = 1;
    [Tooltip("If enabled, remembering this topic also increments interaction count.")]
    [SerializeField] bool countAsInteraction;

    [Header("Events")]
    [Tooltip("Optional event published when this memory topic is remembered.")]
    [SerializeField] GameEventDefinition rememberedEvent;
    [Tooltip("If enabled, memory events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed;
    [Tooltip("If enabled, memory events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public NPCMemoryTopicCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public int Priority => priority;
    public int TrustDelta => trustDelta;
    public int SuspicionDelta => suspicionDelta;
    public int FamiliarityDelta => familiarityDelta;
    public bool CountAsInteraction => countAsInteraction;

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishRemembered(PlayerController player, string npcId, string npcName, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            rememberedEvent,
            $"npc-memory.remembered.{Id}",
            $"{npcName} remembered {DisplayName}.",
            GameEventCategory.NPC,
            GameEventImportance.Info,
            context != null ? context : player,
            "NPCMemoryTopicDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("topicId", Id),
            GameEventPublishing.Value("topicName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("npcId", npcId),
            GameEventPublishing.Value("npcName", npcName));
    }
}
