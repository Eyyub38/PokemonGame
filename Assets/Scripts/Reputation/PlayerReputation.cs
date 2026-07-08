using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerReputation : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save list of known faction reputation values.")]
    [SerializeField] List<ReputationValue> reputations = new List<ReputationValue>();

    public IReadOnlyList<ReputationValue> Reputations => reputations;
    public event Action OnReputationChanged;

    public int GetReputation(ReputationFactionDefinition faction) {
        if(faction == null) {
            return 0;
        }

        var reputation = reputations.FirstOrDefault(r => r.factionId == faction.Id);
        return reputation != null ? Mathf.Clamp(reputation.value, faction.MinValue, faction.MaxValue) : faction.DefaultValue;
    }

    public void AddReputation(ReputationFactionDefinition faction, int amount) {
        if(faction == null || amount == 0) {
            return;
        }

        var reputation = GetOrCreateReputation(faction);
        reputation.value = Mathf.Clamp(reputation.value + amount, faction.MinValue, faction.MaxValue);
        OnReputationChanged?.Invoke();
    }

    public void ApplyChanges(IEnumerable<ReputationChange> changes) {
        if(changes == null) {
            return;
        }

        foreach(var change in changes) {
            if(change != null) {
                AddReputation(change.faction, change.amount);
            }
        }
    }

    ReputationValue GetOrCreateReputation(ReputationFactionDefinition faction) {
        var reputation = reputations.FirstOrDefault(r => r.factionId == faction.Id);
        if(reputation != null) {
            return reputation;
        }

        reputation = new ReputationValue() {
            factionId = faction.Id,
            value = faction.DefaultValue
        };
        reputations.Add(reputation);
        return reputation;
    }

    public object CaptureState() {
        return reputations.Select(r => new ReputationValue() {
            factionId = r.factionId,
            value = r.value
        }).ToList();
    }

    public void RestoreState(object state) {
        reputations = state as List<ReputationValue> ?? new List<ReputationValue>();
        OnReputationChanged?.Invoke();
    }
}
