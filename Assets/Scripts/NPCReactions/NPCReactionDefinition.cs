using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum NPCReactionCategory {
    General,
    Helped,
    Offended,
    Gift,
    Trade,
    Battle,
    Quest,
    Assignment,
    Investigation,
    Law,
    WitnessedCrime,
    Research,
    PokemonCare,
    Shop,
    Rumor,
    Custom
}

public enum NPCReactionRepeatPolicy {
    Always,
    OnceGlobally,
    OncePerNPC,
    OncePerNPCAndSource,
    LimitedGlobally,
    LimitedPerNPC,
    LimitedPerNPCAndSource
}

[CreateAssetMenu(menuName = "NPC Reactions/NPC Reaction Definition")]
public class NPCReactionDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this reaction. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in debug and future UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note explaining when this reaction should be applied.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad reaction category used by requirements, dialog and future UI filters.")]
    [SerializeField] NPCReactionCategory category = NPCReactionCategory.General;
    [Tooltip("Free-form tags used by requirements, dialog and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Priority hint for future systems that choose between multiple possible reactions.")]
    [SerializeField] int priority;

    [Header("Requirements")]
    [Tooltip("Player requirements that must be met before this reaction can apply.")]
    [SerializeField] List<ActivityRequirement> playerRequirements = new List<ActivityRequirement>();

    [Header("Repeat Rules")]
    [Tooltip("How often this reaction can be applied.")]
    [SerializeField] NPCReactionRepeatPolicy repeatPolicy = NPCReactionRepeatPolicy.Always;
    [Tooltip("Maximum applications used by Limited repeat policies.")]
    [Min(1)]
    [SerializeField] int maxApplications = 1;
    [Tooltip("Minimum in-game hours before the same matching reaction can apply again. 0 disables cooldown.")]
    [Min(0)]
    [SerializeField] int cooldownHours;

    [Header("NPC Memory")]
    [Tooltip("If enabled, the reacting NPC writes this event into PlayerNPCMemoryLog.")]
    [SerializeField] bool recordNPCMemory = true;
    [Tooltip("Interaction type stored in PlayerNPCMemoryLog when memory is recorded.")]
    [SerializeField] NPCInteractionMemoryType memoryInteractionType = NPCInteractionMemoryType.Custom;
    [Tooltip("Optional memory topic remembered by the reacting NPC.")]
    [SerializeField] NPCMemoryTopicDefinition memoryTopic;
    [Tooltip("Trust added to the reacting NPC memory.")]
    [SerializeField] int trustDelta;
    [Tooltip("Suspicion added to the reacting NPC memory.")]
    [SerializeField] int suspicionDelta;
    [Tooltip("Familiarity added to the reacting NPC memory.")]
    [SerializeField] int familiarityDelta;

    [Header("Relationship")]
    [Tooltip("Relationship delta applied to the reacting NPC's Relationship Subject from NPCMemoryProfile.")]
    [SerializeField] int reactorRelationshipDelta;
    [Tooltip("Extra relationship changes applied regardless of the reacting NPC.")]
    [SerializeField] List<RelationshipChange> relationshipChanges = new List<RelationshipChange>();

    [Header("Reputation")]
    [Tooltip("Reputation delta applied to the reacting NPC's Reputation Faction from NPCMemoryProfile.")]
    [SerializeField] int reactorFactionReputationDelta;
    [Tooltip("Extra reputation changes applied regardless of the reacting NPC.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();

    [Header("Progression")]
    [Tooltip("Milestones completed when this reaction applies.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, permits, badges or ranks granted when this reaction applies.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();

    [Header("Law")]
    [Tooltip("If enabled, this reaction records a law violation through PlayerLawLog.")]
    [SerializeField] bool recordLawViolation;
    [Tooltip("Law violation recorded when Record Law Violation is enabled.")]
    [SerializeField] LawViolationDefinition lawViolation;
    [Tooltip("If enabled, the law violation also applies its configured consequences.")]
    [SerializeField] bool applyLawConsequences = true;

    [Header("Events")]
    [Tooltip("Optional event published when this reaction applies.")]
    [SerializeField] GameEventDefinition appliedEvent;
    [Tooltip("If enabled, reaction events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, reaction events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public NPCReactionCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public int Priority => priority;
    public IReadOnlyList<ActivityRequirement> PlayerRequirements => playerRequirements;
    public NPCReactionRepeatPolicy RepeatPolicy => repeatPolicy;
    public int MaxApplications => Mathf.Max(1, maxApplications);
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public bool RecordNPCMemory => recordNPCMemory;
    public NPCMemoryTopicDefinition MemoryTopic => memoryTopic;
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges;
    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges;
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete;
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants;
    public bool RecordLawViolation => recordLawViolation;
    public LawViolationDefinition LawViolation => lawViolation;

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanApply(PlayerController player, NPCMemoryProfile npc, PlayerNPCReactionLog log, string sourceId, out string failureMessage) {
        failureMessage = null;
        if(player == null) {
            failureMessage = "Player is missing.";
            return false;
        }

        if(playerRequirements != null) {
            foreach(var requirement in playerRequirements) {
                if(requirement == null) {
                    continue;
                }

                if(!requirement.IsMet(player)) {
                    failureMessage = requirement.FailureMessage;
                    return false;
                }
            }
        }

        if(log == null) {
            return true;
        }

        string npcId = npc != null ? npc.NpcId : null;
        if(CooldownHours > 0) {
            int hours = log.GetHoursSinceLastReaction(this, GetCooldownNPCFilter(npcId), GetCooldownSourceFilter(sourceId));
            if(hours >= 0 && hours < CooldownHours) {
                failureMessage = $"Reaction is on cooldown for {CooldownHours - hours} more hour(s).";
                return false;
            }
        }

        int count = GetRepeatCount(log, npcId, sourceId);
        switch(repeatPolicy) {
            case NPCReactionRepeatPolicy.OnceGlobally:
                return count <= 0;
            case NPCReactionRepeatPolicy.OncePerNPC:
                return count <= 0;
            case NPCReactionRepeatPolicy.OncePerNPCAndSource:
                return count <= 0;
            case NPCReactionRepeatPolicy.LimitedGlobally:
                return count < MaxApplications;
            case NPCReactionRepeatPolicy.LimitedPerNPC:
                return count < MaxApplications;
            case NPCReactionRepeatPolicy.LimitedPerNPCAndSource:
                return count < MaxApplications;
            default:
                return true;
        }
    }

    public bool Apply(PlayerController player, NPCMemoryProfile npc, string sourceId, UnityEngine.Object context, out string failureMessage) {
        var log = player != null ? player.GetComponent<PlayerNPCReactionLog>() : null;
        if(log == null && player != null) {
            log = player.gameObject.AddComponent<PlayerNPCReactionLog>();
        }

        if(!CanApply(player, npc, log, sourceId, out failureMessage)) {
            return false;
        }

        ApplyNPCMemory(player, npc, sourceId);
        ApplyRelationshipChanges(player, npc);
        ApplyReputationChanges(player, npc);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, this);

        if(recordLawViolation && lawViolation != null) {
            player.GetComponent<PlayerLawLog>()?.RecordViolation(lawViolation, sourceId, npc != null ? npc.NpcId : null, applyLawConsequences, context != null ? context : this);
        }

        var record = log?.RecordReaction(this, npc, sourceId, context != null ? context : this);
        PublishApplied(player, npc, sourceId, context != null ? context : this, record);
        failureMessage = null;
        return true;
    }

    int GetRepeatCount(PlayerNPCReactionLog log, string npcId, string sourceId) {
        switch(repeatPolicy) {
            case NPCReactionRepeatPolicy.OncePerNPC:
            case NPCReactionRepeatPolicy.LimitedPerNPC:
                return log.GetCount(this, npcId, null);
            case NPCReactionRepeatPolicy.OncePerNPCAndSource:
            case NPCReactionRepeatPolicy.LimitedPerNPCAndSource:
                return log.GetCount(this, npcId, sourceId);
            default:
                return log.GetCount(this, null, null);
        }
    }

    string GetCooldownNPCFilter(string npcId) {
        return repeatPolicy == NPCReactionRepeatPolicy.OncePerNPC
            || repeatPolicy == NPCReactionRepeatPolicy.OncePerNPCAndSource
            || repeatPolicy == NPCReactionRepeatPolicy.LimitedPerNPC
            || repeatPolicy == NPCReactionRepeatPolicy.LimitedPerNPCAndSource
            ? npcId
            : null;
    }

    string GetCooldownSourceFilter(string sourceId) {
        return repeatPolicy == NPCReactionRepeatPolicy.OncePerNPCAndSource
            || repeatPolicy == NPCReactionRepeatPolicy.LimitedPerNPCAndSource
            ? sourceId
            : null;
    }

    void ApplyNPCMemory(PlayerController player, NPCMemoryProfile npc, string sourceId) {
        if(!recordNPCMemory || npc == null) {
            return;
        }

        npc.RecordInteraction(player, memoryInteractionType, memoryTopic, trustDelta, suspicionDelta, familiarityDelta, sourceId);
    }

    void ApplyRelationshipChanges(PlayerController player, NPCMemoryProfile npc) {
        var relationships = player.GetComponent<PlayerRelationships>();
        if(relationships == null) {
            return;
        }

        if(npc != null && npc.RelationshipSubject != null && reactorRelationshipDelta != 0) {
            relationships.AddRelationship(npc.RelationshipSubject, reactorRelationshipDelta);
        }

        relationships.ApplyChanges(relationshipChanges);
    }

    void ApplyReputationChanges(PlayerController player, NPCMemoryProfile npc) {
        var reputation = player.GetComponent<PlayerReputation>();
        if(reputation == null) {
            return;
        }

        if(npc != null && npc.ReputationFaction != null && reactorFactionReputationDelta != 0) {
            reputation.AddReputation(npc.ReputationFaction, reactorFactionReputationDelta);
        }

        reputation.ApplyChanges(reputationChanges);
    }

    void PublishApplied(PlayerController player, NPCMemoryProfile npc, string sourceId, UnityEngine.Object context, NPCReactionRecord record) {
        GameEventPublishing.PublishOptional(
            appliedEvent,
            $"npc-reaction.applied.{Id}",
            $"{DisplayName} applied.",
            GameEventCategory.NPC,
            GameEventImportance.Info,
            context != null ? context : player,
            "NPCReactionDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("reactionId", Id),
            GameEventPublishing.Value("reactionName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("npcId", npc != null ? npc.NpcId : string.Empty),
            GameEventPublishing.Value("npcName", npc != null ? npc.DisplayName : string.Empty),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("recordId", record != null ? record.recordId : string.Empty));
    }
}
