using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC Schedule/Schedule Definition")]
public class NPCScheduleDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id for this schedule. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in editor/debug. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note explaining this NPC's routine.")]
    [TextArea][SerializeField] string description;
    [Header("Entries")]
    [Tooltip("Entries are checked from top to bottom. The first matching entry is applied.")]
    [SerializeField] List<NPCScheduleEntry> entries = new List<NPCScheduleEntry>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<NPCScheduleEntry> Entries => entries;

    public NPCScheduleEntry GetEntryForNow(PlayerController player) {
        for(int i = 0; i < entries.Count; i++) {
            var entry = entries[i];
            if(entry != null && entry.IsMatch(player)) {
                return entry;
            }
        }

        return null;
    }
}

[System.Serializable]
public class NPCScheduleEntry {
    [Tooltip("Optional id for this entry. Empty uses the waypoint key.")]
    [SerializeField] string id;
    [Tooltip("Key that NPCScheduleController maps to a scene Transform waypoint.")]
    [SerializeField] string waypointKey;
    [Tooltip("If disabled, the NPC is hidden and cannot be interacted with.")]
    [SerializeField] bool visible = true;
    [Tooltip("Direction the NPC faces after this entry is applied.")]
    [SerializeField] FacingDirection facingDirection = FacingDirection.Down;
    [Tooltip("Optional day periods when this entry can match. Empty means any period.")]
    [SerializeField] List<DayPeriod> activePeriods = new List<DayPeriod>();
    [Tooltip("Movement pattern used while this entry is active.")]
    [SerializeField] List<Vector2> movementPattern = new List<Vector2>();
    [Tooltip("Optional milestone required for this entry to match.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional world event condition for this entry.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state of the required world event.")]
    [SerializeField] bool requireWorldEventActive = true;

    public string Id => string.IsNullOrWhiteSpace(id) ? waypointKey : id;
    public string WaypointKey => waypointKey;
    public bool Visible => visible;
    public FacingDirection FacingDirection => facingDirection;
    public IReadOnlyList<Vector2> MovementPattern => movementPattern;

    public bool IsMatch(PlayerController player) {
        if(activePeriods.Count > 0) {
            if(TimeSystem.i == null || !activePeriods.Contains(TimeSystem.i.CurrentPeriod)) {
                return false;
            }
        }

        if(requiredMilestone != null) {
            var milestones = player != null ? player.GetComponent<PlayerMilestones>() : null;
            if(milestones == null || !milestones.HasMilestone(requiredMilestone)) {
                return false;
            }
        }

        if(requiredWorldEvent != null) {
            bool active = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredWorldEvent);
            if(requireWorldEventActive != active) {
                return false;
            }
        }

        return true;
    }
}
