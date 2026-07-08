using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerRelationships : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save list of known relationship values.")]
    [SerializeField] List<RelationshipValue> relationships = new List<RelationshipValue>();

    public IReadOnlyList<RelationshipValue> Relationships => relationships;
    public event Action OnRelationshipsChanged;

    public int GetRelationship(RelationshipSubjectDefinition subject) {
        if(subject == null) {
            return 0;
        }

        var relationship = relationships.FirstOrDefault(r => r.subjectId == subject.Id);
        return relationship != null ? Mathf.Clamp(relationship.value, subject.MinValue, subject.MaxValue) : subject.DefaultValue;
    }

    public string GetRelationshipTier(RelationshipSubjectDefinition subject) {
        return subject != null ? subject.GetTierName(GetRelationship(subject)) : string.Empty;
    }

    public void AddRelationship(RelationshipSubjectDefinition subject, int amount) {
        if(subject == null || amount == 0) {
            return;
        }

        var relationship = GetOrCreateRelationship(subject);
        relationship.value = Mathf.Clamp(relationship.value + amount, subject.MinValue, subject.MaxValue);
        OnRelationshipsChanged?.Invoke();
    }

    public void ApplyChanges(IEnumerable<RelationshipChange> changes) {
        if(changes == null) {
            return;
        }

        foreach(var change in changes) {
            if(change != null) {
                AddRelationship(change.subject, change.amount);
            }
        }
    }

    RelationshipValue GetOrCreateRelationship(RelationshipSubjectDefinition subject) {
        var relationship = relationships.FirstOrDefault(r => r.subjectId == subject.Id);
        if(relationship != null) {
            return relationship;
        }

        relationship = new RelationshipValue() {
            subjectId = subject.Id,
            value = subject.DefaultValue
        };
        relationships.Add(relationship);
        return relationship;
    }

    public object CaptureState() {
        return relationships.Select(r => new RelationshipValue() {
            subjectId = r.subjectId,
            value = r.value
        }).ToList();
    }

    public void RestoreState(object state) {
        relationships = state as List<RelationshipValue> ?? new List<RelationshipValue>();
        OnRelationshipsChanged?.Invoke();
    }
}
