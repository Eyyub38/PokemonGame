using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "World Events/Event Definition")]
public class WorldEventDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id for save/debug references. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Description of what this event means in the world.")]
    [TextArea][SerializeField] string description;

    [Header("Schedule")]
    [Tooltip("If enabled, this event can activate automatically from day/time rules.")]
    [SerializeField] bool autoActivateBySchedule;
    [Tooltip("If enabled, start/end day limits are checked.")]
    [SerializeField] bool scheduledByDay;
    [Tooltip("First in-game day this event can be active.")]
    [Min(1)]
    [SerializeField] int startDay = 1;
    [Tooltip("Last in-game day this event can be active.")]
    [Min(1)]
    [SerializeField] int endDay = 1;
    [Tooltip("Optional day periods when this event is active. Empty means all periods.")]
    [SerializeField] List<DayPeriod> activePeriods = new List<DayPeriod>();

    [Header("Activity Effects")]
    [Tooltip("If enabled, this event applies to every activity.")]
    [SerializeField] bool affectsAllActivities;
    [Tooltip("Specific activities affected when Affects All Activities is disabled.")]
    [SerializeField] List<ActivityDefinition> affectedActivities = new List<ActivityDefinition>();
    [Tooltip("If enabled, affected activities cannot be performed while this event is active.")]
    [SerializeField] bool blocksActivities;
    [Tooltip("Message shown when this event blocks an activity.")]
    [SerializeField] string blockedActivityMessage = "This activity is unavailable right now.";
    [Tooltip("Multiplier applied to affected activity XP. 1 means no change.")]
    [Min(0f)]
    [SerializeField] float experienceMultiplier = 1f;
    [Tooltip("Extra reputation changes applied whenever an affected activity completes.")]
    [SerializeField] List<ReputationChange> reputationChangesOnActivity = new List<ReputationChange>();
    [Header("Events")]
    [Tooltip("Optional event published when this world event becomes active. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition activatedEvent;
    [Tooltip("Optional event published when this world event stops being active. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition deactivatedEvent;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public bool BlocksActivities => blocksActivities;
    public string BlockedActivityMessage => string.IsNullOrWhiteSpace(blockedActivityMessage) ? "This activity is unavailable right now." : blockedActivityMessage;
    public float ExperienceMultiplier => Mathf.Max(0f, experienceMultiplier);
    public IReadOnlyList<ReputationChange> ReputationChangesOnActivity => reputationChangesOnActivity;
    public GameEventDefinition ActivatedEvent => activatedEvent;
    public GameEventDefinition DeactivatedEvent => deactivatedEvent;

    public bool IsActiveNow(TimeSystem timeSystem) {
        if(!autoActivateBySchedule) {
            return false;
        }

        if(timeSystem == null) {
            return false;
        }

        if(scheduledByDay && (timeSystem.Day < startDay || timeSystem.Day > endDay)) {
            return false;
        }

        return activePeriods.Count == 0 || activePeriods.Contains(timeSystem.CurrentPeriod);
    }

    public bool Affects(ActivityDefinition activity) {
        if(activity == null) {
            return affectsAllActivities;
        }

        return affectsAllActivities || affectedActivities.Contains(activity);
    }
}
