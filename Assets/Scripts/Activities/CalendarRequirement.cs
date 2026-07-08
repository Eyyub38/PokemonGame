using UnityEngine;

public enum CalendarRequirementMode {
    EventUnlocked,
    EventSeen,
    EventCompleted,
    EventActive,
    EventVisible,
    EventCategorySeenCount,
    EventTagSeenCount,
    EventDismissed
}

[CreateAssetMenu(menuName = "Activities/Requirements/Calendar Requirement")]
public class CalendarRequirement : ActivityRequirement {
    [Tooltip("Which calendar value this requirement checks.")]
    [SerializeField] CalendarRequirementMode mode = CalendarRequirementMode.EventSeen;
    [Tooltip("Calendar event checked by event-specific modes.")]
    [SerializeField] CalendarEventDefinition calendarEvent;
    [Tooltip("Calendar category checked by Event Category Seen Count mode.")]
    [SerializeField] CalendarEventCategory category = CalendarEventCategory.General;
    [Tooltip("Tag checked by Event Tag Seen Count mode.")]
    [SerializeField] string tag;
    [Tooltip("Minimum count required by count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected calendar condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerCalendarLog>() : null;
        bool result = mode switch {
            CalendarRequirementMode.EventUnlocked => log != null && log.HasUnlockedEvent(calendarEvent),
            CalendarRequirementMode.EventCompleted => log != null && log.HasCompletedEvent(calendarEvent),
            CalendarRequirementMode.EventActive => calendarEvent != null && calendarEvent.IsActiveNow(),
            CalendarRequirementMode.EventVisible => calendarEvent != null && calendarEvent.CanShow(player, log, out _),
            CalendarRequirementMode.EventCategorySeenCount => log != null && log.GetSeenCountByCategory(category) >= Mathf.Max(0, requiredCount),
            CalendarRequirementMode.EventTagSeenCount => log != null && log.GetSeenCountWithTag(tag) >= Mathf.Max(0, requiredCount),
            CalendarRequirementMode.EventDismissed => log != null && log.IsDismissed(calendarEvent),
            _ => log != null && log.HasSeenEvent(calendarEvent)
        };

        return mustBeMet ? result : !result;
    }
}
