using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCompetitionInvitationLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save invitation, qualifier pass and wildcard records owned by this player.")]
    [SerializeField] List<PlayerCompetitionInvitationRecord> invitations = new List<PlayerCompetitionInvitationRecord>();
    [Tooltip("Runtime/save history of invitation use during registrations.")]
    [SerializeField] List<PlayerCompetitionInvitationUseRecord> useHistory = new List<PlayerCompetitionInvitationUseRecord>();

    public IReadOnlyList<PlayerCompetitionInvitationRecord> Invitations => invitations;
    public IReadOnlyList<PlayerCompetitionInvitationUseRecord> UseHistory => useHistory;
    public event Action<CompetitionInvitationDefinition, PlayerCompetitionInvitationRecord> OnInvitationGranted;
    public event Action<CompetitionInvitationDefinition, PlayerCompetitionInvitationUseRecord> OnInvitationUsed;
    public event Action OnCompetitionInvitationLogChanged;

    public bool CanGrant(CompetitionInvitationDefinition invitation, out string failureMessage) {
        if(invitation == null) {
            failureMessage = "A competition invitation is required.";
            return false;
        }

        var record = GetRecord(invitation);
        if(invitation.GrantMode == CompetitionInvitationGrantMode.OnceEver && record != null) {
            failureMessage = $"{invitation.DisplayName} was already granted.";
            return false;
        }

        if(invitation.GrantMode == CompetitionInvitationGrantMode.RefreshExistingOnly && record == null) {
            failureMessage = $"{invitation.DisplayName} cannot be refreshed because it is not owned.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public PlayerCompetitionInvitationRecord RecordGrant(CompetitionInvitationDefinition invitation, string sourceId = null) {
        if(invitation == null) {
            return null;
        }

        var record = GetRecord(invitation);
        if(record == null) {
            record = new PlayerCompetitionInvitationRecord {
                invitationId = invitation.Id,
                invitationName = invitation.DisplayName,
                kind = invitation.Kind.ToString(),
                sourceId = sourceId
            };
            invitations.Add(record);
        }

        record.grantCount++;
        record.usesGrantedPerGrant = invitation.UsesGranted;
        record.unlimitedUses = invitation.UnlimitedUses;
        record.lastGrantedTotalHour = GetCurrentTotalHour();
        record.sourceId = sourceId;

        if(invitation.Expires && (record.expiresTotalHour < 0 || invitation.RefreshExpirationOnGrant)) {
            record.expiresTotalHour = GetCurrentTotalHour() + invitation.DefaultDurationHours;
        } else if(!invitation.Expires) {
            record.expiresTotalHour = -1;
        }

        OnInvitationGranted?.Invoke(invitation, record);
        OnCompetitionInvitationLogChanged?.Invoke();
        return record;
    }

    public bool HasUsableInvitation(CompetitionInvitationDefinition invitation, out string failureMessage) {
        var record = GetRecord(invitation);
        if(record == null) {
            failureMessage = $"{invitation?.DisplayName ?? "Invitation"} is not owned.";
            return false;
        }

        return record.IsUsable(GetCurrentTotalHour(), out failureMessage);
    }

    public CompetitionInvitationDefinition FindUsableInvitation(IEnumerable<CompetitionInvitationDefinition> invitationsToSearch, CompetitionRegistrationDefinition registration, CompetitionRegistrationWindowDefinition registrationWindow, out string failureMessage) {
        failureMessage = null;
        foreach(var invitation in invitationsToSearch ?? Enumerable.Empty<CompetitionInvitationDefinition>()) {
            if(invitation == null || !invitation.MatchesRegistration(registration, registrationWindow)) {
                continue;
            }

            if(HasUsableInvitation(invitation, out _)) {
                return invitation;
            }
        }

        failureMessage = "No usable competition invitation was found.";
        return null;
    }

    public CompetitionInvitationDefinition FindAnyUsableInvitation(CompetitionRegistrationDefinition registration, CompetitionRegistrationWindowDefinition registrationWindow, out string failureMessage) {
        var definitions = Resources.LoadAll<CompetitionInvitationDefinition>("")
            .Where(invitation => invitation != null && invitation.MatchesRegistration(registration, registrationWindow));
        return FindUsableInvitation(definitions, registration, registrationWindow, out failureMessage);
    }

    public bool RecordUse(CompetitionInvitationDefinition invitation, CompetitionRegistrationDefinition registration, CompetitionRegistrationWindowDefinition registrationWindow, string sourceId, out string failureMessage) {
        if(!HasUsableInvitation(invitation, out failureMessage)) {
            return false;
        }

        var record = GetRecord(invitation);
        if(record == null) {
            failureMessage = $"{invitation.DisplayName} is not owned.";
            return false;
        }

        if(!record.unlimitedUses) {
            record.usedCount++;
        }

        var useRecord = new PlayerCompetitionInvitationUseRecord {
            invitationId = invitation.Id,
            invitationName = invitation.DisplayName,
            registrationId = registration != null ? registration.Id : string.Empty,
            registrationName = registration != null ? registration.DisplayName : string.Empty,
            rosterId = registration?.Roster != null ? registration.Roster.Id : string.Empty,
            rosterName = registration?.Roster != null ? registration.Roster.DisplayName : string.Empty,
            competitionId = registration?.Competition != null ? registration.Competition.Id : string.Empty,
            competitionName = registration?.Competition != null ? registration.Competition.DisplayName : string.Empty,
            windowId = registrationWindow != null ? registrationWindow.Id : string.Empty,
            windowName = registrationWindow != null ? registrationWindow.DisplayName : string.Empty,
            usedTotalHour = GetCurrentTotalHour(),
            sourceId = sourceId
        };

        useHistory.Add(useRecord);
        OnInvitationUsed?.Invoke(invitation, useRecord);
        OnCompetitionInvitationLogChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public bool HasInvitation(CompetitionInvitationDefinition invitation) {
        return GetRecord(invitation) != null;
    }

    public int GetAvailableUseCount(CompetitionInvitationDefinition invitation) {
        var record = GetRecord(invitation);
        return record != null ? record.GetAvailableUseCount() : 0;
    }

    PlayerCompetitionInvitationRecord GetRecord(CompetitionInvitationDefinition invitation) {
        string invitationId = invitation != null ? invitation.Id : string.Empty;
        return string.IsNullOrWhiteSpace(invitationId)
            ? null
            : invitations.FirstOrDefault(record => record != null && record.invitationId == invitationId);
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerCompetitionInvitationLogSaveData {
            invitations = invitations.Where(record => record != null).Select(record => record.Clone()).ToList(),
            useHistory = useHistory.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCompetitionInvitationLogSaveData;
        invitations = saveData?.invitations?.Where(record => record != null).Select(record => record.Clone()).ToList() ?? new List<PlayerCompetitionInvitationRecord>();
        useHistory = saveData?.useHistory?.Where(record => record != null).Select(record => record.Clone()).ToList() ?? new List<PlayerCompetitionInvitationUseRecord>();
        OnCompetitionInvitationLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerCompetitionInvitationRecord {
    [Tooltip("Saved invitation id.")]
    public string invitationId;
    [Tooltip("Saved invitation display name.")]
    public string invitationName;
    [Tooltip("Saved invitation kind.")]
    public string kind;
    [Tooltip("How many times this invitation has been granted or refreshed.")]
    [Min(0)]
    public int grantCount;
    [Tooltip("Number of uses granted by each grant. 0 means unlimited uses.")]
    [Min(0)]
    public int usesGrantedPerGrant = 1;
    [Tooltip("If enabled, this invitation can be used without counting down uses while active.")]
    public bool unlimitedUses;
    [Tooltip("How many counted uses have been consumed.")]
    [Min(0)]
    public int usedCount;
    [Tooltip("Last in-game total hour when this invitation was granted.")]
    public int lastGrantedTotalHour = -1;
    [Tooltip("In-game total hour when this invitation expires. -1 means no expiration.")]
    public int expiresTotalHour = -1;
    [Tooltip("Short source id that last granted this invitation.")]
    public string sourceId;

    public bool IsUsable(int currentTotalHour, out string failureMessage) {
        if(expiresTotalHour >= 0 && currentTotalHour >= expiresTotalHour) {
            failureMessage = $"{invitationName} has expired.";
            return false;
        }

        if(!unlimitedUses && GetAvailableUseCount() <= 0) {
            failureMessage = $"{invitationName} has no uses left.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public int GetAvailableUseCount() {
        if(unlimitedUses) {
            return int.MaxValue;
        }

        return Mathf.Max(0, grantCount * Mathf.Max(0, usesGrantedPerGrant) - usedCount);
    }

    public PlayerCompetitionInvitationRecord Clone() {
        return new PlayerCompetitionInvitationRecord {
            invitationId = invitationId,
            invitationName = invitationName,
            kind = kind,
            grantCount = grantCount,
            usesGrantedPerGrant = usesGrantedPerGrant,
            unlimitedUses = unlimitedUses,
            usedCount = usedCount,
            lastGrantedTotalHour = lastGrantedTotalHour,
            expiresTotalHour = expiresTotalHour,
            sourceId = sourceId
        };
    }
}

[Serializable]
public class PlayerCompetitionInvitationUseRecord {
    [Tooltip("Saved invitation id.")]
    public string invitationId;
    [Tooltip("Saved invitation display name.")]
    public string invitationName;
    [Tooltip("Saved registration id.")]
    public string registrationId;
    [Tooltip("Saved registration display name.")]
    public string registrationName;
    [Tooltip("Saved roster id.")]
    public string rosterId;
    [Tooltip("Saved roster display name.")]
    public string rosterName;
    [Tooltip("Saved competition id.")]
    public string competitionId;
    [Tooltip("Saved competition display name.")]
    public string competitionName;
    [Tooltip("Saved registration window id, if any.")]
    public string windowId;
    [Tooltip("Saved registration window display name, if any.")]
    public string windowName;
    [Tooltip("In-game total hour when this invitation was used.")]
    public int usedTotalHour = -1;
    [Tooltip("Short source id that used this invitation.")]
    public string sourceId;

    public PlayerCompetitionInvitationUseRecord Clone() {
        return new PlayerCompetitionInvitationUseRecord {
            invitationId = invitationId,
            invitationName = invitationName,
            registrationId = registrationId,
            registrationName = registrationName,
            rosterId = rosterId,
            rosterName = rosterName,
            competitionId = competitionId,
            competitionName = competitionName,
            windowId = windowId,
            windowName = windowName,
            usedTotalHour = usedTotalHour,
            sourceId = sourceId
        };
    }
}

[Serializable]
public class PlayerCompetitionInvitationLogSaveData {
    [Tooltip("Saved invitation, qualifier pass and wildcard records.")]
    public List<PlayerCompetitionInvitationRecord> invitations = new List<PlayerCompetitionInvitationRecord>();
    [Tooltip("Saved invitation use history.")]
    public List<PlayerCompetitionInvitationUseRecord> useHistory = new List<PlayerCompetitionInvitationUseRecord>();
}
