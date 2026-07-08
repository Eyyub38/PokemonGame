using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum NPCInteractionMemoryType {
    Generic,
    Conversation,
    Gift,
    Trade,
    Battle,
    Quest,
    Assignment,
    Investigation,
    Law,
    Help,
    Custom
}

public class PlayerNPCMemoryLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save memory states for NPCs the player has interacted with.")]
    [SerializeField] List<NPCMemoryState> npcMemories = new List<NPCMemoryState>();

    public IReadOnlyList<NPCMemoryState> NPCMemories => npcMemories;
    public event Action<string> OnNPCMemoryChanged;
    public event Action OnNPCMemoryLogChanged;

    public NPCMemoryState RecordInteraction(
        string npcId,
        string npcName,
        NPCInteractionMemoryType interactionType = NPCInteractionMemoryType.Conversation,
        NPCMemoryTopicDefinition topic = null,
        int trustDelta = 0,
        int suspicionDelta = 0,
        int familiarityDelta = 1,
        string sourceId = null,
        UnityEngine.Object context = null
    ) {
        if(string.IsNullOrWhiteSpace(npcId)) {
            return null;
        }

        var state = GetOrCreateState(npcId, npcName);
        state.met = true;
        state.interactionCount++;
        state.lastInteractionType = interactionType;
        state.lastSourceId = sourceId;
        state.lastInteractionDay = GetCurrentDay();
        state.lastInteractionAbsoluteHour = GetCurrentAbsoluteHour();
        state.interactions.Add(new NPCInteractionMemoryEntry {
            interactionType = interactionType,
            sourceId = sourceId,
            day = state.lastInteractionDay,
            absoluteHour = state.lastInteractionAbsoluteHour
        });
        ApplyDeltas(state, trustDelta, suspicionDelta, familiarityDelta);

        if(topic != null) {
            RememberTopicInternal(state, topic, topic.TrustDelta, topic.SuspicionDelta, topic.FamiliarityDelta, sourceId, context, false);
        }

        PublishInteraction(state, interactionType, context);
        NotifyChanged(state.npcId);
        return state;
    }

    public bool RememberTopic(string npcId, string npcName, NPCMemoryTopicDefinition topic, string sourceId = null, UnityEngine.Object context = null) {
        if(string.IsNullOrWhiteSpace(npcId) || topic == null) {
            return false;
        }

        var state = GetOrCreateState(npcId, npcName);
        state.met = true;
        bool remembered = RememberTopicInternal(state, topic, topic.TrustDelta, topic.SuspicionDelta, topic.FamiliarityDelta, sourceId, context, topic.CountAsInteraction);
        NotifyChanged(state.npcId);
        return remembered;
    }

    public bool ForgetTopic(string npcId, NPCMemoryTopicDefinition topic) {
        if(string.IsNullOrWhiteSpace(npcId) || topic == null) {
            return false;
        }

        var state = GetState(npcId);
        if(state == null) {
            return false;
        }

        bool removed = state.topics.RemoveAll(entry => entry != null && entry.topicId == topic.Id) > 0;
        if(removed) {
            NotifyChanged(npcId);
        }
        return removed;
    }

    public bool HasMet(string npcId) {
        return GetState(npcId)?.met ?? false;
    }

    public int GetInteractionCount(string npcId) {
        return Mathf.Max(0, GetState(npcId)?.interactionCount ?? 0);
    }

    public int GetInteractionCountByType(string npcId, NPCInteractionMemoryType interactionType) {
        var state = GetState(npcId);
        if(state == null) {
            return 0;
        }

        return state.interactions.Count(entry => entry != null && entry.interactionType == interactionType);
    }

    public bool HasTopic(string npcId, NPCMemoryTopicDefinition topic) {
        return topic != null && HasTopic(npcId, topic.Id);
    }

    public bool HasTopic(string npcId, string topicId) {
        var state = GetState(npcId);
        return state != null && !string.IsNullOrWhiteSpace(topicId) && state.topics.Any(entry => entry != null && entry.topicId == topicId);
    }

    public int GetTopicCount(string npcId, NPCMemoryTopicDefinition topic = null) {
        var state = GetState(npcId);
        if(state == null) {
            return 0;
        }

        if(topic == null) {
            return state.topics.Count(entry => entry != null);
        }

        var entry = state.topics.FirstOrDefault(topicState => topicState != null && topicState.topicId == topic.Id);
        return Mathf.Max(0, entry?.rememberedCount ?? 0);
    }

    public int GetTopicCountWithTag(string npcId, string tag) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        var state = GetState(npcId);
        if(state == null) {
            return 0;
        }

        int count = 0;
        foreach(var topicState in state.topics) {
            var topic = ResolveTopic(topicState?.topicId);
            if(topic != null && topic.HasTag(tag)) {
                count += Mathf.Max(0, topicState.rememberedCount);
            }
        }
        return count;
    }

    public int GetTrust(string npcId) {
        return GetState(npcId)?.trust ?? 0;
    }

    public int GetSuspicion(string npcId) {
        return GetState(npcId)?.suspicion ?? 0;
    }

    public int GetFamiliarity(string npcId) {
        return GetState(npcId)?.familiarity ?? 0;
    }

    public int GetHoursSinceLastInteraction(string npcId) {
        var state = GetState(npcId);
        if(state == null || state.lastInteractionAbsoluteHour < 0) {
            return -1;
        }

        return Mathf.Max(0, GetCurrentAbsoluteHour() - state.lastInteractionAbsoluteHour);
    }

    bool RememberTopicInternal(
        NPCMemoryState state,
        NPCMemoryTopicDefinition topic,
        int trustDelta,
        int suspicionDelta,
        int familiarityDelta,
        string sourceId,
        UnityEngine.Object context,
        bool countAsInteraction
    ) {
        if(state == null || topic == null) {
            return false;
        }

        var topicState = state.topics.FirstOrDefault(entry => entry != null && entry.topicId == topic.Id);
        if(topicState == null) {
            topicState = new NPCMemoryTopicState {
                topicId = topic.Id,
                topicName = topic.DisplayName,
                category = topic.Category
            };
            state.topics.Add(topicState);
        }

        topicState.rememberedCount++;
        topicState.lastSourceId = sourceId;
        topicState.lastRememberedDay = GetCurrentDay();
        topicState.lastRememberedAbsoluteHour = GetCurrentAbsoluteHour();
        state.lastTopicId = topic.Id;
        state.lastTopicName = topic.DisplayName;
        state.lastSourceId = sourceId;
        ApplyDeltas(state, trustDelta, suspicionDelta, familiarityDelta);

        if(countAsInteraction) {
            state.interactionCount++;
            state.lastInteractionType = NPCInteractionMemoryType.Custom;
            state.lastInteractionDay = GetCurrentDay();
            state.lastInteractionAbsoluteHour = GetCurrentAbsoluteHour();
            state.interactions.Add(new NPCInteractionMemoryEntry {
                interactionType = NPCInteractionMemoryType.Custom,
                sourceId = sourceId,
                day = state.lastInteractionDay,
                absoluteHour = state.lastInteractionAbsoluteHour
            });
        }

        topic.PublishRemembered(GetComponent<PlayerController>(), state.npcId, state.npcName, context != null ? context : this);
        return true;
    }

    void ApplyDeltas(NPCMemoryState state, int trustDelta, int suspicionDelta, int familiarityDelta) {
        if(state == null) {
            return;
        }

        state.trust += trustDelta;
        state.suspicion += suspicionDelta;
        state.familiarity += familiarityDelta;
    }

    NPCMemoryState GetOrCreateState(string npcId, string npcName) {
        var state = GetState(npcId);
        if(state != null) {
            if(!string.IsNullOrWhiteSpace(npcName)) {
                state.npcName = npcName;
            }
            return state;
        }

        state = new NPCMemoryState {
            npcId = npcId,
            npcName = string.IsNullOrWhiteSpace(npcName) ? npcId : npcName,
            firstMetDay = GetCurrentDay(),
            firstMetAbsoluteHour = GetCurrentAbsoluteHour(),
            lastInteractionDay = -1,
            lastInteractionAbsoluteHour = -1
        };
        npcMemories.Add(state);
        return state;
    }

    NPCMemoryState GetState(string npcId) {
        if(string.IsNullOrWhiteSpace(npcId)) {
            return null;
        }

        return npcMemories.FirstOrDefault(state => state != null && state.npcId == npcId);
    }

    NPCMemoryTopicDefinition ResolveTopic(string topicId) {
        if(string.IsNullOrWhiteSpace(topicId)) {
            return null;
        }

        return Resources.LoadAll<NPCMemoryTopicDefinition>("").FirstOrDefault(topic => topic != null && topic.Id == topicId);
    }

    void PublishInteraction(NPCMemoryState state, NPCInteractionMemoryType interactionType, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            null,
            $"npc-memory.interaction.{state.npcId}",
            $"{state.npcName} interaction recorded.",
            GameEventCategory.NPC,
            GameEventImportance.Trace,
            context != null ? context : this,
            "PlayerNPCMemoryLog",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("npcId", state.npcId),
            GameEventPublishing.Value("npcName", state.npcName),
            GameEventPublishing.Value("interactionType", interactionType),
            GameEventPublishing.Value("interactionCount", state.interactionCount),
            GameEventPublishing.Value("trust", state.trust),
            GameEventPublishing.Value("suspicion", state.suspicion),
            GameEventPublishing.Value("familiarity", state.familiarity));
    }

    void NotifyChanged(string npcId) {
        OnNPCMemoryChanged?.Invoke(npcId);
        OnNPCMemoryLogChanged?.Invoke();
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerNPCMemoryLogSaveData {
            npcMemories = npcMemories.Where(state => state != null).Select(state => state.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerNPCMemoryLogSaveData;
        npcMemories = saveData?.npcMemories?.Where(entry => entry != null).Select(entry => new NPCMemoryState(entry)).ToList() ?? new List<NPCMemoryState>();
        OnNPCMemoryLogChanged?.Invoke();
    }
}

[Serializable]
public class NPCMemoryState {
    [Tooltip("Stable NPC id.")]
    public string npcId;
    [Tooltip("NPC display name for fallback/debug output.")]
    public string npcName;
    [Tooltip("If enabled, the player has met this NPC.")]
    public bool met;
    [Tooltip("Total interactions recorded with this NPC.")]
    [Min(0)]
    public int interactionCount;
    [Tooltip("Current trust value for this NPC.")]
    public int trust;
    [Tooltip("Current suspicion value for this NPC.")]
    public int suspicion;
    [Tooltip("Current familiarity value for this NPC.")]
    public int familiarity;
    [Tooltip("In-game day this NPC was first met.")]
    public int firstMetDay = -1;
    [Tooltip("Absolute in-game hour this NPC was first met.")]
    public int firstMetAbsoluteHour = -1;
    [Tooltip("In-game day of the last interaction.")]
    public int lastInteractionDay = -1;
    [Tooltip("Absolute in-game hour of the last interaction.")]
    public int lastInteractionAbsoluteHour = -1;
    [Tooltip("Last interaction type recorded for this NPC.")]
    public NPCInteractionMemoryType lastInteractionType = NPCInteractionMemoryType.Generic;
    [Tooltip("Last memory topic id remembered for this NPC.")]
    public string lastTopicId;
    [Tooltip("Last memory topic display name remembered for this NPC.")]
    public string lastTopicName;
    [Tooltip("Last system/source id that changed this memory.")]
    public string lastSourceId;
    [Tooltip("Recorded interaction entries.")]
    public List<NPCInteractionMemoryEntry> interactions = new List<NPCInteractionMemoryEntry>();
    [Tooltip("Recorded topic memories.")]
    public List<NPCMemoryTopicState> topics = new List<NPCMemoryTopicState>();

    public NPCMemoryState() {
    }

    public NPCMemoryState(NPCMemoryStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        npcId = saveData.npcId;
        npcName = saveData.npcName;
        met = saveData.met;
        interactionCount = Mathf.Max(0, saveData.interactionCount);
        trust = saveData.trust;
        suspicion = saveData.suspicion;
        familiarity = saveData.familiarity;
        firstMetDay = saveData.firstMetDay;
        firstMetAbsoluteHour = saveData.firstMetAbsoluteHour;
        lastInteractionDay = saveData.lastInteractionDay;
        lastInteractionAbsoluteHour = saveData.lastInteractionAbsoluteHour;
        lastInteractionType = saveData.lastInteractionType;
        lastTopicId = saveData.lastTopicId;
        lastTopicName = saveData.lastTopicName;
        lastSourceId = saveData.lastSourceId;
        interactions = saveData.interactions?.Where(entry => entry != null).Select(entry => new NPCInteractionMemoryEntry(entry)).ToList() ?? new List<NPCInteractionMemoryEntry>();
        topics = saveData.topics?.Where(entry => entry != null).Select(entry => new NPCMemoryTopicState(entry)).ToList() ?? new List<NPCMemoryTopicState>();
    }

    public NPCMemoryStateSaveData ToSaveData() {
        return new NPCMemoryStateSaveData {
            npcId = npcId,
            npcName = npcName,
            met = met,
            interactionCount = interactionCount,
            trust = trust,
            suspicion = suspicion,
            familiarity = familiarity,
            firstMetDay = firstMetDay,
            firstMetAbsoluteHour = firstMetAbsoluteHour,
            lastInteractionDay = lastInteractionDay,
            lastInteractionAbsoluteHour = lastInteractionAbsoluteHour,
            lastInteractionType = lastInteractionType,
            lastTopicId = lastTopicId,
            lastTopicName = lastTopicName,
            lastSourceId = lastSourceId,
            interactions = interactions?.Where(entry => entry != null).Select(entry => entry.ToSaveData()).ToList() ?? new List<NPCInteractionMemoryEntrySaveData>(),
            topics = topics?.Where(entry => entry != null).Select(entry => entry.ToSaveData()).ToList() ?? new List<NPCMemoryTopicStateSaveData>()
        };
    }
}

[Serializable]
public class NPCInteractionMemoryEntry {
    [Tooltip("Interaction type recorded for this entry.")]
    public NPCInteractionMemoryType interactionType = NPCInteractionMemoryType.Generic;
    [Tooltip("Optional source id that caused this interaction.")]
    public string sourceId;
    [Tooltip("In-game day this interaction occurred.")]
    public int day;
    [Tooltip("Absolute in-game hour this interaction occurred.")]
    public int absoluteHour;

    public NPCInteractionMemoryEntry() {
    }

    public NPCInteractionMemoryEntry(NPCInteractionMemoryEntrySaveData saveData) {
        if(saveData == null) {
            return;
        }

        interactionType = saveData.interactionType;
        sourceId = saveData.sourceId;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
    }

    public NPCInteractionMemoryEntrySaveData ToSaveData() {
        return new NPCInteractionMemoryEntrySaveData {
            interactionType = interactionType,
            sourceId = sourceId,
            day = day,
            absoluteHour = absoluteHour
        };
    }
}

[Serializable]
public class NPCMemoryTopicState {
    [Tooltip("Saved topic id.")]
    public string topicId;
    [Tooltip("Saved topic display name for fallback/debug output.")]
    public string topicName;
    [Tooltip("Saved topic category.")]
    public NPCMemoryTopicCategory category;
    [Tooltip("Number of times this topic was remembered.")]
    [Min(0)]
    public int rememberedCount;
    [Tooltip("Last source id that remembered this topic.")]
    public string lastSourceId;
    [Tooltip("In-game day this topic was last remembered.")]
    public int lastRememberedDay = -1;
    [Tooltip("Absolute in-game hour this topic was last remembered.")]
    public int lastRememberedAbsoluteHour = -1;

    public NPCMemoryTopicState() {
    }

    public NPCMemoryTopicState(NPCMemoryTopicStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        topicId = saveData.topicId;
        topicName = saveData.topicName;
        category = saveData.category;
        rememberedCount = Mathf.Max(0, saveData.rememberedCount);
        lastSourceId = saveData.lastSourceId;
        lastRememberedDay = saveData.lastRememberedDay;
        lastRememberedAbsoluteHour = saveData.lastRememberedAbsoluteHour;
    }

    public NPCMemoryTopicStateSaveData ToSaveData() {
        return new NPCMemoryTopicStateSaveData {
            topicId = topicId,
            topicName = topicName,
            category = category,
            rememberedCount = rememberedCount,
            lastSourceId = lastSourceId,
            lastRememberedDay = lastRememberedDay,
            lastRememberedAbsoluteHour = lastRememberedAbsoluteHour
        };
    }
}

[Serializable]
public class PlayerNPCMemoryLogSaveData {
    public List<NPCMemoryStateSaveData> npcMemories;
}

[Serializable]
public class NPCMemoryStateSaveData {
    public string npcId;
    public string npcName;
    public bool met;
    public int interactionCount;
    public int trust;
    public int suspicion;
    public int familiarity;
    public int firstMetDay;
    public int firstMetAbsoluteHour;
    public int lastInteractionDay;
    public int lastInteractionAbsoluteHour;
    public NPCInteractionMemoryType lastInteractionType;
    public string lastTopicId;
    public string lastTopicName;
    public string lastSourceId;
    public List<NPCInteractionMemoryEntrySaveData> interactions;
    public List<NPCMemoryTopicStateSaveData> topics;
}

[Serializable]
public class NPCInteractionMemoryEntrySaveData {
    public NPCInteractionMemoryType interactionType;
    public string sourceId;
    public int day;
    public int absoluteHour;
}

[Serializable]
public class NPCMemoryTopicStateSaveData {
    public string topicId;
    public string topicName;
    public NPCMemoryTopicCategory category;
    public int rememberedCount;
    public string lastSourceId;
    public int lastRememberedDay;
    public int lastRememberedAbsoluteHour;
}
