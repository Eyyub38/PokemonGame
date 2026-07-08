using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCompetitionVenueLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save history of venue, arena, gym and stadium usage.")]
    [SerializeField] List<PlayerCompetitionVenueRecord> venueHistory = new List<PlayerCompetitionVenueRecord>();

    public IReadOnlyList<PlayerCompetitionVenueRecord> VenueHistory => venueHistory;
    public event Action<CompetitionVenueDefinition, PlayerCompetitionVenueRecord> OnVenueUsed;
    public event Action OnCompetitionVenueLogChanged;

    public bool CanUse(CompetitionVenueDefinition venue, string sourceId, ConsequenceChainRepeatMode repeatMode, int cooldownHours, int maxUseCount, out string failureMessage) {
        if(venue == null) {
            failureMessage = "A competition venue is required.";
            return false;
        }

        int totalSuccessfulUses = GetUseCount(venue, includeBlocked: false);
        if(maxUseCount > 0 && totalSuccessfulUses >= maxUseCount) {
            failureMessage = $"{venue.DisplayName} has reached its maximum use count.";
            return false;
        }

        string normalizedSource = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && totalSuccessfulUses > 0) {
            failureMessage = $"{venue.DisplayName} has already been used.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetUseCount(venue, normalizedSource, includeBlocked: false) > 0) {
            failureMessage = $"{venue.DisplayName} has already been used from this source.";
            return false;
        }

        var lastUse = GetLastUse(venue, normalizedSource, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastUse != null && lastUse.day == GetCurrentDay()) {
            failureMessage = $"{venue.DisplayName} can only be used once per day from this source.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastUse != null) {
            int elapsed = GetCurrentTotalHour() - lastUse.usedTotalHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{venue.DisplayName} opens again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public PlayerCompetitionVenueRecord RecordUse(CompetitionVenueDefinition venue, CompetitionVenuePurpose purpose, CompetitionRegistrationDefinition registration, CompetitionRosterDefinition roster, string sourceId, bool blocked, string failureMessage) {
        if(venue == null) {
            return null;
        }

        var record = new PlayerCompetitionVenueRecord {
            recordId = Guid.NewGuid().ToString("N"),
            venueId = venue.Id,
            venueName = venue.DisplayName,
            kind = venue.Kind.ToString(),
            purpose = purpose.ToString(),
            sourceId = NormalizeSourceId(sourceId),
            registrationId = registration != null ? registration.Id : string.Empty,
            registrationName = registration != null ? registration.DisplayName : string.Empty,
            rosterId = roster != null ? roster.Id : registration?.Roster != null ? registration.Roster.Id : string.Empty,
            rosterName = roster != null ? roster.DisplayName : registration?.Roster != null ? registration.Roster.DisplayName : string.Empty,
            competitionId = roster?.Competition != null ? roster.Competition.Id : registration?.Competition != null ? registration.Competition.Id : string.Empty,
            competitionName = roster?.Competition != null ? roster.Competition.DisplayName : registration?.Competition != null ? registration.Competition.DisplayName : string.Empty,
            sceneName = venue.ResolveSceneName(),
            locationKey = venue.ResolveLocationKey(),
            day = GetCurrentDay(),
            usedTotalHour = GetCurrentTotalHour(),
            blocked = blocked,
            failureMessage = failureMessage
        };

        venueHistory.Add(record);
        OnVenueUsed?.Invoke(venue, record);
        OnCompetitionVenueLogChanged?.Invoke();
        return record;
    }

    public bool HasUsedVenue(CompetitionVenueDefinition venue, bool includeBlocked = false) {
        return GetUseCount(venue, includeBlocked: includeBlocked) > 0;
    }

    public int GetUseCount(CompetitionVenueDefinition venue = null, string sourceId = null, bool includeBlocked = false) {
        string venueId = venue != null ? venue.Id : null;
        string normalizedSource = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return venueHistory.Count(record => Matches(record, venueId, normalizedSource, includeBlocked));
    }

    public PlayerCompetitionVenueRecord GetLastUse(CompetitionVenueDefinition venue = null, string sourceId = null, bool includeBlocked = false) {
        string venueId = venue != null ? venue.Id : null;
        string normalizedSource = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return venueHistory
            .Where(record => Matches(record, venueId, normalizedSource, includeBlocked))
            .OrderByDescending(record => record.usedTotalHour)
            .ThenByDescending(record => record.day)
            .FirstOrDefault();
    }

    bool Matches(PlayerCompetitionVenueRecord record, string venueId, string sourceId, bool includeBlocked) {
        return record != null
            && (includeBlocked || !record.blocked)
            && (string.IsNullOrWhiteSpace(venueId) || record.venueId == venueId)
            && (string.IsNullOrWhiteSpace(sourceId) || record.sourceId == sourceId);
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "competition-venue" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerCompetitionVenueLogSaveData {
            venueHistory = venueHistory.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCompetitionVenueLogSaveData;
        venueHistory = saveData?.venueHistory?.Where(record => record != null).Select(record => record.Clone()).ToList() ?? new List<PlayerCompetitionVenueRecord>();
        OnCompetitionVenueLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerCompetitionVenueRecord {
    [Tooltip("Unique runtime/save id for this venue record.")]
    public string recordId;
    [Tooltip("Saved venue id.")]
    public string venueId;
    [Tooltip("Saved venue display name.")]
    public string venueName;
    [Tooltip("Saved venue kind.")]
    public string kind;
    [Tooltip("Saved venue purpose such as registration, bracket or match.")]
    public string purpose;
    [Tooltip("Source id used by repeat/cooldown rules.")]
    public string sourceId;
    [Tooltip("Saved registration id, if this venue was used for registration.")]
    public string registrationId;
    [Tooltip("Saved registration display name, if any.")]
    public string registrationName;
    [Tooltip("Saved roster id, if any.")]
    public string rosterId;
    [Tooltip("Saved roster display name, if any.")]
    public string rosterName;
    [Tooltip("Saved competition id, if any.")]
    public string competitionId;
    [Tooltip("Saved competition display name, if any.")]
    public string competitionName;
    [Tooltip("Scene name connected to this venue use.")]
    public string sceneName;
    [Tooltip("Location key connected to this venue use.")]
    public string locationKey;
    [Tooltip("In-game day when this venue was used.")]
    public int day;
    [Tooltip("Absolute in-game hour when this venue was used.")]
    public int usedTotalHour = -1;
    [Tooltip("If enabled, this venue attempt was blocked.")]
    public bool blocked;
    [Tooltip("Failure message saved for blocked attempts.")]
    public string failureMessage;

    public PlayerCompetitionVenueRecord Clone() {
        return new PlayerCompetitionVenueRecord {
            recordId = recordId,
            venueId = venueId,
            venueName = venueName,
            kind = kind,
            purpose = purpose,
            sourceId = sourceId,
            registrationId = registrationId,
            registrationName = registrationName,
            rosterId = rosterId,
            rosterName = rosterName,
            competitionId = competitionId,
            competitionName = competitionName,
            sceneName = sceneName,
            locationKey = locationKey,
            day = day,
            usedTotalHour = usedTotalHour,
            blocked = blocked,
            failureMessage = failureMessage
        };
    }
}

[Serializable]
public class PlayerCompetitionVenueLogSaveData {
    [Tooltip("Saved competition venue history.")]
    public List<PlayerCompetitionVenueRecord> venueHistory = new List<PlayerCompetitionVenueRecord>();
}
