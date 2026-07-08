using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCompetitionRegistrationLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save history of competition registrations made by this player.")]
    [SerializeField] List<PlayerCompetitionRegistrationRecord> registrationHistory = new List<PlayerCompetitionRegistrationRecord>();

    public IReadOnlyList<PlayerCompetitionRegistrationRecord> RegistrationHistory => registrationHistory;
    public event Action<CompetitionRegistrationDefinition, PlayerCompetitionRegistrationRecord> OnRegistered;
    public event Action OnCompetitionRegistrationLogChanged;

    public bool CanRegister(CompetitionRegistrationDefinition registration, string contextKey, out string failureMessage) {
        if(registration == null) {
            failureMessage = "A competition registration is required.";
            return false;
        }

        if(registration.MaxRegistrationCount > 0 && GetRegistrationCount(registration) >= registration.MaxRegistrationCount) {
            failureMessage = $"{registration.DisplayName} has reached its registration limit.";
            return false;
        }

        var repeatMode = registration.RepeatMode;
        if(repeatMode == CompetitionRegistrationRepeatMode.Always) {
            failureMessage = null;
            return true;
        }

        if(string.IsNullOrWhiteSpace(contextKey)) {
            contextKey = registration.Id;
        }

        if(repeatMode == CompetitionRegistrationRepeatMode.CooldownHours) {
            int remainingHours = GetRemainingCooldownHours(registration, contextKey);
            if(remainingHours > 0) {
                failureMessage = $"{registration.DisplayName} opens again in {remainingHours} hour(s).";
                return false;
            }

            failureMessage = null;
            return true;
        }

        if(HasRegistered(registration, contextKey)) {
            failureMessage = $"{registration.DisplayName} is already registered.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public PlayerCompetitionRegistrationRecord RecordRegistration(CompetitionRegistrationDefinition registration, CompetitionRegistrationContext context, PlayerCompetitionBracketState bracketState, string sourceId = null) {
        if(registration == null) {
            return null;
        }

        var record = new PlayerCompetitionRegistrationRecord {
            registrationId = registration.Id,
            registrationName = registration.DisplayName,
            contextKey = context != null ? context.BuildContextKey(registration.RepeatMode) : registration.Id,
            rosterId = registration.Roster != null ? registration.Roster.Id : string.Empty,
            rosterName = registration.Roster != null ? registration.Roster.DisplayName : string.Empty,
            competitionId = registration.Competition != null ? registration.Competition.Id : string.Empty,
            competitionName = registration.Competition != null ? registration.Competition.DisplayName : string.Empty,
            seasonId = registration.Season != null ? registration.Season.Id : string.Empty,
            seasonName = registration.Season != null ? registration.Season.DisplayName : string.Empty,
            rankingId = registration.Ranking != null ? registration.Ranking.Id : string.Empty,
            rankingName = registration.Ranking != null ? registration.Ranking.DisplayName : string.Empty,
            windowId = context?.Window != null ? context.Window.Id : string.Empty,
            windowName = context?.Window != null ? context.Window.DisplayName : string.Empty,
            windowOccurrenceKey = context?.Window != null ? context.Window.BuildOccurrenceKey() : string.Empty,
            invitationId = context?.Invitation != null ? context.Invitation.Id : string.Empty,
            invitationName = context?.Invitation != null ? context.Invitation.DisplayName : string.Empty,
            venueId = context?.Venue != null ? context.Venue.Id : string.Empty,
            venueName = context?.Venue != null ? context.Venue.DisplayName : string.Empty,
            bracketSeed = bracketState != null ? bracketState.seed : 0,
            generatedBracket = bracketState != null,
            registeredTotalHour = GetCurrentTotalHour(),
            moneyPaid = context != null ? context.MoneyPaid : 0f,
            sourceId = sourceId
        };

        registrationHistory.Add(record);
        OnRegistered?.Invoke(registration, record);
        OnCompetitionRegistrationLogChanged?.Invoke();
        return record;
    }

    public bool HasRegistered(CompetitionRegistrationDefinition registration) {
        return GetRegistrationCount(registration) > 0;
    }

    public bool HasRegistered(CompetitionRegistrationDefinition registration, string contextKey) {
        return GetRegistrationCount(registration, contextKey) > 0;
    }

    public bool HasRegisteredRoster(CompetitionRosterDefinition roster) {
        return GetRosterRegistrationCount(roster) > 0;
    }

    public int GetRegistrationCount(CompetitionRegistrationDefinition registration) {
        string registrationId = registration != null ? registration.Id : string.Empty;
        return string.IsNullOrWhiteSpace(registrationId)
            ? 0
            : registrationHistory.Count(record => record != null && record.registrationId == registrationId);
    }

    public int GetRegistrationCount(CompetitionRegistrationDefinition registration, string contextKey) {
        string registrationId = registration != null ? registration.Id : string.Empty;
        if(string.IsNullOrWhiteSpace(registrationId) || string.IsNullOrWhiteSpace(contextKey)) {
            return 0;
        }

        return registrationHistory.Count(record => record != null && record.registrationId == registrationId && record.contextKey == contextKey);
    }

    public int GetRosterRegistrationCount(CompetitionRosterDefinition roster) {
        string rosterId = roster != null ? roster.Id : string.Empty;
        return string.IsNullOrWhiteSpace(rosterId)
            ? 0
            : registrationHistory.Count(record => record != null && record.rosterId == rosterId);
    }

    public int GetRemainingCooldownHours(CompetitionRegistrationDefinition registration, string contextKey) {
        if(registration == null || registration.CooldownHours <= 0) {
            return 0;
        }

        var lastRegistration = registrationHistory
            .Where(record => record != null
                && record.registrationId == registration.Id
                && (string.IsNullOrWhiteSpace(contextKey) || record.contextKey == contextKey))
            .OrderByDescending(record => record.registeredTotalHour)
            .FirstOrDefault();

        if(lastRegistration == null || lastRegistration.registeredTotalHour < 0) {
            return 0;
        }

        int readyAt = lastRegistration.registeredTotalHour + registration.CooldownHours;
        return Mathf.Max(0, readyAt - GetCurrentTotalHour());
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerCompetitionRegistrationLogSaveData {
            registrationHistory = registrationHistory.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCompetitionRegistrationLogSaveData;
        registrationHistory = saveData?.registrationHistory?.Where(entry => entry != null).Select(entry => entry.Clone()).ToList() ?? new List<PlayerCompetitionRegistrationRecord>();
        OnCompetitionRegistrationLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerCompetitionRegistrationRecord {
    [Tooltip("Saved registration id.")]
    public string registrationId;
    [Tooltip("Saved registration display name.")]
    public string registrationName;
    [Tooltip("Repeat/cooldown context key used by this registration.")]
    public string contextKey;
    [Tooltip("Saved roster id.")]
    public string rosterId;
    [Tooltip("Saved roster display name.")]
    public string rosterName;
    [Tooltip("Saved competition id.")]
    public string competitionId;
    [Tooltip("Saved competition display name.")]
    public string competitionName;
    [Tooltip("Saved season id.")]
    public string seasonId;
    [Tooltip("Saved season display name.")]
    public string seasonName;
    [Tooltip("Saved ranking id.")]
    public string rankingId;
    [Tooltip("Saved ranking display name.")]
    public string rankingName;
    [Tooltip("Saved registration window id, if a scheduled window opened this registration.")]
    public string windowId;
    [Tooltip("Saved registration window display name, if a scheduled window opened this registration.")]
    public string windowName;
    [Tooltip("Saved registration window occurrence key, usually window id plus current in-game day.")]
    public string windowOccurrenceKey;
    [Tooltip("Saved invitation, qualifier pass or wildcard id used by this registration.")]
    public string invitationId;
    [Tooltip("Saved invitation, qualifier pass or wildcard display name used by this registration.")]
    public string invitationName;
    [Tooltip("Saved venue, arena, gym or stadium id used by this registration.")]
    public string venueId;
    [Tooltip("Saved venue, arena, gym or stadium display name used by this registration.")]
    public string venueName;
    [Tooltip("Generated bracket seed, if a bracket was created by this registration.")]
    public int bracketSeed;
    [Tooltip("Whether registration generated a bracket immediately.")]
    public bool generatedBracket;
    [Tooltip("Money paid during registration.")]
    public float moneyPaid;
    [Tooltip("In-game total hour when this registration was made.")]
    public int registeredTotalHour = -1;
    [Tooltip("Short source id that recorded this registration.")]
    public string sourceId;

    public PlayerCompetitionRegistrationRecord Clone() {
        return new PlayerCompetitionRegistrationRecord {
            registrationId = registrationId,
            registrationName = registrationName,
            contextKey = contextKey,
            rosterId = rosterId,
            rosterName = rosterName,
            competitionId = competitionId,
            competitionName = competitionName,
            seasonId = seasonId,
            seasonName = seasonName,
            rankingId = rankingId,
            rankingName = rankingName,
            windowId = windowId,
            windowName = windowName,
            windowOccurrenceKey = windowOccurrenceKey,
            invitationId = invitationId,
            invitationName = invitationName,
            venueId = venueId,
            venueName = venueName,
            bracketSeed = bracketSeed,
            generatedBracket = generatedBracket,
            moneyPaid = moneyPaid,
            registeredTotalHour = registeredTotalHour,
            sourceId = sourceId
        };
    }
}

[Serializable]
public class PlayerCompetitionRegistrationLogSaveData {
    [Tooltip("Saved competition registration history.")]
    public List<PlayerCompetitionRegistrationRecord> registrationHistory = new List<PlayerCompetitionRegistrationRecord>();
}
