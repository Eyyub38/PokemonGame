using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerNPCReactionLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save history of NPC reactions applied to the player.")]
    [SerializeField] List<NPCReactionRecord> reactions = new List<NPCReactionRecord>();

    public IReadOnlyList<NPCReactionRecord> Reactions => reactions;
    public event Action<NPCReactionRecord> OnReactionRecorded;
    public event Action OnReactionLogChanged;

    public NPCReactionRecord RecordReaction(NPCReactionDefinition reaction, NPCMemoryProfile npc, string sourceId = null, UnityEngine.Object context = null) {
        if(reaction == null) {
            return null;
        }

        var record = new NPCReactionRecord {
            recordId = Guid.NewGuid().ToString("N"),
            reactionId = reaction.Id,
            reactionName = reaction.DisplayName,
            category = reaction.Category,
            npcId = npc != null ? npc.NpcId : string.Empty,
            npcName = npc != null ? npc.DisplayName : string.Empty,
            sourceId = sourceId,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour()
        };
        reactions.Add(record);
        OnReactionRecorded?.Invoke(record);
        OnReactionLogChanged?.Invoke();
        return record;
    }

    public int GetCount(NPCReactionDefinition reaction = null, string npcId = null, string sourceId = null) {
        return reactions.Count(record => Matches(record, reaction, npcId, sourceId));
    }

    public int GetCountByCategory(NPCReactionCategory category, string npcId = null, string sourceId = null) {
        return reactions.Count(record => record != null
            && record.category == category
            && MatchesFilter(record.npcId, npcId)
            && MatchesFilter(record.sourceId, sourceId));
    }

    public int GetCountWithTag(string tag, string npcId = null, string sourceId = null) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        int count = 0;
        foreach(var record in reactions) {
            if(record == null || !MatchesFilter(record.npcId, npcId) || !MatchesFilter(record.sourceId, sourceId)) {
                continue;
            }

            var reaction = ResolveReaction(record.reactionId);
            if(reaction != null && reaction.HasTag(tag)) {
                count++;
            }
        }
        return count;
    }

    public int GetHoursSinceLastReaction(NPCReactionDefinition reaction = null, string npcId = null, string sourceId = null) {
        var latest = reactions
            .Where(record => Matches(record, reaction, npcId, sourceId))
            .OrderByDescending(record => record.absoluteHour)
            .FirstOrDefault();

        if(latest == null || latest.absoluteHour < 0) {
            return -1;
        }

        return Mathf.Max(0, GetCurrentAbsoluteHour() - latest.absoluteHour);
    }

    bool Matches(NPCReactionRecord record, NPCReactionDefinition reaction, string npcId, string sourceId) {
        return record != null
            && (reaction == null || record.reactionId == reaction.Id)
            && MatchesFilter(record.npcId, npcId)
            && MatchesFilter(record.sourceId, sourceId);
    }

    bool MatchesFilter(string value, string filter) {
        return string.IsNullOrWhiteSpace(filter) || value == filter;
    }

    NPCReactionDefinition ResolveReaction(string reactionId) {
        if(string.IsNullOrWhiteSpace(reactionId)) {
            return null;
        }

        return Resources.LoadAll<NPCReactionDefinition>("").FirstOrDefault(reaction => reaction != null && reaction.Id == reactionId);
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerNPCReactionLogSaveData {
            reactions = reactions.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerNPCReactionLogSaveData;
        reactions = saveData?.reactions?.Where(record => record != null).Select(record => new NPCReactionRecord(record)).ToList() ?? new List<NPCReactionRecord>();
        OnReactionLogChanged?.Invoke();
    }
}

[Serializable]
public class NPCReactionRecord {
    [Tooltip("Stable runtime record id.")]
    public string recordId;
    [Tooltip("Reaction definition id that was applied.")]
    public string reactionId;
    [Tooltip("Reaction display name saved for fallback/debug output.")]
    public string reactionName;
    [Tooltip("Saved reaction category.")]
    public NPCReactionCategory category;
    [Tooltip("NPC id that reacted. Empty means the reaction was global or source-only.")]
    public string npcId;
    [Tooltip("NPC display name saved for fallback/debug output.")]
    public string npcName;
    [Tooltip("Source id that triggered this reaction.")]
    public string sourceId;
    [Tooltip("In-game day this reaction was applied.")]
    public int day;
    [Tooltip("Absolute in-game hour this reaction was applied.")]
    public int absoluteHour;

    public NPCReactionRecord() {
    }

    public NPCReactionRecord(NPCReactionRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        reactionId = saveData.reactionId;
        reactionName = saveData.reactionName;
        category = saveData.category;
        npcId = saveData.npcId;
        npcName = saveData.npcName;
        sourceId = saveData.sourceId;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
    }

    public NPCReactionRecordSaveData ToSaveData() {
        return new NPCReactionRecordSaveData {
            recordId = recordId,
            reactionId = reactionId,
            reactionName = reactionName,
            category = category,
            npcId = npcId,
            npcName = npcName,
            sourceId = sourceId,
            day = day,
            absoluteHour = absoluteHour
        };
    }
}

[Serializable]
public class PlayerNPCReactionLogSaveData {
    public List<NPCReactionRecordSaveData> reactions;
}

[Serializable]
public class NPCReactionRecordSaveData {
    public string recordId;
    public string reactionId;
    public string reactionName;
    public NPCReactionCategory category;
    public string npcId;
    public string npcName;
    public string sourceId;
    public int day;
    public int absoluteHour;
}
