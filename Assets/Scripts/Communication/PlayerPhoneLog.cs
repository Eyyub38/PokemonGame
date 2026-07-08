using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPhoneLog : MonoBehaviour, ISavable {
    [Header("Runtime Data")]
    [Tooltip("Known contacts. Usually filled by PhoneContactSource, quest rewards, or successful calls.")]
    [SerializeField] List<PhoneKnownContactRecord> knownContacts = new List<PhoneKnownContactRecord>();
    [Tooltip("Recent call history used by cooldowns, daily limits and debug UI.")]
    [SerializeField] List<PhoneCallHistoryRecord> callHistory = new List<PhoneCallHistoryRecord>();

    [Header("Save")]
    [Tooltip("Maximum call history rows kept in saves. 0 means history is not saved.")]
    [Min(0)]
    [SerializeField] int maxSavedHistory = 80;
    [Tooltip("If enabled, expired temporary contacts are pruned when state is restored or queried.")]
    [SerializeField] bool pruneExpiredContacts = true;

    public IReadOnlyList<PhoneKnownContactRecord> KnownContacts => knownContacts;
    public IReadOnlyList<PhoneCallHistoryRecord> CallHistory => callHistory;

    void Awake() {
        PruneExpiredContacts();
    }

    public bool HasContact(PhoneContactDefinition contact) {
        return contact != null && HasContact(contact.Id);
    }

    public bool HasContact(string contactId) {
        if(string.IsNullOrWhiteSpace(contactId)) {
            return false;
        }

        PruneExpiredContacts();
        return knownContacts.Any(record => record != null && string.Equals(record.contactId, contactId, StringComparison.OrdinalIgnoreCase));
    }

    public bool LearnContact(PhoneContactDefinition contact, string sourceId = null, int durationHours = 0) {
        if(contact == null) {
            return false;
        }

        return LearnContact(contact.Id, contact.DisplayName, contact.PhoneNumber, contact.ContactType, sourceId, durationHours);
    }

    public bool LearnContact(string contactId, string displayName, string phoneNumber, PhoneContactType contactType, string sourceId = null, int durationHours = 0) {
        if(string.IsNullOrWhiteSpace(contactId)) {
            return false;
        }

        PruneExpiredContacts();

        var currentAbsoluteHour = GetCurrentAbsoluteHour();
        var existing = knownContacts.FirstOrDefault(record => record != null && string.Equals(record.contactId, contactId, StringComparison.OrdinalIgnoreCase));
        int expiresAt = durationHours > 0 ? currentAbsoluteHour + durationHours : -1;

        if(existing != null) {
            existing.displayName = displayName;
            existing.phoneNumber = phoneNumber;
            existing.contactType = contactType;
            existing.sourceId = sourceId;
            existing.expiresAbsoluteHour = expiresAt;
            return false;
        }

        knownContacts.Add(new PhoneKnownContactRecord {
            contactId = contactId,
            displayName = displayName,
            phoneNumber = phoneNumber,
            contactType = contactType,
            learnedAbsoluteHour = currentAbsoluteHour,
            expiresAbsoluteHour = expiresAt,
            sourceId = sourceId
        });
        return true;
    }

    public bool ForgetContact(PhoneContactDefinition contact) {
        return contact != null && ForgetContact(contact.Id);
    }

    public bool ForgetContact(string contactId) {
        if(string.IsNullOrWhiteSpace(contactId)) {
            return false;
        }

        int removed = knownContacts.RemoveAll(record => record != null && string.Equals(record.contactId, contactId, StringComparison.OrdinalIgnoreCase));
        return removed > 0;
    }

    public int CountCallsOnDay(string contactId, int day) {
        if(string.IsNullOrWhiteSpace(contactId)) {
            return 0;
        }

        return callHistory.Count(record =>
            record != null
            && record.day == day
            && string.Equals(record.contactId, contactId, StringComparison.OrdinalIgnoreCase));
    }

    public int GetLastCallAbsoluteHour(string contactId) {
        if(string.IsNullOrWhiteSpace(contactId)) {
            return -1;
        }

        var record = callHistory
            .Where(entry => entry != null && string.Equals(entry.contactId, contactId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(entry => entry.absoluteHour)
            .FirstOrDefault();
        return record != null ? record.absoluteHour : -1;
    }

    public void RecordCall(PhoneCallResult result) {
        if(result == null || string.IsNullOrWhiteSpace(result.contactId)) {
            return;
        }

        callHistory.Add(new PhoneCallHistoryRecord {
            contactId = result.contactId,
            contactName = result.contactName,
            terminalId = result.terminalId,
            status = result.status,
            success = result.success,
            message = result.message,
            day = result.day,
            hour = result.hour,
            absoluteHour = result.absoluteHour,
            openedPokemonStorage = result.openedPokemonStorage
        });

        if(maxSavedHistory > 0 && callHistory.Count > maxSavedHistory) {
            callHistory = callHistory
                .OrderByDescending(record => record.absoluteHour)
                .Take(maxSavedHistory)
                .OrderBy(record => record.absoluteHour)
                .ToList();
        }
    }

    public object CaptureState() {
        PruneExpiredContacts();
        return new PlayerPhoneLogSaveData {
            knownContacts = knownContacts.Select(record => record.Clone()).ToList(),
            callHistory = maxSavedHistory > 0
                ? callHistory.OrderByDescending(record => record.absoluteHour).Take(maxSavedHistory).Select(record => record.Clone()).ToList()
                : new List<PhoneCallHistoryRecord>()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerPhoneLogSaveData;
        if(saveData == null) {
            return;
        }

        knownContacts = saveData.knownContacts != null
            ? saveData.knownContacts.Where(record => record != null).Select(record => record.Clone()).ToList()
            : new List<PhoneKnownContactRecord>();
        callHistory = saveData.callHistory != null
            ? saveData.callHistory.Where(record => record != null).Select(record => record.Clone()).ToList()
            : new List<PhoneCallHistoryRecord>();
        PruneExpiredContacts();
    }

    void PruneExpiredContacts() {
        if(!pruneExpiredContacts) {
            return;
        }

        int currentAbsoluteHour = GetCurrentAbsoluteHour();
        knownContacts.RemoveAll(record => record == null || record.IsExpired(currentAbsoluteHour));
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }
}

[Serializable]
public class PhoneKnownContactRecord {
    [Tooltip("Known contact id.")]
    public string contactId;
    [Tooltip("Known contact display name.")]
    public string displayName;
    [Tooltip("Known phone number or code.")]
    public string phoneNumber;
    [Tooltip("Known contact category.")]
    public PhoneContactType contactType;
    [Tooltip("Absolute in-game hour when this contact was learned.")]
    public int learnedAbsoluteHour;
    [Tooltip("Absolute in-game hour when this contact expires. -1 means permanent.")]
    public int expiresAbsoluteHour = -1;
    [Tooltip("Source id that granted this contact.")]
    public string sourceId;

    public bool IsExpired(int currentAbsoluteHour) {
        return expiresAbsoluteHour >= 0 && currentAbsoluteHour >= expiresAbsoluteHour;
    }

    public PhoneKnownContactRecord Clone() {
        return new PhoneKnownContactRecord {
            contactId = contactId,
            displayName = displayName,
            phoneNumber = phoneNumber,
            contactType = contactType,
            learnedAbsoluteHour = learnedAbsoluteHour,
            expiresAbsoluteHour = expiresAbsoluteHour,
            sourceId = sourceId
        };
    }
}

[Serializable]
public class PhoneCallHistoryRecord {
    [Tooltip("Contact id used by this call.")]
    public string contactId;
    [Tooltip("Contact display name used by this call.")]
    public string contactName;
    [Tooltip("Terminal id used by this call.")]
    public string terminalId;
    [Tooltip("Final call status.")]
    public PhoneCallStatus status;
    [Tooltip("If enabled, the call succeeded.")]
    public bool success;
    [Tooltip("Readable result or failure text.")]
    public string message;
    [Tooltip("In-game day when this call happened.")]
    public int day;
    [Tooltip("In-game hour when this call happened.")]
    public int hour;
    [Tooltip("Absolute in-game hour when this call happened.")]
    public int absoluteHour;
    [Tooltip("If enabled, this call opened Pokemon storage.")]
    public bool openedPokemonStorage;

    public PhoneCallHistoryRecord Clone() {
        return new PhoneCallHistoryRecord {
            contactId = contactId,
            contactName = contactName,
            terminalId = terminalId,
            status = status,
            success = success,
            message = message,
            day = day,
            hour = hour,
            absoluteHour = absoluteHour,
            openedPokemonStorage = openedPokemonStorage
        };
    }
}

[Serializable]
public class PlayerPhoneLogSaveData {
    public List<PhoneKnownContactRecord> knownContacts = new List<PhoneKnownContactRecord>();
    public List<PhoneCallHistoryRecord> callHistory = new List<PhoneCallHistoryRecord>();
}
