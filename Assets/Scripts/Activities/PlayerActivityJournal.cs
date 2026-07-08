using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerActivityJournal : MonoBehaviour, ISavable {
    [SerializeField] List<ActivityJournalEntry> entries = new List<ActivityJournalEntry>();

    public IReadOnlyList<ActivityJournalEntry> Entries => entries;
    public event Action OnActivityJournalChanged;

    public int GetLifetimeCompletions(ActivityDefinition activity) {
        return activity != null ? GetLifetimeCompletions(activity.Id) : 0;
    }

    public int GetLifetimeCompletions(string activityId) {
        if(string.IsNullOrWhiteSpace(activityId)) {
            return 0;
        }

        return entries.FirstOrDefault(e => e.activityId == activityId)?.lifetimeCompleted ?? 0;
    }

    public int GetActiveDays(ActivityDefinition activity) {
        return activity != null ? GetActiveDays(activity.Id) : 0;
    }

    public int GetActiveDays(string activityId) {
        if(string.IsNullOrWhiteSpace(activityId)) {
            return 0;
        }

        return entries.FirstOrDefault(e => e.activityId == activityId)?.activeDays ?? 0;
    }

    public bool CanPerform(ActivityDefinition activity, int dailyLimit, int cooldownHours, out string failureMessage) {
        return CanPerform(activity, dailyLimit, cooldownHours, out failureMessage, out _);
    }

    public bool CanPerform(ActivityDefinition activity, int dailyLimit, int cooldownHours, out string failureMessage, out ActivityJournalBlockReason blockReason) {
        failureMessage = null;
        blockReason = ActivityJournalBlockReason.None;
        if(activity == null) {
            return true;
        }

        var entry = GetOrCreateEntry(activity.Id);
        int currentDay = GetCurrentDay();
        int currentAbsoluteHour = GetCurrentAbsoluteHour();

        if(entry.day != currentDay) {
            entry.day = currentDay;
            entry.completedToday = 0;
        }

        if(dailyLimit > 0 && entry.completedToday >= dailyLimit) {
            failureMessage = $"{activity.DisplayName} cannot be done again today.";
            blockReason = ActivityJournalBlockReason.DailyLimit;
            return false;
        }

        if(cooldownHours > 0 && entry.lastCompletionAbsoluteHour >= 0) {
            int elapsedHours = currentAbsoluteHour - entry.lastCompletionAbsoluteHour;
            if(elapsedHours < cooldownHours) {
                failureMessage = $"{activity.DisplayName} will be available again in {cooldownHours - elapsedHours} hour(s).";
                blockReason = ActivityJournalBlockReason.Cooldown;
                return false;
            }
        }

        return true;
    }

    public void RecordCompletion(ActivityDefinition activity) {
        if(activity == null) {
            return;
        }

        var entry = GetOrCreateEntry(activity.Id);
        int currentDay = GetCurrentDay();
        if(entry.day != currentDay) {
            entry.day = currentDay;
            entry.completedToday = 0;
            entry.activeDays++;
        }

        if(entry.activeDays <= 0) {
            entry.activeDays = 1;
        }

        entry.completedToday++;
        entry.lifetimeCompleted++;
        entry.lastCompletionAbsoluteHour = GetCurrentAbsoluteHour();
        OnActivityJournalChanged?.Invoke();
    }

    ActivityJournalEntry GetOrCreateEntry(string activityId) {
        var entry = entries.FirstOrDefault(e => e.activityId == activityId);
        if(entry != null) {
            return entry;
        }

        entry = new ActivityJournalEntry() {
            activityId = activityId,
            day = GetCurrentDay(),
            completedToday = 0,
            lifetimeCompleted = 0,
            activeDays = 0,
            lastCompletionAbsoluteHour = -1
        };
        entries.Add(entry);
        return entry;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? TimeSystem.i.Day : 1;
    }

    int GetCurrentAbsoluteHour() {
        if(TimeSystem.i == null) {
            return 0;
        }

        return TimeSystem.i.Day * 24 + TimeSystem.i.Hour;
    }

    public object CaptureState() {
        return entries.Select(e => new ActivityJournalEntry() {
            activityId = e.activityId,
            day = e.day,
            completedToday = e.completedToday,
            lifetimeCompleted = e.lifetimeCompleted,
            activeDays = e.activeDays,
            lastCompletionAbsoluteHour = e.lastCompletionAbsoluteHour
        }).ToList();
    }

    public void RestoreState(object state) {
        entries = state as List<ActivityJournalEntry> ?? new List<ActivityJournalEntry>();
        OnActivityJournalChanged?.Invoke();
    }
}

[Serializable]
public enum ActivityJournalBlockReason {
    None,
    DailyLimit,
    Cooldown
}

[Serializable]
public class ActivityJournalEntry {
    public string activityId;
    public int day;
    public int completedToday;
    public int lifetimeCompleted;
    public int activeDays;
    public int lastCompletionAbsoluteHour = -1;
}
