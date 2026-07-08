using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerResearchLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save list of research subject progress.")]
    [SerializeField] List<ResearchEntry> entries = new List<ResearchEntry>();

    public IReadOnlyList<ResearchEntry> Entries => entries;
    public event Action<ResearchEntry> OnResearchUpdated;

    public ResearchEntry GetEntry(ResearchSubjectDefinition subject) {
        if(subject == null) {
            return null;
        }

        return GetOrCreateEntry(subject.Id);
    }

    public bool IsCompleted(ResearchSubjectDefinition subject) {
        var entry = GetEntry(subject);
        return entry != null && entry.completed;
    }

    public ResearchEntry AddProgress(ResearchSubjectDefinition subject, int points) {
        if(subject == null || points <= 0) {
            return null;
        }

        var entry = GetOrCreateEntry(subject.Id);
        entry.points = Mathf.Clamp(entry.points + points, 0, subject.RequiredResearchPoints);
        if(entry.points >= subject.RequiredResearchPoints) {
            entry.completed = true;
        }

        OnResearchUpdated?.Invoke(entry);
        return entry;
    }

    ResearchEntry GetOrCreateEntry(string subjectId) {
        var entry = entries.FirstOrDefault(e => e.subjectId == subjectId);
        if(entry != null) {
            return entry;
        }

        entry = new ResearchEntry() { subjectId = subjectId };
        entries.Add(entry);
        return entry;
    }

    public object CaptureState() {
        return new PlayerResearchSaveData() {
            entries = entries.Select(e => new ResearchEntrySaveData() {
                subjectId = e.subjectId,
                points = e.points,
                completed = e.completed
            }).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerResearchSaveData;
        if(saveData == null) {
            return;
        }

        entries = saveData.entries?.Select(e => new ResearchEntry() {
            subjectId = e.subjectId,
            points = e.points,
            completed = e.completed
        }).ToList() ?? new List<ResearchEntry>();
    }
}

[Serializable]
public class ResearchEntry {
    [Tooltip("Saved research subject id.")]
    public string subjectId;
    [Tooltip("Current research points collected for this subject.")]
    [Min(0)]
    public int points;
    [Tooltip("Whether this research subject has reached its required points.")]
    public bool completed;
}

[Serializable]
public class PlayerResearchSaveData {
    public List<ResearchEntrySaveData> entries;
}

[Serializable]
public class ResearchEntrySaveData {
    public string subjectId;
    public int points;
    public bool completed;
}
