using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerSocialActivityLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save history of social activity attempts and completions.")]
    [SerializeField] List<SocialActivityResult> history = new List<SocialActivityResult>();

    public IReadOnlyList<SocialActivityResult> History => history;
    public event Action<SocialActivityResult> OnSocialActivityRecorded;

    public bool CanRun(SocialActivityDefinition activity, int dailyLimit, int cooldownHours, out string failureMessage) {
        failureMessage = null;
        if(activity == null) {
            return true;
        }

        int limit = Mathf.Max(0, dailyLimit);
        if(limit > 0 && GetCompletionsToday(activity) >= limit) {
            failureMessage = $"{activity.DisplayName} cannot be done again today.";
            return false;
        }

        int cooldown = Mathf.Max(0, cooldownHours);
        if(cooldown > 0) {
            int hoursSinceLast = GetHoursSinceLastCompletion(activity);
            if(hoursSinceLast >= 0 && hoursSinceLast < cooldown) {
                failureMessage = $"{activity.DisplayName} will be available again in {cooldown - hoursSinceLast} hour(s).";
                return false;
            }
        }

        return true;
    }

    public void Record(SocialActivityResult result) {
        if(result == null) {
            return;
        }

        history.Add(Clone(result));
        OnSocialActivityRecorded?.Invoke(result);
    }

    public int GetLifetimeCompletions(SocialActivityDefinition activity) {
        return activity != null ? GetLifetimeCompletions(activity.Id) : 0;
    }

    public int GetLifetimeCompletions(string activityId) {
        if(string.IsNullOrWhiteSpace(activityId)) {
            return 0;
        }

        return history.Count(record => record != null && record.success && record.activityId == activityId);
    }

    public int GetCompletionsToday(SocialActivityDefinition activity) {
        return activity != null ? GetCompletionsToday(activity.Id) : 0;
    }

    public int GetCompletionsToday(string activityId) {
        if(string.IsNullOrWhiteSpace(activityId)) {
            return 0;
        }

        int currentDay = GetCurrentDay();
        return history.Count(record => record != null && record.success && record.activityId == activityId && record.day == currentDay);
    }

    public int GetHoursSinceLastCompletion(SocialActivityDefinition activity) {
        return activity != null ? GetHoursSinceLastCompletion(activity.Id) : -1;
    }

    public int GetHoursSinceLastCompletion(string activityId) {
        if(string.IsNullOrWhiteSpace(activityId)) {
            return -1;
        }

        var latest = history
            .Where(record => record != null && record.success && record.activityId == activityId)
            .OrderByDescending(record => record.absoluteHour)
            .FirstOrDefault();

        if(latest == null || latest.absoluteHour < 0) {
            return -1;
        }

        return Mathf.Max(0, GetCurrentAbsoluteHour() - latest.absoluteHour);
    }

    public IEnumerable<SocialActivityResult> GetRecent(int count = 20, bool successesOnly = false) {
        IEnumerable<SocialActivityResult> query = history.Where(record => record != null);
        if(successesOnly) {
            query = query.Where(record => record.success);
        }

        return query.OrderByDescending(record => record.absoluteHour).Take(Mathf.Max(1, count));
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    SocialActivityResult Clone(SocialActivityResult result) {
        return new SocialActivityResult {
            success = result.success,
            activityId = result.activityId,
            activityName = result.activityName,
            kind = result.kind,
            sourceId = result.sourceId,
            message = result.message,
            day = result.day,
            hour = result.hour,
            absoluteHour = result.absoluteHour,
            companions = CloneParticipants(result.companions),
            pokemon = CloneParticipants(result.pokemon)
        };
    }

    List<SocialActivityParticipantRecord> CloneParticipants(IEnumerable<SocialActivityParticipantRecord> records) {
        if(records == null) {
            return new List<SocialActivityParticipantRecord>();
        }

        return records
            .Where(record => record != null)
            .Select(record => new SocialActivityParticipantRecord {
                id = record.id,
                displayName = record.displayName,
                detail = record.detail
            })
            .ToList();
    }

    public object CaptureState() {
        return history.Select(Clone).ToList();
    }

    public void RestoreState(object state) {
        history = state as List<SocialActivityResult> ?? new List<SocialActivityResult>();
    }
}
