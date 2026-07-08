using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerDialogGraphLog : MonoBehaviour, ISavable {
    [Header("Runtime Data")]
    [Tooltip("Conversation graph history used by conditions, debugging and future UI.")]
    [SerializeField] List<DialogGraphHistoryRecord> history = new List<DialogGraphHistoryRecord>();

    [Header("Save")]
    [Tooltip("Maximum history rows kept in saves. 0 means history is not saved.")]
    [Min(0)]
    [SerializeField] int maxSavedHistory = 120;

    public IReadOnlyList<DialogGraphHistoryRecord> History => history;

    public void RecordNode(DialogGraphDefinition graph, DialogGraphNode node, string speakerId) {
        if(graph == null || node == null) {
            return;
        }

        AddRecord(new DialogGraphHistoryRecord {
            graphId = graph.Id,
            graphName = graph.DisplayName,
            nodeId = node.Id,
            nodeName = node.DisplayName,
            speakerId = speakerId,
            recordType = DialogGraphHistoryRecordType.NodeVisited,
            day = GetDay(),
            hour = GetHour(),
            absoluteHour = GetAbsoluteHour()
        });
    }

    public void RecordChoice(DialogGraphDefinition graph, DialogGraphNode node, DialogGraphChoice choice, string speakerId) {
        if(graph == null || choice == null) {
            return;
        }

        AddRecord(new DialogGraphHistoryRecord {
            graphId = graph.Id,
            graphName = graph.DisplayName,
            nodeId = node != null ? node.Id : string.Empty,
            nodeName = node != null ? node.DisplayName : string.Empty,
            choiceId = choice.Id,
            choiceText = choice.DisplayText,
            intent = choice.Intent,
            speakerId = speakerId,
            recordType = DialogGraphHistoryRecordType.ChoiceSelected,
            day = GetDay(),
            hour = GetHour(),
            absoluteHour = GetAbsoluteHour()
        });
    }

    public int GetGraphVisitCount(DialogGraphDefinition graph) {
        return graph != null ? GetGraphVisitCount(graph.Id) : 0;
    }

    public int GetGraphVisitCount(string graphId) {
        if(string.IsNullOrWhiteSpace(graphId)) {
            return 0;
        }

        return history.Count(record => record != null
            && record.recordType == DialogGraphHistoryRecordType.NodeVisited
            && string.Equals(record.graphId, graphId, StringComparison.OrdinalIgnoreCase));
    }

    public int GetChoiceCount(string graphId, string choiceId) {
        if(string.IsNullOrWhiteSpace(graphId) || string.IsNullOrWhiteSpace(choiceId)) {
            return 0;
        }

        return history.Count(record => record != null
            && record.recordType == DialogGraphHistoryRecordType.ChoiceSelected
            && string.Equals(record.graphId, graphId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(record.choiceId, choiceId, StringComparison.OrdinalIgnoreCase));
    }

    public int GetIntentCount(DialogChoiceIntent intent, string speakerId = null) {
        return history.Count(record => record != null
            && record.recordType == DialogGraphHistoryRecordType.ChoiceSelected
            && record.intent == intent
            && (string.IsNullOrWhiteSpace(speakerId) || string.Equals(record.speakerId, speakerId, StringComparison.OrdinalIgnoreCase)));
    }

    public object CaptureState() {
        return maxSavedHistory > 0
            ? history.OrderByDescending(record => record.absoluteHour).Take(maxSavedHistory).Select(record => record.Clone()).ToList()
            : new List<DialogGraphHistoryRecord>();
    }

    public void RestoreState(object state) {
        history = state as List<DialogGraphHistoryRecord> ?? new List<DialogGraphHistoryRecord>();
    }

    void AddRecord(DialogGraphHistoryRecord record) {
        if(record == null) {
            return;
        }

        history.Add(record);
        if(maxSavedHistory > 0 && history.Count > maxSavedHistory) {
            history = history
                .OrderByDescending(entry => entry.absoluteHour)
                .Take(maxSavedHistory)
                .OrderBy(entry => entry.absoluteHour)
                .ToList();
        }
    }

    int GetDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetHour() {
        return TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0;
    }

    int GetAbsoluteHour() {
        return GetDay() * 24 + GetHour();
    }
}

public enum DialogGraphHistoryRecordType {
    NodeVisited,
    ChoiceSelected
}

[Serializable]
public class DialogGraphHistoryRecord {
    [Tooltip("Conversation graph id.")]
    public string graphId;
    [Tooltip("Conversation graph display name.")]
    public string graphName;
    [Tooltip("Node id involved in this record.")]
    public string nodeId;
    [Tooltip("Node display name involved in this record.")]
    public string nodeName;
    [Tooltip("Choice id selected in this record.")]
    public string choiceId;
    [Tooltip("Choice text selected in this record.")]
    public string choiceText;
    [Tooltip("Intent selected in this record.")]
    public DialogChoiceIntent intent;
    [Tooltip("Speaker/NPC id used by this record.")]
    public string speakerId;
    [Tooltip("Record kind.")]
    public DialogGraphHistoryRecordType recordType;
    [Tooltip("In-game day when this record was created.")]
    public int day;
    [Tooltip("In-game hour when this record was created.")]
    public int hour;
    [Tooltip("Absolute in-game hour when this record was created.")]
    public int absoluteHour;

    public DialogGraphHistoryRecord Clone() {
        return new DialogGraphHistoryRecord {
            graphId = graphId,
            graphName = graphName,
            nodeId = nodeId,
            nodeName = nodeName,
            choiceId = choiceId,
            choiceText = choiceText,
            intent = intent,
            speakerId = speakerId,
            recordType = recordType,
            day = day,
            hour = hour,
            absoluteHour = absoluteHour
        };
    }
}
