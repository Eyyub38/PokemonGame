using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompetitionRegistrationWindowCalendarMode {
    Ignore,
    EventActive,
    EventVisible,
    EventScheduledToday,
    EventUnlocked,
    EventSeen,
    EventCompleted
}

[CreateAssetMenu(menuName = "Competitions/Registration Window Definition")]
public class CompetitionRegistrationWindowDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this registration window. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future registration, calendar or tournament UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this registration window.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as weekly, qualifier, kanto, frontier, championship or city-cup.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Calendar Link")]
    [Tooltip("Optional calendar event that can control this registration window.")]
    [SerializeField] CalendarEventDefinition calendarEvent;
    [Tooltip("How the linked calendar event gates this registration window.")]
    [SerializeField] CompetitionRegistrationWindowCalendarMode calendarMode = CompetitionRegistrationWindowCalendarMode.Ignore;

    [Header("Manual Schedule")]
    [Tooltip("If enabled, the manual day/period schedule below must be open.")]
    [SerializeField] bool useManualSchedule = true;
    [Tooltip("How this manual window repeats after Start Day.")]
    [SerializeField] CalendarRepeatMode repeatMode = CalendarRepeatMode.Once;
    [Tooltip("First in-game day this manual window can open.")]
    [Min(1)]
    [SerializeField] int startDay = 1;
    [Tooltip("If enabled, End Day limits future manual openings.")]
    [SerializeField] bool useEndDay = true;
    [Tooltip("Last in-game day this manual window can open when Use End Day is enabled.")]
    [Min(1)]
    [SerializeField] int endDay = 1;
    [Tooltip("Interval in days when Repeat Mode is Every N Days.")]
    [Min(1)]
    [SerializeField] int repeatEveryDays = 1;
    [Tooltip("Weekdays used by Weekly repeat mode. Empty means every weekday is valid.")]
    [SerializeField] List<WeekDay> activeWeekDays = new List<WeekDay>();
    [Tooltip("Specific in-game days used by Specific Days repeat mode.")]
    [SerializeField] List<int> specificDays = new List<int>();
    [Tooltip("Allowed day periods. Empty means any time during a scheduled day.")]
    [SerializeField] List<DayPeriod> activePeriods = new List<DayPeriod>();

    [Header("Registration Filters")]
    [Tooltip("Optional roster this window is meant for. Empty allows any roster.")]
    [SerializeField] CompetitionRosterDefinition rosterFilter;
    [Tooltip("Optional competition this window is meant for. Empty allows any competition.")]
    [SerializeField] CompetitionDefinition competitionFilter;
    [Tooltip("Optional season this window is meant for. Empty allows any season.")]
    [SerializeField] CompetitionSeasonDefinition seasonFilter;
    [Tooltip("Optional ranking track this window is meant for. Empty allows any ranking.")]
    [SerializeField] CompetitionRankingDefinition rankingFilter;
    [Tooltip("Registration tags required before this window can apply. Empty means no tag filter.")]
    [SerializeField] List<string> requiredRegistrationTags = new List<string>();
    [Tooltip("How required registration tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode registrationTagMatchMode = ConsequenceRequirementMatchMode.All;

    [Header("Access")]
    [Tooltip("How additional window requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this window opens.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this window is closed and no more specific reason exists.")]
    [TextArea]
    [SerializeField] string closedMessage = "Registration is closed right now.";

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public CalendarEventDefinition CalendarEvent => calendarEvent;
    public CompetitionRegistrationWindowCalendarMode CalendarMode => calendarMode;
    public bool UseManualSchedule => useManualSchedule;
    public CalendarRepeatMode RepeatMode => repeatMode;
    public int StartDay => Mathf.Max(1, startDay);
    public bool UseEndDay => useEndDay;
    public int EndDay => Mathf.Max(StartDay, endDay);
    public int RepeatEveryDays => Mathf.Max(1, repeatEveryDays);
    public IReadOnlyList<WeekDay> ActiveWeekDays => activeWeekDays != null ? (IReadOnlyList<WeekDay>)activeWeekDays : Array.Empty<WeekDay>();
    public IReadOnlyList<int> SpecificDays => specificDays != null ? (IReadOnlyList<int>)specificDays : Array.Empty<int>();
    public IReadOnlyList<DayPeriod> ActivePeriods => activePeriods != null ? (IReadOnlyList<DayPeriod>)activePeriods : Array.Empty<DayPeriod>();
    public IReadOnlyList<string> RequiredRegistrationTags => requiredRegistrationTags != null ? (IReadOnlyList<string>)requiredRegistrationTags : Array.Empty<string>();
    public ConsequenceRequirementMatchMode RegistrationTagMatchMode => registrationTagMatchMode;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();

    public bool IsOpen(PlayerController player, CompetitionRegistrationDefinition registration, out string failureMessage) {
        if(!MatchesRegistration(registration)) {
            failureMessage = string.IsNullOrWhiteSpace(closedMessage) ? $"{DisplayName} does not apply to this registration." : closedMessage;
            return false;
        }

        if(!PassesCalendarGate(player, out failureMessage)) {
            return false;
        }

        if(useManualSchedule && !IsManualScheduleOpen()) {
            failureMessage = string.IsNullOrWhiteSpace(closedMessage) ? $"{DisplayName} is closed right now." : closedMessage;
            return false;
        }

        if(!RequirementsMet(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool IsManualScheduleOpen() {
        if(!useManualSchedule) {
            return true;
        }

        int day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
        bool periodOpen = activePeriods == null
            || activePeriods.Count == 0
            || activePeriods.Contains(TimeSystem.i != null ? TimeSystem.i.CurrentPeriod : DayPeriod.None);
        return IsScheduledForDay(day) && periodOpen;
    }

    public bool IsScheduledForDay(int day) {
        day = Mathf.Max(1, day);
        if(day < StartDay) {
            return false;
        }

        if(useEndDay && day > EndDay) {
            return false;
        }

        return repeatMode switch {
            CalendarRepeatMode.Daily => true,
            CalendarRepeatMode.EveryNDays => (day - StartDay) % RepeatEveryDays == 0,
            CalendarRepeatMode.Weekly => activeWeekDays == null || activeWeekDays.Count == 0 || activeWeekDays.Contains(GetWeekDay(day)),
            CalendarRepeatMode.SpecificDays => specificDays != null && specificDays.Contains(day),
            _ => day == StartDay
        };
    }

    public int GetNextOpenDay(int currentDay, int maxLookAheadDays = 60) {
        currentDay = Mathf.Max(1, currentDay);
        int lookAhead = Mathf.Max(0, maxLookAheadDays);
        for(int day = currentDay; day <= currentDay + lookAhead; day++) {
            if(PassesScheduleForDay(day)) {
                return day;
            }
        }

        return -1;
    }

    public string BuildOccurrenceKey() {
        int day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
        return $"{Id}:day:{day}";
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    bool MatchesRegistration(CompetitionRegistrationDefinition registration) {
        if(registration == null) {
            return rosterFilter == null
                && competitionFilter == null
                && seasonFilter == null
                && rankingFilter == null
                && (requiredRegistrationTags == null || requiredRegistrationTags.All(string.IsNullOrWhiteSpace));
        }

        if(rosterFilter != null && registration.Roster != rosterFilter) {
            return false;
        }

        if(competitionFilter != null && registration.Competition != competitionFilter) {
            return false;
        }

        if(seasonFilter != null && registration.Season != seasonFilter) {
            return false;
        }

        if(rankingFilter != null && registration.Ranking != rankingFilter) {
            return false;
        }

        var requiredTags = requiredRegistrationTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() ?? new List<string>();
        if(requiredTags.Count == 0) {
            return true;
        }

        if(registrationTagMatchMode == ConsequenceRequirementMatchMode.Any) {
            return requiredTags.Any(registration.HasTag);
        }

        return requiredTags.All(registration.HasTag);
    }

    bool PassesCalendarGate(PlayerController player, out string failureMessage) {
        if(calendarMode == CompetitionRegistrationWindowCalendarMode.Ignore) {
            failureMessage = null;
            return true;
        }

        if(calendarEvent == null) {
            failureMessage = string.IsNullOrWhiteSpace(closedMessage) ? $"{DisplayName} has no calendar event assigned." : closedMessage;
            return false;
        }

        var calendarLog = player != null ? player.GetComponent<PlayerCalendarLog>() : null;
        failureMessage = null;
        bool passed = calendarMode switch {
            CompetitionRegistrationWindowCalendarMode.EventActive => calendarEvent.IsActiveNow(),
            CompetitionRegistrationWindowCalendarMode.EventVisible => calendarEvent.CanShow(player, calendarLog, out failureMessage),
            CompetitionRegistrationWindowCalendarMode.EventScheduledToday => calendarEvent.IsScheduledForDay(TimeSystem.i != null ? TimeSystem.i.Day : 1),
            CompetitionRegistrationWindowCalendarMode.EventUnlocked => calendarLog != null ? calendarLog.HasUnlockedEvent(calendarEvent) : calendarEvent.UnlockedByDefault,
            CompetitionRegistrationWindowCalendarMode.EventSeen => calendarLog != null && calendarLog.HasSeenEvent(calendarEvent),
            CompetitionRegistrationWindowCalendarMode.EventCompleted => calendarLog != null && calendarLog.HasCompletedEvent(calendarEvent),
            _ => true
        };

        if(passed) {
            failureMessage = null;
            return true;
        }

        if(string.IsNullOrWhiteSpace(failureMessage)) {
            failureMessage = string.IsNullOrWhiteSpace(closedMessage) ? $"{DisplayName} is not open on the calendar." : closedMessage;
        }

        return false;
    }

    bool PassesScheduleForDay(int day) {
        bool manualPass = !useManualSchedule || IsScheduledForDay(day);
        if(!manualPass) {
            return false;
        }

        if(calendarMode == CompetitionRegistrationWindowCalendarMode.Ignore || calendarEvent == null) {
            return true;
        }

        if(calendarMode == CompetitionRegistrationWindowCalendarMode.EventActive
            || calendarMode == CompetitionRegistrationWindowCalendarMode.EventVisible
            || calendarMode == CompetitionRegistrationWindowCalendarMode.EventScheduledToday) {
            return calendarEvent.IsScheduledForDay(day);
        }

        return true;
    }

    bool RequirementsMet(PlayerController player, out string failureMessage) {
        var activeRequirements = requirements?.Where(requirement => requirement != null).ToList() ?? new List<ActivityRequirement>();
        if(activeRequirements.Count == 0) {
            failureMessage = null;
            return true;
        }

        if(requirementMatchMode == ConsequenceRequirementMatchMode.Any) {
            foreach(var requirement in activeRequirements) {
                if(requirement.IsMet(player)) {
                    failureMessage = null;
                    return true;
                }
            }

            failureMessage = activeRequirements.FirstOrDefault()?.FailureMessage ?? closedMessage;
            return false;
        }

        foreach(var requirement in activeRequirements) {
            if(!requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    WeekDay GetWeekDay(int day) {
        int index = Mathf.Abs(Mathf.Max(1, day) - 1) % 7;
        return (WeekDay)index;
    }
}
